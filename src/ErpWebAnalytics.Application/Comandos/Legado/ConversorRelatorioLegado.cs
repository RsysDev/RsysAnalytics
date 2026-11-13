using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ErpWebAnalytics.Application.Comandos.Legado;

/// <summary>
/// Traduz um relatório do ERP legado (<c>XmlParametros</c>, <c>XmlRegras</c>, <c>XmlComandos</c>) para um <see cref="ComandoSql"/>.
/// Função pura, sem banco. O que não tem tradução segura vira um aviso, e o texto original é mantido para revisão.
/// </summary>
public static class ConversorRelatorioLegado
{
    private static readonly Regex IdInvalido = new(@"[^A-Za-z0-9_.\-]", RegexOptions.Compiled);
    private static readonly Regex NomeInvalido = new(@"\W", RegexOptions.Compiled);
    private static readonly Regex ColunaSimples = new(@"^[A-Za-z_][\w.\[\]]*$", RegexOptions.Compiled);
    private static readonly Regex Diretiva = new(@"\$(SENAOSE|SENAO|SE|FIMSE)\b", RegexOptions.Compiled);
    private static readonly Regex Atomo = new(
        @"^\[(?<p>\w+)(?::(?<i>\d))?\]\s*(?<op>==|!=|>=|<=|>|<|=)\s*(?<v>.*)$", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex UsoParametro = new(@"(?<![.\w])\[(?<p>\w+)(?::\d)?\](?![.\w])", RegexOptions.Compiled);

    private static readonly HashSet<string> SistemaConhecido = new(StringComparer.OrdinalIgnoreCase)
        { "IdUsuario", "IdEmpresa", "IdEmpresas", "LayoutSelecionado" };

    private sealed class P
    {
        public string Id = "";
        public string? Campo, Descricao, Padrao, Padrao2, Chave;
        public TipoParametro Tipo = TipoParametro.String;
        public bool ValorUnico, Obrigatorio;
        public OperadorComparacao? Fixa;
        public OrigemParametro Origem = OrigemParametro.Chamador;
        public List<OpcaoParametro> Opcoes = [];
        public IEnumerable<string> ChavesLista => Opcoes.Select(o => o.Valor).Where(v => v.Length > 0);
    }

    public static ConversaoLegado Converter(RelatorioLegado r)
    {
        var avisos = new List<string>();
        var ps = new Dictionary<string, P>(StringComparer.OrdinalIgnoreCase);
        var ordem = new List<P>();

        // ---------- parâmetros ----------
        foreach (var el in Elementos(r.XmlParametros, "parâmetros", avisos))
        {
            var id = V(el, "IdParametro");
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^\w+$") || ps.ContainsKey(id)) { avisos.Add($"Parâmetro '{id}' ignorado (id vazio, inválido ou repetido)."); continue; }
            var p = new P { Id = id };
            var tipoLegado = V(el, "TipoCampo");
            p.Tipo = tipoLegado.ToLowerInvariant() switch
            {
                "inteiro" => TipoParametro.Inteiro,
                "decimal" => TipoParametro.Decimal,
                "date" => TipoParametro.Date,
                "datefixedtime" => TipoParametro.Date,
                "datetime" => TipoParametro.DateTime,
                "checkbox" => TipoParametro.Checkbox,
                _ => TipoParametro.String,
            };
            if (tipoLegado.Equals("DateFixedTime", StringComparison.OrdinalIgnoreCase)) avisos.Add($"[{id}]: tipo DateFixedTime tratado como Date (hora fixa do legado não é replicada).");
            else if (tipoLegado.Length > 0 && !Regex.IsMatch(tipoLegado, "^(String|Inteiro|Decimal|Date|DateTime|Checkbox)$", RegexOptions.IgnoreCase))
                avisos.Add($"[{id}]: tipo '{tipoLegado}' desconhecido, usado como texto.");

            var campo = V(el, "CampoReferente");
            if (campo.Length > 0 && ColunaSimples.IsMatch(campo)) p.Campo = campo;
            else if (campo.Length > 0) avisos.Add($"[{id}]: CampoReferente '{campo}' não parece uma coluna e foi descartado.");

            p.ValorUnico = Bool(el, "FlagValorUnico");
            p.Obrigatorio = Bool(el, "FlagObrigatorio");

            var lista = V(el, "ListaPesquisa");
            var rotulo = V(el, "Label");
            p.Descricao = rotulo; // a lista de ListaPesquisa vira opções (combobox), não texto da descrição
            p.Opcoes = Opcoes(lista);

            p.Padrao = Padrao(el, "ValorDefault1", id, avisos);
            p.Padrao2 = Padrao(el, "ValorDefault2", id, avisos);

            var fixa = V(el, "CondicaoFixa");
            if (fixa.Length > 0)
            {
                p.Fixa = fixa.ToLowerInvariant().Replace("_", "").Replace(" ", "") switch
                {
                    "igual" or "equal" or "equals" or "=" => OperadorComparacao.Igual,
                    "diferente" or "notequal" or "<>" => OperadorComparacao.Diferente,
                    "maior" or "greater" => OperadorComparacao.Maior,
                    "maiorigual" or "greaterorequal" => OperadorComparacao.MaiorIgual,
                    "menor" or "less" => OperadorComparacao.Menor,
                    "menorigual" or "lessorequal" => OperadorComparacao.MenorIgual,
                    "entre" or "between" => OperadorComparacao.Between,
                    "like" or "contem" or "contém" => OperadorComparacao.Like,
                    "notlike" or "naocontem" or "nãocontém" => OperadorComparacao.NotLike,
                    _ => null,
                };
                if (p.Fixa is null) avisos.Add($"[{id}]: condição fixa '{fixa}' sem equivalente; o parâmetro ficou com comparação livre.");
            }

            switch (V(el, "ParametroSistema").ToLowerInvariant())
            {
                case "selecaoempresas":
                    p.Origem = OrigemParametro.Servidor; p.Chave = "idsEmpresa"; p.Tipo = TipoParametro.Inteiro; p.ValorUnico = false;
                    break;
                case "usuariologado":
                    p.Origem = OrigemParametro.Servidor; p.Chave = "idUsuario"; p.Tipo = TipoParametro.Inteiro; p.ValorUnico = true;
                    break;
            }
            ps[id] = p; ordem.Add(p);
        }

        // ---------- passos (comandos) ----------
        var passos = new List<PassoSql>();
        var nomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var comandos = Elementos(r.XmlComandos, "comandos", avisos).ToList();
        if (comandos.Count == 0) avisos.Add("O relatório não tem comandos SQL.");
        foreach (var el in comandos)
        {
            var sql = (el.Element("Comando")?.Value ?? "").Trim('\r', '\n', ' ', '\t');
            if (sql.Length == 0) continue;
            var nome = NomeInvalido.Replace(V(el, "Id"), "");
            if (nome.Length == 0) nome = $"resultado{passos.Count + 1}";
            for (var n = 2; !nomes.Add(nome); n++) nome = $"{NomeInvalido.Replace(V(el, "Id"), "")}{n}";
            _ = int.TryParse(V(el, "Ordem"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ord);
            passos.Add(new PassoSql { Ordem = ord, Nome = nome, Sql = sql, SomenteExecucao = Bool(el, "FlagExecucao") });
        }

        // ---------- regras ($SE ... $FIMSE -> casos estruturados) ----------
        var regras = new List<RegraComando>();
        var comparados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var el in Elementos(r.XmlRegras, "regras", avisos))
        {
            var id = V(el, "IdRegra");
            if (!Regex.IsMatch(id, @"^\w+$")) { avisos.Add($"Regra '{id}' ignorada (id inválido)."); continue; }
            regras.Add(ConverterRegra(id, el.Element("Descricao")?.Value ?? "", ps, ordem, comparados, avisos));
        }

        // ---------- parâmetros de sistema usados no SQL sem declaração ----------
        var textos = passos.Select(p => p.Sql).Concat(regras.SelectMany(g => g.Casos.Select(k => k.Sql).Append(g.Senao ?? "")));
        foreach (var token in textos.SelectMany(t => UsoParametro.Matches(t).Select(m => m.Groups["p"].Value)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (ps.ContainsKey(token) || !SistemaConhecido.Contains(token)) continue;
            var novo = new P { Id = token, ValorUnico = true, Tipo = TipoParametro.Inteiro, Origem = OrigemParametro.Servidor };
            switch (token.ToLowerInvariant())
            {
                case "idusuario": novo.Chave = "idUsuario"; break;
                case "idempresa" or "idempresas":
                    novo.Chave = "idsEmpresa"; novo.ValorUnico = false;
                    avisos.Add($"[{token}] era a empresa logada; agora são as empresas do servidor (Painel:IdsEmpresa). Em [{token}] só vale o 1º valor: prefira {{Empresas as Coluna}}.");
                    break;
                default:
                    novo.Origem = OrigemParametro.Chamador; novo.Tipo = TipoParametro.String; novo.ValorUnico = true; novo.Chave = null;
                    novo.Descricao = "Parâmetro do sistema do legado (ex.: layout selecionado).";
                    avisos.Add($"[{token}] é um parâmetro do sistema legado e passou a ser um parâmetro de texto opcional.");
                    break;
            }
            ps[token] = novo; ordem.Add(novo);
        }

        // ---------- parâmetros que, no legado, eram nomes de coluna colados no SQL -> regra com lista fechada ----------
        var regraIds = new HashSet<string>(regras.Select(g => g.Id), StringComparer.OrdinalIgnoreCase);
        var colunas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // parâmetro -> id da regra
        foreach (var p in ordem.Where(p => p.Origem == OrigemParametro.Chamador && p.Tipo == TipoParametro.String && p.ChavesLista.Any()
                                           && p.ChavesLista.All(k => Regex.IsMatch(k, @"^[A-Za-z_]\w*\.\w+$"))))
        {
            var rid = p.Id + "Coluna";
            for (var n = 2; !regraIds.Add(rid); n++) rid = $"{p.Id}Coluna{n}";
            colunas[p.Id] = rid;
            regras.Add(new RegraComando
            {
                Id = rid,
                Casos = p.ChavesLista.Select(k => new CasoRegra { Se = [new CondicaoRegra { Param = p.Id, Op = OperadorCondicao.Igual, Valor = k }], Sql = k }).ToList(),
                // Opção vazia ("Sem quebra") = nada no SQL; sem ela, vale a primeira coluna.
                Senao = p.Opcoes.Any(o => o.Valor.Length == 0) ? "" : p.ChavesLista.First(),
            });
            if (p.Padrao is null && !p.Opcoes.Any(o => o.Valor.Length == 0)) p.Padrao = p.ChavesLista.First();
            avisos.Add($"[{p.Id}] era um nome de coluna colado no SQL; virou a regra %{rid}% com lista fechada ({string.Join(", ", p.ChavesLista)}{(p.Opcoes.Any(o => o.Valor.Length == 0) ? " ou vazio" : "")}). Confira se o SQL usa %{rid}% onde antes usava [{p.Id}].");
        }

        // ---------- ajustes de SQL (nos passos e nos trechos das regras) ----------
        string Ajustar(string sql) => AjustarSql(sql, ps, colunas, avisos);
        for (var i = 0; i < passos.Count; i++)
            passos[i] = new PassoSql { Ordem = passos[i].Ordem, Nome = passos[i].Nome, SomenteExecucao = passos[i].SomenteExecucao, Sql = Ajustar(passos[i].Sql) };
        regras = regras.Select(g => new RegraComando
        {
            Id = g.Id,
            Casos = g.Casos.Select(k => new CasoRegra { Se = k.Se, Sql = colunas.ContainsValue(g.Id) ? k.Sql : Ajustar(k.Sql) }).ToList(),
            Senao = colunas.ContainsValue(g.Id) ? g.Senao : g.Senao is null ? null : Ajustar(g.Senao),
        }).ToList();

        // ---------- escrita escondida em passo "que devolve dados" ----------
        for (var i = 0; i < passos.Count; i++)
        {
            if (passos[i].SomenteExecucao) continue;
            if (DetectorEscrita.Encontrar(passos[i].Sql) is not { } palavra) continue;
            avisos.Add($"⚠ O comando '{passos[i].Nome}' contém '{palavra}' (altera dados) e também devolve dados. Foi marcado como somente execução: separe em dois passos (preparo e leitura).");
            passos[i] = new PassoSql { Ordem = passos[i].Ordem, Nome = passos[i].Nome, Sql = passos[i].Sql, SomenteExecucao = true };
        }
        if (passos.Any(p => p.SomenteExecucao) && passos.All(p => p.SomenteExecucao))
            avisos.Add("Nenhum passo devolve dados; o catálogo exige ao menos um. Separe a leitura em um passo próprio antes de salvar.");

        // Com lista fechada, o padrão precisa ser uma das opções.
        foreach (var p in ordem.Where(p => p.Opcoes.Count > 0))
        {
            if (p.Padrao is not null && p.Opcoes.All(o => o.Valor != p.Padrao)) { avisos.Add($"[{p.Id}]: o padrão '{p.Padrao}' do legado não está na lista de opções e foi descartado."); p.Padrao = null; }
            if (p.Padrao2 is not null && p.Opcoes.All(o => o.Valor != p.Padrao2)) p.Padrao2 = null;
        }

        // Parâmetro inteiro sem valor é "vazio" aqui (no legado era 0): as regras que o comparam precisam de padrão.
        var comPadrao = new List<string>();
        foreach (var p in ordem.Where(p => p.Tipo == TipoParametro.Inteiro && p.Origem == OrigemParametro.Chamador && p.ValorUnico && p.Padrao is null && comparados.Contains(p.Id)
                                           && (p.Opcoes.Count == 0 || p.Opcoes.Any(o => o.Valor == "0"))))
        {
            p.Padrao = "0"; comPadrao.Add(p.Id);
        }
        if (comPadrao.Count > 0) avisos.Add($"Padrão 0 definido em {string.Join(", ", comPadrao)}: no legado um número sem valor valia 0; aqui seria 'vazio' e as regras não casariam.");

        if (passos.Any(p => Regex.IsMatch(p.Sql, @"(?im)^\s*declare\b")) && passos.Count > 1)
            avisos.Add("Há DECLARE em um passo: variáveis não valem entre passos (cada passo é um lote). Repita o DECLARE no passo que a usa.");

        var comandoId = IdInvalido.Replace(r.IdRelatorio.Trim(), "_");
        if (comandoId.Length > 100) comandoId = comandoId[..100];
        var titulo = (r.Titulo ?? "").Trim();

        var comando = new ComandoSql
        {
            Id = comandoId,
            Descricao = ($"{(titulo.Length > 0 ? titulo : r.IdRelatorio)} (importado de {r.Tabela}.{r.IdRelatorio}). Ajuste a descrição: o Claude a lê para decidir quando usar o comando.").Trim(),
            Ativa = false, // só vale depois de revisado e testado
            MaxLinhas = 5000,
            TimeoutSeg = 60,
            Parametros = ordem.Select(Para).ToList(),
            Regras = regras,
            Passos = passos.OrderBy(p => p.Ordem).Select((p, i) => new PassoSql { Ordem = i + 1, Nome = p.SomenteExecucao ? null : p.Nome, Sql = p.Sql, SomenteExecucao = p.SomenteExecucao }).ToList(),
        };
        avisos.Insert(0, "O comando foi criado inativo: revise, teste e só então ative.");
        return new ConversaoLegado(comando, avisos);
    }

    /// <summary>"''=Sem quebra; P.IdGrupo=Grupo" -> opções de combobox. <c>''</c> ou <c>""</c> é o valor vazio; sem '=' o rótulo é o próprio valor.</summary>
    private static List<OpcaoParametro> Opcoes(string lista)
    {
        var saida = new List<OpcaoParametro>();
        foreach (var item in lista.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var corte = item.IndexOf('=');
            var valor = (corte < 0 ? item : item[..corte]).Trim();
            if (valor is "''" or "\"\"") valor = "";
            else if (valor.Length >= 2 && (valor[0] is '\'' or '"') && valor[^1] == valor[0]) valor = valor[1..^1];
            var rotulo = (corte < 0 ? valor : item[(corte + 1)..]).Trim();
            if (rotulo.Length == 0) rotulo = valor.Length == 0 ? "(vazio)" : valor;
            if (saida.All(o => o.Valor != valor)) saida.Add(new OpcaoParametro { Valor = valor, Rotulo = rotulo });
        }
        return saida;
    }

    private static ParametroComando Para(P p) => new()
    {
        Id = p.Id,
        Campo = p.Campo,
        Descricao = string.IsNullOrWhiteSpace(p.Descricao) ? null : p.Descricao,
        Tipo = p.Tipo,
        ValorUnico = p.ValorUnico,
        Obrigatorio = p.Obrigatorio && p.Origem == OrigemParametro.Chamador,
        Padrao = p.Padrao,
        Padrao2 = p.Padrao2,
        CondicaoFixa = p.Fixa,
        Origem = p.Origem,
        Chave = p.Chave,
        Opcoes = p.Opcoes.Count > 0 ? p.Opcoes : null,
    };

    // ---------- XML ----------

    private static IEnumerable<XElement> Elementos(string? xml, string rotulo, List<string> avisos)
    {
        if (string.IsNullOrWhiteSpace(xml)) return [];
        try { return XDocument.Parse(xml.Trim()).Root?.Elements().ToList() ?? []; }
        catch (System.Xml.XmlException e)
        {
            avisos.Add($"XML de {rotulo} ilegível: {e.Message}");
            return [];
        }
    }

    private static string V(XElement el, string nome) => (el.Element(nome)?.Value ?? "").Trim();
    private static bool Bool(XElement el, string nome) => V(el, nome).Equals("true", StringComparison.OrdinalIgnoreCase);

    private static string? Padrao(XElement el, string nome, string id, List<string> avisos)
    {
        var v = V(el, nome);
        if (v.Length == 0) return null;
        if (v.Equals("DataAtual", StringComparison.OrdinalIgnoreCase) || v.Equals("DataHoraAtual", StringComparison.OrdinalIgnoreCase))
        {
            avisos.Add($"[{id}]: padrão '{v}' (data de hoje) não existe aqui; trate no SQL, por exemplo isnull([{id}], cast(getdate() as date)).");
            return null;
        }
        return v.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : v;
    }

    // ---------- regras ----------

    private static RegraComando ConverterRegra(string id, string texto, Dictionary<string, P> ps, List<P> ordem, HashSet<string> comparados, List<string> avisos)
    {
        var marcas = Diretiva.Matches(texto);
        if (marcas.Count == 0) return new RegraComando { Id = id, Senao = texto.Trim('\r', '\n', ' ', '\t') };

        RegraComando Original(string motivo)
        {
            avisos.Add($"Regra '{id}': {motivo}. O texto original foi mantido no 'senão' para revisão manual.");
            return new RegraComando { Id = id, Senao = texto.Trim('\r', '\n', ' ', '\t') };
        }

        if (texto[..marcas[0].Index].Trim().Length > 0) return Original("há texto antes do primeiro $SE");
        var casos = new List<CasoRegra>();
        string? senao = null;
        var i = 0;
        var aberto = false;
        while (i < marcas.Count)
        {
            var m = marcas[i];
            var tipo = m.Groups[1].Value;
            var inicio = m.Index + m.Length;
            var fim = i + 1 < marcas.Count ? marcas[i + 1].Index : texto.Length;
            string? expr = null;
            if (tipo is "SE" or "SENAOSE")
            {
                var dois = ColunaDaExpressao(texto, inicio);
                if (dois < 0 || dois > fim) return Original("não encontrei o ':' que fecha a condição");
                expr = texto[inicio..dois];
                inicio = dois + 1;
            }
            var corpo = texto[inicio..fim].Trim('\r', '\n', ' ', '\t');

            switch (tipo)
            {
                case "SE":
                    if (aberto) return Original("$SE aninhado");
                    aberto = true;
                    goto case "SENAOSE";
                case "SENAOSE":
                    if (!aberto) return Original("$SENAOSE sem $SE");
                    if (senao is not null) return Original("há $SENAOSE depois do $SENAO");
                    if (!TentarCondicoes(expr!, ps, ordem, comparados, avisos, out var conds)) return Original($"condição não suportada ({expr!.Trim()})");
                    casos.Add(new CasoRegra { Se = conds, Sql = corpo });
                    break;
                case "SENAO":
                    if (!aberto) return Original("$SENAO sem $SE");
                    senao = corpo;
                    break;
                case "FIMSE":
                    if (!aberto) return Original("$FIMSE sem $SE");
                    aberto = false;
                    if (texto[(m.Index + m.Length)..].Trim().Length > 0) return Original("há texto depois do $FIMSE");
                    break;
            }
            i++;
        }
        if (aberto) return Original("falta o $FIMSE");
        return new RegraComando { Id = id, Casos = casos, Senao = senao };
    }

    /// <summary>Posição do ':' que termina a expressão (fora de [ ], de ( ) e de textos entre aspas).</summary>
    private static int ColunaDaExpressao(string texto, int de)
    {
        int colchete = 0, parentese = 0;
        char? aspas = null;
        for (var k = de; k < texto.Length; k++)
        {
            var ch = texto[k];
            if (aspas is not null) { if (ch == aspas) aspas = null; continue; }
            switch (ch)
            {
                case '\'' or '"': aspas = ch; break;
                case '[': colchete++; break;
                case ']': colchete--; break;
                case '(': parentese++; break;
                case ')': parentese--; break;
                case ':' when colchete <= 0 && parentese <= 0: return k;
                case '\n' when colchete <= 0 && parentese <= 0: break; // expressão em várias linhas é aceita
            }
        }
        return -1;
    }

    private static bool TentarCondicoes(string expr, Dictionary<string, P> ps, List<P> ordem, HashSet<string> comparados, List<string> avisos, out List<CondicaoRegra> saida)
    {
        saida = [];
        expr = SemParenteses(expr.Trim());
        if (SemTextos(expr).Contains("||")) return false;

        foreach (var parte in DividirE(expr))
        {
            var m = Atomo.Match(SemParenteses(parte.Trim()));
            if (!m.Success) return false;
            var nome = m.Groups["p"].Value;
            if (m.Groups["i"].Success && m.Groups["i"].Value != "0") avisos.Add($"[{nome}:{m.Groups["i"].Value}] (2º valor) em regra: só o 1º valor é comparado.");
            if (!ps.TryGetValue(nome, out var p))
            {
                p = new P { Id = nome, ValorUnico = true, Descricao = "Declarado automaticamente: usado numa regra do legado." };
                ps[nome] = p; ordem.Add(p);
                avisos.Add($"[{nome}] é usado numa regra mas não estava declarado; foi criado como parâmetro de texto opcional.");
            }

            var v = m.Groups["v"].Value.Trim();
            var literal = v.Length >= 2 && (v[0] == '\'' || v[0] == '"') && v[^1] == v[0] ? v[1..^1] : v;
            var op = m.Groups["op"].Value;
            var nulo = literal.Length == 0 || literal.Equals("null", StringComparison.OrdinalIgnoreCase);
            OperadorCondicao cond;
            if (nulo && op is "!=") cond = OperadorCondicao.Preenchido;
            else if (nulo && op is "==" or "=") cond = OperadorCondicao.Vazio;
            else
            {
                cond = op switch
                {
                    "==" or "=" => OperadorCondicao.Igual,
                    "!=" => OperadorCondicao.Diferente,
                    ">" => OperadorCondicao.Maior,
                    ">=" => OperadorCondicao.MaiorIgual,
                    "<" => OperadorCondicao.Menor,
                    _ => OperadorCondicao.MenorIgual,
                };
                comparados.Add(nome);
            }
            saida.Add(new CondicaoRegra { Param = nome, Op = cond, Valor = cond is OperadorCondicao.Preenchido or OperadorCondicao.Vazio ? null : literal });
        }
        return saida.Count > 0;
    }

    private static string SemTextos(string s) => Regex.Replace(s, @"'[^']*'|""[^""]*""", "''");

    private static string SemParenteses(string s)
    {
        while (s.Length >= 2 && s[0] == '(' && s[^1] == ')')
        {
            var nivel = 0; var envolve = true;
            for (var k = 0; k < s.Length - 1; k++)
            {
                if (s[k] == '(') nivel++;
                else if (s[k] == ')') nivel--;
                if (nivel == 0) { envolve = false; break; }
            }
            if (!envolve) break;
            s = s[1..^1].Trim();
        }
        return s;
    }

    private static IEnumerable<string> DividirE(string expr)
    {
        var parte = new StringBuilder();
        char? aspas = null;
        for (var k = 0; k < expr.Length; k++)
        {
            var ch = expr[k];
            if (aspas is not null) { parte.Append(ch); if (ch == aspas) aspas = null; continue; }
            if (ch is '\'' or '"') { aspas = ch; parte.Append(ch); continue; }
            if (ch == '&' && k + 1 < expr.Length && expr[k + 1] == '&') { yield return parte.ToString(); parte.Clear(); k++; continue; }
            parte.Append(ch);
        }
        yield return parte.ToString();
    }

    // ---------- SQL ----------

    private static string AjustarSql(string sql, Dictionary<string, P> ps, Dictionary<string, string> colunas, List<string> avisos)
    {
        // Nome de coluna colado no SQL: [Ordem] -> %OrdemColuna% (não mexe em '[Ordem]' entre aspas, que é texto).
        foreach (var (param, regra) in colunas)
            sql = Regex.Replace(sql, $@"(?<!')\[{Regex.Escape(param)}(?::\d)?\](?!')", $"%{regra}%", RegexOptions.IgnoreCase);

        // '[X]' (valor entre aspas colado no legado) -> [X] (parâmetro).
        sql = Regex.Replace(sql, @"'\[(?<p>\w+)(?::\d)?\]'", m => ps.ContainsKey(m.Groups["p"].Value) ? $"[{m.Groups["p"].Value}]" : m.Value);

        // coluna IN [X] (lista colada no legado) -> {X as coluna} (condição parametrizada).
        sql = Regex.Replace(sql, @"(?<c>[A-Za-z_][\w.]*)\s+in\s+\[(?<p>\w+)\]", m =>
            ps.ContainsKey(m.Groups["p"].Value) ? $"{{{m.Groups["p"].Value} as {m.Groups["c"].Value}}}" : m.Value, RegexOptions.IgnoreCase);

        if (Regex.IsMatch(sql, @"\bin\s*\(\s*\[\w+\]\s*\)", RegexOptions.IgnoreCase))
            avisos.Add("Há 'in ([Param])': só o 1º valor do parâmetro entra. Para listas use {Param as coluna}.");
        return sql;
    }
}

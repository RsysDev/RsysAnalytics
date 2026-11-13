using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ErpWebAnalytics.Application.Comandos;

/// <summary>
/// Transforma o SQL do catálogo no SQL final. Mantém a sintaxe do interpretador do ERP, mas gera parâmetros
/// (<c>@pN</c>) em vez de inserir valores no texto:
/// <list type="bullet">
/// <item><c>;</c> no início da linha: comentário (removido; <c>;WITH</c> é mantido).</item>
/// <item><c>%Regra%</c>: trecho escolhido por uma regra (ver <see cref="RegraComando"/>).</item>
/// <item><c>[Param]</c>: valor do parâmetro (<c>[Param:1]</c> = segundo valor de um Between). Sem valor vira <c>null</c>.</item>
/// <item><c>{Param}</c>: condição pronta (<c>(Coluna = @p1)</c>); <c>{Param as p.Coluna}</c> troca a coluna. Sem valor = vazio.</item>
/// <item>Linha iniciada por <c>#</c>: só entra se todos os parâmetros que cita tiverem valor.</item>
/// </list>
/// Só são substituídos nomes declarados (parâmetros e regras): <c>[dbo].[Tabela]</c> e <c>LIKE '%a%'</c> passam intactos.
/// </summary>
public static class InterpretadorSql
{
    private const int ProfundidadeMaximaRegras = 10;

    // {Param} / {Param as coluna}  |  [Param] / [Param:0|1]
    private static readonly Regex Placeholder = new(
        @"\{(?<c>\w+)(?:\s+as\s+(?<as>[\w.\[\]]+))?\}|\[(?<v>\w+)(?::(?<i>[01]))?\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UsoRegra = new(@"%(?<r>\w+)%", RegexOptions.Compiled);

    private static readonly string[] FormatosData = ["yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff"];

    /// <param name="comando">Comando do catálogo.</param>
    /// <param name="sql">Texto de um passo do comando (os parâmetros e as regras vêm de <paramref name="comando"/>).</param>
    /// <param name="valores">Valores por Id de parâmetro (chamador + servidor). Ausente = sem valor (aplica <c>Padrao</c>).</param>
    public static SqlGerado Gerar(ComandoSql comando, string sql, IReadOnlyDictionary<string, IReadOnlyList<ValorParametro>> valores)
    {
        var parametros = comando.Parametros.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var efetivos = ResolverValores(comando.Parametros, valores);
        var regras = comando.Regras.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);

        var linhas = SemComentarios(sql);
        linhas = ExpandirRegras(linhas, regras, parametros, efetivos);

        var saida = new StringBuilder();
        var sqlParametros = new List<ParametroSql>();
        foreach (var linha in linhas)
        {
            var processada = ProcessarLinha(linha, parametros, efetivos, sqlParametros);
            if (processada != null) saida.AppendLine(processada);
        }

        return new SqlGerado(saida.ToString(), sqlParametros);
    }

    // ---------- valores ----------

    private sealed record Valor(OperadorComparacao Op, object? V1, object? V2);

    private static Dictionary<string, List<Valor>> ResolverValores(
        List<ParametroComando> parametros, IReadOnlyDictionary<string, IReadOnlyList<ValorParametro>> informados)
    {
        var resultado = new Dictionary<string, List<Valor>>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in parametros)
        {
            IReadOnlyList<ValorParametro> brutos = informados.TryGetValue(p.Id, out var l) ? l : [];
            brutos = brutos.Where(v => !string.IsNullOrWhiteSpace(v.Valor1) || !string.IsNullOrWhiteSpace(v.Valor2)).ToList();

            if (brutos.Count == 0 && !string.IsNullOrWhiteSpace(p.Padrao))
            {
                var op = p.CondicaoFixa ?? (string.IsNullOrWhiteSpace(p.Padrao2) ? OperadorComparacao.Igual : OperadorComparacao.Between);
                brutos = [new ValorParametro(op, p.Padrao, p.Padrao2)];
            }

            if (brutos.Count == 0)
            {
                if (p.Obrigatorio) throw new ComandoInvalidoException($"O parâmetro '{p.Id}' é obrigatório.");
                resultado[p.Id] = [];
                continue;
            }

            if (p.ValorUnico) brutos = [brutos[0] with { Op = OperadorComparacao.Igual, Valor2 = null }];

            resultado[p.Id] = brutos
                .Select(v =>
                {
                    var op = p.CondicaoFixa ?? v.Op;
                    ValidarOperador(p, op);
                    return new Valor(op, Converter(p, v.Valor1), Converter(p, v.Valor2));
                })
                .ToList();
        }
        return resultado;
    }

    private static void ValidarOperador(ParametroComando p, OperadorComparacao op)
    {
        var ok = p.Tipo switch
        {
            TipoParametro.Checkbox => op is OperadorComparacao.Igual or OperadorComparacao.Diferente,
            TipoParametro.String => true,
            _ => op is not (OperadorComparacao.Like or OperadorComparacao.NotLike),
        };
        if (!ok) throw new ComandoInvalidoException($"O operador '{op}' não é válido para o parâmetro '{p.Id}' ({p.Tipo}).");
    }

    private static object? Converter(ParametroComando p, string? bruto)
    {
        if (string.IsNullOrWhiteSpace(bruto)) return null;
        bruto = bruto.Trim();
        var inv = CultureInfo.InvariantCulture;
        switch (p.Tipo)
        {
            case TipoParametro.String:
                return bruto;
            case TipoParametro.Inteiro:
                if (long.TryParse(bruto, NumberStyles.Integer, inv, out var l)) return l;
                break;
            case TipoParametro.Decimal:
                if (decimal.TryParse(bruto, NumberStyles.Number, inv, out var d)) return d;
                break;
            case TipoParametro.Date:
            case TipoParametro.DateTime:
                if (DateTime.TryParseExact(bruto, FormatosData, inv, DateTimeStyles.None, out var dt))
                    return p.Tipo == TipoParametro.Date ? dt.Date : dt;
                break;
            case TipoParametro.Checkbox:
                if (bruto is "1" || bruto.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
                if (bruto is "0" || bruto.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
                break;
        }
        var esperado = p.Tipo is TipoParametro.Date or TipoParametro.DateTime ? "data ISO (yyyy-MM-dd)" : p.Tipo.ToString();
        throw new ComandoInvalidoException($"Valor '{bruto}' inválido para o parâmetro '{p.Id}': esperado {esperado}.");
    }

    // ---------- linhas e regras ----------

    private static List<string> SemComentarios(string sql) =>
        sql.Split(["\r\n", "\n", "\r"], StringSplitOptions.None)
            .Where(l =>
            {
                var t = l.TrimStart();
                return t.Length > 0 && (!t.StartsWith(';') || t.StartsWith(";WITH", StringComparison.OrdinalIgnoreCase));
            })
            .ToList();

    private static List<string> ExpandirRegras(
        List<string> linhas, Dictionary<string, RegraComando> regras,
        Dictionary<string, ParametroComando> parametros, Dictionary<string, List<Valor>> valores)
    {
        if (regras.Count == 0) return linhas;

        for (var passo = 0; passo < ProfundidadeMaximaRegras; passo++)
        {
            var houveSubstituicao = false;
            var novas = new List<string>();
            foreach (var linha in linhas)
            {
                var nova = UsoRegra.Replace(linha, m =>
                {
                    if (!regras.TryGetValue(m.Groups["r"].Value, out var regra)) return m.Value;
                    houveSubstituicao = true;
                    return AvaliarRegra(regra, parametros, valores);
                });
                novas.AddRange(nova.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).Where(l => l.Trim().Length > 0));
            }
            linhas = novas;
            if (!houveSubstituicao) return linhas;
        }
        throw new ComandoInvalidoException("Regras aninhadas demais (possível referência circular).");
    }

    private static string AvaliarRegra(RegraComando regra, Dictionary<string, ParametroComando> parametros, Dictionary<string, List<Valor>> valores)
    {
        foreach (var caso in regra.Casos)
            if (caso.Se.All(c => AvaliarCondicao(regra, c, parametros, valores)))
                return caso.Sql;
        return regra.Senao ?? "";
    }

    private static bool AvaliarCondicao(
        RegraComando regra, CondicaoRegra c, Dictionary<string, ParametroComando> parametros, Dictionary<string, List<Valor>> valores)
    {
        if (!parametros.TryGetValue(c.Param, out var p))
            throw new ComandoInvalidoException($"A regra '{regra.Id}' usa o parâmetro '{c.Param}', que não existe.");

        var primeiro = valores[p.Id].FirstOrDefault()?.V1;
        switch (c.Op)
        {
            case OperadorCondicao.Preenchido: return primeiro != null;
            case OperadorCondicao.Vazio: return primeiro == null;
        }

        if (primeiro == null) return false;
        var literal = Converter(p, c.Valor)
            ?? throw new ComandoInvalidoException($"A regra '{regra.Id}' compara '{c.Param}' sem informar 'valor'.");
        var cmp = primeiro is string s
            ? string.Compare(s, (string)literal, StringComparison.OrdinalIgnoreCase)
            : Comparer<object>.Default.Compare(primeiro, literal);
        return c.Op switch
        {
            OperadorCondicao.Igual => cmp == 0,
            OperadorCondicao.Diferente => cmp != 0,
            OperadorCondicao.Maior => cmp > 0,
            OperadorCondicao.MaiorIgual => cmp >= 0,
            OperadorCondicao.Menor => cmp < 0,
            OperadorCondicao.MenorIgual => cmp <= 0,
            _ => false,
        };
    }

    // ---------- placeholders ----------

    private static string? ProcessarLinha(
        string linha, Dictionary<string, ParametroComando> parametros,
        Dictionary<string, List<Valor>> valores, List<ParametroSql> saida)
    {
        var inicio = linha.Length - linha.TrimStart().Length;
        var condicional = inicio < linha.Length && linha[inicio] == '#';
        if (condicional) linha = linha.Remove(inicio, 1);

        var semValor = false;
        var resultado = Placeholder.Replace(linha, m =>
        {
            var ehCondicao = m.Groups["c"].Success;
            var id = ehCondicao ? m.Groups["c"].Value : m.Groups["v"].Value;
            if (!parametros.TryGetValue(id, out var p)) return m.Value;

            var lista = valores[p.Id];
            if (ehCondicao)
            {
                var coluna = m.Groups["as"].Success ? m.Groups["as"].Value : p.ColunaEfetiva;
                var cond = Condicao(p, lista, coluna, saida);
                if (cond.Length == 0) semValor = true;
                return cond;
            }

            var primeiro = lista.FirstOrDefault();
            var valor = m.Groups["i"].Value == "1" ? primeiro?.V2 : primeiro?.V1;
            if (valor == null)
            {
                semValor = true;
                return "null";
            }
            return NovoParametro(saida, valor, p.Tipo);
        });

        return condicional && semValor ? null : resultado;
    }

    private static string NovoParametro(List<ParametroSql> saida, object valor, TipoParametro tipo)
    {
        var nome = "@p" + (saida.Count + 1);
        saida.Add(new ParametroSql(nome, valor, tipo));
        return nome;
    }

    private static string Condicao(ParametroComando p, List<Valor> valores, string coluna, List<ParametroSql> saida)
    {
        if (valores.Count == 0) return "";
        var partes = valores.Select(v => Parte(p, v, coluna, saida)).ToList();
        return "(" + string.Join(" or ", partes) + ")";
    }

    private static string Parte(ParametroComando p, Valor v, string coluna, List<ParametroSql> saida)
    {
        if (v.Op == OperadorComparacao.Between)
        {
            if (v.V1 == null || v.V2 == null)
                throw new ComandoInvalidoException($"O parâmetro '{p.Id}' com 'between' exige valor e valor2.");
            var a = NovoParametro(saida, v.V1, p.Tipo);
            var b = NovoParametro(saida, v.V2, p.Tipo);
            return p.Tipo == TipoParametro.Date
                ? $"{coluna} >= {a} and {coluna} < dateadd(day, 1, {b})"   // data inclusiva, mesmo com hora gravada
                : $"{coluna} between {a} and {b}";
        }

        if (v.V1 == null)
        {
            return v.Op switch
            {
                OperadorComparacao.Igual or OperadorComparacao.Like => $"{coluna} is null",
                OperadorComparacao.Diferente or OperadorComparacao.NotLike => $"{coluna} is not null",
                _ => throw new ComandoInvalidoException($"O parâmetro '{p.Id}' exige um valor para '{v.Op}'."),
            };
        }

        var n = NovoParametro(saida, v.V1, p.Tipo);
        if (p.Tipo == TipoParametro.Date)
        {
            // Comparações por dia inteiro: coluna pode ter hora.
            switch (v.Op)
            {
                case OperadorComparacao.Igual: return $"{coluna} >= {n} and {coluna} < dateadd(day, 1, {n})";
                case OperadorComparacao.Diferente: return $"({coluna} < {n} or {coluna} >= dateadd(day, 1, {n}))";
                case OperadorComparacao.Maior: return $"{coluna} >= dateadd(day, 1, {n})";
                case OperadorComparacao.MenorIgual: return $"{coluna} < dateadd(day, 1, {n})";
            }
        }

        var sinal = v.Op switch
        {
            OperadorComparacao.Igual => "=",
            OperadorComparacao.Diferente => "<>",
            OperadorComparacao.Maior => ">",
            OperadorComparacao.MaiorIgual => ">=",
            OperadorComparacao.Menor => "<",
            OperadorComparacao.MenorIgual => "<=",
            OperadorComparacao.Like => "like",
            OperadorComparacao.NotLike => "not like",
            _ => throw new ComandoInvalidoException($"Operador '{v.Op}' não suportado."),
        };
        return $"{coluna} {sinal} {n}";
    }
}

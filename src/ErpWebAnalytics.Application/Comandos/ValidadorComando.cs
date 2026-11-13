using System.Globalization;
using System.Text.RegularExpressions;

namespace ErpWebAnalytics.Application.Comandos;

/// <summary>Regras que um comando precisa cumprir para ser gravado ou testado. Normaliza os passos (ordem e nomes).</summary>
public static class ValidadorComando
{
    private static readonly Regex IdComando = new(@"^[A-Za-z0-9_.\-]{1,100}$", RegexOptions.Compiled);
    private static readonly Regex Identificador = new(@"^\w{1,64}$", RegexOptions.Compiled);

    /// <summary>
    /// Passos em ordem de execução (empate = ordem de aparição). Passos que devolvem dados ganham nome
    /// (<c>resultado1</c>, <c>resultado2</c>... se vazio). Lança <see cref="ComandoInvalidoException"/> se algo estiver errado.
    /// </summary>
    public static List<PassoSql> NormalizarPassos(string id, IEnumerable<PassoSql> passos)
    {
        var lista = passos.ToList();
        if (lista.Any(p => string.IsNullOrWhiteSpace(p.Sql)))
            throw new ComandoInvalidoException($"O comando '{id}' tem um passo sem SQL.");

        var ordenados = lista.OrderBy(p => p.Ordem).ToList(); // OrderBy é estável
        var resultado = new List<PassoSql>();
        var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var contador = 0;
        foreach (var p in ordenados)
        {
            if (p.SomenteExecucao) { resultado.Add(p); continue; }
            contador++;
            var nome = string.IsNullOrWhiteSpace(p.Nome) ? $"resultado{contador}" : p.Nome.Trim();
            if (!usados.Add(nome))
                throw new ComandoInvalidoException($"O comando '{id}' repete o nome de resultado '{nome}'.");
            resultado.Add(new PassoSql { Ordem = p.Ordem, Nome = nome, Sql = p.Sql, SomenteExecucao = false });
        }

        if (contador == 0)
            throw new ComandoInvalidoException($"O comando '{id}' não tem nenhum passo que devolva dados.");
        return resultado;
    }

    /// <summary>Valida o comando inteiro e devolve os passos normalizados. Junta todos os problemas numa só mensagem.</summary>
    public static List<PassoSql> Validar(ComandoSql c)
    {
        var erros = new List<string>();

        if (!IdComando.IsMatch(c.Id ?? "")) erros.Add("Id: use letras, números, _ . - (até 100 caracteres).");
        if (string.IsNullOrWhiteSpace(c.Descricao)) erros.Add("Descrição é obrigatória (o Claude a lê para saber quando usar o comando).");
        else if (c.Descricao.Length > 1000) erros.Add("Descrição: no máximo 1000 caracteres.");
        if (c.MaxLinhas is < 1 or > 5000) erros.Add("Máximo de linhas: de 1 a 5000.");
        if (c.TimeoutSeg is < 1 or > 120) erros.Add("Timeout: de 1 a 120 segundos.");

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in c.Parametros)
        {
            if (!Identificador.IsMatch(p.Id ?? "")) { erros.Add($"Parâmetro '{p.Id}': id inválido (letras, números e _)."); continue; }
            if (!ids.Add(p.Id!)) erros.Add($"Parâmetro '{p.Id}' repetido.");
            if (p.Origem == OrigemParametro.Servidor &&
                !(string.Equals(p.Chave, "idsEmpresa", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Chave, "idUsuario", StringComparison.OrdinalIgnoreCase)))
                erros.Add($"Parâmetro '{p.Id}': origem Servidor exige chave idsEmpresa ou idUsuario.");
            if (p.Opcoes is { Count: > 0 })
            {
                var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var o in p.Opcoes)
                {
                    if (string.IsNullOrWhiteSpace(o.Rotulo)) erros.Add($"Parâmetro '{p.Id}': toda opção precisa de um rótulo.");
                    if (!vistos.Add(o.Valor ?? "")) erros.Add($"Parâmetro '{p.Id}': opção '{o.Valor}' repetida.");
                }
                foreach (var padrao in new[] { p.Padrao, p.Padrao2 }.Where(x => !string.IsNullOrEmpty(x)))
                    if (!vistos.Contains(padrao!)) erros.Add($"Parâmetro '{p.Id}': o padrão '{padrao}' não está entre as opções.");
            }
            ValidarPadrao(erros, p, p.Padrao, "padrão");
            ValidarPadrao(erros, p, p.Padrao2, "padrão 2");
        }

        var regras = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in c.Regras)
        {
            if (!Identificador.IsMatch(r.Id ?? "")) { erros.Add($"Regra '{r.Id}': id inválido (letras, números e _)."); continue; }
            if (!regras.Add(r.Id!)) erros.Add($"Regra '{r.Id}' repetida.");
            foreach (var caso in r.Casos)
            {
                foreach (var cond in caso.Se.Where(x => !ids.Contains(x.Param)))
                    erros.Add($"Regra '{r.Id}': a condição usa o parâmetro '{cond.Param}', que não existe.");
            }
        }

        // Regras podem usar outras regras (%Outra% dentro do trecho); referência circular nunca termina de expandir.
        var porId = c.Regras.Where(r => !string.IsNullOrWhiteSpace(r.Id)).GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var r in porId.Values)
            if (AlcancaA(r, r.Id, porId, [])) erros.Add($"Regra '{r.Id}': referência circular (ela usa a si mesma, direta ou por outras regras).");

        List<PassoSql> passos = [];
        try { passos = NormalizarPassos(c.Id ?? "", c.Passos); }
        catch (ComandoInvalidoException e) { erros.Add(e.Message); }
        foreach (var p in passos.Where(p => !string.IsNullOrWhiteSpace(p.Nome) && !Identificador.IsMatch(p.Nome)))
            erros.Add($"Nome de resultado '{p.Nome}': use letras, números e _.");

        // Passo que "devolve dados" roda em executar_comando (somente leitura): não pode escrever, nem por uma regra que ele use.
        foreach (var p in passos.Where(p => !p.SomenteExecucao))
        {
            var texto = new List<string> { p.Sql };
            var vistas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fila = new Queue<string>(UsoRegra.Matches(p.Sql).Select(m => m.Groups[1].Value).Where(porId.ContainsKey));
            while (fila.Count > 0)
            {
                var id = fila.Dequeue();
                if (!vistas.Add(id)) continue;
                var r = porId[id];
                var trechos = r.Casos.Select(k => k.Sql).Append(r.Senao ?? "");
                foreach (var t in trechos)
                {
                    texto.Add(t ?? "");
                    foreach (Match m in UsoRegra.Matches(t ?? "")) if (porId.ContainsKey(m.Groups[1].Value)) fila.Enqueue(m.Groups[1].Value);
                }
            }
            if (DetectorEscrita.Encontrar(string.Join("\n", texto)) is { } palavra)
                erros.Add($"Resultado '{p.Nome}': contém '{palavra}', que altera dados. Passos que devolvem dados são somente leitura: " +
                          "marque o passo como \"somente execução\" (e leia os dados em outro passo).");
        }

        if (erros.Count > 0) throw new ComandoInvalidoException(string.Join(" ", erros));
        return passos;
    }

    private static readonly Regex UsoRegra = new(@"%(\w+)%", RegexOptions.Compiled);

    private static IEnumerable<string> RegrasUsadas(RegraComando r, Dictionary<string, RegraComando> todas) =>
        r.Casos.Select(k => k.Sql).Append(r.Senao ?? "")
            .SelectMany(t => UsoRegra.Matches(t ?? "").Select(m => m.Groups[1].Value))
            .Where(todas.ContainsKey);

    /// <summary>true se, partindo de <paramref name="r"/>, a regra <paramref name="alvo"/> é alcançada (ciclo quando r = alvo).</summary>
    private static bool AlcancaA(RegraComando r, string alvo, Dictionary<string, RegraComando> todas, HashSet<string> visitadas)
    {
        foreach (var usada in RegrasUsadas(r, todas))
        {
            if (usada.Equals(alvo, StringComparison.OrdinalIgnoreCase)) return true;
            if (visitadas.Add(usada) && AlcancaA(todas[usada], alvo, todas, visitadas)) return true;
        }
        return false;
    }

    private static void ValidarPadrao(List<string> erros, ParametroComando p, string? valor, string rotulo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        var ok = p.Tipo switch
        {
            TipoParametro.Inteiro => long.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            TipoParametro.Decimal => decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            TipoParametro.Date or TipoParametro.DateTime => DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            TipoParametro.Checkbox => valor is "true" or "false" or "1" or "0",
            _ => true,
        };
        if (!ok) erros.Add($"Parâmetro '{p.Id}': {rotulo} '{valor}' não é um valor {p.Tipo}.");
    }
}

using System.Text;

namespace ErpWebAnalytics.Application.Perfis.Dinamico;

/// <summary>Linha lida do banco, por nome de coluna (sem diferenciar maiúsculas). Colunas auxiliares de relação também entram.</summary>
public sealed class Linha : Dictionary<string, object?>
{
    public Linha() : base(StringComparer.OrdinalIgnoreCase) { }
}

public enum OperadorFiltro { Igual, Diferente, Maior, MaiorOuIgual, Menor, MenorOuIgual, Contem, ComecaCom, Em, Nulo }

public sealed record CondicaoFiltro(string Coluna, OperadorFiltro Operador, object? Valor);

/// <summary>Condições do mesmo nó se combinam com E; os nós de <see cref="E"/> também; os de <see cref="Ou"/> formam um grupo com OU.</summary>
public sealed class NoFiltro
{
    public List<CondicaoFiltro> Condicoes { get; } = [];
    public List<NoFiltro> E { get; } = [];
    public List<NoFiltro> Ou { get; } = [];
}

public sealed record OrdemColuna(string Coluna, bool Descendente);

public sealed record ParametroConsulta(string Nome, object? Valor, string TipoSql);

public sealed record ConsultaGerada(string Sql, IReadOnlyList<ParametroConsulta> Parametros);

/// <summary>
/// Monta o SQL das consultas do GraphQL dinâmico. Função pura: todo valor vira parâmetro, e todo nome de coluna é conferido
/// contra a estrutura real da tabela e citado com colchetes, então nada vindo do cliente entra no texto do SQL.
/// </summary>
public static class ConstrutorConsulta
{
    public const int MaxEmItens = 500;

    private sealed class Ctx(TabelaBanco tabela)
    {
        public readonly TabelaBanco Tabela = tabela;
        public readonly List<ParametroConsulta> Parametros = [];

        public ColunaBanco Coluna(string nome) =>
            Tabela.Colunas.FirstOrDefault(c => c.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"A coluna '{nome}' não existe em {Tabela.Chave}.");

        public string Q(string coluna) => "[" + Coluna(coluna).Nome.Replace("]", "]]") + "]";

        public string P(object? valor, string tipoSql)
        {
            var nome = "@p" + Parametros.Count;
            Parametros.Add(new ParametroConsulta(nome, valor, tipoSql));
            return nome;
        }
    }

    private static string Origem(TabelaBanco t) => $"[{t.Schema.Replace("]", "]]")}].[{t.Nome.Replace("]", "]]")}]";

    public static ConsultaGerada Listar(
        TabelaBanco tabela, IReadOnlyList<string> colunas, NoFiltro? filtro, IReadOnlyList<OrdemColuna> ordem, int pular, int tomar)
    {
        var c = new Ctx(tabela);
        var sb = new StringBuilder("select ").AppendJoin(", ", colunas.Select(c.Q)).Append(" from ").Append(Origem(tabela));
        AppendOnde(sb, c, filtro);
        sb.Append(" order by ").Append(OrderBy(c, ordem));
        sb.Append(" offset ").Append(Math.Max(0, pular)).Append(" rows fetch next ").Append(Math.Max(1, tomar)).Append(" rows only");
        return new ConsultaGerada(sb.ToString(), c.Parametros);
    }

    public static ConsultaGerada Contar(TabelaBanco tabela, NoFiltro? filtro)
    {
        var c = new Ctx(tabela);
        var sb = new StringBuilder("select count_big(*) from ").Append(Origem(tabela));
        AppendOnde(sb, c, filtro);
        return new ConsultaGerada(sb.ToString(), c.Parametros);
    }

    /// <summary>Linhas cujas colunas <paramref name="destinos"/> valem uma das <paramref name="chaves"/> (uma relação "para um").</summary>
    public static ConsultaGerada PorChaves(
        TabelaBanco tabela, IReadOnlyList<string> colunas, IReadOnlyList<string> destinos, IReadOnlyList<object?[]> chaves)
    {
        var c = new Ctx(tabela);
        var sb = new StringBuilder("select ").AppendJoin(", ", colunas.Select(c.Q)).Append(" from ").Append(Origem(tabela))
            .Append(" where ").Append(CondicaoChaves(c, destinos, chaves));
        return new ConsultaGerada(sb.ToString(), c.Parametros);
    }

    /// <summary>
    /// Como <see cref="PorChaves"/>, mas devolve só uma página por chave (uma relação "para muitos"): numera as linhas de cada chave
    /// e fica com as posições de <paramref name="pular"/>+1 até <paramref name="pular"/>+<paramref name="tomar"/>.
    /// </summary>
    public static ConsultaGerada PorChavesPaginado(
        TabelaBanco tabela, IReadOnlyList<string> colunas, IReadOnlyList<string> destinos, IReadOnlyList<object?[]> chaves,
        IReadOnlyList<OrdemColuna> ordem, int pular, int tomar)
    {
        var c = new Ctx(tabela);
        var particao = string.Join(", ", destinos.Select(c.Q));
        var interna = new StringBuilder("select ").AppendJoin(", ", colunas.Select(c.Q))
            .Append(", row_number() over (partition by ").Append(particao).Append(" order by ").Append(OrderBy(c, ordem)).Append(") as [__rn] from ")
            .Append(Origem(tabela)).Append(" where ").Append(CondicaoChaves(c, destinos, chaves));
        var sql = $"select {string.Join(", ", colunas.Select(x => "x." + c.Q(x)))} from ({interna}) x where x.[__rn] > {Math.Max(0, pular)} and x.[__rn] <= {Math.Max(0, pular) + Math.Max(1, tomar)} order by x.[__rn]";
        return new ConsultaGerada(sql, c.Parametros);
    }

    /// <summary>
    /// <c>update ... set coluna = @valor ... where chave in (...)</c>: sempre por chave primária (as chaves vêm de uma leitura prévia
    /// limitada), então o filtro do cliente nunca é aplicado direto ao UPDATE.
    /// </summary>
    public static ConsultaGerada Atualizar(
        TabelaBanco tabela, IReadOnlyList<(string Coluna, object? Valor)> valores, IReadOnlyList<string> chaves, IReadOnlyList<object?[]> linhasChaves)
    {
        if (valores.Count == 0) throw new ArgumentException("Nada para atualizar.");
        var c = new Ctx(tabela);
        var sets = string.Join(", ", valores.Select(v => $"{c.Q(v.Coluna)} = {c.P(v.Valor, c.Coluna(v.Coluna).TipoSql)}"));
        return new ConsultaGerada($"update {Origem(tabela)} set {sets} where {CondicaoChaves(c, chaves, linhasChaves)}", c.Parametros);
    }

    // ---------- partes ----------

    private static string OrderBy(Ctx c, IReadOnlyList<OrdemColuna> ordem) =>
        ordem.Count == 0 ? "(select null)" : string.Join(", ", ordem.Select(o => c.Q(o.Coluna) + (o.Descendente ? " desc" : " asc")));

    private static void AppendOnde(StringBuilder sb, Ctx c, NoFiltro? filtro)
    {
        var onde = filtro is null ? null : No(c, filtro);
        if (!string.IsNullOrEmpty(onde)) sb.Append(" where ").Append(onde);
    }

    private static string CondicaoChaves(Ctx c, IReadOnlyList<string> destinos, IReadOnlyList<object?[]> chaves)
    {
        if (chaves.Count == 0) return "1 = 0";
        if (destinos.Count == 1)
        {
            var tipo = c.Coluna(destinos[0]).TipoSql;
            return $"{c.Q(destinos[0])} in ({string.Join(", ", chaves.Select(k => c.P(k[0], tipo)))})";
        }
        return "(" + string.Join(" or ", chaves.Select(k =>
            "(" + string.Join(" and ", destinos.Select((d, i) => $"{c.Q(d)} = {c.P(k[i], c.Coluna(d).TipoSql)}")) + ")")) + ")";
    }

    private static string? No(Ctx c, NoFiltro no)
    {
        var partes = new List<string>();
        partes.AddRange(no.Condicoes.Select(x => Condicao(c, x)));
        foreach (var filho in no.E) if (No(c, filho) is { Length: > 0 } s) partes.Add("(" + s + ")");
        var ou = no.Ou.Select(f => No(c, f)).Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (ou.Count > 0) partes.Add("(" + string.Join(" or ", ou.Select(s => "(" + s + ")")) + ")");
        return partes.Count == 0 ? null : string.Join(" and ", partes);
    }

    private static string Condicao(Ctx c, CondicaoFiltro f)
    {
        var col = c.Coluna(f.Coluna);
        var q = c.Q(f.Coluna);
        switch (f.Operador)
        {
            case OperadorFiltro.Nulo: return f.Valor is true ? $"{q} is null" : $"{q} is not null";
            case OperadorFiltro.Em:
                var itens = (f.Valor as System.Collections.IEnumerable)?.Cast<object?>().Where(v => v is not null).Take(MaxEmItens).ToList() ?? [];
                return itens.Count == 0 ? "1 = 0" : $"{q} in ({string.Join(", ", itens.Select(v => c.P(v, col.TipoSql)))})";
            case OperadorFiltro.Contem: return $"{q} like {c.P("%" + EscaparLike(f.Valor?.ToString() ?? "") + "%", col.TipoSql)}";
            case OperadorFiltro.ComecaCom: return $"{q} like {c.P(EscaparLike(f.Valor?.ToString() ?? "") + "%", col.TipoSql)}";
        }
        var op = f.Operador switch
        {
            OperadorFiltro.Igual => "=", OperadorFiltro.Diferente => "<>", OperadorFiltro.Maior => ">", OperadorFiltro.MaiorOuIgual => ">=",
            OperadorFiltro.Menor => "<", _ => "<=",
        };
        // Comparar com nulo nunca é verdadeiro em SQL: igual/diferente com valor nulo viram IS [NOT] NULL.
        if (f.Valor is null) return op switch { "=" => $"{q} is null", "<>" => $"{q} is not null", _ => "1 = 0" };
        return $"{q} {op} {c.P(f.Valor, col.TipoSql)}";
    }

    /// <summary>Texto literal dentro de LIKE: %, _ e [ perdem o significado especial.</summary>
    internal static string EscaparLike(string s) => s.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}

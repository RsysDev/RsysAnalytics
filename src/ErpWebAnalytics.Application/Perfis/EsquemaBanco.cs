using System.Text.RegularExpressions;

namespace ErpWebAnalytics.Application.Perfis;

/// <summary>Tipos que o GraphQL dinâmico sabe expor. Colunas de outros tipos (binários, xml, geography...) não são oferecidas.</summary>
public enum TipoCampo { Texto, Inteiro, Longo, Decimal, Booleano, DataHora, Data, Guid, NaoSuportado }

public sealed record ColunaBanco(string Nome, string TipoSql, TipoCampo Tipo, bool Nulavel, bool Chave, bool Identidade, bool Sensivel);

public sealed record ForeignKeyBanco(
    string Nome, string SchemaAlvo, string TabelaAlvo, IReadOnlyList<ParColunas> Colunas);

public sealed record TabelaBancoResumo(string Schema, string Nome, bool EhView, int Colunas)
{
    public string Chave => $"{Schema}.{Nome}";
}

/// <summary>Estrutura de uma tabela, lida do próprio banco. <c>Referenciadas</c>: chaves de outras tabelas que apontam para esta.</summary>
public sealed record TabelaBanco(
    string Schema, string Nome, bool EhView, IReadOnlyList<ColunaBanco> Colunas,
    IReadOnlyList<ForeignKeyBanco> ForeignKeys, IReadOnlyList<ForeignKeyBanco> Referenciadas)
{
    public string Chave => $"{Schema}.{Nome}";
}

/// <summary>Lê o esquema do banco (SQL Server) sem entities: o mesmo código serve para qualquer um dos sistemas.</summary>
public interface IEsquemaBancoLeitor
{
    Task<IReadOnlyList<TabelaBancoResumo>> ListarAsync(string? busca, CancellationToken ct);

    /// <summary>Null se a tabela/view não existir. <paramref name="chave"/>: <c>schema.nome</c> (sem schema = dbo).</summary>
    Task<TabelaBanco?> ObterAsync(string chave, CancellationToken ct);
}

public static class TiposBanco
{
    /// <summary>Nomes de coluna que costumam guardar dado pessoal, segredo ou custo: começam ocultos na tela.</summary>
    private static readonly Regex NomeSensivel = new(
        @"cpf|cnpj|senha|password|passwd|token|secret|segredo|custo|margem|comiss|salario|cartao|cvv|chave(acesso|api)|pin$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool PareceSensivel(string coluna) => NomeSensivel.IsMatch(coluna);

    public static TipoCampo Mapear(string tipoSql) => tipoSql.ToLowerInvariant() switch
    {
        "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" or "time" => TipoCampo.Texto,
        "tinyint" or "smallint" or "int" => TipoCampo.Inteiro,
        "bigint" => TipoCampo.Longo,
        "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => TipoCampo.Decimal,
        "bit" => TipoCampo.Booleano,
        "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => TipoCampo.DataHora,
        "date" => TipoCampo.Data,
        "uniqueidentifier" => TipoCampo.Guid,
        _ => TipoCampo.NaoSuportado,
    };

    /// <summary>"dbo.tbEntidade" ou "tbEntidade" -> (schema, nome). Retorna null se o formato for inválido.</summary>
    public static (string Schema, string Nome)? Separar(string chave)
    {
        var partes = chave.Trim().Split('.');
        if (partes.Length is < 1 or > 2 || partes.Any(p => !Regex.IsMatch(p, @"^\w+$"))) return null;
        return partes.Length == 1 ? ("dbo", partes[0]) : (partes[0], partes[1]);
    }
}

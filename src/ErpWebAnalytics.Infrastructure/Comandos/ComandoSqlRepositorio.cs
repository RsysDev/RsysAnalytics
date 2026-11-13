using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpWebAnalytics.Infrastructure.Comandos;

/// <summary>
/// Manutenção de Analytics.ComandoSQL (a única escrita da API). Tudo é parametrizado e restrito à ChaveEmpresa informada.
/// Requer um login do banco com permissão de escrita nessa tabela.
/// </summary>
internal sealed class ComandoSqlRepositorio(AppDbContext db) : IComandoSqlRepositorio
{
    private static readonly JsonSerializerOptions JsonLeitura = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    // Gravação legível: enums em camelCase ("maiorIgual", "date"), sem campos nulos.
    private static readonly JsonSerializerOptions JsonGravacao = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public async Task<IReadOnlyList<ComandoSql>> ListarTodosAsync(string chaveEmpresa, CancellationToken ct)
    {
        var linhas = await db.Database.SqlQueryRaw<LinhaEdicao>("""
            select cast(Id as nvarchar(100)) as Id,
                   cast(Descricao as nvarchar(max)) as Descricao,
                   cast(Parametros as nvarchar(max)) as Parametros,
                   cast(Regras as nvarchar(max)) as Regras,
                   cast([SQL] as nvarchar(max)) as Sql,
                   cast(Ativa as bit) as Ativa,
                   cast(MaxLinhas as int) as MaxLinhas,
                   cast(TimeoutSeg as int) as TimeoutSeg
            from Analytics.ComandoSQL
            where ChaveEmpresa = @chave
            order by Id
            """, new SqlParameter("@chave", chaveEmpresa)).ToListAsync(ct);

        return linhas.Select(Converter).ToList();
    }

    public async Task SalvarAsync(string chaveEmpresa, ComandoSql c, bool criar, CancellationToken ct)
    {
        var parametros = c.Parametros.Count == 0 ? null : JsonSerializer.Serialize(c.Parametros, JsonGravacao);
        var regras = c.Regras.Count == 0 ? null : JsonSerializer.Serialize(c.Regras, JsonGravacao);
        var passos = JsonSerializer.Serialize(
            c.Passos.Select(p => new PassoSql { Ordem = p.Ordem, Nome = string.IsNullOrWhiteSpace(p.Nome) ? null : p.Nome.Trim(), Sql = p.Sql, SomenteExecucao = p.SomenteExecucao }),
            JsonGravacao);

        var sql = criar
            ? """
              insert Analytics.ComandoSQL (ChaveEmpresa, Id, Descricao, Parametros, Regras, [SQL], Ativa, MaxLinhas, TimeoutSeg)
              values (@chave, @id, @descricao, @parametros, @regras, @sql, @ativa, @max, @timeout)
              """
            : """
              update Analytics.ComandoSQL
                 set Descricao = @descricao, Parametros = @parametros, Regras = @regras, [SQL] = @sql,
                     Ativa = @ativa, MaxLinhas = @max, TimeoutSeg = @timeout
               where ChaveEmpresa = @chave and Id = @id
              """;

        int afetadas;
        try
        {
            afetadas = await db.Database.ExecuteSqlRawAsync(sql, [
                new SqlParameter("@chave", SqlDbType.VarChar, 50) { Value = chaveEmpresa },
                new SqlParameter("@id", SqlDbType.VarChar, 100) { Value = c.Id },
                new SqlParameter("@descricao", SqlDbType.NVarChar, 1000) { Value = c.Descricao },
                new SqlParameter("@parametros", SqlDbType.NVarChar, -1) { Value = (object?)parametros ?? DBNull.Value },
                new SqlParameter("@regras", SqlDbType.NVarChar, -1) { Value = (object?)regras ?? DBNull.Value },
                new SqlParameter("@sql", SqlDbType.NVarChar, -1) { Value = passos },
                new SqlParameter("@ativa", SqlDbType.Bit) { Value = c.Ativa },
                new SqlParameter("@max", SqlDbType.Int) { Value = c.MaxLinhas },
                new SqlParameter("@timeout", SqlDbType.Int) { Value = c.TimeoutSeg },
            ], ct);
        }
        catch (SqlException e) when (e.Number is 2627 or 2601)
        {
            throw new ComandoInvalidoException($"Já existe um comando com o id '{c.Id}'.");
        }

        if (afetadas == 0) throw new ComandoInvalidoException($"Comando '{c.Id}' não encontrado (foi excluído por outra pessoa?).");
    }

    public async Task<bool> ExcluirAsync(string chaveEmpresa, string id, CancellationToken ct) =>
        await db.Database.ExecuteSqlRawAsync(
            "delete Analytics.ComandoSQL where ChaveEmpresa = @chave and Id = @id",
            [new SqlParameter("@chave", SqlDbType.VarChar, 50) { Value = chaveEmpresa }, new SqlParameter("@id", SqlDbType.VarChar, 100) { Value = id }],
            ct) > 0;

    private static ComandoSql Converter(LinhaEdicao l)
    {
        // JSON ilegível não derruba a lista: o comando vem vazio, com o motivo em Erro, para ser corrigido na tela.
        try
        {
            return new ComandoSql
            {
                Id = l.Id,
                Descricao = l.Descricao,
                Ativa = l.Ativa,
                MaxLinhas = l.MaxLinhas,
                TimeoutSeg = l.TimeoutSeg,
                Parametros = Ler<List<ParametroComando>>(l.Parametros) ?? [],
                Regras = Ler<List<RegraComando>>(l.Regras) ?? [],
                Passos = ComandoSqlCatalogo.LerPassosBrutos(l.Id, l.Sql),
            };
        }
        catch (Exception e) when (e is JsonException or ComandoInvalidoException)
        {
            return new ComandoSql
            {
                Id = l.Id, Descricao = l.Descricao, Ativa = l.Ativa, MaxLinhas = l.MaxLinhas, TimeoutSeg = l.TimeoutSeg,
                Passos = [new PassoSql { Ordem = 1, Sql = l.Sql }],
                Erro = e.Message,
            };
        }
    }

    private static T? Ler<T>(string? json) => string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonLeitura);
}

internal sealed class LinhaEdicao
{
    public string Id { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string? Parametros { get; set; }
    public string? Regras { get; set; }
    public string Sql { get; set; } = "";
    public bool Ativa { get; set; }
    public int MaxLinhas { get; set; }
    public int TimeoutSeg { get; set; }
}

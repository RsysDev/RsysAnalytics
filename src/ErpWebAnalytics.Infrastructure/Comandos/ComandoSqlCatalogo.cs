using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ErpWebAnalytics.Infrastructure.Comandos;

/// <summary>Lê Analytics.ComandoSQL. Sem cache: uma edição no catálogo vale na chamada seguinte.</summary>
internal sealed class ComandoSqlCatalogo(AppDbContext db, ILogger<ComandoSqlCatalogo> logger) : IComandoSqlCatalogo
{
    // CAST explícito: o EF exige que o tipo da coluna bata com a propriedade (ver CLAUDE.md).
    private const string Select = """
        select cast(Id as nvarchar(100)) as Id,
               cast(Descricao as nvarchar(max)) as Descricao,
               cast(Parametros as nvarchar(max)) as Parametros,
               cast(Regras as nvarchar(max)) as Regras,
               cast([SQL] as nvarchar(max)) as Sql,
               cast(MaxLinhas as int) as MaxLinhas,
               cast(TimeoutSeg as int) as TimeoutSeg
        from Analytics.ComandoSQL
        where ChaveEmpresa = @chave and Ativa = 1
        """;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public async Task<IReadOnlyList<ComandoSql>> ListarAsync(string chaveEmpresa, CancellationToken ct)
    {
        var linhas = await db.Database
            .SqlQueryRaw<ComandoSqlLinha>(Select + " order by Id", new SqlParameter("@chave", chaveEmpresa))
            .ToListAsync(ct);

        var resultado = new List<ComandoSql>();
        foreach (var l in linhas)
        {
            try { resultado.Add(Converter(l)); }
            catch (ComandoInvalidoException e) { logger.LogWarning("Comando ignorado na listagem: {Mensagem}", e.Message); }
        }
        return resultado;
    }

    public async Task<ComandoSql?> ObterAsync(string chaveEmpresa, string id, CancellationToken ct)
    {
        var linha = await db.Database
            .SqlQueryRaw<ComandoSqlLinha>(Select + " and Id = @id", new SqlParameter("@chave", chaveEmpresa), new SqlParameter("@id", id))
            .ToListAsync(ct);
        return linha.Count == 0 ? null : Converter(linha[0]);
    }

    private static ComandoSql Converter(ComandoSqlLinha l)
    {
        try
        {
            return new ComandoSql
            {
                Id = l.Id,
                Descricao = l.Descricao,
                Passos = LerPassos(l.Id, l.Sql),
                MaxLinhas = l.MaxLinhas,
                TimeoutSeg = l.TimeoutSeg,
                Parametros = Ler<List<ParametroComando>>(l.Parametros) ?? [],
                Regras = Ler<List<RegraComando>>(l.Regras) ?? [],
            };
        }
        catch (JsonException e)
        {
            throw new ComandoInvalidoException($"O comando '{l.Id}' tem JSON inválido em Parametros/Regras: {e.Message}");
        }
    }

    // Coluna SQL: lista JSON de passos ([{ordem, nome, sql, somenteExecucao}]) ou, por compatibilidade, um único SELECT em texto.
    private static readonly Regex ComecaComoLista = new(@"^\s*\[\s*\{", RegexOptions.Compiled);

    internal static List<PassoSql> LerPassosBrutos(string id, string texto)
    {
        if (!ComecaComoLista.IsMatch(texto)) return [new PassoSql { Ordem = 1, Sql = texto }];
        try { return JsonSerializer.Deserialize<List<PassoSql>>(texto, Json) ?? []; }
        catch (JsonException e) { throw new ComandoInvalidoException($"O comando '{id}' tem JSON inválido em SQL: {e.Message}"); }
    }

    internal static List<PassoSql> LerPassos(string id, string texto) =>
        ValidadorComando.NormalizarPassos(id, LerPassosBrutos(id, texto));

    private static T? Ler<T>(string? json) => string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, Json);
}

internal sealed class ComandoSqlLinha
{
    public string Id { get; set; } = "";
    public string Descricao { get; set; } = "";
    public string? Parametros { get; set; }
    public string? Regras { get; set; }
    public string Sql { get; set; } = "";
    public int MaxLinhas { get; set; }
    public int TimeoutSeg { get; set; }
}

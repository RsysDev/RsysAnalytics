using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Application.Perfis;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpWebAnalytics.Infrastructure.Perfis;

/// <summary>Lê e grava o perfil em Analytics.PerfilGraphQL (uma linha por empresa e sistema, JSON na coluna Perfil).</summary>
internal sealed class PerfilRepositorio(AppDbContext db) : IPerfilRepositorio
{
    private static readonly JsonSerializerOptions Leitura = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions Gravacao = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public async Task<PerfilGraphQL?> ObterAsync(string chaveEmpresa, string sistema, CancellationToken ct)
    {
        try
        {
            var json = await db.Database.SqlQueryRaw<LinhaPerfil>(
                    "select cast(Perfil as nvarchar(max)) as Perfil from Analytics.PerfilGraphQL where ChaveEmpresa = @chave and Sistema = @sistema",
                    new SqlParameter("@chave", chaveEmpresa), new SqlParameter("@sistema", sistema))
                .ToListAsync(ct);
            if (json.Count == 0) return null;
            return JsonSerializer.Deserialize<PerfilGraphQL>(json[0].Perfil, Leitura);
        }
        catch (SqlException e) when (e.Number == 208) // objeto inexistente
        {
            throw new ComandoInvalidoException("A tabela Analytics.PerfilGraphQL não existe neste banco. Rode Infrastructure/Perfis/Analytics.PerfilGraphQL.sql.");
        }
        catch (JsonException e)
        {
            throw new ComandoInvalidoException($"O perfil gravado para '{sistema}' tem JSON inválido: {e.Message}");
        }
    }

    public async Task SalvarAsync(string chaveEmpresa, PerfilGraphQL perfil, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(perfil, Gravacao);
        SqlParameter[] Parametros() =>
        [
            new("@chave", SqlDbType.VarChar, 50) { Value = chaveEmpresa },
            new("@sistema", SqlDbType.VarChar, 50) { Value = perfil.Sistema },
            new("@perfil", SqlDbType.NVarChar, -1) { Value = json },
        ];

        try
        {
            var afetadas = await db.Database.ExecuteSqlRawAsync(
                "update Analytics.PerfilGraphQL set Perfil = @perfil, AtualizadoEm = sysdatetime() where ChaveEmpresa = @chave and Sistema = @sistema",
                Parametros(), ct);
            if (afetadas == 0)
                await db.Database.ExecuteSqlRawAsync(
                    "insert Analytics.PerfilGraphQL (ChaveEmpresa, Sistema, Perfil) values (@chave, @sistema, @perfil)", Parametros(), ct);
        }
        catch (SqlException e) when (e.Number == 208)
        {
            throw new ComandoInvalidoException("A tabela Analytics.PerfilGraphQL não existe neste banco. Rode Infrastructure/Perfis/Analytics.PerfilGraphQL.sql.");
        }
    }
}

internal sealed class LinhaPerfil
{
    public string Perfil { get; set; } = "";
}

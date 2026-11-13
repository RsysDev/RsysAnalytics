using System.Data;
using System.Text.RegularExpressions;
using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Application.Comandos.Legado;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ErpWebAnalytics.Infrastructure.Comandos;

/// <summary>
/// Lê as tabelas de relatórios do ERP legado por ADO.NET, sem entities: o outro banco pode ter tabelas diferentes, então
/// cada tabela só é oferecida se existir e tiver as colunas esperadas. As tabelas vêm de <c>Importacao:Tabelas</c>
/// (padrão: fwRelatorio e fwRelatorioDinamico) e só esses nomes são aceitos, nunca texto vindo do chamador.
/// </summary>
internal sealed class LegadoRelatorioLeitor(AppDbContext db, IConfiguration configuracao) : ILegadoRelatorioLeitor
{
    private static readonly string[] Padrao = ["fwRelatorio", "fwRelatorioDinamico"];
    private static readonly string[] Obrigatorias = ["IdRelatorio", "Titulo", "XmlParametros", "XmlRegras", "XmlComandos"];
    private static readonly Regex NomeValido = new(@"^\w+(\.\w+)?$", RegexOptions.Compiled);

    private IEnumerable<string> Configuradas()
    {
        var lista = configuracao.GetSection("Importacao:Tabelas").GetChildren().Select(c => c.Value).OfType<string>().ToArray();
        return (lista.Length > 0 ? lista : Padrao).Where(t => NomeValido.IsMatch(t));
    }

    private static string Citar(string nome) => string.Join('.', nome.Split('.').Select(p => $"[{p}]"));

    private async Task<System.Data.Common.DbConnection> ConexaoAsync(CancellationToken ct)
    {
        var c = db.Database.GetDbConnection();
        if (c.State != ConnectionState.Open) await c.OpenAsync(ct);
        return c;
    }

    public async Task<IReadOnlyList<TabelaLegado>> TabelasDisponiveisAsync(CancellationToken ct)
    {
        var conexao = await ConexaoAsync(ct);
        var achadas = new List<TabelaLegado>();
        foreach (var nome in Configuradas())
        {
            await using var cmd = (SqlCommand)conexao.CreateCommand();
            cmd.CommandText = """
                select c.name from sys.columns c where c.object_id = object_id(@tabela) and c.name in ('IdRelatorio','Titulo','XmlParametros','XmlRegras','XmlComandos')
                """;
            cmd.Parameters.Add(new SqlParameter("@tabela", SqlDbType.NVarChar, 300) { Value = Citar(nome) });
            var colunas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var r = await cmd.ExecuteReaderAsync(ct))
                while (await r.ReadAsync(ct)) colunas.Add(r.GetString(0));
            if (!Obrigatorias.All(colunas.Contains)) continue;

            await using var conta = (SqlCommand)conexao.CreateCommand();
            conta.CommandText = $"select count(*) from {Citar(nome)}";
            achadas.Add(new TabelaLegado(nome, Convert.ToInt32(await conta.ExecuteScalarAsync(ct))));
        }
        return achadas;
    }

    private async Task<string> TabelaAsync(string pedida, CancellationToken ct)
    {
        var disponiveis = await TabelasDisponiveisAsync(ct);
        return disponiveis.FirstOrDefault(t => t.Nome.Equals(pedida, StringComparison.OrdinalIgnoreCase))?.Nome
            ?? throw new ComandoInvalidoException($"A tabela '{pedida}' não está disponível para importação neste banco.");
    }

    public async Task<IReadOnlyList<RelatorioLegadoResumo>> ListarAsync(string tabela, CancellationToken ct)
    {
        var nome = await TabelaAsync(tabela, ct);
        var conexao = await ConexaoAsync(ct);
        var tem = await TemColunaAsync(conexao, nome, "Tag", ct);
        await using var cmd = (SqlCommand)conexao.CreateCommand();
        cmd.CommandText = $"select IdRelatorio, Titulo, {(tem ? "Tag" : "cast(null as varchar(100))")} from {Citar(nome)} order by IdRelatorio";
        var lista = new List<RelatorioLegadoResumo>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
            lista.Add(new RelatorioLegadoResumo(r.GetString(0).Trim(), r.IsDBNull(1) ? "" : r.GetString(1).Trim(), r.IsDBNull(2) ? null : r.GetString(2).Trim()));
        return lista;
    }

    public async Task<RelatorioLegado?> ObterAsync(string tabela, string idRelatorio, CancellationToken ct)
    {
        var nome = await TabelaAsync(tabela, ct);
        var conexao = await ConexaoAsync(ct);
        await using var cmd = (SqlCommand)conexao.CreateCommand();
        cmd.CommandText = $"select IdRelatorio, Titulo, XmlParametros, XmlRegras, XmlComandos from {Citar(nome)} where IdRelatorio = @id";
        cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.VarChar, 100) { Value = idRelatorio });
        await using var r = await cmd.ExecuteReaderAsync(ct);
        if (!await r.ReadAsync(ct)) return null;
        string? S(int i) => r.IsDBNull(i) ? null : r.GetString(i);
        return new RelatorioLegado(nome, r.GetString(0).Trim(), S(1), S(2), S(3), S(4));
    }

    private static async Task<bool> TemColunaAsync(System.Data.Common.DbConnection conexao, string tabela, string coluna, CancellationToken ct)
    {
        await using var cmd = (SqlCommand)conexao.CreateCommand();
        cmd.CommandText = "select count(*) from sys.columns where object_id = object_id(@t) and name = @c";
        cmd.Parameters.Add(new SqlParameter("@t", SqlDbType.NVarChar, 300) { Value = Citar(tabela) });
        cmd.Parameters.Add(new SqlParameter("@c", SqlDbType.NVarChar, 128) { Value = coluna });
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) > 0;
    }
}

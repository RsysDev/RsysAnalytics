using System.Data;
using System.Reflection;
using ErpWebAnalytics.Application.Pedidos;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpWebAnalytics.Infrastructure.Pedidos;

/// <summary>
/// Executa as consultas de pedidos em SQL parametrizado (arquivos .sql embutidos). O SQL é mantido em T-SQL porque
/// depende de views e funções do ERP (viewPedidosComDevolucao, dbo.CalcularMargemLucroPedidoNF, ...).
/// </summary>
internal sealed class PedidoQueries(AppDbContext db) : IPedidoQueries
{
    private const int CommandTimeoutSeconds = 120;

    public async Task<IReadOnlyList<PedidoResumo>> ObterRelacaoPedidosAsync(FiltroPedidos filtro, CancellationToken ct)
    {
        var (sql, parameters) = Montar("RelacaoPedidos.sql", filtro);
        db.Database.SetCommandTimeout(CommandTimeoutSeconds);
        return await db.Database.SqlQueryRaw<PedidoResumo>(sql, parameters).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ItemPedido>> ObterItensPedidosAsync(FiltroPedidos filtro, CancellationToken ct)
    {
        var (sql, parameters) = Montar("ItensPedidos.sql", filtro);
        db.Database.SetCommandTimeout(CommandTimeoutSeconds);
        return await db.Database.SqlQueryRaw<ItemPedido>(sql, parameters).ToListAsync(ct);
    }

    private static (string Sql, object[] Parameters) Montar(string arquivo, FiltroPedidos filtro)
    {
        if (filtro.IdsEmpresa is not { Count: > 0 })
            throw new ArgumentException("Informe ao menos uma empresa em idsEmpresa.", nameof(filtro));

        // Listas de inteiros são inseridas como literais (int.ToString é seguro contra injeção) para manter o plano de execução do IN.
        var sql = LerSql(arquivo)
            .Replace("{{EMPRESAS}}", Lista(filtro.IdsEmpresa))
            .Replace("{{TIPOS_MOVIMENTO}}", Lista(filtro.TiposMovimento is { Count: > 0 } t ? t : [0]));

        object[] parameters =
        [
            new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = filtro.IdUsuario },
            new SqlParameter("@IdEmpresaChave", SqlDbType.Int) { Value = filtro.IdsEmpresa[0] },
            new SqlParameter("@TipoPedido", SqlDbType.Int) { Value = filtro.TipoPedido ?? 0 },
            new SqlParameter("@FlagPendencia", SqlDbType.Int) { Value = filtro.FlagPendencia ?? -1 },
            new SqlParameter("@QuebraVendedor", SqlDbType.Int) { Value = (int)(filtro.QuebraVendedor ?? QuebraVendedor.Interno) },
            new SqlParameter("@IncluirDevolucoes", SqlDbType.Bit) { Value = filtro.IncluirDevolucoes ?? true },
            new SqlParameter("@DataInicio", SqlDbType.DateTime) { Value = filtro.DataInicio is { } di ? di.ToDateTime(TimeOnly.MinValue) : DBNull.Value },
            new SqlParameter("@DataFim", SqlDbType.DateTime) { Value = filtro.DataFim is { } df ? df.ToDateTime(TimeOnly.MinValue) : DBNull.Value },
        ];

        return (sql, parameters);
    }

    private static string Lista(IEnumerable<int> valores) =>
        string.Join(",", valores.Distinct().Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    private static string LerSql(string arquivo)
    {
        var assembly = typeof(PedidoQueries).Assembly;
        var nome = assembly.GetManifestResourceNames().Single(n => n.EndsWith("." + arquivo, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(nome)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

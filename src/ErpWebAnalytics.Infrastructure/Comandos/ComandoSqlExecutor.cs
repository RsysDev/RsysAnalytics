using System.Data;
using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpWebAnalytics.Infrastructure.Comandos;

/// <summary>
/// Executa o SQL gerado por ADO.NET (as colunas só são conhecidas em tempo de execução, então não há DTO nem EF).
/// Devolve o primeiro conjunto de resultados, cortado em maxLinhas.
/// </summary>
internal sealed class ComandoSqlExecutor(AppDbContext db) : IComandoSqlExecutor
{
    public async Task<ResultadoComando> ExecutarAsync(
        IReadOnlyList<PassoGerado> passos, int timeoutSeg, int maxLinhas, ModoTransacao transacao, CancellationToken ct)
    {
        var conexao = db.Database.GetDbConnection();
        if (conexao.State != ConnectionState.Open) await conexao.OpenAsync(ct);

        await using var tx = transacao != ModoTransacao.Nenhuma ? (SqlTransaction)await conexao.BeginTransactionAsync(ct) : null;
        var resultados = new List<ResultadoConsulta>();
        try
        {
            foreach (var passo in passos)
            {
                await using var cmd = (SqlCommand)conexao.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = passo.Sql.Texto;
                cmd.CommandTimeout = timeoutSeg;
                foreach (var p in passo.Sql.Parametros) cmd.Parameters.Add(Converter(p));

                if (passo.SomenteExecucao) await cmd.ExecuteNonQueryAsync(ct);
                else resultados.Add(await LerAsync(cmd, passo.Nome ?? "resultado", maxLinhas, ct));
            }
            if (tx != null)
            {
                if (transacao == ModoTransacao.Reverter) await tx.RollbackAsync(ct);
                else await tx.CommitAsync(ct);
            }
        }
        catch
        {
            if (tx != null) await tx.RollbackAsync(CancellationToken.None);
            throw;
        }
        return new ResultadoComando(resultados);
    }

    private static async Task<ResultadoConsulta> LerAsync(SqlCommand cmd, string nome, int maxLinhas, CancellationToken ct)
    {
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var colunas = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        var linhas = new List<object?[]>();
        var truncado = false;
        while (await reader.ReadAsync(ct))
        {
            if (linhas.Count >= maxLinhas) { truncado = true; cmd.Cancel(); break; }
            var valores = new object?[colunas.Count];
            for (var i = 0; i < valores.Length; i++)
                valores[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            linhas.Add(valores);
        }
        return new ResultadoConsulta(nome, colunas, linhas, truncado);
    }

    private static SqlParameter Converter(ParametroSql p) => p.Tipo switch
    {
        // varchar (não nvarchar): as colunas do ERP são varchar e nvarchar impediria o uso de índices.
        TipoParametro.String => new SqlParameter(p.Nome, SqlDbType.VarChar) { Value = p.Valor },
        TipoParametro.Inteiro => new SqlParameter(p.Nome, SqlDbType.BigInt) { Value = p.Valor },
        TipoParametro.Decimal => new SqlParameter(p.Nome, SqlDbType.Decimal) { Value = p.Valor, Precision = 38, Scale = EscalaDe((decimal)p.Valor) },
        TipoParametro.Date or TipoParametro.DateTime => new SqlParameter(p.Nome, SqlDbType.DateTime) { Value = p.Valor },
        TipoParametro.Checkbox => new SqlParameter(p.Nome, SqlDbType.Bit) { Value = p.Valor },
        _ => throw new ArgumentOutOfRangeException(nameof(p)),
    };

    private static byte EscalaDe(decimal d) => (byte)((decimal.GetBits(d)[3] >> 16) & 0x7F);
}

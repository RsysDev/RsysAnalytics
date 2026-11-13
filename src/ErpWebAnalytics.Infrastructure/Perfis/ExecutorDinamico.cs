using System.Data;
using System.Globalization;
using ErpWebAnalytics.Application.Perfis;
using ErpWebAnalytics.Application.Perfis.Dinamico;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpWebAnalytics.Infrastructure.Perfis;

/// <summary>
/// Executa as consultas do GraphQL dinâmico por ADO.NET. Cada chamada usa uma conexão própria (do pool de contextos), porque os
/// resolvers do GraphQL rodam em paralelo. Valores voltam normalizados para os tipos que os escalares do GraphQL esperam.
/// </summary>
internal sealed class ExecutorDinamico(IDbContextFactory<AppDbContext> fabrica) : IExecutorDinamico
{
    private const int TimeoutSegundos = 30;

    public async Task<IReadOnlyList<Linha>> LerAsync(ConsultaGerada consulta, TabelaBanco tabela, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var conexao = await AbrirAsync(db, ct);
        return await LerAsync(conexao, null, consulta, tabela, ct);
    }

    public async Task<long> ContarAsync(ConsultaGerada consulta, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var conexao = await AbrirAsync(db, ct);
        await using var cmd = Comando(conexao, null, consulta);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<Linha>> AtualizarAsync(
        TabelaCompilada tabela, NoFiltro filtro, IReadOnlyList<(string Coluna, object? Valor)> valores, int limite, bool simular, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var conexao = await AbrirAsync(db, ct);
        await using var tx = (SqlTransaction)await conexao.BeginTransactionAsync(ct);
        try
        {
            // 1) Quais linhas o filtro atinge (uma a mais que o limite, só para saber se passou).
            var ordem = tabela.Chaves.Select(c => new OrdemColuna(c, false)).ToList();
            var alvo = await LerAsync(conexao, tx, ConstrutorConsulta.Listar(tabela.Banco, tabela.Chaves, filtro, ordem, 0, limite + 1), tabela.Banco, ct);
            if (alvo.Count == 0) { await tx.RollbackAsync(ct); return []; }
            if (alvo.Count > limite)
                throw new AtualizacaoRecusadaException($"O filtro atinge mais de {limite} linha(s). Refine o filtro ou aumente o limite (a atualização foi desfeita).");

            // 2) Atualiza sempre por chave primária, nunca pelo filtro do cliente.
            var chaves = alvo.Select(l => tabela.Chaves.Select(k => l[k]).ToArray()).ToList();
            await using (var up = Comando(conexao, tx, ConstrutorConsulta.Atualizar(tabela.Banco, valores, tabela.Chaves, chaves)))
                await up.ExecuteNonQueryAsync(ct);

            // 3) Relê as linhas já alteradas (dentro da mesma transação).
            var linhas = await LerAsync(conexao, tx, ConstrutorConsulta.PorChaves(tabela.Banco, tabela.Colunas, tabela.Chaves, chaves), tabela.Banco, ct);

            if (simular) await tx.RollbackAsync(ct); else await tx.CommitAsync(ct);
            return linhas;
        }
        catch
        {
            try { await tx.RollbackAsync(CancellationToken.None); } catch (InvalidOperationException) { /* transação já encerrada */ }
            throw;
        }
    }

    // ---------- apoio ----------

    private static async Task<SqlConnection> AbrirAsync(AppDbContext db, CancellationToken ct)
    {
        var conexao = (SqlConnection)db.Database.GetDbConnection();
        if (conexao.State != ConnectionState.Open) await conexao.OpenAsync(ct);
        return conexao;
    }

    private static SqlCommand Comando(SqlConnection conexao, SqlTransaction? tx, ConsultaGerada consulta)
    {
        var cmd = conexao.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = consulta.Sql;
        cmd.CommandTimeout = TimeoutSegundos;
        foreach (var p in consulta.Parametros) cmd.Parameters.Add(Parametro(p));
        return cmd;
    }

    private static async Task<IReadOnlyList<Linha>> LerAsync(SqlConnection conexao, SqlTransaction? tx, ConsultaGerada consulta, TabelaBanco tabela, CancellationToken ct)
    {
        await using var cmd = Comando(conexao, tx, consulta);
        var colunas = tabela.Colunas.ToDictionary(c => c.Nome, StringComparer.OrdinalIgnoreCase);

        var linhas = new List<Linha>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var linha = new Linha();
            for (var i = 0; i < r.FieldCount; i++)
            {
                var nome = r.GetName(i);
                if (nome == "__rn") continue;
                linha[nome] = r.IsDBNull(i) ? null : Normalizar(r.GetValue(i), colunas.GetValueOrDefault(nome));
            }
            linhas.Add(linha);
        }
        return linhas;
    }

    /// <summary>Parâmetro com o mesmo tipo da coluna (varchar vs nvarchar, int vs bigint...), para o SQL Server poder usar índices.</summary>
    private static SqlParameter Parametro(ParametroConsulta p)
    {
        var (tipo, clr) = p.TipoSql.ToLowerInvariant() switch
        {
            "char" or "varchar" or "text" => (SqlDbType.VarChar, typeof(string)),
            "nchar" or "nvarchar" or "ntext" => (SqlDbType.NVarChar, typeof(string)),
            "tinyint" => (SqlDbType.TinyInt, typeof(byte)),
            "smallint" => (SqlDbType.SmallInt, typeof(short)),
            "int" => (SqlDbType.Int, typeof(int)),
            "bigint" => (SqlDbType.BigInt, typeof(long)),
            "bit" => (SqlDbType.Bit, typeof(bool)),
            "decimal" or "numeric" => (SqlDbType.Decimal, typeof(decimal)),
            "money" => (SqlDbType.Money, typeof(decimal)),
            "smallmoney" => (SqlDbType.SmallMoney, typeof(decimal)),
            "float" => (SqlDbType.Float, typeof(double)),
            "real" => (SqlDbType.Real, typeof(float)),
            "datetime" => (SqlDbType.DateTime, typeof(DateTime)),
            "smalldatetime" => (SqlDbType.SmallDateTime, typeof(DateTime)),
            "datetime2" => (SqlDbType.DateTime2, typeof(DateTime)),
            "date" => (SqlDbType.Date, typeof(DateTime)),
            "datetimeoffset" => (SqlDbType.DateTimeOffset, typeof(DateTimeOffset)),
            "uniqueidentifier" => (SqlDbType.UniqueIdentifier, typeof(Guid)),
            _ => (SqlDbType.Variant, typeof(object)),
        };

        var parametro = new SqlParameter(p.Nome, tipo);
        parametro.Value = p.Valor is null ? DBNull.Value : Converter(p.Valor, clr);
        if (tipo == SqlDbType.Decimal && p.Valor is not null)
        {
            var d = Convert.ToDecimal(parametro.Value, CultureInfo.InvariantCulture);
            parametro.Precision = 38;
            parametro.Scale = (byte)((decimal.GetBits(d)[3] >> 16) & 0x7F);
        }
        return parametro;
    }

    private static object Converter(object valor, Type clr)
    {
        if (clr == typeof(object) || clr.IsInstanceOfType(valor)) return valor;
        if (valor is DateTimeOffset dto && clr == typeof(DateTime)) return dto.DateTime;
        if (valor is DateTime dt && clr == typeof(DateTimeOffset)) return new DateTimeOffset(dt);
        if (valor is string s && clr == typeof(Guid)) return Guid.Parse(s);
        return Convert.ChangeType(valor, clr, CultureInfo.InvariantCulture);
    }

    /// <summary>Tipos do SQL Server -> tipos que os escalares do GraphQL (Int, Long, Decimal, DateTime...) sabem serializar.</summary>
    private static object Normalizar(object valor, ColunaBanco? coluna) => coluna?.Tipo switch
    {
        TipoCampo.Inteiro => Convert.ToInt32(valor, CultureInfo.InvariantCulture),
        TipoCampo.Longo => Convert.ToInt64(valor, CultureInfo.InvariantCulture),
        TipoCampo.Decimal => Convert.ToDecimal(valor, CultureInfo.InvariantCulture),
        TipoCampo.Texto => TextoDe(valor, coluna),
        TipoCampo.DataHora or TipoCampo.Data => valor is DateTime dt ? DateTime.SpecifyKind(dt, DateTimeKind.Unspecified) : valor,
        _ => valor,
    };

    /// <summary>char/nchar vêm preenchidos com espaços até o tamanho da coluna: ruído para quem consome. varchar fica como está.</summary>
    private static string TextoDe(object valor, ColunaBanco? coluna)
    {
        var texto = valor as string ?? Convert.ToString(valor, CultureInfo.InvariantCulture) ?? "";
        return coluna?.TipoSql is "char" or "nchar" ? texto.TrimEnd() : texto;
    }
}

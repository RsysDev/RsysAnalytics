using System.Data;
using ErpWebAnalytics.Application.Perfis;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ErpWebAnalytics.Infrastructure.Perfis;

/// <summary>
/// Lê tabelas, views, colunas e chaves do SQL Server pelas views de catálogo (sys.*), sem entities.
/// O schema <c>Analytics</c> (catálogo desta API) e os objetos do sistema ficam de fora.
/// </summary>
internal sealed class EsquemaBancoLeitor(AppDbContext db) : IEsquemaBancoLeitor
{
    private async Task<SqlCommand> ComandoAsync(string sql, CancellationToken ct)
    {
        var c = db.Database.GetDbConnection();
        if (c.State != ConnectionState.Open) await c.OpenAsync(ct);
        var cmd = (SqlCommand)c.CreateCommand();
        cmd.CommandText = sql;
        return cmd;
    }

    public async Task<IReadOnlyList<TabelaBancoResumo>> ListarAsync(string? busca, CancellationToken ct)
    {
        await using var cmd = await ComandoAsync("""
            select s.name, o.name, cast(case o.type when 'V' then 1 else 0 end as bit),
                   (select count(*) from sys.columns c where c.object_id = o.object_id)
              from sys.objects o
              join sys.schemas s on s.schema_id = o.schema_id
             where o.type in ('U', 'V') and o.is_ms_shipped = 0
               and s.name not in ('Analytics', 'sys', 'INFORMATION_SCHEMA')
               and (@busca is null or o.name like '%' + @busca + '%' or s.name + '.' + o.name like '%' + @busca + '%')
             order by s.name, o.name
            """, ct);
        cmd.Parameters.Add(new SqlParameter("@busca", SqlDbType.NVarChar, 200) { Value = string.IsNullOrWhiteSpace(busca) ? DBNull.Value : busca.Trim() });

        var lista = new List<TabelaBancoResumo>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) lista.Add(new(r.GetString(0), r.GetString(1), r.GetBoolean(2), r.GetInt32(3)));
        return lista;
    }

    public async Task<TabelaBanco?> ObterAsync(string chave, CancellationToken ct)
    {
        if (TiposBanco.Separar(chave) is not { } id) return null;
        var nomeCompleto = $"[{id.Schema}].[{id.Nome}]";

        bool ehView;
        await using (var cmd = await ComandoAsync(
            "select cast(case type when 'V' then 1 else 0 end as bit) from sys.objects where object_id = object_id(@n) and type in ('U','V') and is_ms_shipped = 0", ct))
        {
            cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 300) { Value = nomeCompleto });
            var v = await cmd.ExecuteScalarAsync(ct);
            if (v is null or DBNull) return null;
            ehView = (bool)v;
        }

        var colunas = new List<ColunaBanco>();
        await using (var cmd = await ComandoAsync("""
            select c.name, t.name, c.is_nullable, c.is_identity,
                   cast(case when exists (select 1 from sys.index_columns ic
                                            join sys.indexes i on i.object_id = ic.object_id and i.index_id = ic.index_id
                                           where i.is_primary_key = 1 and ic.object_id = c.object_id and ic.column_id = c.column_id)
                             then 1 else 0 end as bit)
              from sys.columns c
              join sys.types t on t.user_type_id = c.system_type_id
             where c.object_id = object_id(@n)
             order by c.column_id
            """, ct))
        {
            cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 300) { Value = nomeCompleto });
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
            {
                var nome = r.GetString(0); var tipo = r.GetString(1);
                colunas.Add(new ColunaBanco(nome, tipo, TiposBanco.Mapear(tipo), r.GetBoolean(2), r.GetBoolean(4), r.GetBoolean(3), TiposBanco.PareceSensivel(nome)));
            }
        }

        var fks = await ChavesAsync(nomeCompleto, saindo: true, ct);
        var referenciadas = await ChavesAsync(nomeCompleto, saindo: false, ct);
        return new TabelaBanco(id.Schema, id.Nome, ehView, colunas, fks, referenciadas);
    }

    /// <summary>saindo=true: chaves desta tabela para outras; false: chaves de outras tabelas para esta (Origem/Destino já vêm do ponto de vista desta).</summary>
    private async Task<IReadOnlyList<ForeignKeyBanco>> ChavesAsync(string nomeCompleto, bool saindo, CancellationToken ct)
    {
        await using var cmd = await ComandoAsync($"""
            select fk.name, so.name, ss.name, o.name, pc.name, rc.name
              from sys.foreign_keys fk
              join sys.foreign_key_columns fkc on fkc.constraint_object_id = fk.object_id
              join sys.columns pc on pc.object_id = fkc.parent_object_id and pc.column_id = fkc.parent_column_id
              join sys.columns rc on rc.object_id = fkc.referenced_object_id and rc.column_id = fkc.referenced_column_id
              join sys.objects o on o.object_id = {(saindo ? "fk.referenced_object_id" : "fk.parent_object_id")}
              join sys.schemas ss on ss.schema_id = o.schema_id
              join sys.objects so on so.object_id = fk.object_id
             where {(saindo ? "fk.parent_object_id" : "fk.referenced_object_id")} = object_id(@n)
             order by fk.name, fkc.constraint_column_id
            """, ct);
        cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 300) { Value = nomeCompleto });

        var porNome = new Dictionary<string, (string Schema, string Tabela, List<ParColunas> Pares)>();
        await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var nome = r.GetString(0);
            if (!porNome.TryGetValue(nome, out var item)) porNome[nome] = item = (r.GetString(2), r.GetString(3), []);
            // saindo: Origem = coluna desta tabela (pai); entrando: Origem = coluna desta tabela (referenciada).
            item.Pares.Add(saindo
                ? new ParColunas { Origem = r.GetString(4), Destino = r.GetString(5) }
                : new ParColunas { Origem = r.GetString(5), Destino = r.GetString(4) });
        }
        return porNome.Select(kv => new ForeignKeyBanco(kv.Key, kv.Value.Schema, kv.Value.Tabela, kv.Value.Pares)).ToList();
    }
}

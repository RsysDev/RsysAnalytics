using System.Text.RegularExpressions;

namespace ErpWebAnalytics.Application.Comandos.Legado;

public interface IImportacaoLegadoService
{
    /// <summary>Tabelas de relatórios legados que existem neste banco. Vazio = a importação não é oferecida.</summary>
    Task<IReadOnlyList<TabelaLegado>> TabelasAsync(CancellationToken ct);

    Task<IReadOnlyList<RelatorioLegadoItem>> ListarAsync(string chaveEmpresa, string tabela, CancellationToken ct);

    /// <summary>Converte sem gravar: o resultado é um rascunho para a tela de manutenção.</summary>
    Task<ConversaoLegado> ConverterAsync(string tabela, string idRelatorio, CancellationToken ct);
}

public sealed class ImportacaoLegadoService(ILegadoRelatorioLeitor leitor, IComandoSqlRepositorio repositorio) : IImportacaoLegadoService
{
    public Task<IReadOnlyList<TabelaLegado>> TabelasAsync(CancellationToken ct) => leitor.TabelasDisponiveisAsync(ct);

    public async Task<IReadOnlyList<RelatorioLegadoItem>> ListarAsync(string chaveEmpresa, string tabela, CancellationToken ct)
    {
        var relatorios = await leitor.ListarAsync(tabela, ct);
        var existentes = (await repositorio.ListarTodosAsync(chaveEmpresa, ct)).Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return relatorios.Select(r =>
        {
            var id = IdDoComando(r.IdRelatorio);
            return new RelatorioLegadoItem(r.IdRelatorio, r.Titulo, r.Tag, id, existentes.Contains(id));
        }).ToList();
    }

    public async Task<ConversaoLegado> ConverterAsync(string tabela, string idRelatorio, CancellationToken ct)
    {
        var relatorio = await leitor.ObterAsync(tabela, idRelatorio, ct)
            ?? throw new ComandoInvalidoException($"Relatório '{idRelatorio}' não encontrado em {tabela}.");
        return ConversorRelatorioLegado.Converter(relatorio);
    }

    private static string IdDoComando(string idRelatorio)
    {
        var id = Regex.Replace(idRelatorio.Trim(), @"[^A-Za-z0-9_.\-]", "_");
        return id.Length > 100 ? id[..100] : id;
    }
}

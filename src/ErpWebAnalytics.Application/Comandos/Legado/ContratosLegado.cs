namespace ErpWebAnalytics.Application.Comandos.Legado;

/// <summary>Tabela de relatórios legados encontrada no banco (existe e tem as colunas esperadas).</summary>
public sealed record TabelaLegado(string Nome, int Total);

public sealed record RelatorioLegadoResumo(string IdRelatorio, string Titulo, string? Tag);

/// <summary>Linha crua da tabela legada: os três XMLs ainda não interpretados.</summary>
public sealed record RelatorioLegado(
    string Tabela, string IdRelatorio, string? Titulo, string? XmlParametros, string? XmlRegras, string? XmlComandos);

/// <summary><c>IdComando</c>: id que o comando teria no catálogo. <c>Existe</c>: já há um comando com esse id.</summary>
public sealed record RelatorioLegadoItem(string IdRelatorio, string Titulo, string? Tag, string IdComando, bool Existe);

/// <summary>Rascunho convertido (nada foi gravado) e o que o conversor não conseguiu traduzir sozinho.</summary>
public sealed record ConversaoLegado(ComandoSql Comando, IReadOnlyList<string> Avisos);

/// <summary>
/// Lê relatórios do ERP legado sem entities: o esquema do outro banco pode ser diferente, então a leitura é SQL puro
/// e só oferece tabelas que existem e têm as colunas esperadas.
/// </summary>
public interface ILegadoRelatorioLeitor
{
    Task<IReadOnlyList<TabelaLegado>> TabelasDisponiveisAsync(CancellationToken ct);

    /// <summary>Lança <see cref="ComandoInvalidoException"/> se a tabela não estiver entre as disponíveis.</summary>
    Task<IReadOnlyList<RelatorioLegadoResumo>> ListarAsync(string tabela, CancellationToken ct);

    Task<RelatorioLegado?> ObterAsync(string tabela, string idRelatorio, CancellationToken ct);
}

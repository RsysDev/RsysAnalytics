namespace ErpWebAnalytics.Application.Comandos;

public interface IComandoSqlAdminService
{
    Task<IReadOnlyList<ComandoSql>> ListarAsync(string chaveEmpresa, CancellationToken ct);
    Task SalvarAsync(string chaveEmpresa, ComandoSql comando, bool criar, CancellationToken ct);
    Task ExcluirAsync(string chaveEmpresa, string id, CancellationToken ct);
}

/// <summary>Manutenção do catálogo: valida antes de gravar, para que o MCP nunca encontre uma linha que não consegue ler.</summary>
public sealed class ComandoSqlAdminService(IComandoSqlRepositorio repositorio) : IComandoSqlAdminService
{
    public Task<IReadOnlyList<ComandoSql>> ListarAsync(string chaveEmpresa, CancellationToken ct) =>
        repositorio.ListarTodosAsync(chaveEmpresa, ct);

    public Task SalvarAsync(string chaveEmpresa, ComandoSql comando, bool criar, CancellationToken ct)
    {
        ValidadorComando.Validar(comando);
        return repositorio.SalvarAsync(chaveEmpresa, comando, criar, ct);
    }

    public async Task ExcluirAsync(string chaveEmpresa, string id, CancellationToken ct)
    {
        if (!await repositorio.ExcluirAsync(chaveEmpresa, id, ct))
            throw new ComandoInvalidoException($"Comando '{id}' não encontrado.");
    }
}

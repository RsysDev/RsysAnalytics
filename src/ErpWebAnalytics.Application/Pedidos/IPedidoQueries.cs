namespace ErpWebAnalytics.Application.Pedidos;

/// <summary>Consultas de leitura (lado Query do CQRS) sobre pedidos.</summary>
public interface IPedidoQueries
{
    Task<IReadOnlyList<PedidoResumo>> ObterRelacaoPedidosAsync(FiltroPedidos filtro, CancellationToken ct);

    Task<IReadOnlyList<ItemPedido>> ObterItensPedidosAsync(FiltroPedidos filtro, CancellationToken ct);
}

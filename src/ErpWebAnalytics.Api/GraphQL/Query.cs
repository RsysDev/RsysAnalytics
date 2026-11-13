using ErpWebAnalytics.Api.GraphQL.Types;
using ErpWebAnalytics.Application.Desempenho;
using ErpWebAnalytics.Application.Pedidos;
using ErpWebAnalytics.Domain.Entities;
using ErpWebAnalytics.Infrastructure.Persistence;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace ErpWebAnalytics.Api.GraphQL;

public class Query
{
    /// <summary>Verifica se a API GraphQL está no ar.</summary>
    public string Health() => "ok";

    // ---- Consultas navegáveis (IQueryable): o cliente escolhe campos, filtros, ordenação e página ----

    /// <summary>Pedidos. Peça só os campos necessários e sempre filtre por período (dataMovimento) e empresa.</summary>
    [UsePaging(DefaultPageSize = 50, MaxPageSize = 200, IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering(typeof(PedidoFilterType))]
    [UseSorting(typeof(PedidoSortType))]
    public IQueryable<FtPedido> Pedidos(AppDbContext db) => db.FtPedido.AsNoTracking();

    /// <summary>Entidades (clientes/fornecedores).</summary>
    [UsePaging(DefaultPageSize = 50, MaxPageSize = 200, IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering(typeof(EntidadeFilterType))]
    [UseSorting(typeof(EntidadeSortType))]
    public IQueryable<TbEntidade> Entidades(AppDbContext db) => db.TbEntidade.AsNoTracking();

    /// <summary>Vendedores.</summary>
    [UsePaging(DefaultPageSize = 50, MaxPageSize = 200, IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering(typeof(VendedorFilterType))]
    [UseSorting(typeof(VendedorSortType))]
    public IQueryable<FtVendedor> Vendedores(AppDbContext db) => db.FtVendedor.AsNoTracking();

    /// <summary>Produtos.</summary>
    [UsePaging(DefaultPageSize = 50, MaxPageSize = 200, IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering(typeof(ProdutoFilterType))]
    [UseSorting(typeof(ProdutoSortType))]
    public IQueryable<PrProduto> Produtos(AppDbContext db) => db.PrProduto.AsNoTracking();

    /// <summary>
    /// Painel mensal de vendas: totais da loja, ranking de vendedores internos (realizado, ticket médio, cupons,
    /// margem, devoluções), série diária e últimas vendas. Já vem agregado: prefira esta consulta a somar relacaoPedidos.
    /// Realizado = venda bruta - devoluções. Margem e lucro só aparecem se o idUsuario puder ver custo.
    /// </summary>
    public Task<DesempenhoMensal> DesempenhoMensal(
        int ano, int mes, List<int> idsEmpresa, int idUsuario,
        [Service] IDesempenhoService desempenho, CancellationToken ct) =>
        desempenho.ObterMensalAsync(ano, mes, idsEmpresa, idUsuario, ct);

    // ---- Relatórios consolidados em SQL (regras do ERP: devoluções, custo/margem/comissão por usuário) ----

    /// <summary>
    /// Relação de pedidos: um registro por pedido, mais as notas fiscais de devolução (valores negativos) quando
    /// incluirDevolucoes for true. Aplica a permissão de custo/margem/comissão do idUsuario.
    /// Informe sempre idsEmpresa, idUsuario e, de preferência, um período.
    /// </summary>
    public Task<IReadOnlyList<PedidoResumo>> RelacaoPedidos(
        FiltroPedidos filtro, [Service] IPedidoQueries queries, CancellationToken ct) =>
        queries.ObterRelacaoPedidosAsync(filtro, ct);

    /// <summary>
    /// Itens dos pedidos que atendem ao filtro (mesmos critérios de relacaoPedidos), com estoque, saldo a atender e margem.
    /// Relacione com relacaoPedidos por idPedido.
    /// </summary>
    public Task<IReadOnlyList<ItemPedido>> RelacaoItensPedidos(
        FiltroPedidos filtro, [Service] IPedidoQueries queries, CancellationToken ct) =>
        queries.ObterItensPedidosAsync(filtro, ct);
}

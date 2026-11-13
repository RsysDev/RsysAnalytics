using ErpWebAnalytics.Domain.Entities;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Sorting;

namespace ErpWebAnalytics.Api.GraphQL.Types;

// Filtros e ordenações explícitos: o cliente só pode filtrar/ordenar por campos que também são expostos no tipo.

public class PedidoFilterType : FilterInputType<FtPedido>
{
    protected override void Configure(IFilterInputTypeDescriptor<FtPedido> d)
    {
        d.Name("PedidoFilterInput");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdPedido);
        d.Field(x => x.IdEmpresa);
        d.Field(x => x.IdEntidade);
        d.Field(x => x.TipoPedido);
        d.Field(x => x.StatusPedido);
        d.Field(x => x.FlagPendencia);
        d.Field(x => x.DataMovimento);
        d.Field(x => x.DataFaturamento);
        d.Field(x => x.DataPrevisaoEntrega);
        d.Field(x => x.IdOperacao);
        d.Field(x => x.IdVendedorInterno);
        d.Field(x => x.IdVendedorExterno);
        d.Field(x => x.ValorTotalPedido);
        d.Field(x => x.NumPedCliente);
    }
}

public class PedidoSortType : SortInputType<FtPedido>
{
    protected override void Configure(ISortInputTypeDescriptor<FtPedido> d)
    {
        d.Name("PedidoSortInput");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdPedido);
        d.Field(x => x.DataMovimento);
        d.Field(x => x.DataFaturamento);
        d.Field(x => x.ValorTotalPedido);
        d.Field(x => x.IdEntidade);
    }
}

public class EntidadeFilterType : FilterInputType<TbEntidade>
{
    protected override void Configure(IFilterInputTypeDescriptor<TbEntidade> d)
    {
        d.Name("EntidadeFilterInput");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdEntidade);
        d.Field(x => x.RazaoSocial);
        d.Field(x => x.NomeFantasia);
        d.Field(x => x.IdCidade);
        d.Field(x => x.IdColigacao);
        d.Field(x => x.FlagInativa);
    }
}

public class EntidadeSortType : SortInputType<TbEntidade>
{
    protected override void Configure(ISortInputTypeDescriptor<TbEntidade> d)
    {
        d.Name("EntidadeSortInput");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdEntidade);
        d.Field(x => x.RazaoSocial);
        d.Field(x => x.DataCadastro);
    }
}

public class VendedorFilterType : FilterInputType<FtVendedor>
{
    protected override void Configure(IFilterInputTypeDescriptor<FtVendedor> d)
    {
        d.Name("VendedorFilterInput");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdVendedor);
        d.Field(x => x.Nome);
        d.Field(x => x.TipoVendedor);
        d.Field(x => x.FlagInativo);
    }
}

public class VendedorSortType : SortInputType<FtVendedor>
{
    protected override void Configure(ISortInputTypeDescriptor<FtVendedor> d)
    {
        d.Name("VendedorSortInput");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdVendedor);
        d.Field(x => x.Nome);
    }
}

public class ProdutoFilterType : FilterInputType<PrProduto>
{
    protected override void Configure(IFilterInputTypeDescriptor<PrProduto> d)
    {
        d.Name("ProdutoFilterInput");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdProduto);
        d.Field(x => x.Descricao);
        d.Field(x => x.IdMarca);
        d.Field(x => x.IdGrupo);
        d.Field(x => x.IdClassificacao);
        d.Field(x => x.NumFabricante);
        d.Field(x => x.NumOriginal);
        d.Field(x => x.IdFornecedor);
    }
}

public class ProdutoSortType : SortInputType<PrProduto>
{
    protected override void Configure(ISortInputTypeDescriptor<PrProduto> d)
    {
        d.Name("ProdutoSortInput");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdProduto);
        d.Field(x => x.Descricao);
    }
}

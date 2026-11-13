using ErpWebAnalytics.Domain.Entities;
using HotChocolate.Types;

namespace ErpWebAnalytics.Api.GraphQL.Types;

// Tipos GraphQL curados sobre as entities do ERP: expõem só campos escolhidos, com nomes claros.
// Campos de custo, margem e comissão ficam de fora até existir autenticação/autorização
// (use relacaoPedidos/relacaoItensPedidos, que aplicam a permissão do usuário).

public class PedidoType : ObjectType<FtPedido>
{
    protected override void Configure(IObjectTypeDescriptor<FtPedido> d)
    {
        d.Name("Pedido").Description("Pedido de venda, cotação ou pendência.");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdPedido);
        d.Field(x => x.IdEmpresa);
        d.Field(x => x.IdEntidade);
        d.Field(x => x.TipoPedido).Description("0 Pedido, 1 Cotação, 2 Pendência.");
        d.Field(x => x.StatusPedido).Description("0 Em Digitação, 1 Digitado, 2 Faturamento Parcial, 3 Faturado, 4 Cancelado.");
        d.Field(x => x.FlagPendencia);
        d.Field(x => x.DataCriacao);
        d.Field(x => x.DataMovimento);
        d.Field(x => x.DataPrevisaoEntrega);
        d.Field(x => x.DataFaturamento);
        d.Field(x => x.ValorProdutos);
        d.Field(x => x.ValorServicos);
        d.Field(x => x.ValorDescProdutos);
        d.Field(x => x.ValorDescServicos);
        d.Field(x => x.ValorFrete);
        d.Field(x => x.ValorSeguro);
        d.Field(x => x.ValorDespesas);
        d.Field(x => x.ValorOutrasDespesas);
        d.Field(x => x.ValorIpi);
        d.Field(x => x.ValorSubTrib);
        d.Field(x => x.ValorTotalPedido);
        d.Field(x => x.NumPedCliente);
        d.Field(x => x.IdVendedorInterno);
        d.Field(x => x.IdVendedorExterno);
        d.Field(x => x.Pmv).Name("pmv").Description("Prazo médio de venda.");
        d.Field(x => x.IdEntidadeNavigation).Name("entidade");
        d.Field(x => x.IdCondPagtoNavigation).Name("condicaoPagamento");
        d.Field(x => x.IdOperacaoNavigation).Name("operacao");
        d.Field(x => x.IdVendedorInternoNavigation).Name("vendedorInterno");
        d.Field(x => x.IdVendedorExternoNavigation).Name("vendedorExterno");
        d.Field(x => x.FtPedidoItem).Name("itens");
        d.Field(x => x.StatusPedidoDesc).Name("statusPedidoDesc");
    }
}

public class PedidoItemType : ObjectType<FtPedidoItem>
{
    protected override void Configure(IObjectTypeDescriptor<FtPedidoItem> d)
    {
        d.Name("PedidoItem").Description("Item (produto ou serviço) de um pedido.");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdPedidoItem);
        d.Field(x => x.IdPedido);
        d.Field(x => x.IdProduto);
        d.Field(x => x.IdServico);
        d.Field(x => x.Descricao);
        d.Field(x => x.NumFabricante);
        d.Field(x => x.QuantPedida);
        d.Field(x => x.QuantAtendida);
        d.Field(x => x.QuantFaturada);
        d.Field(x => x.QuantDevolvida);
        d.Field(x => x.ValorBruto);
        d.Field(x => x.PercDesconto);
        d.Field(x => x.ValorLiquido);
        d.Field(x => x.ValorTotal);
        d.Field(x => x.IdProdutoNavigation).Name("produto");
    }
}

public class EntidadeType : ObjectType<TbEntidade>
{
    protected override void Configure(IObjectTypeDescriptor<TbEntidade> d)
    {
        d.Name("Entidade").Description("Cliente, fornecedor ou outra entidade cadastrada.");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdEntidade);
        d.Field(x => x.RazaoSocial);
        d.Field(x => x.NomeFantasia);
        d.Field(x => x.TipoPessoa);
        d.Field(x => x.NumCnpj).Name("cpfCnpj");
        d.Field(x => x.IdCidade);
        d.Field(x => x.Bairro);
        d.Field(x => x.Cep);
        d.Field(x => x.DataCadastro);
        d.Field(x => x.IdColigacao);
        d.Field(x => x.FlagInativa);
        d.Field(x => x.IdCidadeNavigation).Name("cidade");
    }
}

public class VendedorType : ObjectType<FtVendedor>
{
    protected override void Configure(IObjectTypeDescriptor<FtVendedor> d)
    {
        d.Name("Vendedor");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdVendedor);
        d.Field(x => x.Nome);
        d.Field(x => x.TipoVendedor).Description("'Interno' ou 'Externo'.");
        d.Field(x => x.FlagInativo);
        d.Field(x => x.IdSupervisor);
    }
}

public class ProdutoType : ObjectType<PrProduto>
{
    protected override void Configure(IObjectTypeDescriptor<PrProduto> d)
    {
        d.Name("Produto");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdProduto);
        d.Field(x => x.Descricao);
        d.Field(x => x.Complemento);
        d.Field(x => x.IdUnidadeVenda);
        d.Field(x => x.IdMarca);
        d.Field(x => x.IdGrupo);
        d.Field(x => x.IdClassificacao);
        d.Field(x => x.NumOriginal);
        d.Field(x => x.NumFabricante);
        d.Field(x => x.Medida);
        d.Field(x => x.PesoBruto);
        d.Field(x => x.PesoLiquido);
        d.Field(x => x.FlagBloqVenda);
        d.Field(x => x.IdFornecedor);
    }
}

public class CidadeType : ObjectType<TbCidade>
{
    protected override void Configure(IObjectTypeDescriptor<TbCidade> d)
    {
        d.Name("Cidade");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdCidade);
        d.Field(x => x.Nome);
        d.Field(x => x.IdEstado);
        d.Field(x => x.CodigoIbge);
    }
}

public class OperacaoType : ObjectType<FtOperacao>
{
    protected override void Configure(IObjectTypeDescriptor<FtOperacao> d)
    {
        d.Name("Operacao").Description("Operação fiscal.");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdOperacao);
        d.Field(x => x.Descricao);
        d.Field(x => x.TipoMovimento).Description("0 Venda/Compra, 1 Devolução, 2 Diversos, 3 Transferência.");
    }
}

public class CondicaoPagamentoType : ObjectType<FtCondPagto>
{
    protected override void Configure(IObjectTypeDescriptor<FtCondPagto> d)
    {
        d.Name("CondicaoPagamento");
        d.BindFieldsExplicitly();
        d.Field(x => x.IdCondPagto);
        d.Field(x => x.Descricao);
        d.Field(x => x.QuantParcela);
    }
}

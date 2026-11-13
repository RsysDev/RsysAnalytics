namespace ErpWebAnalytics.Application.Pedidos;

/// <summary>
/// Linha da relação de pedidos. Cada linha é um pedido (CountPedido = 1) ou uma nota fiscal de devolução (CountNota = 1,
/// com valores negativos e IdPedido = 0).
/// </summary>
public class PedidoResumo
{
    /// <summary>1 quando a linha é um pedido.</summary>
    public int CountPedido { get; set; }
    /// <summary>1 quando a linha é uma nota fiscal de devolução.</summary>
    public int CountNota { get; set; }
    /// <summary>Chave de ordenação: IdPedido ou IdNotaFiscal.</summary>
    public string Ordem { get; set; } = "";
    public int IdEmpresa { get; set; }
    /// <summary>Tipo do registro conforme viewPedidosComDevolucao.</summary>
    public string? TipoPedidoNota { get; set; }
    public int IdPedido { get; set; }
    /// <summary>Código da entidade (cliente); usa a entidade principal da coligação quando existir.</summary>
    public string? EntidadeCodigo { get; set; }
    /// <summary>Razão social; prefixada com '*' quando vem da entidade principal da coligação.</summary>
    public string? EntidadeRazaoSocial { get; set; }
    public DateTime? DataMovimento { get; set; }
    public int? VendedorInterno { get; set; }
    public int? VendedorExterno { get; set; }
    public string? CondicaoPagto { get; set; }
    public DateTime? DataPrevisaoEntrega { get; set; }
    /// <summary>0 Em Digitação, 1 Digitado, 2 Faturamento Parcial, 3 Faturado, 4 Cancelado.</summary>
    public int? StatusPedido { get; set; }
    /// <summary>Prazo médio de venda.</summary>
    public decimal? PMV { get; set; }
    public int? IdOperacao { get; set; }
    public DateTime? DataFaturamento { get; set; }
    /// <summary>Número da nota fiscal.</summary>
    public string? NotaFiscal { get; set; }
    public decimal ValorProdutos { get; set; }
    public decimal ValorServicos { get; set; }
    public decimal ValorSeguro { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorSeguroDespesas { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorProdutosComDevolucao { get; set; }
    public decimal ValorServicosComDevolucao { get; set; }
    public decimal ValorSeguroComDevolucao { get; set; }
    public decimal ValorFreteComDevolucao { get; set; }
    public decimal ValorSeguroDespesasComDevolucao { get; set; }
    public decimal ValorDescontoComDevolucao { get; set; }
    public decimal DevValorProdutos { get; set; }
    public decimal DevValorServicos { get; set; }
    public decimal DevValorSeguro { get; set; }
    public decimal DevValorFrete { get; set; }
    public decimal DevValorSeguroDespesas { get; set; }
    public decimal DevValorDesconto { get; set; }
    /// <summary>Custo; zerado quando o usuário não tem permissão de ver custo.</summary>
    public decimal ValorCustoFor { get; set; }
    public decimal ValorOutrasDespesas { get; set; }
    public string? DescStatusPedido { get; set; }
    /// <summary>0 Pedido, 1 Cotação, 2 Pendência.</summary>
    public int? TipoPedido { get; set; }
    public string? DescTipoPedido { get; set; }
    public string? NomeVendedorInterno { get; set; }
    public string? NomeVendedorExterno { get; set; }
    public string? NomeVendedorParticipante { get; set; }
    public decimal ValorVenda { get; set; }
    public decimal ValorVendaComDevolucao { get; set; }
    public decimal ValorTotalPedido { get; set; }
    public decimal ValorTotalPedidoComDevolucao { get; set; }
    public decimal ValorTotalDevolucao { get; set; }
    public string? OperacaoFiscal { get; set; }
    public string? TipoMovimentoOperacao { get; set; }
    /// <summary>Margem de lucro; zero quando o usuário não tem permissão.</summary>
    public decimal? Margem { get; set; }
    /// <summary>Indica se o usuário pode ver a margem.</summary>
    public bool ExibeMargem { get; set; }
    /// <summary>Texto de quebra por vendedor conforme o critério escolhido.</summary>
    public string? QuebraVendedorDescricao { get; set; }
    public decimal? ValorBaseComissao { get; set; }
    public decimal? ValorComissao { get; set; }
    public int? IdCidade { get; set; }
    public string? NomeCidade { get; set; }
    public string? Estado { get; set; }
    public string? DescricaoLoja { get; set; }
}

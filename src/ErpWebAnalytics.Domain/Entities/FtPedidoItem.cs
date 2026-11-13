using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class FtPedidoItem
{
    public int IdPedidoItem { get; set; }

    public int? IdPedido { get; set; }

    public int? IdProduto { get; set; }

    public int? IdMarca { get; set; }

    public string? IdClassificacao { get; set; }

    public int? IdServico { get; set; }

    public int? IdEmpresa { get; set; }

    public int? IdListaPreco { get; set; }

    public DateTime? DataCriacao { get; set; }

    public DateTime? DataMovimento { get; set; }

    public decimal? QuantAtendida { get; set; }

    public decimal? ValorBruto { get; set; }

    public decimal? PercDesconto { get; set; }

    public decimal? PercAcrescimo { get; set; }

    public decimal? ValorLiquido { get; set; }

    public decimal? ValorTotal { get; set; }

    public string? Descricao { get; set; }

    public string? NumOriginal { get; set; }

    public string? NumFabricante { get; set; }

    /// <summary>
    /// Tipo de Item no pedido
    ///    0-Produto
    ///    1-Serviço
    /// </summary>
    public short? TipoItem { get; set; }

    public decimal? AliqIpi { get; set; }

    public decimal? ValorBaseIpi { get; set; }

    public decimal? ValorBasePis { get; set; }

    public decimal? AliqPis { get; set; }

    public decimal? ValorBaseCofins { get; set; }

    public decimal? AliqCofins { get; set; }

    public decimal? AliqIcmsst { get; set; }

    public decimal? ValorBaseIcmsst { get; set; }

    public decimal? AliqIcms { get; set; }

    public decimal? ValorBaseIcms { get; set; }

    public decimal? ValorBaseIss { get; set; }

    public decimal? AliqIss { get; set; }

    public decimal? ValorIcms { get; set; }

    public decimal? ValorIpi { get; set; }

    public decimal? ValorPis { get; set; }

    public decimal? ValorCofins { get; set; }

    public decimal? ValorIcmsst { get; set; }

    public decimal? PercMvaIcmsst { get; set; }

    public decimal? ValorIss { get; set; }

    public decimal? ValorCustoFor { get; set; }

    public decimal? ValorCustoMed { get; set; }

    public decimal? ValorCustoUlt { get; set; }

    public decimal? ValorReducaoMargem { get; set; }

    public string? NumPedCliente { get; set; }

    public string? IdCfop { get; set; }

    public string? CstIcms { get; set; }

    public string? CstIpi { get; set; }

    public string? CstPis { get; set; }

    public string? CstCofins { get; set; }

    public string? CstCsosn { get; set; }

    public int? IdAplicacao { get; set; }

    public string? Observacao { get; set; }

    public decimal? QuantPedida { get; set; }

    public decimal? QuantFaturada { get; set; }

    public int? NumItemPedCliente { get; set; }

    public decimal? ValorDescProdutoRateado { get; set; }

    public decimal? ValorDescServicoRateado { get; set; }

    public decimal? ValorSeguro { get; set; }

    public decimal? ValorFrete { get; set; }

    public decimal? ValorDespesas { get; set; }

    public decimal? QuantDevolvida { get; set; }

    public string? IdTributacao { get; set; }

    public int? IdColaborador { get; set; }

    public bool? FlagBloqueioDesconto { get; set; }

    public int? IdPromocaoProduto { get; set; }

    public decimal? ValorBaseComissaoInterno { get; set; }

    public decimal? ValorComissaoInterno { get; set; }

    public decimal? ValorBaseComissaoExterno { get; set; }

    public decimal? ValorComissaoExterno { get; set; }

    public decimal? ValorAcrescimoCusto { get; set; }

    public decimal? ValorOutrasDespesas { get; set; }

    public int? IdLocalizacaoLote { get; set; }

    public int? IdEmpresaEstoque { get; set; }

    public int? IdLoteProduto { get; set; }

    public decimal? ValorPartilhaIcmsdestino { get; set; }

    public decimal? ValorFcp { get; set; }

    public short? StatusPedido { get; set; }

    public short? TipoPedido { get; set; }

    public decimal? MargemLucro { get; set; }

    public decimal? PerDespAdmCalc { get; set; }

    public decimal? ValorDespAdmCalc { get; set; }

    public string? IdMoeda { get; set; }

    public decimal? ValorCreditoRessarcSt { get; set; }

    public DateTime? DataEstoque { get; set; }

    public bool? FlagControleBaseTroca { get; set; }

    public string? IdEntidade { get; set; }

    public decimal? ValorCustoBaseTroca { get; set; }

    public decimal? ValorCustoForOriginal { get; set; }

    public int? IdOperacao { get; set; }

    public decimal? PercDescontoCondPagto { get; set; }

    public decimal? PercAcrescimoCondPagto { get; set; }

    public decimal? PercAcrescimoIcms { get; set; }

    public decimal? PercDescontoTransf271 { get; set; }

    public decimal? PercDescontoCalculado { get; set; }

    public decimal? PercAcrescimoCalculado { get; set; }

    public decimal? ValorOriginalPreco1 { get; set; }

    public int? SeqOrdenacao { get; set; }

    public virtual FtPedido? IdPedidoNavigation { get; set; }

    public virtual PrProduto? IdProdutoNavigation { get; set; }
}

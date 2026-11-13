using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class FiNotaFiscal
{
    public int IdNotaFiscal { get; set; }

    /// <summary>
    /// 0-Pedido
    ///    1-Cotação
    /// </summary>
    public short? TipoMovimento { get; set; }

    public string? IdEntidade { get; set; }

    public string? NumeroDocumento { get; set; }

    public string? Serie { get; set; }

    public string? Modelo { get; set; }

    public string? Especie { get; set; }

    public short? IdCondPagto { get; set; }

    public int IdEmpresa { get; set; }

    public int? IdOperacao { get; set; }

    public string? IdTransportadora { get; set; }

    public DateTime? DataCriacao { get; set; }

    public DateTime? DataMovimento { get; set; }

    public DateTime? DataEmissao { get; set; }

    public DateTime? DataSaidaEntrada { get; set; }

    public decimal? ValorProdutos { get; set; }

    public decimal? ValorServicos { get; set; }

    public decimal? ValorDescProdutos { get; set; }

    public decimal? ValorDescServicos { get; set; }

    public decimal? ValorFrete { get; set; }

    public decimal? ValorSeguro { get; set; }

    public decimal? ValorDespesas { get; set; }

    public decimal? ValorBaseIcms { get; set; }

    public decimal? ValorIcms { get; set; }

    public decimal? ValorBaseIcmsst { get; set; }

    public decimal? ValorIcmsst { get; set; }

    public decimal? ValorBaseIcmsstexcluso { get; set; }

    public decimal? ValorIcmsstexcluso { get; set; }

    public decimal? ValorBaseIpi { get; set; }

    public decimal? ValorIpi { get; set; }

    public decimal? ValorBasePis { get; set; }

    public decimal? ValorPis { get; set; }

    public decimal? ValorBaseCofins { get; set; }

    public decimal? ValorCofins { get; set; }

    public decimal? ValorBaseIi { get; set; }

    public decimal? ValorIi { get; set; }

    public decimal? ValorBaseIss { get; set; }

    public decimal? ValorIss { get; set; }

    public decimal? ValorRetencao { get; set; }

    public decimal? ValorImpostoConsumidorFinal { get; set; }

    public string? NfeChaveAcesso { get; set; }

    public short? NfeTipoEmissao { get; set; }

    public short? NfeFinalidadeEmissao { get; set; }

    public short? NfeTipoAmbiente { get; set; }

    public DateTime? NfeDataEntradaContingencia { get; set; }

    public string? NfeJustificativaContingencia { get; set; }

    public short? NfeStatusNota { get; set; }

    public short? TipoModalidadeFrete { get; set; }

    public int? IdUsuario { get; set; }

    public decimal? ValorCustoSer { get; set; }

    public decimal? ValorCustoFor { get; set; }

    public decimal? ValorCustoMed { get; set; }

    public decimal? ValorCustoUlt { get; set; }

    public decimal? ValorReducaoProdutos { get; set; }

    public decimal? ValorReducaoServicos { get; set; }

    /// <summary>
    /// 0 - Nota Fiscal
    ///    1 - Nota Fiscal e Cupom Fiscal
    ///    2 - Cupom Fiscal
    /// </summary>
    public short? TipoNotaCupom { get; set; }

    public string? NumPedCliente { get; set; }

    public int? IdMinuta { get; set; }

    /// <summary>
    ///  0    - Própria
    ///      1    - Terceiros
    /// </summary>
    public short? TipoEmissao { get; set; }

    public int? IdBanco { get; set; }

    /// <summary>
    /// 0-Venda Futura
    /// </summary>
    public short? RefTipoOrigem { get; set; }

    public int? RefIdOrigem { get; set; }

    public int? NfeCampoChave { get; set; }

    public decimal? ValorAdicional { get; set; }

    public decimal? ValorFcp { get; set; }

    public decimal? ValorPartilhaIcmsorigem { get; set; }

    public decimal? ValorPartilhaIcmsdestino { get; set; }

    public decimal? ValorDespNaoInclusas { get; set; }

    public short? TipoAlteracaoCusto { get; set; }

    public short? StatusNota { get; set; }

    public short? TipoIndicadorPresenca { get; set; }

    public bool? FlagConferida { get; set; }

    public int? IdFatura { get; set; }

    public int? NumeroRps { get; set; }

    public byte[] Versao { get; set; } = null!;

    public decimal? ValorIpidevol { get; set; }

    public string? DestNome { get; set; }

    public string? DestCpfCnpj { get; set; }

    public decimal? ValorFcpst { get; set; }

    public decimal? ValorFcpstret { get; set; }

    public short? StatusAuditoria { get; set; }

    public decimal? ValorAcrescimoCustoProdutos { get; set; }

    public decimal? ValorAcrescimoCustoServicos { get; set; }

    public string? Exufembarque { get; set; }

    public string? ExlocalEmbarque { get; set; }

    public string? ExlocalDespacho { get; set; }

    public string? FinalidadeEntrada { get; set; }

    public string? IdEntidadeOriginal { get; set; }

    public int? IdOperacaoAnterior { get; set; }

    public decimal? ValorInclusoIcms { get; set; }

    public decimal? ValorInclusoPis { get; set; }

    public decimal? ValorInclusoCofins { get; set; }

    public int? Pmv { get; set; }

    public int? IdControleInterno { get; set; }

    public short? QuantCasasDecimais { get; set; }

    public decimal? ValorIcmsdesoneracao { get; set; }

    public DateTime? DataAtualizacaoCusto { get; set; }

    public bool? FlagObsManual { get; set; }

    public decimal? ValorInclusoIi { get; set; }

    public decimal? ValorTotalNota { get; set; }

    public bool? FlagDanfeSimplificado { get; set; }

    public int? IdUsuarioConfirmacao { get; set; }

    public DateTime? DataConfirmacao { get; set; }

    public decimal? IbsCbsValorBase { get; set; }

    public decimal? IbsValorImposto { get; set; }

    public decimal? IbsValorImpCredPre { get; set; }

    public decimal? IbsValorImpCredPreSusp { get; set; }

    public decimal? CbsValorDiferimento { get; set; }

    public decimal? CbsValorDevTrib { get; set; }

    public decimal? CbsValorImposto { get; set; }

    public decimal? CbsValorImpCredPre { get; set; }

    public decimal? CbsValorImpCredPreSusp { get; set; }

    public decimal? IsValorBase { get; set; }

    public decimal? IsValorImposto { get; set; }

    public decimal? IbsUfvalorDiferimento { get; set; }

    public decimal? IbsUfvalorDevTrib { get; set; }

    public decimal? IbsUfvalorImposto { get; set; }

    public decimal? IbsMunValorDiferimento { get; set; }

    public decimal? IbsMunValorDevTrib { get; set; }

    public decimal? IbsMunValorImposto { get; set; }

    public string? ChaveDpsnfse { get; set; }

    public short? NfeTipoNotaDebito { get; set; }

    public short? NfeTipoNotaCredito { get; set; }

    public int? IdCidadePrestacao { get; set; }

    public int? IdEcommercePedido { get; set; }

    public virtual ICollection<FiNotaFiscalPedido> FiNotaFiscalPedido { get; set; } = new List<FiNotaFiscalPedido>();

    public virtual FtCondPagto? IdCondPagtoNavigation { get; set; }

    public virtual TbEntidade? IdEntidadeNavigation { get; set; }

    public virtual FtOperacao? IdOperacaoNavigation { get; set; }

    public virtual TbEntidade? IdTransportadoraNavigation { get; set; }
}

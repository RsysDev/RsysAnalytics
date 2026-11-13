using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class FtPedido
{
    public int IdPedido { get; set; }

    public int? IdEmpresa { get; set; }

    public string? IdEntidade { get; set; }

    public short? IdCondPagto { get; set; }

    public int? IdOperacao { get; set; }

    /// <summary>
    /// 0-Pedido
    ///    1-Cotação
    /// </summary>
    public short? TipoPedido { get; set; }

    public DateTime? DataCriacao { get; set; }

    public DateTime DataMovimento { get; set; }

    public DateTime? DataPrevisaoEntrega { get; set; }

    public DateTime? DataFaturamento { get; set; }

    public decimal? ValorProdutos { get; set; }

    public decimal? ValorServicos { get; set; }

    public decimal? ValorDescProdutos { get; set; }

    public decimal? ValorDescServicos { get; set; }

    public decimal? ValorFrete { get; set; }

    public decimal? ValorSeguro { get; set; }

    public decimal? ValorDespesas { get; set; }

    public decimal? ValorIpi { get; set; }

    public decimal? ValorSubTrib { get; set; }

    public decimal? ValorTotalPedido { get; set; }

    public decimal? ValorOutrasDespesas { get; set; }

    /// <summary>
    /// 0 - Em Digitação
    ///    1 - Digitado
    ///    2 - Faturamento Parcial
    ///    3 - Faturado
    ///    4 - Cancelado
    /// </summary>
    public short? StatusPedido { get; set; }

    public decimal? ValorCustoSer { get; set; }

    public decimal? ValorCustoFor { get; set; }

    public decimal? ValorCustoMed { get; set; }

    public decimal? ValorCustoUlt { get; set; }

    public decimal? ValorReducaoProdutos { get; set; }

    public decimal? ValorReducaoServicos { get; set; }

    public string? NumPedCliente { get; set; }

    public int? IdMinuta { get; set; }

    public int? IdBanco { get; set; }

    /// <summary>
    /// 1- Venda Futura
    /// </summary>
    public short? RefTipoOrigem { get; set; }

    public int? IdVendedorInterno { get; set; }

    public int? IdVendedorExterno { get; set; }

    public bool? FlagPendencia { get; set; }

    public decimal? ValorBaseComissaoInterno { get; set; }

    public decimal? ValorComissaoInterno { get; set; }

    public decimal? ValorBaseComissaoExterno { get; set; }

    public decimal? ValorComissaoExterno { get; set; }

    public bool? FlagImpressao { get; set; }

    public int? IdEquipamento { get; set; }

    public int? IdOrdemServico { get; set; }

    public int? IdContaContabil { get; set; }

    public bool? FlagProntoSeparacao { get; set; }

    public byte[] Versao { get; set; } = null!;

    public int? IdRegiao { get; set; }

    public int? NumeroViasImpressao { get; set; }

    public int? IdNaturezaPref { get; set; }

    public int? QtdParcelaPref { get; set; }

    public string? ObservacaoPagamento { get; set; }

    public string? DestNome { get; set; }

    public string? DestCpfCnpj { get; set; }

    public bool? FlagLiberadoAvista { get; set; }

    /// <summary>
    /// 0 - Desbloqueado
    ///    1 - Bloqueio Total
    ///    2 - Bloqueio de Itens
    ///    3 - Removido todos os Bloqueios
    /// </summary>
    public int? TipoBloqueioEdicao { get; set; }

    public short? TipoImpressao { get; set; }

    public decimal? ValorAcrescimoCustoProdutos { get; set; }

    public decimal? ValorAcrescimoCustoServicos { get; set; }

    public decimal? ValorRetencao { get; set; }

    public int? IdContatoEntidade { get; set; }

    public decimal? MargemProdutos { get; set; }

    public decimal? MargemServicos { get; set; }

    public decimal? MargemTotalPedido { get; set; }

    public int? IdUsuarioInc { get; set; }

    public int? IdUsuarioAlt { get; set; }

    public DateTime? DataAlteracao { get; set; }

    public int? Pmv { get; set; }

    public int? IdTipoOrigem { get; set; }

    public int? IdEmpresaOriginal { get; set; }

    public decimal? ValorCredito { get; set; }

    public short? IdPlataforma { get; set; }

    public int? IdLojaPlataforma { get; set; }

    public int? IdCidadePrestacao { get; set; }

    public int? IdEcommercePedido { get; set; }

    public virtual ICollection<FiNotaFiscalPedido> FiNotaFiscalPedido { get; set; } = new List<FiNotaFiscalPedido>();

    public virtual ICollection<FtPedidoItem> FtPedidoItem { get; set; } = new List<FtPedidoItem>();

    public virtual ICollection<FtPedidoVendedor> FtPedidoVendedor { get; set; } = new List<FtPedidoVendedor>();

    public virtual FtCondPagto? IdCondPagtoNavigation { get; set; }

    public virtual TbEntidade? IdEntidadeNavigation { get; set; }

    public virtual FtOperacao? IdOperacaoNavigation { get; set; }

    public virtual FtVendedor? IdVendedorExternoNavigation { get; set; }

    public virtual FtVendedor? IdVendedorInternoNavigation { get; set; }
    
    public string? StatusPedidoDesc { 
        get {
            return StatusPedido switch
            {
                0 => "Em Digitação",
                1 => "Digitado",
                2 => "Faturamento Parcial",
                3 => "Faturado",
                4 => "Cancelado",
                _ => string.Empty,
            };
        } 
    }
}

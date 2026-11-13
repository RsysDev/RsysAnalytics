using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class FtVendedor
{
    public int IdVendedor { get; set; }

    public string? Nome { get; set; }

    public string? TipoVendedor { get; set; }

    public bool? FlagInativo { get; set; }

    /// <summary>
    /// 0-Não Calculada
    ///    1-Comissão fixa
    ///    2-Comissão Sobre margem de lucro
    /// </summary>
    public short? TipoComissao { get; set; }

    public decimal? AliqComissaoDireta { get; set; }

    public decimal? AliqComissaoIndireta { get; set; }

    public string? Email { get; set; }

    public int? IdSupervisor { get; set; }

    public virtual ICollection<FtPedido> FtPedidoIdVendedorExternoNavigation { get; set; } = new List<FtPedido>();

    public virtual ICollection<FtPedido> FtPedidoIdVendedorInternoNavigation { get; set; } = new List<FtPedido>();

    public virtual ICollection<FtPedidoVendedor> FtPedidoVendedor { get; set; } = new List<FtPedidoVendedor>();
}

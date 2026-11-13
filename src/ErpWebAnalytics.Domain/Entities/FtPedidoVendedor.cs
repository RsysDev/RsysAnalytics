using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class FtPedidoVendedor
{
    public int IdPedido { get; set; }

    public int IdVendedor { get; set; }

    public string? TipoVendedor { get; set; }

    public virtual FtPedido IdPedidoNavigation { get; set; } = null!;

    public virtual FtVendedor IdVendedorNavigation { get; set; } = null!;
}

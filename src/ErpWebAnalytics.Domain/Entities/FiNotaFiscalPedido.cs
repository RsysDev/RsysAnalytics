using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class FiNotaFiscalPedido
{
    public int IdNotaFiscal { get; set; }

    public int IdPedido { get; set; }

    public int IdSeparacao { get; set; }

    public bool? FlagFaturamentoAvulso { get; set; }

    public virtual FiNotaFiscal IdNotaFiscalNavigation { get; set; } = null!;

    public virtual FtPedido IdPedidoNavigation { get; set; } = null!;
}

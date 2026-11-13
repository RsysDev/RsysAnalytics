using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class ViewPedidosComDevolucao
{
    public int Tipo { get; set; }

    public int? IdPedido { get; set; }

    public int? IdNotaFiscal { get; set; }

    public DateTime? DataFaturamento { get; set; }

    public DateTime? DataMovimento { get; set; }

    public int? IdEmpresa { get; set; }

    public string? IdEntidade { get; set; }

    public int? IdOperacao { get; set; }
}

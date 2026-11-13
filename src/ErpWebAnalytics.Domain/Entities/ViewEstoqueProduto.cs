using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class ViewEstoqueProduto
{
    public int IdProduto { get; set; }

    public int IdEmpresaEstoque { get; set; }

    public decimal? QuantEstoque { get; set; }

    public decimal? QuantReservadaEnt { get; set; }

    public decimal? QuantReservadaSai { get; set; }

    public decimal? QuantPedidoAberto { get; set; }

    public decimal? QuantPendenciaVenda { get; set; }

    public int? QuantEstoqueMinimo { get; set; }

    public decimal? QuantEstoqueDisponivel { get; set; }

    public decimal? QuantEstoqueTransito { get; set; }
}

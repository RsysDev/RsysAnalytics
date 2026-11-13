namespace ErpWebAnalytics.Application.Pedidos;

/// <summary>Item de um pedido (produto ou serviço) com saldo a atender.</summary>
public class ItemPedido
{
    public int IdPedido { get; set; }
    /// <summary>Id do produto, ou do serviço quando não houver produto.</summary>
    public int? IdProdutoServico { get; set; }
    public string? Descricao { get; set; }
    public decimal? QuantEstoque { get; set; }
    public decimal? QuantDisponivel { get; set; }
    public decimal? QuantPedida { get; set; }
    public decimal? QuantAtendida { get; set; }
    /// <summary>QuantPedida - QuantAtendida.</summary>
    public decimal? QuantSaldo { get; set; }
    public decimal? PercDesconto { get; set; }
    public decimal? ValorLiquido { get; set; }
    /// <summary>Custo; zerado quando o usuário não tem permissão.</summary>
    public decimal? ValorCustoFor { get; set; }
    /// <summary>QuantSaldo * ValorLiquido.</summary>
    public decimal? ValorTotal { get; set; }
    public string? Medida { get; set; }
    public decimal? Margem { get; set; }
    public string? NumFabricante { get; set; }
}

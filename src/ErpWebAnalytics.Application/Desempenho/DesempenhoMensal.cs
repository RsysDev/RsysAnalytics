namespace ErpWebAnalytics.Application.Desempenho;

/// <summary>Indicadores de vendas de um conjunto de pedidos (loja inteira ou um vendedor).</summary>
public class IndicadoresVenda
{
    /// <summary>Venda bruta: soma dos pedidos, antes das devoluções.</summary>
    public decimal VendaBruta { get; set; }
    /// <summary>Valor devolvido no período (positivo).</summary>
    public decimal ValorDevolucoes { get; set; }
    /// <summary>Realizado = venda bruta - devoluções.</summary>
    public decimal Realizado { get; set; }
    /// <summary>Quantidade de pedidos (cupons).</summary>
    public int Pedidos { get; set; }
    /// <summary>Quantidade de notas fiscais de devolução.</summary>
    public int Devolucoes { get; set; }
    /// <summary>VendaBruta / Pedidos.</summary>
    public decimal TicketMedio { get; set; }
    /// <summary>Lucro bruto = realizado - custo (já líquido de devoluções). Zero se o usuário não vê custo.</summary>
    public decimal LucroBruto { get; set; }
    /// <summary>Margem em % sobre o realizado. Zero se o usuário não vê custo.</summary>
    public decimal MargemPercentual { get; set; }
    /// <summary>Devoluções em % da venda bruta.</summary>
    public decimal DevolucoesPercentual { get; set; }
}

/// <summary>Indicadores de um vendedor.</summary>
public class DesempenhoVendedor : IndicadoresVenda
{
    public int IdVendedor { get; set; }
    public string Nome { get; set; } = "";
}

/// <summary>Realizado de um dia do mês.</summary>
public class VendaDia
{
    public DateOnly Data { get; set; }
    public decimal Realizado { get; set; }
    public int Pedidos { get; set; }
}

/// <summary>Venda recente, para o letreiro do painel.</summary>
public class UltimaVenda
{
    public int IdPedido { get; set; }
    public DateTime DataMovimento { get; set; }
    public string? Vendedor { get; set; }
    public decimal Valor { get; set; }
}

/// <summary>Totais da loja em um mês (formato plano, uma linha por mês).</summary>
public class LojaMes : IndicadoresVenda
{
    /// <summary>Mês no formato yyyy-MM.</summary>
    public string Mes { get; set; } = "";
}

/// <summary>Indicadores de um vendedor em um mês.</summary>
public class VendedorMes : DesempenhoVendedor
{
    /// <summary>Mês no formato yyyy-MM.</summary>
    public string Mes { get; set; } = "";
}

/// <summary>Realizado de um dia, com o mês a que pertence.</summary>
public class VendaDiaMes : VendaDia
{
    /// <summary>Mês no formato yyyy-MM.</summary>
    public string Mes { get; set; } = "";
}

/// <summary>Vários meses em listas planas, prontas para tabelas e gráficos: um painel consome tudo numa só chamada.</summary>
public class DesempenhoPeriodo
{
    /// <summary>Quando o instantâneo foi gerado (UTC).</summary>
    public DateTime GeradoEm { get; set; }
    public List<LojaMes> Loja { get; set; } = [];
    public List<VendedorMes> Vendedores { get; set; } = [];
    public List<VendaDiaMes> PorDia { get; set; } = [];
    /// <summary>Últimas vendas do mês mais recente do período.</summary>
    public List<UltimaVenda> UltimasVendas { get; set; } = [];
}

/// <summary>Resumo de um mês: loja, ranking de vendedores, série diária e últimas vendas.</summary>
public class DesempenhoMensal
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    /// <summary>Dias do mês (para calcular meta por dia).</summary>
    public int DiasNoMes { get; set; }
    public IndicadoresVenda Loja { get; set; } = new();
    /// <summary>Vendedores ordenados pelo realizado, do maior para o menor.</summary>
    public List<DesempenhoVendedor> Vendedores { get; set; } = [];
    public List<VendaDia> PorDia { get; set; } = [];
    public List<UltimaVenda> UltimasVendas { get; set; } = [];
}

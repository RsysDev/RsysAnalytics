namespace ErpWebAnalytics.Application.Pedidos;

/// <summary>Critério de agrupamento/quebra por vendedor.</summary>
public enum QuebraVendedor
{
    /// <summary>Vendedor interno.</summary>
    Interno = 0,
    /// <summary>Vendedor externo.</summary>
    Externo = 1,
    /// <summary>Vendedor participante.</summary>
    Participante = 2
}

/// <summary>Filtros da relação de pedidos e de seus itens.</summary>
public class FiltroPedidos
{
    /// <summary>Empresas (IdEmpresa) a considerar. Obrigatório.</summary>
    public List<int> IdsEmpresa { get; set; } = [];

    /// <summary>Usuário (IdUsuario) cujas permissões de custo/margem/comissão se aplicam.</summary>
    public int IdUsuario { get; set; }

    /// <summary>Tipos de movimento da operação fiscal: 0 Venda/Compra, 1 Devolução, 2 Diversos, 3 Transferência. Padrão: [0].</summary>
    public List<int>? TiposMovimento { get; set; }

    /// <summary>Tipo do pedido: 0 Pedido, 1 Cotação, 2 Pendência, 999 todos. Padrão: 0.</summary>
    public int? TipoPedido { get; set; }

    /// <summary>Flag de pendência: -1 todos, 0 ou 1 para filtrar. Padrão: -1.</summary>
    public int? FlagPendencia { get; set; }

    /// <summary>Define o vendedor usado na quebra e nas comissões. Padrão: Interno.</summary>
    public QuebraVendedor? QuebraVendedor { get; set; }

    /// <summary>Inclui no resultado as notas fiscais de devolução (valores negativos). Padrão: true. Não afeta os itens.</summary>
    public bool? IncluirDevolucoes { get; set; }

    /// <summary>Data de movimento inicial (inclusive).</summary>
    public DateOnly? DataInicio { get; set; }

    /// <summary>Data de movimento final (inclusive).</summary>
    public DateOnly? DataFim { get; set; }
}

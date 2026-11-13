using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class TbCidade
{
    public int IdCidade { get; set; }

    public string? Nome { get; set; }

    public int? CodigoIbge { get; set; }

    public string? IdEstado { get; set; }

    public int? IdPaisExterior { get; set; }

    public virtual ICollection<TbEntidade> TbEntidadeIdCidadeCobNavigation { get; set; } = new List<TbEntidade>();

    public virtual ICollection<TbEntidade> TbEntidadeIdCidadeNavigation { get; set; } = new List<TbEntidade>();
}

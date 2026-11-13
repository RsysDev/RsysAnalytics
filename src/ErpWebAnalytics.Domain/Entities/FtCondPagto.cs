using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class FtCondPagto
{
    public short IdCondPagto { get; set; }

    public string? Descricao { get; set; }

    public short? QuantParcela { get; set; }

    public int? TipoVencimento { get; set; }

    /// <summary>
    /// 0-Vazio
    ///    1-Libera Bloqueio Credito Parcial e Total
    ///    2-Libera Bloqueio Credito Parcial
    /// </summary>
    public int? TipoVendaCreditoBloqueada { get; set; }

    public bool? FlagSomaValorSt { get; set; }

    public bool? FlagSomaValorIpi { get; set; }

    public bool? FlagSomaValorFrete { get; set; }

    public bool? FlagSomaValorSeguro { get; set; }

    public bool? FlagSomaValorDespesas { get; set; }

    public string? ObsNotaFiscal { get; set; }

    public int? TipoVencimentoManual { get; set; }

    public string? TempCodigo { get; set; }

    public decimal? PercDesconto { get; set; }

    public decimal? PercAcrescimo { get; set; }

    public bool? FlagBloqConferencia { get; set; }

    public bool? FlagVendaAvista { get; set; }

    public bool? FlagInativa { get; set; }

    public int? TipoMovimentoCondPagto { get; set; }

    public virtual ICollection<FiNotaFiscal> FiNotaFiscal { get; set; } = new List<FiNotaFiscal>();

    public virtual ICollection<FtPedido> FtPedido { get; set; } = new List<FtPedido>();

    public virtual ICollection<TbEntidade> TbEntidade { get; set; } = new List<TbEntidade>();
}

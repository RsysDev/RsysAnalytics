using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class FtOperacao
{
    public int IdOperacao { get; set; }

    public string? Descricao { get; set; }

    public int? IdTipoFiscal { get; set; }

    /// <summary>
    /// 0-Baixa reservada
    ///    1-Baixa efetivada
    ///    2-Não Movimenta estoque
    /// </summary>
    public short? TipoBaixaEstoque { get; set; }

    /// <summary>
    /// 1=NF-e normal
    ///    2=NF-e complementar
    ///    3=NF-e de ajuste
    ///    4=Devolução de mercadoria
    /// </summary>
    public short? FinalidadeNfe { get; set; }

    public int? TipoOperacao { get; set; }

    /// <summary>
    /// 0-Venda/Compra
    ///    1-Devolução
    ///    2-Diversos
    /// </summary>
    public int? TipoMovimento { get; set; }

    /// <summary>
    /// 0 - Permite Vencimentos
    ///    1 - Não Permite Vencimentos
    ///    2 - Vencimentos obrigatórios
    /// </summary>
    public int? TipoVencimento { get; set; }

    public string? DescricaoFiscal { get; set; }

    public short? TipoAtualizaCusto { get; set; }

    public bool? FlagTituloCredito { get; set; }

    public short? TipoIndicadorPresenca { get; set; }

    /// <summary>
    /// 0 - Atender direto
    /// -Quantidade atendida = pedida automaticamente
    /// ex: serviço, complemento de icms, venda balcao
    /// 
    /// 1 - Permite informar a quantidade atendida
    /// pode informar 0 ou a quantidade total
    /// so consegue faturar se fizer por separação parcial/total ou se acertar na mao as quantidades atendidas
    /// 
    /// 2 - Não Permite informar a quantidade atendida
    /// quantidade atendida tem que ser 0
    /// só consegue faturar se fizer a separação total ou parcial e processar pela parcial.
    /// </summary>
    public int? TipoFormaAtendimento { get; set; }

    public bool? FlagPermiteFaturamentoAvulso { get; set; }

    public bool? FlagExigeAuditoria { get; set; }

    public int? TipoEntrega { get; set; }

    public bool? FlagProcessoConferencia { get; set; }

    public bool? FlagPermiteTrocarEntidadeProcessamento { get; set; }

    public bool? FlagImprimeMinuta { get; set; }

    public short? TipoBloqueio { get; set; }

    public bool? FlagControleBaseTroca { get; set; }

    public short? TipoSomaSugestao { get; set; }

    public short? TipoProcessoSeparacao { get; set; }

    public short? TipoCalculoCustoMedio { get; set; }

    public bool? FlagAliqIcmsacrescPreco { get; set; }

    public bool? FlagProcessoGuarda { get; set; }

    public bool? FlagBloqueiaBaixaCompra { get; set; }

    public short? TipoCarteiraTitulo { get; set; }

    public short? TipoNotaDebito { get; set; }

    public short? TipoNotaCredito { get; set; }

    public int? FormaConferenciaPedido { get; set; }

    public bool? FlagInativa { get; set; }

    public virtual ICollection<FiNotaFiscal> FiNotaFiscal { get; set; } = new List<FiNotaFiscal>();

    public virtual ICollection<FtPedido> FtPedido { get; set; } = new List<FtPedido>();
}

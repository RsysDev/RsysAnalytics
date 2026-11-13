using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class PrProduto
{
    public int IdProduto { get; set; }

    public string? IdClassificacao { get; set; }

    public string? IdUnidadeVenda { get; set; }

    public string? IdUnidadeCompra { get; set; }

    public string? Descricao { get; set; }

    public string? Complemento { get; set; }

    public string? Aplicacao { get; set; }

    public int? TipoItemNfe { get; set; }

    /// <summary>
    /// 00 – Mercadoria para Revenda;
    ///    01 – Matéria-Prima;
    ///    02 – Embalagem;
    ///    03 – Produto em Processo;
    ///    04 – Produto Acabado;
    ///    05 – Subproduto;
    ///    06 – Produto Intermediário;
    ///    07 – Material de Uso e Consumo;
    ///    08 – Ativo Imobilizado;
    ///    09 – Serviços;
    ///    10 – Outros insumos;
    ///    99 – Outras
    /// </summary>
    public string? TipoItemSped { get; set; }

    public int? EmbCompra { get; set; }

    public int? EmbVenda { get; set; }

    public decimal? Largura { get; set; }

    public decimal? Altura { get; set; }

    public decimal? Comprimento { get; set; }

    public decimal? PesoBruto { get; set; }

    public decimal? PesoLiquido { get; set; }

    public short? TipoOrigemFiscal { get; set; }

    public string? IdTributacao { get; set; }

    /// <summary>
    /// 0-Controle de Localização definida pelo sistema
    ///    1-Estoque por Lote e Localização específica
    /// </summary>
    public short? TipoLote { get; set; }

    public bool? FlagBloqVenda { get; set; }

    public int? IdGrupo { get; set; }

    public string? IdFornecedor { get; set; }

    public int? IdMarca { get; set; }

    public string? NumOriginal { get; set; }

    public string? NumFabricante { get; set; }

    public bool? FlagProdutoNaoFiscal { get; set; }

    /// <summary>
    /// 0 - Produto Cadastrado com todas as informações necessárias, 
    ///     com liberação para venda.
    /// 
    ///     1 - Produto em inicio de processo de cadastro, precisando completar
    /// 	informações importantes.
    /// </summary>
    public bool? FlagPreCadastro { get; set; }

    public string? CodigoAntigo { get; set; }

    public int? IdSimilar { get; set; }

    public int? IdDimensao { get; set; }

    public string? IdLinha { get; set; }

    public int? CodigoAnp { get; set; }

    public decimal? PercGasGlppetroleo { get; set; }

    public decimal? PercGasGlpnac { get; set; }

    public decimal? PercGasGlpimp { get; set; }

    public bool? FlagBloqueioCompra { get; set; }

    public bool? FlagPossuiImagens { get; set; }

    public string? Medida { get; set; }

    public string? RefSiteIntegracao { get; set; }

    public int? IdProdutoBaseTroca { get; set; }

    public string? IdUnidadeExterior { get; set; }

    public decimal? FatorUnidadeExterior { get; set; }

    public string? CodigoAuxiliar1 { get; set; }

    public string? CodigoAuxiliar2 { get; set; }

    public short? TipoClassificacao { get; set; }

    public string? MotivoBloqueioCompra { get; set; }

    public int? IdFinalidade { get; set; }

    public string? RtcCodigoCst { get; set; }

    public string? RtcCodigocClassTrib { get; set; }

    public virtual ICollection<FtPedidoItem> FtPedidoItem { get; set; } = new List<FtPedidoItem>();

    public virtual TbEntidade? IdFornecedorNavigation { get; set; }

    public virtual PrProduto? IdProdutoBaseTrocaNavigation { get; set; }

    public virtual ICollection<PrProduto> InverseIdProdutoBaseTrocaNavigation { get; set; } = new List<PrProduto>();
}

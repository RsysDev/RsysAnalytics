using System;
using System.Collections.Generic;

namespace ErpWebAnalytics.Domain.Entities;

public partial class TbEntidade
{
    public string IdEntidade { get; set; } = null!;

    public int IdTipoContribuinte { get; set; }

    public int? IdAtividade { get; set; }

    public string? IdTipoEntidade { get; set; }

    public string? RazaoSocial { get; set; }

    public string? NomeFantasia { get; set; }

    /// <summary>
    /// Somente diferenciação para tipo de pessoa:
    ///    
    ///    Juridica
    ///    Fisica
    ///    
    ///    
    /// </summary>
    public string? TipoPessoa { get; set; }

    public string? NumCnpj { get; set; }

    public string? NumInscricaoEstadual { get; set; }

    public string? NumInscricaoMunicipal { get; set; }

    public string? NumSuframa { get; set; }

    public string? PaginaWeb { get; set; }

    public DateTime? DataFundacao { get; set; }

    public DateTime? DataCadastro { get; set; }

    public string? Logradouro { get; set; }

    public string? Numero { get; set; }

    public string? Complemento { get; set; }

    public string? Bairro { get; set; }

    public string? Cep { get; set; }

    public int? IdCidade { get; set; }

    public string? TelefoneCom1 { get; set; }

    public string? TelefoneCom2 { get; set; }

    public string? TelefoneRes { get; set; }

    public string? TelefoneCel { get; set; }

    public string? Email { get; set; }

    /// <summary>
    /// 1=Contribuinte ICMS (informar a IE do destinatário);
    ///    2=Contribuinte isento de Inscrição no cadastro de Contribuintes do ICMS;
    ///    9=Não Contribuinte, que pode ou não possuir Inscrição Estadual no Cadastro de Contribuintes do ICMS;
    /// </summary>
    public short? TipoIndicadorIe { get; set; }

    public string? Cnae { get; set; }

    /// <summary>
    /// Tipo de Regime de Apuração
    ///    
    ///    0 - SIMPLES NACIONAL
    ///    1 - NORMAL - REGIME PERIÓDICO DE APURAÇÃO
    ///       
    ///    
    /// </summary>
    public int? TipoRegimeApuracao { get; set; }

    public string? Latitude { get; set; }

    public string? Longitude { get; set; }

    public byte[] Versao { get; set; } = null!;

    public string? LogradouroCob { get; set; }

    public string? NumeroCob { get; set; }

    public string? ComplementoCob { get; set; }

    public string? EnderecoCob { get; set; }

    public string? BairroCob { get; set; }

    public string? Cepcob { get; set; }

    public int? IdCidadeCob { get; set; }

    public string? TelefoneCob { get; set; }

    public int? IdRegiao { get; set; }

    public decimal? ValorFrete { get; set; }

    public short? IdCondPagto { get; set; }

    public string Endereco { get; set; } = null!;

    public int? IdColigacao { get; set; }

    public bool? FlagObservacaoDestaque { get; set; }

    public decimal? ValorDespesas { get; set; }

    public string? TelefoneWhatsApp { get; set; }

    public decimal? ValorLimiteCreditoUnificado { get; set; }

    public bool? FlagInativa { get; set; }

    public virtual ICollection<FiNotaFiscal> FiNotaFiscalIdEntidadeNavigation { get; set; } = new List<FiNotaFiscal>();

    public virtual ICollection<FiNotaFiscal> FiNotaFiscalIdTransportadoraNavigation { get; set; } = new List<FiNotaFiscal>();

    public virtual ICollection<FtPedido> FtPedido { get; set; } = new List<FtPedido>();

    public virtual TbCidade? IdCidadeCobNavigation { get; set; }

    public virtual TbCidade? IdCidadeNavigation { get; set; }

    public virtual FtCondPagto? IdCondPagtoNavigation { get; set; }

    public virtual ICollection<PrProduto> PrProduto { get; set; } = new List<PrProduto>();
}

using System;
using System.Collections.Generic;
using ErpWebAnalytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpWebAnalytics.Infrastructure.Persistence;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<FiNotaFiscal> FiNotaFiscal { get; set; }

    public virtual DbSet<FiNotaFiscalPedido> FiNotaFiscalPedido { get; set; }

    public virtual DbSet<FtCondPagto> FtCondPagto { get; set; }

    public virtual DbSet<FtOperacao> FtOperacao { get; set; }

    public virtual DbSet<FtPedido> FtPedido { get; set; }

    public virtual DbSet<FtPedidoItem> FtPedidoItem { get; set; }

    public virtual DbSet<FtPedidoVendedor> FtPedidoVendedor { get; set; }

    public virtual DbSet<FtVendedor> FtVendedor { get; set; }

    public virtual DbSet<PrProduto> PrProduto { get; set; }

    public virtual DbSet<TbCidade> TbCidade { get; set; }

    public virtual DbSet<TbEntidade> TbEntidade { get; set; }

    public virtual DbSet<ViewEstoqueProduto> ViewEstoqueProduto { get; set; }

    public virtual DbSet<ViewPedidosComDevolucao> ViewPedidosComDevolucao { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Latin1_General_CI_AS");

        modelBuilder.Entity<FiNotaFiscal>(entity =>
        {
            entity.HasKey(e => e.IdNotaFiscal).HasName("PK_FINOTAFISCAL");

            entity.ToTable("fiNotaFiscal", tb => tb.HasTrigger("fiNotaFiscalTrg"));

            entity.HasIndex(e => new { e.IdEntidade, e.DataMovimento }, "fiNotaFiscal1");

            entity.HasIndex(e => e.NumeroDocumento, "fiNotaFiscal2");

            entity.HasIndex(e => e.DataMovimento, "fiNotaFiscal3");

            entity.HasIndex(e => e.IdFatura, "fiNotafiscal4");

            entity.Property(e => e.IdNotaFiscal).ValueGeneratedNever();
            entity.Property(e => e.CbsValorDevTrib).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CbsValorDiferimento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CbsValorImpCredPre).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CbsValorImpCredPreSusp).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CbsValorImposto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ChaveDpsnfse)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("ChaveDPSNFSe");
            entity.Property(e => e.DataAtualizacaoCusto).HasColumnType("datetime");
            entity.Property(e => e.DataConfirmacao).HasColumnType("datetime");
            entity.Property(e => e.DataCriacao).HasColumnType("datetime");
            entity.Property(e => e.DataEmissao).HasColumnType("datetime");
            entity.Property(e => e.DataMovimento).HasColumnType("datetime");
            entity.Property(e => e.DataSaidaEntrada).HasColumnType("datetime");
            entity.Property(e => e.DestCpfCnpj)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DestNome)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Especie)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.ExlocalDespacho)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("EXLocalDespacho");
            entity.Property(e => e.ExlocalEmbarque)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("EXLocalEmbarque");
            entity.Property(e => e.Exufembarque)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("EXUFEmbarque");
            entity.Property(e => e.FinalidadeEntrada)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IbsCbsValorBase).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IbsMunValorDevTrib).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IbsMunValorDiferimento).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IbsMunValorImposto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IbsUfvalorDevTrib)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IbsUFValorDevTrib");
            entity.Property(e => e.IbsUfvalorDiferimento)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IbsUFValorDiferimento");
            entity.Property(e => e.IbsUfvalorImposto)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("IbsUFValorImposto");
            entity.Property(e => e.IbsValorImpCredPre).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IbsValorImpCredPreSusp).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IbsValorImposto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IdEntidade)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IdEntidadeOriginal)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IdTransportadora)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IsValorBase).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IsValorImposto).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Modelo)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.NfeCampoChave).HasColumnName("NFeCampoChave");
            entity.Property(e => e.NfeChaveAcesso)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NFeChaveAcesso");
            entity.Property(e => e.NfeDataEntradaContingencia)
                .HasColumnType("datetime")
                .HasColumnName("NFeDataEntradaContingencia");
            entity.Property(e => e.NfeFinalidadeEmissao).HasColumnName("NFeFinalidadeEmissao");
            entity.Property(e => e.NfeJustificativaContingencia)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NFeJustificativaContingencia");
            entity.Property(e => e.NfeStatusNota).HasColumnName("NFeStatusNota");
            entity.Property(e => e.NfeTipoAmbiente).HasColumnName("NFeTipoAmbiente");
            entity.Property(e => e.NfeTipoEmissao).HasColumnName("NFeTipoEmissao");
            entity.Property(e => e.NfeTipoNotaCredito).HasColumnName("NFeTipoNotaCredito");
            entity.Property(e => e.NfeTipoNotaDebito).HasColumnName("NFeTipoNotaDebito");
            entity.Property(e => e.NumPedCliente)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumeroRps).HasColumnName("NumeroRPS");
            entity.Property(e => e.Pmv).HasColumnName("PMV");
            entity.Property(e => e.RefTipoOrigem).HasComment("0-Venda Futura");
            entity.Property(e => e.Serie)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TipoAlteracaoCusto).HasDefaultValue((short)0);
            entity.Property(e => e.TipoEmissao).HasComment(" 0    - Própria\r\n     1    - Terceiros");
            entity.Property(e => e.TipoMovimento).HasComment("0-Pedido\r\n   1-Cotação");
            entity.Property(e => e.TipoNotaCupom).HasComment("0 - Nota Fiscal\r\n   1 - Nota Fiscal e Cupom Fiscal\r\n   2 - Cupom Fiscal");
            entity.Property(e => e.ValorAcrescimoCustoProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorAcrescimoCustoServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorAdicional).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorBaseCofins)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseCOFINS");
            entity.Property(e => e.ValorBaseIcms)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseICMS");
            entity.Property(e => e.ValorBaseIcmsst)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseICMSST");
            entity.Property(e => e.ValorBaseIcmsstexcluso)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseICMSSTExcluso");
            entity.Property(e => e.ValorBaseIi)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseII");
            entity.Property(e => e.ValorBaseIpi)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseIPI");
            entity.Property(e => e.ValorBaseIss)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseISS");
            entity.Property(e => e.ValorBasePis)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBasePIS");
            entity.Property(e => e.ValorCofins)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorCOFINS");
            entity.Property(e => e.ValorCustoFor).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCustoMed).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCustoSer).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCustoUlt).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDescProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDescServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDespNaoInclusas).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDespesas).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorFcp)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorFCP");
            entity.Property(e => e.ValorFcpst)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorFCPST");
            entity.Property(e => e.ValorFcpstret)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorFCPSTRet");
            entity.Property(e => e.ValorFrete).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorIcms)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorICMS");
            entity.Property(e => e.ValorIcmsdesoneracao)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorICMSDesoneracao");
            entity.Property(e => e.ValorIcmsst)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorICMSST");
            entity.Property(e => e.ValorIcmsstexcluso)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorICMSSTExcluso");
            entity.Property(e => e.ValorIi)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorII");
            entity.Property(e => e.ValorImpostoConsumidorFinal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorInclusoCofins)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorInclusoCOFINS");
            entity.Property(e => e.ValorInclusoIcms)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorInclusoICMS");
            entity.Property(e => e.ValorInclusoIi)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorInclusoII");
            entity.Property(e => e.ValorInclusoPis)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorInclusoPIS");
            entity.Property(e => e.ValorIpi)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorIPI");
            entity.Property(e => e.ValorIpidevol)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorIPIDevol");
            entity.Property(e => e.ValorIss)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorISS");
            entity.Property(e => e.ValorPartilhaIcmsdestino)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorPartilhaICMSDestino");
            entity.Property(e => e.ValorPartilhaIcmsorigem)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorPartilhaICMSOrigem");
            entity.Property(e => e.ValorPis)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorPIS");
            entity.Property(e => e.ValorProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorReducaoProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorReducaoServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorRetencao).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorSeguro).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorTotalNota)
                .HasComputedColumnSql("(((((((((((((((((isnull([ValorProdutos],(0))+isnull([ValorServicos],(0)))-isnull([ValorDescProdutos],(0)))-isnull([ValorDescServicos],(0)))+isnull([ValorFrete],(0)))+isnull([ValorSeguro],(0)))+isnull([ValorDespesas],(0)))+isnull([ValorICMSST],(0)))+isnull([ValorIPI],(0)))+isnull([ValorIPIDevol],(0)))-isnull([ValorRetencao],(0)))+isnull([ValorFCPST],(0)))+isnull([ValorFCPSTRet],(0)))+isnull([ValorInclusoICMS],(0)))+isnull([ValorInclusoPIS],(0)))+isnull([ValorInclusoCOFINS],(0)))+isnull([ValorInclusoII],(0)))-isnull([ValorICMSDesoneracao],(0)))", false)
                .HasColumnType("decimal(35, 2)");
            entity.Property(e => e.Versao)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.IdCondPagtoNavigation).WithMany(p => p.FiNotaFiscal)
                .HasForeignKey(d => d.IdCondPagto)
                .HasConstraintName("FK_ftNotaFiscal_ftCondPagto");

            entity.HasOne(d => d.IdEntidadeNavigation).WithMany(p => p.FiNotaFiscalIdEntidadeNavigation)
                .HasForeignKey(d => d.IdEntidade)
                .HasConstraintName("FK_ftNotaFiscal_ftEntidade");

            entity.HasOne(d => d.IdOperacaoNavigation).WithMany(p => p.FiNotaFiscal)
                .HasForeignKey(d => d.IdOperacao)
                .HasConstraintName("FK_ftNotaFiscal_ftOperacao");

            entity.HasOne(d => d.IdTransportadoraNavigation).WithMany(p => p.FiNotaFiscalIdTransportadoraNavigation)
                .HasForeignKey(d => d.IdTransportadora)
                .HasConstraintName("FK_fiNotaFiscal_tbEntidade");
        });

        modelBuilder.Entity<FiNotaFiscalPedido>(entity =>
        {
            entity.HasKey(e => new { e.IdNotaFiscal, e.IdPedido, e.IdSeparacao }).HasName("PK_FINOTAFISCALPEDIDO");

            entity.ToTable("fiNotaFiscalPedido");

            entity.HasIndex(e => e.IdPedido, "fiNotaFiscalPedido1");

            entity.HasOne(d => d.IdNotaFiscalNavigation).WithMany(p => p.FiNotaFiscalPedido)
                .HasForeignKey(d => d.IdNotaFiscal)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotaFiscalPedido_fiNotaFiscal");

            entity.HasOne(d => d.IdPedidoNavigation).WithMany(p => p.FiNotaFiscalPedido)
                .HasForeignKey(d => d.IdPedido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FINOTAFI_REFERENCE_FTPEDIDO");
        });

        modelBuilder.Entity<FtCondPagto>(entity =>
        {
            entity.HasKey(e => e.IdCondPagto).HasName("PK_FTCONDPAGTO");

            entity.ToTable("ftCondPagto");

            entity.Property(e => e.IdCondPagto).ValueGeneratedNever();
            entity.Property(e => e.Descricao)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FlagSomaValorIpi).HasColumnName("FlagSomaValorIPI");
            entity.Property(e => e.FlagSomaValorSt).HasColumnName("FlagSomaValorST");
            entity.Property(e => e.FlagVendaAvista).HasColumnName("FlagVendaAVista");
            entity.Property(e => e.ObsNotaFiscal)
                .HasMaxLength(8000)
                .IsUnicode(false);
            entity.Property(e => e.PercAcrescimo).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PercDesconto).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.TempCodigo)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TipoVendaCreditoBloqueada).HasComment("0-Vazio\r\n   1-Libera Bloqueio Credito Parcial e Total\r\n   2-Libera Bloqueio Credito Parcial");
        });

        modelBuilder.Entity<FtOperacao>(entity =>
        {
            entity.HasKey(e => e.IdOperacao).HasName("PK_FTOPERACAO");

            entity.ToTable("ftOperacao");

            entity.Property(e => e.IdOperacao).ValueGeneratedNever();
            entity.Property(e => e.Descricao)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DescricaoFiscal)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FinalidadeNfe).HasComment("1=NF-e normal\r\n   2=NF-e complementar\r\n   3=NF-e de ajuste\r\n   4=Devolução de mercadoria");
            entity.Property(e => e.FlagAliqIcmsacrescPreco).HasColumnName("FlagAliqICMSAcrescPreco");
            entity.Property(e => e.TipoAtualizaCusto).HasDefaultValue((short)0);
            entity.Property(e => e.TipoBaixaEstoque).HasComment("0-Baixa reservada\r\n   1-Baixa efetivada\r\n   2-Não Movimenta estoque");
            entity.Property(e => e.TipoFormaAtendimento).HasComment("0 - Atender direto\r\n-Quantidade atendida = pedida automaticamente\r\nex: serviço, complemento de icms, venda balcao\r\n\r\n1 - Permite informar a quantidade atendida\r\npode informar 0 ou a quantidade total\r\nso consegue faturar se fizer por separação parcial/total ou se acertar na mao as quantidades atendidas\r\n\r\n2 - Não Permite informar a quantidade atendida\r\nquantidade atendida tem que ser 0\r\nsó consegue faturar se fizer a separação total ou parcial e processar pela parcial.");
            entity.Property(e => e.TipoMovimento).HasComment("0-Venda/Compra\r\n   1-Devolução\r\n   2-Diversos");
            entity.Property(e => e.TipoVencimento).HasComment("0 - Permite Vencimentos\r\n   1 - Não Permite Vencimentos\r\n   2 - Vencimentos obrigatórios");
        });

        modelBuilder.Entity<FtPedido>(entity =>
        {
            entity.HasKey(e => e.IdPedido).HasName("PK_FTPEDIDO");

            entity.ToTable("ftPedido", tb => tb.HasTrigger("ftPedidoTrg"));

            entity.HasIndex(e => e.DataMovimento, "ftPedido1");

            entity.HasIndex(e => new { e.IdEntidade, e.DataMovimento }, "ftPedido2");

            entity.HasIndex(e => e.IdOrdemServico, "ftPedido3");

            entity.HasIndex(e => e.NumPedCliente, "ftPedido4");

            entity.Property(e => e.IdPedido).ValueGeneratedNever();
            entity.Property(e => e.DataAlteracao).HasColumnType("datetime");
            entity.Property(e => e.DataCriacao).HasColumnType("datetime");
            entity.Property(e => e.DataFaturamento).HasColumnType("datetime");
            entity.Property(e => e.DataMovimento).HasColumnType("datetime");
            entity.Property(e => e.DataPrevisaoEntrega).HasColumnType("datetime");
            entity.Property(e => e.DestCpfCnpj)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DestNome)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.FlagLiberadoAvista).HasColumnName("FlagLiberadoAVista");
            entity.Property(e => e.IdEntidade)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.MargemProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MargemServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MargemTotalPedido).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NumPedCliente)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ObservacaoPagamento)
                .HasMaxLength(4000)
                .IsUnicode(false);
            entity.Property(e => e.Pmv).HasColumnName("PMV");
            entity.Property(e => e.RefTipoOrigem).HasComment("1- Venda Futura");
            entity.Property(e => e.StatusPedido).HasComment("0 - Em Digitação\r\n   1 - Digitado\r\n   2 - Faturamento Parcial\r\n   3 - Faturado\r\n   4 - Cancelado");
            entity.Property(e => e.TipoBloqueioEdicao).HasComment("0 - Desbloqueado\r\n   1 - Bloqueio Total\r\n   2 - Bloqueio de Itens\r\n   3 - Removido todos os Bloqueios");
            entity.Property(e => e.TipoPedido).HasComment("0-Pedido\r\n   1-Cotação");
            entity.Property(e => e.ValorAcrescimoCustoProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorAcrescimoCustoServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorBaseComissaoExterno).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorBaseComissaoInterno).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorComissaoExterno).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorComissaoInterno).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCredito).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCustoFor).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCustoMed).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCustoSer).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorCustoUlt).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDescProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDescServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDespesas).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorFrete).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorIpi)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorIPI");
            entity.Property(e => e.ValorOutrasDespesas).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorReducaoProdutos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorReducaoServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorRetencao).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorSeguro).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorServicos).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorSubTrib).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorTotalPedido).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Versao)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.IdCondPagtoNavigation).WithMany(p => p.FtPedido)
                .HasForeignKey(d => d.IdCondPagto)
                .HasConstraintName("FK_ftPedido_ftCondPagto");

            entity.HasOne(d => d.IdEntidadeNavigation).WithMany(p => p.FtPedido)
                .HasForeignKey(d => d.IdEntidade)
                .HasConstraintName("FK_ftPedido_ftEntidade");

            entity.HasOne(d => d.IdOperacaoNavigation).WithMany(p => p.FtPedido)
                .HasForeignKey(d => d.IdOperacao)
                .HasConstraintName("FK_ftPedido_ftOperacao");

            entity.HasOne(d => d.IdVendedorExternoNavigation).WithMany(p => p.FtPedidoIdVendedorExternoNavigation)
                .HasForeignKey(d => d.IdVendedorExterno)
                .HasConstraintName("FK_ftPedidoVendedorExterno_ftVendedor");

            entity.HasOne(d => d.IdVendedorInternoNavigation).WithMany(p => p.FtPedidoIdVendedorInternoNavigation)
                .HasForeignKey(d => d.IdVendedorInterno)
                .HasConstraintName("FK_ftPedidoVendedorInterno_ftVendedor");
        });

        modelBuilder.Entity<FtPedidoItem>(entity =>
        {
            entity.HasKey(e => e.IdPedidoItem).HasName("PK_FTPEDIDOITEM");

            entity.ToTable("ftPedidoItem", tb => tb.HasTrigger("ftPedidoItem_Trigger"));

            entity.HasIndex(e => e.IdPedido, "ftPedidoItem1");

            entity.HasIndex(e => new { e.IdProduto, e.DataMovimento }, "ftPedidoItem2");

            entity.Property(e => e.IdPedidoItem).ValueGeneratedNever();
            entity.Property(e => e.AliqCofins)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("AliqCOFINS");
            entity.Property(e => e.AliqIcms)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("AliqICMS");
            entity.Property(e => e.AliqIcmsst)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("AliqICMSST");
            entity.Property(e => e.AliqIpi)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("AliqIPI");
            entity.Property(e => e.AliqIss)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("AliqISS");
            entity.Property(e => e.AliqPis)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("AliqPIS");
            entity.Property(e => e.CstCofins)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CstCOFINS");
            entity.Property(e => e.CstCsosn)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CstCSOSN");
            entity.Property(e => e.CstIcms)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CstICMS");
            entity.Property(e => e.CstIpi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CstIPI");
            entity.Property(e => e.CstPis)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CstPIS");
            entity.Property(e => e.DataCriacao).HasColumnType("datetime");
            entity.Property(e => e.DataEstoque).HasColumnType("datetime");
            entity.Property(e => e.DataMovimento).HasColumnType("datetime");
            entity.Property(e => e.Descricao)
                .HasMaxLength(400)
                .IsUnicode(false);
            entity.Property(e => e.IdCfop)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("IdCFOP");
            entity.Property(e => e.IdClassificacao)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IdEntidade)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IdMoeda)
                .HasMaxLength(5)
                .IsUnicode(false);
            entity.Property(e => e.IdTributacao)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MargemLucro).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.NumFabricante)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumOriginal)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumPedCliente)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Observacao)
                .HasMaxLength(4000)
                .IsUnicode(false);
            entity.Property(e => e.PerDespAdmCalc).HasColumnType("decimal(18, 3)");
            entity.Property(e => e.PercAcrescimo).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PercAcrescimoCalculado)
                .HasComputedColumnSql("((isnull([PercAcrescimo],(0))+isnull([PercAcrescimoCondPagto],(0)))+isnull([PercAcrescimoICMS],(0)))", false)
                .HasColumnType("decimal(13, 8)");
            entity.Property(e => e.PercAcrescimoCondPagto).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PercAcrescimoIcms)
                .HasColumnType("decimal(11, 8)")
                .HasColumnName("PercAcrescimoICMS");
            entity.Property(e => e.PercDesconto).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PercDescontoCalculado)
                .HasComputedColumnSql("((isnull([PercDesconto],(0))+isnull([PercDescontoCondPagto],(0)))+isnull([PercDescontoTransf271],(0)))", false)
                .HasColumnType("decimal(13, 8)");
            entity.Property(e => e.PercDescontoCondPagto).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PercDescontoTransf271).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.PercMvaIcmsst)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("PercMvaICMSST");
            entity.Property(e => e.QuantAtendida).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.QuantDevolvida).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.QuantFaturada).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.QuantPedida).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.TipoItem).HasComment("Tipo de Item no pedido\r\n   0-Produto\r\n   1-Serviço");
            entity.Property(e => e.ValorAcrescimoCusto).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorBaseCofins)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseCOFINS");
            entity.Property(e => e.ValorBaseComissaoExterno).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorBaseComissaoInterno).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorBaseIcms)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseICMS");
            entity.Property(e => e.ValorBaseIcmsst)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseICMSST");
            entity.Property(e => e.ValorBaseIpi)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseIPI");
            entity.Property(e => e.ValorBaseIss)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBaseISS");
            entity.Property(e => e.ValorBasePis)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorBasePIS");
            entity.Property(e => e.ValorBruto).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorCofins)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorCOFINS");
            entity.Property(e => e.ValorComissaoExterno).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorComissaoInterno).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCreditoRessarcSt)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorCreditoRessarcST");
            entity.Property(e => e.ValorCustoBaseTroca).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorCustoFor).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorCustoForOriginal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorCustoMed).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorCustoUlt).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorDescProdutoRateado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDescServicoRateado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDespAdmCalc).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorDespesas).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorFcp)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorFCP");
            entity.Property(e => e.ValorFrete).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorIcms)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorICMS");
            entity.Property(e => e.ValorIcmsst)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorICMSST");
            entity.Property(e => e.ValorIpi)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorIPI");
            entity.Property(e => e.ValorIss)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorISS");
            entity.Property(e => e.ValorLiquido).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorOriginalPreco1).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorOutrasDespesas).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorPartilhaIcmsdestino)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorPartilhaICMSDestino");
            entity.Property(e => e.ValorPis)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("ValorPIS");
            entity.Property(e => e.ValorReducaoMargem).HasColumnType("decimal(18, 4)");
            entity.Property(e => e.ValorSeguro).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorTotal).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.IdPedidoNavigation).WithMany(p => p.FtPedidoItem)
                .HasForeignKey(d => d.IdPedido)
                .HasConstraintName("FK_ftPedidoItem_ftPedido");

            entity.HasOne(d => d.IdProdutoNavigation).WithMany(p => p.FtPedidoItem)
                .HasForeignKey(d => d.IdProduto)
                .HasConstraintName("FK_ftPedidoItem_prProduto");
        });

        modelBuilder.Entity<FtPedidoVendedor>(entity =>
        {
            entity.HasKey(e => new { e.IdPedido, e.IdVendedor }).HasName("PK_FTPEDIDOVENDEDOR");

            entity.ToTable("ftPedidoVendedor");

            entity.Property(e => e.TipoVendedor)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.IdPedidoNavigation).WithMany(p => p.FtPedidoVendedor)
                .HasForeignKey(d => d.IdPedido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ftPedidoVendedor_ftPedido");

            entity.HasOne(d => d.IdVendedorNavigation).WithMany(p => p.FtPedidoVendedor)
                .HasForeignKey(d => d.IdVendedor)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ftPedidoVendedor_ftVendedor");
        });

        modelBuilder.Entity<FtVendedor>(entity =>
        {
            entity.HasKey(e => e.IdVendedor);

            entity.ToTable("ftVendedor");

            entity.Property(e => e.IdVendedor).ValueGeneratedNever();
            entity.Property(e => e.AliqComissaoDireta).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.AliqComissaoIndireta).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Nome)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TipoComissao).HasComment("0-Não Calculada\r\n   1-Comissão fixa\r\n   2-Comissão Sobre margem de lucro");
            entity.Property(e => e.TipoVendedor)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<PrProduto>(entity =>
        {
            entity.HasKey(e => e.IdProduto).HasName("PK_PRPRODUTO");

            entity.ToTable("prProduto", tb => tb.HasTrigger("prProdutoTrg"));

            entity.HasIndex(e => e.IdSimilar, "prProduto1");

            entity.HasIndex(e => e.NumFabricante, "prproduto2");

            entity.HasIndex(e => e.NumOriginal, "prproduto3");

            entity.Property(e => e.IdProduto).ValueGeneratedNever();
            entity.Property(e => e.Altura).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.Aplicacao).IsUnicode(false);
            entity.Property(e => e.CodigoAnp).HasColumnName("CodigoANP");
            entity.Property(e => e.CodigoAntigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CodigoAuxiliar1)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.CodigoAuxiliar2)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Comprimento).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.Descricao)
                .HasMaxLength(4000)
                .IsUnicode(false);
            entity.Property(e => e.FatorUnidadeExterior).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.FlagPreCadastro).HasComment("0 - Produto Cadastrado com todas as informações necessárias, \r\n    com liberação para venda.\r\n\r\n    1 - Produto em inicio de processo de cadastro, precisando completar\r\n	informações importantes.");
            entity.Property(e => e.IdClassificacao)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IdFornecedor)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IdLinha)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IdTributacao)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.IdUnidadeCompra)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IdUnidadeExterior)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.IdUnidadeVenda)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Largura).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.Medida)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MotivoBloqueioCompra)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumFabricante)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumOriginal)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PercGasGlpimp)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("PercGasGLPImp");
            entity.Property(e => e.PercGasGlpnac)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("PercGasGLPNac");
            entity.Property(e => e.PercGasGlppetroleo)
                .HasColumnType("decimal(7, 4)")
                .HasColumnName("PercGasGLPPetroleo");
            entity.Property(e => e.PesoBruto).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.PesoLiquido).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.RefSiteIntegracao)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RtcCodigoCst)
                .HasMaxLength(3)
                .IsUnicode(false)
                .HasColumnName("RtcCodigoCST");
            entity.Property(e => e.RtcCodigocClassTrib)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TipoItemSped)
                .HasMaxLength(2)
                .IsUnicode(false)
                .IsFixedLength()
                .HasComment("00 – Mercadoria para Revenda;\r\n   01 – Matéria-Prima;\r\n   02 – Embalagem;\r\n   03 – Produto em Processo;\r\n   04 – Produto Acabado;\r\n   05 – Subproduto;\r\n   06 – Produto Intermediário;\r\n   07 – Material de Uso e Consumo;\r\n   08 – Ativo Imobilizado;\r\n   09 – Serviços;\r\n   10 – Outros insumos;\r\n   99 – Outras");
            entity.Property(e => e.TipoLote).HasComment("0-Controle de Localização definida pelo sistema\r\n   1-Estoque por Lote e Localização específica");

            entity.HasOne(d => d.IdFornecedorNavigation).WithMany(p => p.PrProduto)
                .HasForeignKey(d => d.IdFornecedor)
                .HasConstraintName("FK_prProduto_tbEntidade");

            entity.HasOne(d => d.IdProdutoBaseTrocaNavigation).WithMany(p => p.InverseIdProdutoBaseTrocaNavigation)
                .HasForeignKey(d => d.IdProdutoBaseTroca)
                .HasConstraintName("FK_PRPRODUT_REFERENCE_PRPRODUT");
        });

        modelBuilder.Entity<TbCidade>(entity =>
        {
            entity.HasKey(e => e.IdCidade);

            entity.ToTable("tbCidade");

            entity.Property(e => e.IdCidade).ValueGeneratedNever();
            entity.Property(e => e.CodigoIbge).HasColumnName("CodigoIBGE");
            entity.Property(e => e.IdEstado)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Nome)
                .HasMaxLength(255)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TbEntidade>(entity =>
        {
            entity.HasKey(e => e.IdEntidade);

            entity.ToTable("tbEntidade");

            entity.Property(e => e.IdEntidade)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Bairro)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.BairroCob)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Cep)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CEP");
            entity.Property(e => e.Cepcob)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("CEPCob");
            entity.Property(e => e.Cnae)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CNAE");
            entity.Property(e => e.Complemento)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.ComplementoCob)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.DataCadastro).HasColumnType("datetime");
            entity.Property(e => e.DataFundacao).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.Endereco)
                .HasMaxLength(511)
                .IsUnicode(false)
                .HasComputedColumnSql("((isnull([Logradouro],'')+case when isnull([Numero],'')<>'' then ' ' else '' end)+isnull([Numero],''))", false);
            entity.Property(e => e.EnderecoCob)
                .HasMaxLength(511)
                .IsUnicode(false)
                .HasComputedColumnSql("(([LogradouroCob]+case when [NumeroCob]<>'' then ' ' else '' end)+[NumeroCob])", false);
            entity.Property(e => e.IdTipoEntidade)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Latitude)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Logradouro)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.LogradouroCob)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Longitude)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NomeFantasia)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumCnpj)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("NumCNPJ");
            entity.Property(e => e.NumInscricaoEstadual)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumInscricaoMunicipal)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumSuframa)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Numero)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NumeroCob)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PaginaWeb)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.RazaoSocial)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TelefoneCel)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TelefoneCob)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TelefoneCom1)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TelefoneCom2)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TelefoneRes)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TelefoneWhatsApp)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.TipoIndicadorIe)
                .HasComment("1=Contribuinte ICMS (informar a IE do destinatário);\r\n   2=Contribuinte isento de Inscrição no cadastro de Contribuintes do ICMS;\r\n   9=Não Contribuinte, que pode ou não possuir Inscrição Estadual no Cadastro de Contribuintes do ICMS;")
                .HasColumnName("TipoIndicadorIE");
            entity.Property(e => e.TipoPessoa)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasComment("Somente diferenciação para tipo de pessoa:\r\n   \r\n   Juridica\r\n   Fisica\r\n   \r\n   ");
            entity.Property(e => e.TipoRegimeApuracao).HasComment("Tipo de Regime de Apuração\r\n   \r\n   0 - SIMPLES NACIONAL\r\n   1 - NORMAL - REGIME PERIÓDICO DE APURAÇÃO\r\n      \r\n   ");
            entity.Property(e => e.ValorDespesas).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorFrete).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ValorLimiteCreditoUnificado).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Versao)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.IdCidadeNavigation).WithMany(p => p.TbEntidadeIdCidadeNavigation)
                .HasForeignKey(d => d.IdCidade)
                .HasConstraintName("FK_tbEntidade_tbCidade");

            entity.HasOne(d => d.IdCidadeCobNavigation).WithMany(p => p.TbEntidadeIdCidadeCobNavigation)
                .HasForeignKey(d => d.IdCidadeCob)
                .HasConstraintName("FK_tbEntidade_tbcidadeCob");

            entity.HasOne(d => d.IdCondPagtoNavigation).WithMany(p => p.TbEntidade)
                .HasForeignKey(d => d.IdCondPagto)
                .HasConstraintName("FK_tbEntidade_ftCondPagto");
        });

        modelBuilder.Entity<ViewEstoqueProduto>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("ViewEstoqueProduto");

            entity.Property(e => e.QuantEstoque).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.QuantEstoqueDisponivel).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.QuantEstoqueTransito).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.QuantPedidoAberto).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.QuantPendenciaVenda).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.QuantReservadaEnt).HasColumnType("decimal(38, 3)");
            entity.Property(e => e.QuantReservadaSai).HasColumnType("decimal(38, 3)");
        });

        modelBuilder.Entity<ViewPedidosComDevolucao>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("ViewPedidosComDevolucao");

            entity.Property(e => e.DataFaturamento).HasColumnType("datetime");
            entity.Property(e => e.DataMovimento).HasColumnType("datetime");
            entity.Property(e => e.IdEntidade)
                .HasMaxLength(10)
                .IsUnicode(false);
        });
        modelBuilder.HasSequence("CELOCALIZACAOLOTE_00").StartsAt(56864L);
        modelBuilder.HasSequence("CELOTEPRODUTO_00").StartsAt(56864L);
        modelBuilder.HasSequence("CEMOVESTOQUE_00").StartsAt(427077L);
        modelBuilder.HasSequence("FINOTAFISCAL_01").StartsAt(27683L);
        modelBuilder.HasSequence("fiNotaFiscal_02");
        modelBuilder.HasSequence("fiNotaFiscal_10");
        modelBuilder.HasSequence("fiNotaFiscal_12");
        modelBuilder.HasSequence("fiNotaFiscalItem_00");
        modelBuilder.HasSequence("fiSequenciaLoteNFe_01");
        modelBuilder.HasSequence("fiSequenciaLoteNFe_02");
        modelBuilder.HasSequence("FNMOVCAIXA_00");
        modelBuilder.HasSequence("FNMOVCRIACAO_00");
        modelBuilder.HasSequence("FNMOVFINANCEIRO_01").StartsAt(46L);
        modelBuilder.HasSequence("FNMOVFINANCEIRO_02");
        modelBuilder.HasSequence("FNMOVFINANCEIRO_03");
        modelBuilder.HasSequence("FNMOVFINANCEIRO_04");
        modelBuilder.HasSequence("FNMOVFINANCEIRO_05");
        modelBuilder.HasSequence("FNMOVFINANCEIRO_10");
        modelBuilder.HasSequence("FNMOVFINANCEIRO_12");
        modelBuilder.HasSequence("FNMOVTITULO_00");
        modelBuilder.HasSequence("FNTITULO_01").StartsAt(102862L);
        modelBuilder.HasSequence("FNTITULO_02");
        modelBuilder.HasSequence("FNTITULO_03");
        modelBuilder.HasSequence("FNTITULO_04");
        modelBuilder.HasSequence("FNTITULO_05");
        modelBuilder.HasSequence("fnTitulo_10");
        modelBuilder.HasSequence("fnTitulo_12");
        modelBuilder.HasSequence("FTPEDIDO_01");
        modelBuilder.HasSequence("FTPEDIDO_02");
        modelBuilder.HasSequence("FTPEDIDO_10");
        modelBuilder.HasSequence("FTPEDIDO_12");
        modelBuilder.HasSequence("FTPEDIDOITEM_00");
        modelBuilder.HasSequence("PRPRODUTO_00").StartsAt(82109L);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

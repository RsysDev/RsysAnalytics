select
  cast(aux.CountPedido as int) as CountPedido,
  cast(aux.CountNota as int) as CountNota,
  cast(aux.Ordem as varchar(255)) as Ordem,
  cast(aux.IdEmpresa as int) as IdEmpresa,
  cast(aux.TipoPedidoNota as varchar(100)) as TipoPedidoNota,
  cast(aux.IdPedido as int) as IdPedido,
  cast(aux.EntidadeCodigo as varchar(50)) as EntidadeCodigo,
  cast(aux.EntidadeRazaoSocial as varchar(255)) as EntidadeRazaoSocial,
  aux.DataMovimento,
  cast(aux.VendedorInterno as int) as VendedorInterno,
  cast(aux.VendedorExterno as int) as VendedorExterno,
  cast(aux.CondicaoPagto as varchar(255)) as CondicaoPagto,
  aux.DataPrevisaoEntrega,
  cast(aux.StatusPedido as int) as StatusPedido,
  cast(aux.PMV as decimal(18,4)) as PMV,
  cast(aux.IdOperacao as int) as IdOperacao,
  aux.DataFaturamento,
  cast(aux.NotaFiscal as varchar(50)) as NotaFiscal,
  cast(aux.ValorProdutos as decimal(18,4)) as ValorProdutos,
  cast(aux.ValorServicos as decimal(18,4)) as ValorServicos,
  cast(aux.ValorSeguro as decimal(18,4)) as ValorSeguro,
  cast(aux.ValorFrete as decimal(18,4)) as ValorFrete,
  cast(aux.ValorSeguroDespesas as decimal(18,4)) as ValorSeguroDespesas,
  cast(aux.ValorDesconto as decimal(18,4)) as ValorDesconto,
  cast(aux.ValorProdutosComDevolucao as decimal(18,4)) as ValorProdutosComDevolucao,
  cast(aux.ValorServicosComDevolucao as decimal(18,4)) as ValorServicosComDevolucao,
  cast(aux.ValorSeguroComDevolucao as decimal(18,4)) as ValorSeguroComDevolucao,
  cast(aux.ValorFreteComDevolucao as decimal(18,4)) as ValorFreteComDevolucao,
  cast(aux.ValorSeguroDespesasComDevolucao as decimal(18,4)) as ValorSeguroDespesasComDevolucao,
  cast(aux.ValorDescontoComDevolucao as decimal(18,4)) as ValorDescontoComDevolucao,
  cast(aux.DevValorProdutos as decimal(18,4)) as DevValorProdutos,
  cast(aux.DevValorServicos as decimal(18,4)) as DevValorServicos,
  cast(aux.DevValorSeguro as decimal(18,4)) as DevValorSeguro,
  cast(aux.DevValorFrete as decimal(18,4)) as DevValorFrete,
  cast(aux.DevValorSeguroDespesas as decimal(18,4)) as DevValorSeguroDespesas,
  cast(aux.DevValorDesconto as decimal(18,4)) as DevValorDesconto,
  cast(aux.ValorCustoFor as decimal(18,4)) as ValorCustoFor,
  cast(aux.ValorOutrasDespesas as decimal(18,4)) as ValorOutrasDespesas,
  cast(aux.DescStatusPedido as varchar(100)) as DescStatusPedido,
  cast(aux.TipoPedido as int) as TipoPedido,
  cast(aux.DescTipoPedido as varchar(100)) as DescTipoPedido,
  cast(aux.NomeVendedorInterno as varchar(255)) as NomeVendedorInterno,
  cast(aux.NomeVendedorExterno as varchar(255)) as NomeVendedorExterno,
  cast(aux.NomeVendedorParticipante as varchar(max)) as NomeVendedorParticipante,
  cast(aux.ValorVenda as decimal(18,4)) as ValorVenda,
  cast(aux.ValorVendaComDevolucao as decimal(18,4)) as ValorVendaComDevolucao,
  cast(aux.ValorTotalPedido as decimal(18,4)) as ValorTotalPedido,
  cast(aux.ValorTotalPedidoComDevolucao as decimal(18,4)) as ValorTotalPedidoComDevolucao,
  cast(aux.ValorTotalDevolucao as decimal(18,4)) as ValorTotalDevolucao,
  cast(aux.OperacaoFiscal as varchar(255)) as OperacaoFiscal,
  cast(aux.TipoMovimentoOperacao as varchar(100)) as TipoMovimentoOperacao,
  cast(aux.Margem as decimal(18,4)) as Margem,
  cast(aux.ExibeMargem as bit) as ExibeMargem,
  cast(aux.QuebraVendedorDescricao as varchar(500)) as QuebraVendedorDescricao,
  cast(aux.ValorBaseComissao as decimal(18,4)) as ValorBaseComissao,
  cast(aux.ValorComissao as decimal(18,4)) as ValorComissao,
  cast(aux.IdCidade as int) as IdCidade,
  cast(aux.NomeCidade as varchar(255)) as NomeCidade,
  cast(aux.Estado as varchar(20)) as Estado,
  cast(aux.DescricaoLoja as varchar(500)) as DescricaoLoja
from
(
  /* ===== Pedidos ===== */
  select
    1 as CountPedido,
    0 as CountNota,
    ped.IdPedido as Ordem,
    ped.IdEmpresa as IdEmpresa,
    viewPed.Tipo as TipoPedidoNota,
    ped.IdPedido as IdPedido,
    case when EC.IdEntidade is null then ent.IdEntidade else EC.IdEntidade end as EntidadeCodigo,
    case when EC.IdEntidade is null then ent.RazaoSocial else '*' + EC.RazaoSocial end as EntidadeRazaoSocial,
    ped.DataMovimento as DataMovimento,
    ped.IdVendedorInterno as VendedorInterno,
    ped.IdVendedorExterno as VendedorExterno,
    condPagto.Descricao as CondicaoPagto,
    ped.DataPrevisaoEntrega as DataPrevisaoEntrega,
    ped.StatusPedido as StatusPedido,
    ped.PMV as PMV,
    ped.IdOperacao as IdOperacao,
    ped.DataFaturamento as DataFaturamento,
    (select top 1 nf.NumeroDocumento
       from fiNotaFiscalPedido nfPed left join fiNotaFiscal nf on nf.IdNotaFiscal = nfPed.IdNotaFiscal
      where nfPed.idpedido = ped.IdPedido) as NotaFiscal,
    isnull(ped.ValorProdutos, 0) as ValorProdutos,
    isnull(ped.ValorServicos, 0) as ValorServicos,
    isnull(ped.ValorSeguro, 0) as ValorSeguro,
    isnull(ped.ValorFrete, 0) as ValorFrete,
    isnull(ped.ValorSeguro, 0) + isnull(ped.ValorDespesas, 0) as ValorSeguroDespesas,
    isnull(ped.ValorDescProdutos, 0) + isnull(ped.ValorDescServicos, 0) as ValorDesconto,
    isnull(ped.ValorProdutos, 0) as ValorProdutosComDevolucao,
    isnull(ped.ValorServicos, 0) as ValorServicosComDevolucao,
    isnull(ped.ValorSeguro, 0) as ValorSeguroComDevolucao,
    isnull(ped.ValorFrete, 0) as ValorFreteComDevolucao,
    isnull(ped.ValorSeguro, 0) + isnull(ped.ValorDespesas, 0) as ValorSeguroDespesasComDevolucao,
    isnull(ped.ValorDescProdutos, 0) + isnull(ped.ValorDescServicos, 0) as ValorDescontoComDevolucao,
    0 as DevValorProdutos,
    0 as DevValorServicos,
    0 as DevValorSeguro,
    0 as DevValorFrete,
    0 as DevValorSeguroDespesas,
    0 as DevValorDesconto,
    case when usuario.TipoCustoMargem = 0 or usuario.TipoCustoMargem = 1
         then isnull(ped.ValorCustoFor, 0) + isnull(ped.ValorCustoSer, 0)
         else 0 end as ValorCustoFor,
    isnull(ped.ValorOutrasDespesas, 0) as ValorOutrasDespesas,
    case when isnull(ped.StatusPedido, 0) = 0 then 'Em Digitação'
         when ped.StatusPedido = 1 then 'Digitado'
         when ped.StatusPedido = 2 then 'Faturamento Parcial'
         when ped.StatusPedido = 3 then 'Faturado'
         when ped.StatusPedido = 4 then 'Cancelado'
         else '' end as DescStatusPedido,
    ped.TipoPedido as TipoPedido,
    case when ped.TipoPedido = 0 then 'Pedido'
         when ped.TipoPedido = 1 then 'Cotação'
         when ped.TipoPedido = 2 then 'Pendência'
    end as DescTipoPedido,
    vendInt.Nome as NomeVendedorInterno,
    vendExt.Nome as NomeVendedorExterno,
    stuff(
      isnull((select ', ' + auxPart.Nome from (
                select cast(isnull(Vend.IdVendedor, 0) as varchar(255)) + ' - ' + Vend.Nome as Nome
                  from ftpedidovendedor vped
                  left join ftVendedor Vend on Vend.IdVendedor = vped.IdVendedor
                 where vped.IdPedido = ped.IdPedido) auxPart
               group by auxPart.Nome
               for xml path ('')), ''),
      1, 1, '') as NomeVendedorParticipante,
    (isnull(ped.ValorProdutos, 0) + isnull(ped.ValorServicos, 0) - isnull(ped.ValorDescProdutos, 0) - isnull(ped.ValorDescServicos, 0)) as ValorVenda,
    (isnull(ped.ValorProdutos, 0) + isnull(ped.ValorServicos, 0) - isnull(ped.ValorDescProdutos, 0) - isnull(ped.ValorDescServicos, 0)) as ValorVendaComDevolucao,
    ped.ValorTotalPedido as ValorTotalPedido,
    ped.ValorTotalPedido as ValorTotalPedidoComDevolucao,
    0 as ValorTotalDevolucao,
    op.Descricao as OperacaoFiscal,
    case when op.TipoMovimento = 0 then 'Venda/Compra'
         when op.TipoMovimento = 1 then 'Devolução'
         when op.TipoMovimento = 2 then 'Diversos'
         when op.TipoMovimento = 3 then 'Transferência'
    end as TipoMovimentoOperacao,
    case when usuario.TipoCustoMargem in (0, 2) then dbo.CalcularMargemLucroPedidoNF(0, ped.IdPedido, 1) else 0 end as Margem,
    case when usuario.TipoCustoMargem in (0, 2) then 1 else 0 end as ExibeMargem,
    case when @QuebraVendedor = 0 then 'Interno: ' + cast(ped.IdVendedorInterno as varchar) + ' - ' + vendInt.Nome
         when @QuebraVendedor = 1 then 'Externo: ' + cast(ped.IdVendedorExterno as varchar) + ' - ' + vendExt.Nome
         else '' end as QuebraVendedorDescricao,
    case when (select ChaveIdentificacao from fwempresa where IdEmpresa = @IdEmpresaChave) = 'IMPERIOPECAS' and isnull(fwusuario.FlagAdministrador, 0) = 0 then 0
         else case when op.TipoMovimento = 0 then
                case when @QuebraVendedor = 0 then isnull(ped.ValorBaseComissaoInterno, 0)
                     when @QuebraVendedor = 1 then isnull(ped.ValorBaseComissaoExterno, 0)
                     else null end
              end
    end as ValorBaseComissao,
    case when (select ChaveIdentificacao from fwempresa where IdEmpresa = @IdEmpresaChave) = 'IMPERIOPECAS' and isnull(fwusuario.FlagAdministrador, 0) = 0 then 0
         else case when op.TipoMovimento = 0 then
                case when @QuebraVendedor = 0 then isnull(ped.ValorComissaoInterno, 0)
                     when @QuebraVendedor = 1 then isnull(ped.ValorComissaoExterno, 0)
                     else null end
              end
    end as ValorComissao,
    cid.IdCidade as IdCidade,
    cid.Nome as NomeCidade,
    cid.IdEstado as Estado,
    case when isnull(ped.IdLojaPlataforma, 0) > 0 then plat.Descricao + ' - ' + lp.Descricao else '' end as DescricaoLoja
  from viewPedidosComDevolucao viewPed
  left join ftPedido ped on ped.IdPedido = viewPed.IdPedido
  left join tbEntidade ent on ent.IdEntidade = ped.IdEntidade
  left join ftCondPagto condPagto on condPagto.IdCondPagto = ped.IdCondPagto
  left join ftVendedor vendInt on ped.IdVendedorInterno = vendInt.IdVendedor
  left join ftVendedor vendExt on ped.IdVendedorExterno = vendExt.IdVendedor
  left join ftOperacao op on ped.IdOperacao = op.IdOperacao
  left join tbCidade cid on cid.IdCidade = ent.IdCidade
  left join tbUsuario usuario on usuario.IdUsuario = @IdUsuario
  left join fwUsuario fwusuario on fwusuario.IdUsuario = @IdUsuario
  left join plLojaPlataforma lp on lp.IdPlataforma = ped.IdPlataforma and lp.IdLojaPlataforma = ped.IdLojaPlataforma
  left join plSincPlataforma plat on plat.IdPlataforma = ped.IdPlataforma
  left join tbEntidade EC on EC.IdEntidade = (select top 1 e1.IdEntidade
                                               from tbEntidadeColigada e1
                                              where e1.IdColigacao = ent.IdColigacao
                                                and e1.FlagEntidadePrincipal = 1)
  where ped.IdEmpresa in ({{EMPRESAS}})
    and op.TipoMovimento in ({{TIPOS_MOVIMENTO}})
    and ped.StatusPedido <> 4
    and (@TipoPedido = 999 or ped.TipoPedido = @TipoPedido)
    and (@FlagPendencia = -1 or isnull(ped.FlagPendencia, 0) = @FlagPendencia)
    and (@DataInicio is null or ped.DataMovimento >= @DataInicio)
    and (@DataFim is null or ped.DataMovimento < dateadd(day, 1, @DataFim))

  union all

  /* ===== Notas fiscais de devolução ===== */
  select
    0 as CountPedido,
    1 as CountNota,
    cast(nf.IdNotaFiscal as varchar(255)) as Ordem,
    nf.IdEmpresa as IdEmpresa,
    viewPed.Tipo as TipoPedidoNota,
    0 as IdPedido,
    case when EC.IdEntidade is null then ent.IdEntidade else EC.IdEntidade end as EntidadeCodigo,
    case when EC.IdEntidade is null then ent.RazaoSocial else '*' + EC.RazaoSocial end as EntidadeRazaoSocial,
    nf.DataMovimento as DataMovimento,
    VendInt.IdVendedor as VendedorInterno,
    VendExt.IdVendedor as VendedorExterno,
    null as CondicaoPagto,
    null as DataPrevisaoEntrega,
    null as StatusPedido,
    nf.PMV as PMV,
    nf.IdOperacao as IdOperacao,
    nf.DataMovimento as DataFaturamento,
    nf.NumeroDocumento as NotaFiscal,
    0 as ValorProdutos,
    0 as ValorServicos,
    0 as ValorSeguro,
    0 as ValorFrete,
    0 as ValorSeguroDespesas,
    0 as ValorDesconto,
    isnull(nf.ValorProdutos, 0) * -1 as ValorProdutosComDevolucao,
    isnull(0, 0) as ValorServicosComDevolucao,
    isnull(nf.ValorSeguro, 0) * -1 as ValorSeguroComDevolucao,
    isnull(nf.ValorFrete, 0) * -1 as ValorFreteComDevolucao,
    (isnull(nf.ValorSeguro, 0) + isnull(nf.ValorDespesas, 0)) * -1 as ValorSeguroDespesasComDevolucao,
    (isnull(nf.ValorDescProdutos, 0) + isnull(nf.ValorDescServicos, 0)) * -1 as ValorDescontoComDevolucao,
    isnull(nf.ValorProdutos, 0) * -1 as DevValorProdutos,
    0 as DevValorServicos,
    isnull(nf.ValorSeguro, 0) * -1 as DevValorSeguro,
    isnull(nf.ValorFrete, 0) * -1 as DevValorFrete,
    (isnull(nf.ValorSeguro, 0) + isnull(nf.ValorDespesas, 0)) * -1 as DevValorSeguroDespesas,
    (isnull(nf.ValorDescProdutos, 0) + isnull(nf.ValorDescServicos, 0)) * -1 as DevValorDesconto,
    case when isnull(nfVend.ValorNotaProporcional, 0) > 0
         then (round(isnull(nfVend.ValorNotaProporcional, 0) / isnull(nf.ValorTotalNota, 0) * isnull(nf.ValorCustoFor, 0), 2)) * -1
         else isnull(nf.ValorCustoFor, 0) * -1
    end as ValorCustoFor,
    isnull(0, 0) as ValorOutrasDespesas,
    null as DescStatusPedido,
    null as TipoPedido,
    null as DescTipoPedido,
    VendInt.Nome as NomeVendedorInterno,
    VendExt.Nome as NomeVendedorExterno,
    VenParc.Nome as NomeVendedorParticipante,
    0 as ValorVenda,
    case when isnull(nfVend.ValorNotaProporcional, 0) > 0
         then isnull(nfVend.ValorNotaProporcional, 0) * -1
         else (isnull(nf.ValorProdutos, 0) + isnull(nf.ValorServicos, 0) - isnull(nf.ValorDescProdutos, 0) - isnull(nf.ValorDescServicos, 0)) * -1
    end as ValorVendaComDevolucao,
    0 as ValorTotalPedido,
    case when isnull(nfVend.ValorNotaProporcional, 0) > 0
         then isnull(nfVend.ValorNotaProporcional, 0) * -1
         else isnull(nf.ValorTotalNota, 0) * -1
    end as ValorTotalPedidoComDevolucao,
    case when isnull(nfVend.ValorNotaProporcional, 0) > 0
         then isnull(nfVend.ValorNotaProporcional, 0) * -1
         else (isnull(nf.ValorProdutos, 0) + isnull(nf.ValorServicos, 0) - isnull(nf.ValorDescProdutos, 0) - isnull(nf.ValorDescServicos, 0)) * -1
    end as ValorTotalDevolucao,
    op.Descricao as OperacaoFiscal,
    case when op.TipoMovimento = 0 then 'Venda/Compra'
         when op.TipoMovimento = 1 then 'Devolução'
         when op.TipoMovimento = 2 then 'Diversos'
         when op.TipoMovimento = 3 then 'Transferência'
    end as TipoMovimentoOperacao,
    case when usuario.TipoCustoMargem in (0, 2) then dbo.CalcularMargemLucroPedidoNF(1, nf.IdNotaFiscal, 0) else 0 end as Margem,
    case when usuario.TipoCustoMargem in (0, 2) then 1 else 0 end as ExibeMargem,
    case when @QuebraVendedor = 0 then 'Interno: ' + cast(VendInt.IdVendedor as varchar) + ' - ' + VendInt.Nome
         when @QuebraVendedor = 1 then 'Externo: ' + cast(VendExt.IdVendedor as varchar) + ' - ' + VendExt.Nome
         else '' end as QuebraVendedorDescricao,
    0 as ValorBaseComissao,
    0 as ValorComissao,
    cid.IdCidade as IdCidade,
    cid.Nome as NomeCidade,
    cid.IdEstado as Estado,
    '' as DescricaoLoja
  from fiNotaFiscal nf
  left join viewPedidosComDevolucao viewPed on nf.IdNotaFiscal = viewPed.IdNotaFiscal
  left join fiNotaFiscalVendedor nfVend on nfVend.IdNotaFiscal = nf.IdNotaFiscal
  left join tbEntidade ent on ent.IdEntidade = nf.IdEntidade
  left join ftOperacao op on nf.IdOperacao = op.IdOperacao
  left join ftVendedor VendInt on VendInt.IdVendedor = nfVend.IdVendedor and VendInt.TipoVendedor = 'Interno'
  left join ftVendedor VendExt on VendExt.IdVendedor = nfVend.IdVendedor and VendExt.TipoVendedor = 'Externo'
  left join ftVendedor VenParc on VenParc.IdVendedor = nfVend.IdVendedor and nfVend.TipoVendedor = 2 and @QuebraVendedor = 2
  left join tbCidade cid on cid.IdCidade = ent.IdCidade
  left join tbAtividade Ativ on Ativ.IdAtividade = ent.IdAtividade
  left join tbEntidade EC on EC.IdEntidade = null
  left join tbUsuario usuario on usuario.IdUsuario = @IdUsuario
  where nf.IdEmpresa in ({{EMPRESAS}})
    and (nf.TipoMovimento = 1 and op.TipoMovimento = 1)
    and @IncluirDevolucoes = 1
    and (@DataInicio is null or nf.DataMovimento >= @DataInicio)
    and (@DataFim is null or nf.DataMovimento < dateadd(day, 1, @DataFim))
) as aux

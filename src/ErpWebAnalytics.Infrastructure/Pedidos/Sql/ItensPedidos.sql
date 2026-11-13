select
  cast(ped.IdPedido as int) as IdPedido,
  cast(isnull(item.IdProduto, item.IdServico) as int) as IdProdutoServico,
  cast(item.Descricao as varchar(500)) as Descricao,
  cast(est.QuantEstoque as decimal(18,4)) as QuantEstoque,
  cast(est.QuantEstoqueDisponivel as decimal(18,4)) as QuantDisponivel,
  cast(item.QuantPedida as decimal(18,4)) as QuantPedida,
  cast(item.QuantAtendida as decimal(18,4)) as QuantAtendida,
  cast(item.QuantPedida - item.QuantAtendida as decimal(18,4)) as QuantSaldo,
  cast(item.PercDesconto as decimal(18,4)) as PercDesconto,
  cast(item.ValorLiquido as decimal(18,4)) as ValorLiquido,
  cast(case when usuario.TipoCustoMargem = 0 or usuario.TipoCustoMargem = 1
            then item.ValorCustoFor
            else 0 end as decimal(18,4)) as ValorCustoFor,
  cast((item.QuantPedida - item.QuantAtendida) * item.ValorLiquido as decimal(18,4)) as ValorTotal,
  cast(p.Medida as varchar(100)) as Medida,
  cast(dbo.CalcularMargemLucroPedidoNFItem(0, item.IdPedidoItem) as decimal(18,4)) as Margem,
  cast(p.NumFabricante as varchar(255)) as NumFabricante
from ftPedido ped
join ftPedidoItem item on item.IdPedido = ped.IdPedido
left join prProduto p on p.IdProduto = item.IdProduto
left join tbEntidade ent on ent.IdEntidade = ped.IdEntidade
left join ViewEstoqueProduto est on item.IdProduto = est.IdProduto and est.IdEmpresaEstoque = ped.IdEmpresa
left join tbUsuario usuario on usuario.IdUsuario = @IdUsuario
left join ftVendedor vendInt on ped.IdVendedorInterno = vendInt.IdVendedor
left join ftVendedor vendExt on ped.IdVendedorExterno = vendExt.IdVendedor
where ped.IdEmpresa in ({{EMPRESAS}})
  and ped.StatusPedido <> 4
  and (@TipoPedido = 999 or ped.TipoPedido = @TipoPedido)
  and (@FlagPendencia = -1 or isnull(ped.FlagPendencia, 0) = @FlagPendencia)
  and (@DataInicio is null or ped.DataMovimento >= @DataInicio)
  and (@DataFim is null or ped.DataMovimento < dateadd(day, 1, @DataFim))

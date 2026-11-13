-- Comando migrado do relatório legado fwRelatorio.IdRelatorio = 'InventariodeEstoque' (Inventario de Estoque).
-- Rodar depois de Analytics.ComandoSQL.sql. Para reexecutar:
--   DELETE Analytics.ComandoSQL WHERE ChaveEmpresa = 'FUSCAOPRETO' AND Id = 'InventarioDeEstoque';
--
-- Como foi convertido (XML -> JSON):
--  * Dois comandos do legado viram dois resultados nomeados: InventarioDeEstoque e EmpresaMestre. O preparo do saldo
--    (delete/insert/update/exec em TempEstoqueRetroativo) é o passo 1, "somente execução": GRAVA no banco, então o comando
--    só roda em executar_comando_com_escrita (Comandos:PermitirEscrita=true) e o login do banco precisa dessa permissão.
--    Na tela de manutenção o teste roda numa transação que é desfeita no fim.
--  * `in [Empresas]` (lista colada no texto) virou {Empresas as coluna} (condição parametrizada).
--  * '[X]' (valor entre aspas colado) virou [X] (parâmetro). [Ordem] (nome de coluna colado) virou a regra OrdemColuna,
--    com lista fechada de colunas.
--  * Regras $SE/$SENAOSE/$SENAO viraram casos estruturados. Padrões (padrao) 0 nos parâmetros numéricos que as regras
--    comparam, porque aqui parâmetro sem valor é "vazio" e não 0. DataSaldoEstoque vazio = hoje (isnull no SQL).
--  * [IdEmpresa] do EmpresaMestre (empresa logada) virou a lista de empresas do servidor (Painel:IdsEmpresa).
--  * Não migradas: as regras FonteEstoqueNovo e UtilizaPedidosAbertos (nenhum comando as usa; a primeira referencia o alias PAux, inexistente).

INSERT Analytics.ComandoSQL (ChaveEmpresa, Id, Descricao, Parametros, Regras, [SQL], Ativa, MaxLinhas, TimeoutSeg)
VALUES (
 'FUSCAOPRETO',
 'InventarioDeEstoque',
 N'Inventário de estoque (migrado do relatório legado InventariodeEstoque): saldo e custo por produto, com quebra opcional por grupo/marca/fornecedor/linha, retroativo por data ou a partir de um registro de inventário. Resultados: InventarioDeEstoque (uma linha por produto) e EmpresaMestre (cabeçalho da empresa). ATENÇÃO: grava em TempEstoqueRetroativo, então só roda em executar_comando_com_escrita (Comandos:PermitirEscrita).',
 N'[
 {
  "id": "Empresas",
  "tipo": "inteiro",
  "valorUnico": false,
  "campo": "est.IdEmpresaEstoque",
  "origem": "servidor",
  "chave": "idsEmpresa"
 },
 {
  "id": "IdUsuario",
  "tipo": "inteiro",
  "valorUnico": true,
  "origem": "servidor",
  "chave": "idUsuario"
 },
 {
  "id": "TipodeRelatorio",
  "tipo": "string",
  "valorUnico": true,
  "descricao": "S=Sintético; R=Resumido (só muda o título; o layout é do consumidor).",
  "padrao": "S"
 },
 {
  "id": "Quebra",
  "tipo": "inteiro",
  "valorUnico": true,
  "descricao": "0=Nenhuma; 1=Grupo; 2=Marca; 3=Fornecedor; 4=Linha",
  "padrao": "0"
 },
 {
  "id": "Ordem",
  "tipo": "string",
  "valorUnico": true,
  "campo": "Ordem",
  "descricao": "Coluna de ordenação: pr.IdProduto; pr.Descricao; pr.NumOriginal; pr.NumFabricante",
  "padrao": "pr.Descricao"
 },
 {
  "id": "TipoEstoque",
  "tipo": "inteiro",
  "valorUnico": true,
  "descricao": "0=Apenas com saldo em estoque; 1=Todos os itens; 2=Apenas itens negativos",
  "padrao": "0"
 },
 {
  "id": "TipoCusto",
  "tipo": "inteiro",
  "valorUnico": true,
  "descricao": "0=Fornecedor; 1=Médio; 2=Última compra; 3=Última compra real",
  "padrao": "0"
 },
 {
  "id": "DataSaldoEstoque",
  "tipo": "date",
  "valorUnico": true,
  "descricao": "Data do saldo retroativo (yyyy-MM-dd). Vazio = hoje. Ignorada se RegistroInventario for informado."
 },
 {
  "id": "RegistroInventario",
  "tipo": "inteiro",
  "valorUnico": true,
  "campo": "ri.IdRegistroInventario",
  "descricao": "Id do registro de inventário de origem. 0 = saldo retroativo pela data.",
  "padrao": "0"
 },
 {
  "id": "ConsiderarPedidoAberto",
  "tipo": "checkbox",
  "valorUnico": true,
  "campo": "FlagPedidosAberto",
  "descricao": "Abate do saldo os pedidos em aberto.",
  "padrao": "true"
 },
 {
  "id": "prFornecedor",
  "tipo": "string",
  "valorUnico": false,
  "campo": "pr.Idfornecedor",
  "descricao": "Código(s) do fornecedor"
 },
 {
  "id": "prGrupo",
  "tipo": "string",
  "valorUnico": false,
  "campo": "pr.idGrupo",
  "descricao": "Grupo(s) de produto"
 },
 {
  "id": "prMarca",
  "tipo": "string",
  "valorUnico": false,
  "campo": "pr.IdMarca",
  "descricao": "Marca(s)"
 },
 {
  "id": "Linha",
  "tipo": "string",
  "valorUnico": false,
  "campo": "pr.IdLinha",
  "descricao": "Linha(s)"
 },
 {
  "id": "prClassificacao",
  "tipo": "string",
  "valorUnico": false,
  "campo": "pr.IdClassificacao",
  "descricao": "Classificação(ões) fiscal"
 },
 {
  "id": "ExibirNumero",
  "tipo": "string",
  "valorUnico": true,
  "descricao": "F=Nr. Fabricante; O=Nr. Original (padrão O)"
 },
 {
  "id": "prProduto",
  "tipo": "inteiro",
  "valorUnico": false,
  "campo": "pr.IdProduto",
  "descricao": "Código(s) do produto"
 },
 {
  "id": "TipoBloqueio",
  "tipo": "inteiro",
  "valorUnico": true,
  "campo": "TipoBloqueio",
  "descricao": "0=Todos; 1=Apenas itens permitidos para venda (sem bloqueio)",
  "padrao": "0"
 },
 {
  "id": "LayoutSelecionado",
  "tipo": "string",
  "valorUnico": true,
  "descricao": "Use InventariocomImposto para trazer os dados da última compra (nota, data, CFOP, ICMS)."
 }
]',
 N'[
 {
  "id": "TipoEstoque",
  "casos": [
   {
    "se": [
     {
      "param": "TipoEstoque",
      "op": "igual",
      "valor": "0"
     }
    ],
    "sql": "and IsNull(Aux.SaldoEstoque,0) > 0 --Apenas com Saldo Positivo ou Negativo"
   },
   {
    "se": [
     {
      "param": "TipoEstoque",
      "op": "igual",
      "valor": "1"
     }
    ],
    "sql": "--Todos"
   },
   {
    "se": [
     {
      "param": "TipoEstoque",
      "op": "igual",
      "valor": "2"
     }
    ],
    "sql": "and IsNull(Aux.SaldoEstoque,0) < 0  --Apenas itens Negativos"
   }
  ]
 },
 {
  "id": "TipoBloqueio",
  "casos": [
   {
    "se": [
     {
      "param": "TipoBloqueio",
      "op": "igual",
      "valor": "0"
     }
    ],
    "sql": "--Todos itens independente de bloqueio"
   },
   {
    "se": [
     {
      "param": "TipoBloqueio",
      "op": "igual",
      "valor": "1"
     }
    ],
    "sql": "and IsNull(pr.FlagBloqVenda,0) = 0 --Apenas itens sem bloqueio"
   }
  ]
 },
 {
  "id": "ExisteInventario",
  "casos": [
   {
    "se": [
     {
      "param": "RegistroInventario",
      "op": "diferente",
      "valor": "0"
     }
    ],
    "sql": "and exists(Select * from ceRegistroInventarioItem I\n        Where I.IdRegistroInventario = [RegistroInventario]\n          and I.IdProduto = Pr.IdProduto)"
   }
  ]
 },
 {
  "id": "AjustaEstoqueRetroativo",
  "casos": [
   {
    "se": [
     {
      "param": "RegistroInventario",
      "op": "igual",
      "valor": "0"
     }
    ],
    "sql": "update TempEstoqueRetroativo\n     set DataPosicaoEstoque = isnull([DataSaldoEstoque], cast(getdate() as date))\n    where ChaveSequencia = @ChaveSequenciaTemp\n   \n   exec ceCalculoEstoqueRetroativo @ChaveSequenciaTemp, [ConsiderarPedidoAberto]"
   }
  ],
  "senao": "--Origem do inventario\nUpdate TempEstoqueRetroativo\n   Set DataPosicaoEstoque = Aux.DataInventario\n       ,QuantEstoque = Aux.QuantEstoque\n       ,QuantDisponivel = Aux.QuantEstoque                  \n       ,ValorCustoFor = Aux.ValorCustoFor\n       ,ValorCustoMed = Aux.ValorCustoMed\n       ,ValorCustoUlt = Aux.ValorCustoUlt\n       ,ValorCustoUltReal = Aux.ValorCustoUltReal\n  from (Select ri.IdProduto, I.IdempresaEstoque, \n               I.DataInventario,  ri.QuantEstoque, \n               ri.ValorCustoFor, ri.ValorCustoMed, ri.ValorCustoUlt, ri.ValorCustoUltReal\n         from ceRegistroInventario I\n         left join ceRegistroInventarioItem ri on ri.IdRegistroInventario = I.IdRegistroInventario\n        Where I.IdRegistroInventario = [RegistroInventario]\n\t  ) Aux\nwhere TempEstoqueRetroativo.ChaveSequencia = @ChaveSequenciaTemp\n  and TempEstoqueRetroativo.IdProduto     = Aux.IdProduto\n  and TempEstoqueRetroativo.IdEmpresaEstoque = Aux.IdEmpresaEstoque"
 },
 {
  "id": "OrdemColuna",
  "casos": [
   {
    "se": [
     {
      "param": "Ordem",
      "op": "igual",
      "valor": "pr.IdProduto"
     }
    ],
    "sql": "pr.IdProduto"
   },
   {
    "se": [
     {
      "param": "Ordem",
      "op": "igual",
      "valor": "pr.Descricao"
     }
    ],
    "sql": "pr.Descricao"
   },
   {
    "se": [
     {
      "param": "Ordem",
      "op": "igual",
      "valor": "pr.NumOriginal"
     }
    ],
    "sql": "pr.NumOriginal"
   },
   {
    "se": [
     {
      "param": "Ordem",
      "op": "igual",
      "valor": "pr.NumFabricante"
     }
    ],
    "sql": "pr.NumFabricante"
   }
  ],
  "senao": "pr.Descricao"
 }
]',
 N'[
 {
  "ordem": 1,
  "somenteExecucao": true,
  "sql": "declare @ChaveSequenciaTemp varchar(50) = ''InvEstoque-'' + cast([IdUsuario] as varchar(20))\n\nDelete TempEstoqueRetroativo\n Where  ChaveSequencia = @ChaveSequenciaTemp\n \nInsert Into  TempEstoqueRetroativo(ChaveSequencia, DataCriacao, \n       IdProduto, IdEmpresaEstoque)\nSelect @ChaveSequenciaTemp, getdate(),\n       Pr.IdProduto, Emp.IdEmpresaEstoque\n  from PrProduto Pr\n  Left join tbEmpresa Emp on {Empresas as Emp.IdEmpresaEstoque}\nwhere 1 = 1 \n# and {prProduto}\n# and {prFornecedor}\n# and {prMarca}\n# and {prGrupo}\n# and {Linha}\n# and {prClassificacao}\n%ExisteInventario%\nGroup by Pr.IdProduto, Emp.IdEmpresaEstoque\n \n%AjustaEstoqueRetroativo%"
 },
 {
  "ordem": 2,
  "nome": "InventarioDeEstoque",
  "somenteExecucao": false,
  "sql": "declare @ChaveSequenciaTemp varchar(50) = ''InvEstoque-'' + cast([IdUsuario] as varchar(20))\n\nSelect pr.IdProduto as \"Produto.IdProduto\", \n       max(pr.Descricao) as \"Produto.Descricao\",\n       max(pr.NumFabricante) as \"Produto.NumFabricante\",\n       max(pr.IdUnidadeVenda) as \"Produto.UnidadeVenda\",\n       max(pr.IdGrupo) as \"Produto.IdGrupo\",\n       max(Gru.Descricao) as \"Grupo.NomeGrupo\", \n       max(pr.IdMarca) as \"Produto.IdMarca\", \n       max(Mar.Descricao) as \"Marca.Descricao\", \n       max(pr.IdClassificacao) as \"Produto.IdClassificacao\",\n       max(ncm.CodigoNCM)  as \"NCM.CodigoNCM\", \n   \n       sum(Aux.SaldoEstoque) as \"Estoque.QtdEstoqueOficial\", \n       sum(Aux.SaldoEstoque) as \"Estoque.EstoqueDisponivel\",\n       sum(Aux.SaldoEstoque * Aux.CustoUnitario) / sum(Aux.SaldoEstoqueCalculoDivisao)  as \"Estoque.CustoUnitario\",\n\t   sum(Aux.SaldoEstoque * aux.ValorCustoLiquidoUn) / sum(Aux.SaldoEstoqueCalculoDivisao) as \"Estoque.ValorCustoLiquidoUn\",\n\t   sum(Aux.SaldoEstoque * aux.ValorCustoMedUn) / sum(Aux.SaldoEstoqueCalculoDivisao) as \"Estoque.ValorCustoMedUn\",\n\t   sum(Aux.SaldoEstoque * aux.ValorCustoUltUn) / sum(Aux.SaldoEstoqueCalculoDivisao) as \"Estoque.ValorCustoUltUn\",\n\t   Case When Sum(Aux.SaldoEstoque) > 0 Then Sum(Aux.SaldoEstoque * Aux.ValorCustoLiquidoUn)                \n            else 0 end as \"Estoque.ValorCustoLiquidoTotal\",                \n\t   Case When Sum(Aux.SaldoEstoque) > 0 Then Sum(Aux.SaldoEstoque * Aux.ValorCustoMedUn)\n            else 0 end as \"Estoque.ValorCustoMedUnTotal\",                \n\t   Case When Sum(Aux.SaldoEstoque) > 0 Then Sum(Aux.SaldoEstoque * Aux.ValorCustoUltUn)                \n            else 0 end as \"Estoque.ValorCustoUltUnTotal\", \n       Case When Sum(Aux.SaldoEstoque) > 0 Then Sum(Aux.CustoTotal)                 \n            else 0 end as \"Estoque.CustoTotal\",                 \n--       Case When Sum(Aux.SaldoEstoque) > 0 Then Sum(Aux.SaldoEstoque * Aux.CustoUnitario)\n--            else 0 end as \"Estoque.CustoTotalOld\",\n       sum(Aux.SaldoEstoque * Aux.PrecoVenda) / Sum(Aux.SaldoEstoqueCalculoDivisao) as \"Produto.PrecoVenda\",\n       Case When Sum(Aux.SaldoEstoque) > 0 Then Sum(Aux.SaldoEstoque * Aux.PrecoVenda)\n            else 0 end as \"Produto.PrecoVendaTotal\",\n       (SELECT stuff((\n\t\t\tselect '', '' + IsNULL(IsNULL(endloc.NomeEndereco, endlote.NomeEndereco),'''') from ceLoteProduto lote \n\t\t\t left join ceLocalizacaoLote loc on loc.IdLoteProduto = lote.IdLoteProduto and loc.IdEmpresaEstoque = lote.IdEmpresaEstoque \n\t\t\t left join ceEndereco endloc on endloc.IdEndereco = loc.IdEndereco \n\t\t\t left join ceEndereco endlote on endlote.IdEndereco = lote.IdEnderecoPadrao \n\t\t\twhere lote.IdProduto = Pr.IdProduto \n\t\t\t  and lote.IdEmpresaEstoque in (Select Emp.IdEmpresaEstoque from tbEmpresa Emp Where {Empresas as Emp.IdEmpresa}) \n\t\t\t  and isnull(loc.FlagInativo,0) = 0 \n\t\t\tFOR XML PATH('''')\n\t\t\t), 1, 1, '''')) as \"Estoque.Localizacao\",\n       max(pr.IdTributacao) as \"Produto.IdTributacao\", \n       max(Cast(trib.IdTributacao as varchar(10)) + '' - '' + trib.Descricao) as \"Tributacao.Descricao\",       \nCase [Quebra] \n       When 0 Then ''''\n       When 1 Then Cast(Max(Pr.IdGrupo) as varchar(20))\n       When 2 Then Cast(Max(Pr.IdMarca)  as varchar(20))\n       When 3 Then Cast(Max(Pr.IdFornecedor) as varchar(20))       \n       When 4 Then Cast(Max(Pr.IdLinha) as varchar(20))       \n       End as \"Quebra.CampoQuebra\",\n  Case [Quebra] \n       When 0 Then ''''\n       When 1 Then Max(Gru.Descricao)\n       When 2 Then Max(Mar.Descricao)\n       When 4 Then Max(lin.Descricao)\n       When 3 Then Case When isnull(Max(Forn.NomeFantasia),'''') <> '''' Then Max(Forn.NomeFantasia) Else Max(Forn.RazaoSocial) End\n       End as \"Quebra.DescricaoQuebra\",       \n  max(%OrdemColuna%)              as \"Quebra.CampoOrdem\",\n  Case When [ExibirNumero] = ''F'' Then Max(Pr.NumFabricante) Else Max(Pr.NumOriginal) End As \"Produto.ExibirNumero\",\n  \n  Max(Aux.NotaUltimaCompra), max(Aux.DataUltimaCompra), isnull(Max(Aux.CFOP_cst),''0000/000'') CFOP_cst, Max(Aux.Fornecedor), \n  isnull(Max(Aux.AliqICMS),0) AliqICMS,\n  isnull(Max(Aux.ValorCompraImposto),0) ValorCompraImposto,\n  isnull(Max( Aux.SaldoEstoque * Aux.ValorCompraImposto),0) ValorUltCompra, \n  isnull(Max((Aux.SaldoEstoque * Aux.ValorCompraImposto * AliqICMS) / 100 ),0) ICMSUltCompra,\n  Max(pr.Medida) as \"Produto.Medida\"\nFrom (\n   Select T.IdProduto, T.IdEmpresaEstoque,\n          Sum(isnull(T.QuantDisponivel,0)) as SaldoEstoque,\n          case when Sum(isnull(T.QuantDisponivel,0)) = 0 then 1 else Sum(isnull(T.QuantDisponivel,0)) end as SaldoEstoqueCalculoDivisao,          \n          Case [TipoCusto]\n               When 0 THen sum(IsNull(T.ValorCustoFor, 0))\n               When 1 Then sum(IsNull(T.ValorCustoMed, 0))\n               When 2 Then sum(IsNull(T.ValorCustoUlt, 0))\n               When 3 Then sum(IsNull(T.ValorCustoUltReal, 0))\n               End as CustoUnitario,\n          Case [TipoCusto]\n               When 0 THen sum(round(IsNull(T.ValorCustoFor, 0) * isnull(T.QuantDisponivel,0) ,2))                 \n               When 1 Then sum(round(IsNull(T.ValorCustoMed, 0) * isnull(T.QuantDisponivel,0) ,2))                 \n               When 2 Then sum(round(IsNull(T.ValorCustoUlt, 0) * isnull(T.QuantDisponivel,0) ,2))                 \n               When 3 Then sum(round(IsNull(T.ValorCustoUltReal, 0) * isnull(T.QuantDisponivel,0) ,2))                                  \n               End as CustoTotal,                 \n          Max(isnull(pre.PrecoVenda1,0)) as PrecoVenda,\n          max(IsNull(pre.ValorCustoLiquido,0)) as ValorCustoLiquidoUn,\n\t\t  max(Isnull(t.ValorCustoMed,0)) as ValorCustoMedUn,                \n\t\t  max(Isnull(t.ValorCustoUlt,0))  as ValorCustoUltUn,\n          --Dados da Ultima Compra, so retorna quando layout de inventario com impostos\n\t      max(ultCompra.NumeroDocumento) NotaUltimaCompra, \n\t      max(ultCompra.DataMovimento) DataUltimaCompra, \n\t      max(itemImposto.IdCFOP) CFOP, \n\t      max(itemImposto.AliqICMS) AliqICMS,\n\t      max(itemImposto.IdCFOP) + ''/''+ isnull(max(itemImposto.CstICMS),max(itemImposto.CstCSOSN)) as CFOP_CST,\n\t      (select IdEntidade + '' - '' + RazaoSocial from tbEntidade where IdEntidade = max(ultCompra.IdEntidade)) Fornecedor,\n\t  \n\t      sum((isnull(itemUltCompra.ValorTotal,0) + isnull(itemUltCompra.ValorFrete,0) + isnull(itemUltCompra.ValorSeguro,0)  + isnull(itemUltCompra.ValorDespesas,0) +\n              isnull(itemImposto.ValorIPI,0) + isnull(itemImposto.ValorICMSST,0) +  isnull(itemImposto.ValorFCPST,0) - isnull(itemUltCompra.ValorDescProdutoRateado,0) \n              - isnull(itemUltCompra.ValorDescServicoRateado,0)) / \n                            Case When itemUltCompra.QuantAtendida > 0 Then itemUltCompra.QuantAtendida Else 1 End) ValorCompraImposto\n     from TempEstoqueRetroativo T\n          left join tbEmpresa Emp on Emp.IdEmpresa = T.IdEmpresaEstoque\n\t      left join prPrecoProduto pre ON pre.IdProduto = T.IdProduto\n\t                                 and IsNull(pre.FlagPrincipal,0) = 1\n\t                                 and pre.IdGrupoPreco = Emp.IdGrupoPreco\n          Left Join fiNotaFiscalItem itemUltCompra on itemUltCompra.IdNotaFiscalItem = (Select IdNotaFiscalItemULT \n                                                                                          from UltimaNotaCompraProduto(T.IdProduto, T.idEmpresaEstoque, null))\n                                                 and [LayoutSelecionado] = ''InventariocomImposto''    --Somente no layout de inventario com imposto\n          Left Join fiNotaFiscal ultCompra on ultCompra.IdNotaFiscal = ItemUltCOmpra.IdNotaFiscal\n          Left Join fiNotaFiscalItemImposto itemImposto on itemImposto.IdNotaFiscalItemImposto = itemUltCompra.IdNotaFiscalItem\n     Where T.ChaveSequencia = @ChaveSequenciaTemp\n     Group by T.IdProduto, T.IdEmpresaEstoque\n     ) Aux\n   Left join prProduto pr on pr.IdProduto = Aux.IdProduto\n   Left Join prMarca Mar on Mar.IdMarca = pr.IdMarca\n   Left join prGrupo Gru on Gru.IdGrupo = pr.IdGrupo\n   Left join prLinha lin on lin.IdLinha = pr.IdLinha\n   Left Join tbEntidade Forn on Forn.IdEntidade = pr.IdFornecedor\n   Left join prClassificacaoNCM ncm on ncm.IdClassificacao = pr.IdClassificacao\n   Left join fiTributacao trib on trib.IdTributacao = pr.IdTributacao\nwhere 1 = 1 \n  %TipoEstoque%\n  %TipoBloqueio%\nGroup by Pr.IdProduto\nOrder By max(%OrdemColuna%)"
 },
 {
  "ordem": 3,
  "nome": "EmpresaMestre",
  "somenteExecucao": false,
  "sql": "select \n  E.IdEntidade         as \"Ent.Codigo\",\n  E.RazaoSocial        as \"Ent.RazaoSocial\",\n  E.NomeFantasia       as \"Ent.NomeFantasia\",\n  E.TipoPessoa         as \"Ent.TipoPessoa\",\n  E.Bairro             as \"Ent.Bairro\",\n  E.Endereco           as \"Ent.Endereco\",\n  C.Nome               as \"Ent.Cidade\",\n  C.IdEstado           as \"Ent.Estado\", \n  E.CEP                as \"Ent.CEP\",\n  E.TelefoneCom1       as \"Ent.TelCom1\",\n  E.TelefoneCom2       as \"Ent.TelCom2\",\n  E.NumCNPJ            as \"Ent.NumCNPJ\",\n  E.NumInscricaoEstadual    as \"Ent.NumInsEstadual\",\n  E.NumInscricaoMunicipal   as \"Ent.NumInsMunicipal\",\n  [TipodeRelatorio]    as \"TipoRel\",\n  Case When [ExibirNumero] = ''F'' Then ''Nr. Fabricante'' Else ''Nr. Original'' End As \"ExibirNumero\",\n  Case [Quebra]\n       When 0 Then ''''\n       When 1 Then ''Grupo: ''\n       When 2 Then ''Marca: ''\n       When 3 Then ''Fornecedor: ''\n       When 4 Then ''Linha: ''\n       End  as \"TituloQuebra\",\n  Case [TipoCusto]\n       When 0 Then ''Custo Fornecedor ''\n       When 1 Then ''Custo Médio ''\n       When 2 Then ''Custo Ult.Compra ''\n       When 3 Then ''Custo Ult.CompraReal ''\n       End  as \"TituloCusto\",       \n  Case When [RegistroInventario] = 0 \n            Then isnull([DataSaldoEstoque], cast(getdate() as date))\n       Else Inv.DataInventario\n       End as DataEstoque,\n  Case When [RegistroInventario] = 0 \n            Then ''Saldo Retroativo '' \n       Else ''Inventário de Estoque: '' + Cast(Inv.IdRegistroInventario as varchar) \n       End as TituloInventario ,\n  Case When [ConsiderarPedidoAberto] = 1 Then ''Deduz Ped. Aberto'' Else ''''  End as DeduzirPedAberto\nfrom tbEntidade E\n  left join tbCidade C on C.IdCidade = E.IdCidade\n  Left Join ceRegistroInventario inv on Inv.IdRegistroInventario = [RegistroInventario]\nwhere SUBSTRING(E.IdEntidade,1,1) = ''M''\n  and exists (select 1 from tbEmpresa Em\n               where {Empresas as Em.IdEmpresa}\n                 and Em.IdEmpresa = try_cast(SUBSTRING(E.IdEntidade,2,6) as int))"
 }
]',
 1, 5000, 120);
GO

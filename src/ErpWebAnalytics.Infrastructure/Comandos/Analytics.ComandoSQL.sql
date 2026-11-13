-- Catálogo de comandos SQL dinâmicos lidos pelo conector MCP (ferramentas listar_comandos / executar_comando).
-- Rodar uma vez no banco do ERP. A API só LÊ esta tabela; quem grava é o front-end de manutenção (a fazer).
-- Requer SQL Server 2016+ (ISJSON) e nível de compatibilidade >= 130; remova os CHECK se o banco for mais antigo.

IF SCHEMA_ID('Analytics') IS NULL EXEC('CREATE SCHEMA Analytics');
GO

CREATE TABLE Analytics.ComandoSQL
(
    ChaveEmpresa varchar(50)    NOT NULL,            -- dono do comando; a API só enxerga a chave configurada em Comandos:ChaveEmpresa
    Id           varchar(100)   NOT NULL,            -- nome que o Claude usa em executar_comando
    Descricao    nvarchar(1000) NOT NULL,            -- o que o comando devolve e quando usar (o Claude lê isto)
    Parametros   nvarchar(max)  NULL,                -- JSON: lista de parâmetros
    Regras       nvarchar(max)  NULL,                -- JSON: lista de regras (%Regra% no SQL)
    [SQL]        nvarchar(max)  NOT NULL,            -- SELECT único (texto) OU lista JSON de passos [{ordem,nome,sql,somenteExecucao}]; cada sql usa [Param], {Param}, %Regra%, linhas '#' e ';'
    Ativa        bit            NOT NULL CONSTRAINT DF_ComandoSQL_Ativa DEFAULT (1),
    MaxLinhas    int            NOT NULL CONSTRAINT DF_ComandoSQL_MaxLinhas DEFAULT (1000),   -- teto de 5000 imposto pela API
    TimeoutSeg   int            NOT NULL CONSTRAINT DF_ComandoSQL_TimeoutSeg DEFAULT (30),    -- teto de 120 imposto pela API
    CONSTRAINT PK_ComandoSQL PRIMARY KEY (ChaveEmpresa, Id),
    CONSTRAINT CK_ComandoSQL_Parametros CHECK (Parametros IS NULL OR ISJSON(Parametros) = 1),
    CONSTRAINT CK_ComandoSQL_Regras     CHECK (Regras IS NULL OR ISJSON(Regras) = 1)
);
GO

/* Exemplo (ilustrativo: confira os nomes de tabelas/colunas no banco) ------------------------------------------

INSERT Analytics.ComandoSQL (ChaveEmpresa, Id, Descricao, Parametros, Regras, [SQL])
VALUES ('MINHA_EMPRESA', 'vendas_por_cliente',
 N'Total vendido por cliente no período. Informe DataVenda (between) e, opcionalmente, IdEntidade.',
 N'[
    {"id":"DataVenda","campo":"p.DataMovimento","tipo":"Date","obrigatorio":true,"descricao":"Período do pedido"},
    {"id":"IdEntidade","campo":"p.IdEntidade","tipo":"String","descricao":"Código do cliente (ex.: C000429)"},
    {"id":"Empresas","campo":"p.IdEmpresa","tipo":"Inteiro","origem":"Servidor","chave":"idsEmpresa"}
  ]',
 NULL,
 N'select p.IdEntidade, e.Nome, sum(p.ValorTotal) as Total
   from ftPedido p
   join tbEntidade e on e.IdEntidade = p.IdEntidade
   where {Empresas}
     and {DataVenda}
   # and {IdEntidade}
   group by p.IdEntidade, e.Nome
   order by Total desc');

-- Chamada: executar_comando("vendas_por_cliente", {"DataVenda": {"op":"between","valor":"2026-11-01","valor2":"2026-11-30"}})
-----------------------------------------------------------------------------------------------------------------*/

-- Perfil do GraphQL de cada sistema (ERPWEB, RSYS2003, CHAMADOS): quais tabelas e colunas são expostas, com que nomes e relações.
-- Rodar uma vez no banco (depois de Analytics.ComandoSQL.sql, que cria o schema Analytics).
-- A API lê o perfil do sistema configurado em Sistema:Perfil; a tela de manutenção grava (aba "GraphQL").

IF SCHEMA_ID('Analytics') IS NULL EXEC('CREATE SCHEMA Analytics');
GO

CREATE TABLE Analytics.PerfilGraphQL
(
    ChaveEmpresa varchar(50)  NOT NULL,              -- mesma chave de Analytics.ComandoSQL (Comandos:ChaveEmpresa)
    Sistema      varchar(50)  NOT NULL,              -- ERPWEB, RSYS2003, CHAMADOS...
    Perfil       nvarchar(max) NOT NULL,             -- JSON: {sistema, tabelas:[{tabela, nome, consulta, colunas, relacoes}]}
    AtualizadoEm datetime2    NOT NULL CONSTRAINT DF_PerfilGraphQL_Atualizado DEFAULT (sysdatetime()),
    CONSTRAINT PK_PerfilGraphQL PRIMARY KEY (ChaveEmpresa, Sistema),
    CONSTRAINT CK_PerfilGraphQL_Perfil CHECK (ISJSON(Perfil) = 1)
);
GO

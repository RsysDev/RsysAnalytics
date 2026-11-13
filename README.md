# RsysAnalytics

API **GraphQL** e **servidor MCP** em **.NET 9** sobre **SQL Server**, que transforma os dados de um ERP legado em indicadores e consultas prontas para o Claude gerar artefatos (painéis, relatórios) exportáveis a clientes.

![.NET 9](https://img.shields.io/badge/.NET-9-512BD4)
![SQL Server](https://img.shields.io/badge/SQL%20Server-banco--primeiro-CC2927)
![HotChocolate](https://img.shields.io/badge/HotChocolate-16.x-E10098)
![React](https://img.shields.io/badge/React-19-61DAFB)
![MCP](https://img.shields.io/badge/MCP-HTTP%20sem%20estado-black)

---

## Sumário

- [O que faz](#o-que-faz)
- [Arquitetura](#arquitetura)
- [Começando](#começando)
- [Configuração](#configuração)
- [Endpoints](#endpoints)
- [Recursos](#recursos)
- [Segurança](#segurança)
- [Frontend de manutenção](#frontend-de-manutenção)
- [Comandos úteis](#comandos-úteis)

---

## O que faz

| Recurso | Descrição |
| --- | --- |
| **GraphQL navegável** | `pedidos`, `entidades`, `vendedores`, `produtos` com paginação, projeção, filtro e ordenação escolhidos pelo cliente. |
| **Relatórios em SQL** | `relacaoPedidos` e `relacaoItensPedidos`, com devoluções, custo, margem e comissão conforme o usuário. |
| **Desempenho mensal** | `desempenhoMensal` devolve o mês já agregado: loja, ranking de vendedores, série diária e últimas vendas. |
| **Conector MCP** | `/mcp` expõe os indicadores e o catálogo de comandos para o Claude e para artefatos. |
| **Comandos SQL dinâmicos** | Consultas novas **sem deploy**, guardadas em tabela e executadas por parâmetros tipados. |
| **GraphQL por perfil** | Um schema nativo por sistema (ERPWEB, RSYS2003, CHAMADOS) montado a partir de um perfil salvo no banco. |
| **Tela de manutenção** | Frontend React para editar o catálogo, testar comandos e importar relatórios do ERP legado. |

## Arquitetura

Clean Architecture; as dependências apontam para dentro.

```
src/
├── ErpWebAnalytics.Domain          Entities do ERP (scaffold, POCOs)
├── ErpWebAnalytics.Application     Contratos, DTOs, interpretador de comandos, perfis
├── ErpWebAnalytics.Infrastructure  AppDbContext, consultas SQL, repositórios, ADO dinâmico
└── ErpWebAnalytics.Api             Host ASP.NET Core: HotChocolate, MCP, admin, segurança
frontend/                           React 19 + Vite + TypeScript (tela de manutenção)
```

Decisões principais:

- **Banco-primeiro**: sem migrations. O esquema muda no ERP e as entities são regeneradas por scaffold.
- **DbContext em pooled factory**, exigência do HotChocolate.
- **Consultas de relatório em T-SQL parametrizado** (arquivos `.sql` embutidos), porque as regras vivem em views e funções do ERP.
- **Tipos com `BindFieldsExplicitly()`**: só aparece no GraphQL o que for listado. Custo, margem e comissão ficam fora por padrão.

## Começando

### Pré-requisitos

- [.NET SDK 9](https://dotnet.microsoft.com/download)
- Acesso a um SQL Server com o banco do ERP
- Node.js (somente para o frontend)

### Rodando

```bash
# 1. Segredos (não ficam no repositório)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection string>" --project src/ErpWebAnalytics.Api
dotnet user-secrets set "Seguranca:ApiKey" "<chave>" --project src/ErpWebAnalytics.Api

# 2. Subir a API
dotnet run --project src/ErpWebAnalytics.Api
```

Em Development o GraphQL abre no navegador (`/graphql`) sem chave, com introspecção liberada. Veja também o [RODAR.md](RODAR.md).

## Configuração

Os valores sensíveis **não** estão nos `appsettings`: use user-secrets (desenvolvimento) ou variáveis de ambiente (`Seguranca__ApiKey`, `ConnectionStrings__DefaultConnection`, ...).

| Chave | Para que serve |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Banco do ERP. A API não sobe sem ela. |
| `Seguranca:ApiKey` | Protege `/graphql` e `/mcp`. Vazia = sem proteção (só uso local). |
| `Seguranca:AdminKey` | Habilita a tela e a API de manutenção. Vazia = módulo desligado (404). |
| `Seguranca:ApiKeyEscrita` | Chave separada para as mutations de atualização (`X-Write-Key`). |
| `Seguranca:GraphQLAnonimo` | Libera só o `/graphql` em Development. Nunca em ambiente exposto. |
| `Sistema:Perfil` | Sistema atendido pela instalação (ERPWEB, RSYS2003, CHAMADOS). |
| `Comandos:ChaveEmpresa` | Empresa dona do catálogo de comandos. |
| `Comandos:PermitirEscrita` | Permite comandos e mutations de escrita (padrão `false`). |
| `Painel:IdsEmpresa` / `Painel:IdUsuario` | Empresas e usuário do ERP usados pelo conector; definem o que custo/margem mostram. |

## Endpoints

| Rota | Função |
| --- | --- |
| `/graphql` | GraphQL (clássico em EF, ou dinâmico conforme o sistema) |
| `/graphql/perfil` | GraphQL do perfil, quando o sistema é ERPWEB |
| `/mcp` | Servidor MCP (`desempenho_periodo`, `listar_comandos`, `executar_comando`) |
| `/admin/` | Tela de manutenção do catálogo |
| `/admin/api` | API da tela (exige `X-Admin-Key`) |

Usar como conector no Claude Code:

```bash
claude mcp add --transport http painel http://localhost:5099/mcp --header "Authorization: Bearer <chave>"
```

## Recursos

### Desempenho mensal

`desempenhoMensal(ano, mes, idsEmpresa, idUsuario)` agrega em memória as linhas de `relacaoPedidos`:

- **Realizado** = venda bruta − devoluções
- **Ticket** = venda bruta ÷ pedidos
- **Margem** = (realizado − custo) ÷ realizado

### Comandos SQL dinâmicos

Novas consultas sem deploy, em `Analytics.ComandoSQL`, usadas pelo Claude via MCP.

- Parâmetros tipados, regras condicionais e opções em lista.
- Um comando pode ter vários **passos** (vários resultados nomeados).
- **Nenhum valor entra no texto do SQL**: tudo vira `SqlParameter`.
- Somente leitura por padrão; escrita exige habilitação explícita e roda em transação.

### GraphQL por perfil

Cada sistema tem tabelas e colunas diferentes, então o que o GraphQL expõe é um **perfil** salvo em `Analytics.PerfilGraphQL`:

- Nada é exposto por padrão; colunas de dados sensíveis (cpf, senha, custo...) começam desmarcadas.
- Filtros e ordenação só nas colunas marcadas; relações em lote por DataLoader (sem N+1).
- Recarga ao vivo ao salvar o perfil, sem reiniciar.
- Mutations de atualização opcionais, protegidas por chave própria, limite de linhas, filtro obrigatório e modo simulação.

## Segurança

Os artefatos chegam a terceiros, então o isolamento de dados por cliente é requisito central.

- Chave de API em `/graphql` e `/mcp` (`Authorization: Bearer`, `X-Api-Key` ou `?apiKey=` no MCP).
- Empresa e usuário do ERP ficam **no servidor**; o chamador não escolhe.
- O SQL do catálogo é código executável: o login do banco deve ter o mínimo de permissões.
- Introspecção bloqueada fora de Development.
- Nunca versione credenciais reais.

## Frontend de manutenção

```bash
cd frontend
npm install
npm run dev      # desenvolvimento (proxy de /api para a API)
npm run build    # grava em src/ErpWebAnalytics.Api/wwwroot/admin
```

Rode o build antes do `dotnet publish`. Abas: Geral, Parâmetros, Regras, SQL, Testar, além da importação do ERP legado e do editor de perfil GraphQL.

## Comandos úteis

```bash
dotnet build
dotnet run --project src/ErpWebAnalytics.Api
dotnet dotnet-ef dbcontext scaffold ...   # regerar entities (ver CLAUDE.md)
```

> Pacotes EF Core fixados em **9.x** (o `dotnet add package` sem versão traz o 11, incompatível com net9.0).

Mais detalhes de design e convenções estão em [CLAUDE.md](CLAUDE.md).

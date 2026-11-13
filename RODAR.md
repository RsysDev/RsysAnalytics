# ErpWebAnalytics: como rodar

API GraphQL (.NET 9 + SQL Server) com servidor MCP, para consultar dados do ERP e gerar painéis com o Claude.

## Requisitos

- .NET SDK 9.0
- Acesso de rede ao SQL Server do ERP (a connection string está em `appsettings.json`)
- Claude Code (opcional, só para o conector MCP)

## Configuração

Arquivo: `src/ErpWebAnalytics.Api/appsettings.json`

| Chave | Para que serve |
|---|---|
| `ConnectionStrings:DefaultConnection` | Conexão com o SQL Server. Já inclui `TrustServerCertificate=True` (o servidor usa certificado não confiável). |
| `Seguranca:ApiKey` | Chave de acesso. Quando preenchida, `/mcp` e `/graphql` exigem a chave. Vazia = sem proteção. |
| `Seguranca:AdminKey` | Chave da tela de manutenção de comandos (`/admin/`). Vazia = tela e `/admin/api` desligadas. |
| `Comandos:ChaveEmpresa` | Empresa dona do catálogo `Analytics.ComandoSQL` (ex.: `FUSCAOPRETO`). Obrigatória para os comandos e a tela. |
| `Comandos:PermitirEscrita` | `true` libera comandos com passos de execução (update/insert) em `executar_comando_com_escrita`. Padrão `false`. |
| `Importacao:Tabelas` | Tabelas de relatórios do ERP legado oferecidas na tela de importação (padrão `fwRelatorio`, `fwRelatorioDinamico`). Só aparecem se existirem no banco com as colunas esperadas. |
| `Sistema:Perfil` | Sistema que esta instalação serve (`ERPWEB`, `RSYS2003`, `CHAMADOS`). Escolhe o perfil de `Analytics.PerfilGraphQL` e liga a aba GraphQL da tela. Vazio = desligado. |
| `Seguranca:ApiKeyEscrita` | Chave de escrita das mutations do GraphQL do perfil (cabeçalho `X-Write-Key`), diferente da `ApiKey`. Vazia = sem mutations. Exige também `Comandos:PermitirEscrita=true`. |
| `Painel:IdsEmpresa` | Empresas incluídas nos indicadores do conector MCP. Padrão `[1]`. |
| `Painel:IdUsuario` | Usuário do ERP cujas permissões de custo/margem valem para o conector. Padrão `1`. |
| `Painel:CacheMinutos` | Tempo de cache dos indicadores. Padrão `5`. |

Não versione credenciais. Prefira variáveis de ambiente (`Seguranca__ApiKey`, `ConnectionStrings__DefaultConnection`) ou user-secrets:

```powershell
dotnet user-secrets set "Seguranca:ApiKey" "SUA_CHAVE" --project src/ErpWebAnalytics.Api
```

## Subir a API

```powershell
dotnet build
dotnet run --project src/ErpWebAnalytics.Api --urls http://localhost:5099
```

Endpoints:

- `http://localhost:5099/graphql`: GraphQL (e o IDE Nitro no navegador)
- `http://localhost:5099/mcp`: servidor MCP

## Abrir o GraphQL no navegador (demonstração local)

Com a chave configurada, o `/graphql` retorna 401 no navegador. Em desenvolvimento ele já é liberado: `src/ErpWebAnalytics.Api/appsettings.Development.json` tem `"Seguranca": { "GraphQLAnonimo": true }`, que só vale quando `ASPNETCORE_ENVIRONMENT=Development`. O `dotnet run` com o perfil padrão (`Properties/launchSettings.json`) já usa Development; o `/mcp` continua protegido pela chave.

```powershell
dotnet run --project src/ErpWebAnalytics.Api
```

Abra `http://localhost:5094/graphql` (porta do `launchSettings.json`). O log mostra um aviso quando esse modo está ativo.

Se rodar com `--urls` ou `--no-launch-profile`, o ambiente não será Development e a chave volta a ser exigida. Nesse caso defina `$env:ASPNETCORE_ENVIRONMENT = "Development"`, ou ligue o interruptor por variável: `$env:Seguranca__GraphQLAnonimo = "true"`.

**Use só localmente.** Não exponha a API por túnel enquanto estiver em Development (rode com `$env:ASPNETCORE_ENVIRONMENT = "Production"`). O GraphQL dá acesso a pedidos e entidades (inclui CPF/CNPJ). Nunca ligue isso com a API exposta por túnel ou servidor.

Sem o interruptor, chame com a chave:

```powershell
$h = @{ Authorization = "Bearer SUA_CHAVE" }
Invoke-RestMethod -Uri http://localhost:5099/graphql -Method Post -Headers $h -ContentType "application/json" -Body '{"query":"{ health }"}'
```

## Consultas disponíveis

Navegáveis (o cliente escolhe campos, filtros, ordenação e página):

- `pedidos`, `entidades`, `vendedores`, `produtos`

Relatórios agregados (regras do ERP, com devoluções e permissão de custo/margem por usuário):

- `desempenhoMensal(ano, mes, idsEmpresa, idUsuario)`: painel do mês (loja, ranking de vendedores, série diária, últimas vendas)
- `relacaoPedidos(filtro)`: relação de pedidos e devoluções
- `relacaoItensPedidos(filtro)`: itens dos pedidos

Exemplos para colar no IDE:

```graphql
{
  pedidos(first: 5, where: { idEmpresa: { eq: 1 }, statusPedido: { neq: 4 } }, order: { dataMovimento: DESC }) {
    totalCount
    nodes { idPedido dataMovimento valorTotalPedido entidade { razaoSocial } vendedorInterno { nome } }
  }
}
```

```graphql
{
  desempenhoMensal(ano: 2026, mes: 11, idsEmpresa: [1], idUsuario: 1) {
    loja { realizado pedidos ticketMedio margemPercentual devolucoes }
    vendedores { nome realizado pedidos ticketMedio margemPercentual }
  }
}
```

```graphql
query ($filtro: FiltroPedidosInput!) {
  relacaoPedidos(filtro: $filtro) { idPedido dataMovimento descStatusPedido valorVenda margem }
}
# variáveis: { "filtro": { "idsEmpresa": [1], "idUsuario": 1, "dataInicio": "2026-11-01", "dataFim": "2026-11-10" } }
```

Sempre informe um período nas consultas de relação: sem ele a consulta varre todo o histórico.

## Conectar o Claude Code ao MCP

```powershell
claude mcp add --transport http painel http://localhost:5099/mcp --header "Authorization: Bearer SUA_CHAVE"
```

Depois peça, por exemplo: "mostre o desempenho de vendas dos últimos 4 meses". O Claude chama a ferramenta `desempenho_periodo(ano, mes, quantidade)`.

Teste direto, sem o Claude:

```powershell
$h = @{ Accept = "application/json, text/event-stream"; Authorization = "Bearer SUA_CHAVE" }
Invoke-WebRequest -Uri http://localhost:5099/mcp -Method Post -Headers $h -ContentType "application/json" `
  -Body '{"jsonrpc":"2.0","id":1,"method":"tools/list"}' -UseBasicParsing
```

## Painel ao vivo no claude.ai

Um artefato só alcança a API por um conector MCP remoto cadastrado na conta do claude.ai, e o claude.ai não enxerga `localhost`.

1. Exponha a API por um túnel HTTPS (Dev Tunnels, ngrok ou cloudflared) apontando para `http://localhost:5099`.
2. **Não** ative `Seguranca__GraphQLAnonimo` enquanto o túnel estiver aberto.
3. Cadastre `https://SEU-HOST/mcp?apiKey=SUA_CHAVE` como conector personalizado, **sem** autenticação. O diálogo do claude.ai não tem campo para a chave: sem ela na URL o `/mcp` responde 401 e o claude.ai tenta OAuth e falha ("Erro ao Iniciar Autenticação"). A chave na URL só vale no `/mcp` (não no `/graphql`) e pode aparecer em logs do servidor: troque-a ao sair da homologação.
4. Peça ao Claude Code para ligar o painel ao conector.

Não confirmado: se o claude.ai aceita chave estática em conector personalizado ou exige OAuth. Se recusar, é preciso implementar OAuth na API.

## Problemas comuns

| Sintoma | Causa e solução |
|---|---|
| 401 ao abrir `/graphql` no navegador | A chave está configurada. Use `Seguranca__GraphQLAnonimo=true` (só local) ou envie a chave. |
| `dotnet build` falha com "arquivo bloqueado por ErpWebAnalytics.Api" | Há uma instância da API rodando. Feche-a: `Get-Process ErpWebAnalytics.Api \| Stop-Process`. |
| Erro de certificado SSL ao conectar no banco | Mantenha `TrustServerCertificate=True` na connection string. |
| `dotnet add package` traz EF Core 11 e falha | Fixe os pacotes do EF Core em 9.x (`--version 9.0.*`). |
| `relacaoPedidos` lenta | Reduza o período (`dataInicio`/`dataFim`). O timeout do comando é de 120 s. |
| `valorCustoFor` rejeitado em `pedidos` | De propósito: custo, margem e comissão não são expostos nas consultas navegáveis. Use `relacaoPedidos` ou `desempenhoMensal`, que respeitam a permissão do usuário. |

## Estrutura

```
src/
  ErpWebAnalytics.Domain          entities do ERP (geradas por scaffold)
  ErpWebAnalytics.Application     contratos, DTOs e serviços (desempenho)
  ErpWebAnalytics.Infrastructure  EF Core, SQL dos relatórios (Pedidos/Sql/*.sql)
  ErpWebAnalytics.Api             GraphQL (HotChocolate), MCP, chave de API
```

Detalhes de arquitetura e convenções para quem for evoluir o código: [CLAUDE.md](CLAUDE.md).

## Artefato privado:
https://claude.ai/artifact/7izh4ceVwgS5vEMCpTnTf3

## publicar 
dotnet publish src/ErpWebAnalytics.Api -c Release -o ./publish
## Tela de manutenção de comandos (frontend)

Código em `frontend/` (React 19 + Vite). Requer Node 20.19+ ou 22.12+.

```powershell
cd frontend
npm install
npm run dev        # http://localhost:5173, usa a API em http://localhost:5094 (VITE_API_URL muda)
npm run build      # gera src/ErpWebAnalytics.Api/wwwroot/admin, servido em /admin/
```

Para usar: defina `Seguranca:AdminKey` e `Comandos:ChaveEmpresa`, suba a API e abra `/admin/`. A tela grava em `Analytics.ComandoSQL`, então o login do banco precisa de permissão de escrita nessa tabela.

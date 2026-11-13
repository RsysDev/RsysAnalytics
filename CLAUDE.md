# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Objetivo e stack

API **GraphQL** em **.NET 9** com **SQL Server**, seguindo sempre os melhores design patterns (justifique um padrão ao introduzi-lo e não adicione abstrações sem necessidade concreta).

A API será consumida **diretamente** (endpoint GraphQL, sem MCP server) pelo Claude Code para gerar **artefatos** exportáveis a clientes. Consequências:

- Descreva tipos e campos do schema (o Claude Code lê o schema por introspecção; mantenha-a disponível ao menos em desenvolvimento).
- Artefatos chegam a terceiros: isolamento de dados por cliente e controle de acesso são requisitos centrais; nunca exponha dados de um cliente em artefato de outro.

## Arquitetura

Clean Architecture, solution `ErpWebAnalytics.sln`, projetos em `src/`. Dependências apontam para dentro:

- `ErpWebAnalytics.Domain` — entities do ERP em `Entities/` (**geradas por scaffold**, banco-primeiro, POCOs sem dependências).
- `ErpWebAnalytics.Application` — contratos e DTOs; referencia Domain.
- `ErpWebAnalytics.Infrastructure` — `AppDbContext` (scaffold, partial) + SQL Server; referencia Application. Registro de serviços em `DependencyInjection.AddInfrastructure`.
- `ErpWebAnalytics.Api` — host ASP.NET Core com **HotChocolate** (`Program.cs`, tipos GraphQL em `GraphQL/`); referencia Application e Infrastructure. O endpoint é mapeado em `/graphql`.

O DbContext é registrado como **pooled factory** (`AddPooledDbContextFactory`), exigência do HotChocolate; `RegisterDbContextFactory<AppDbContext>()` no schema o injeta nos resolvers.

## Dois estilos de consulta GraphQL

1. **Navegáveis (`pedidos`, `entidades`, `vendedores`, `produtos`)** — resolvers retornam `IQueryable` das entities com `[UsePaging] [UseProjection] [UseFiltering] [UseSorting]`; o cliente escolhe campos, filtros, ordem e página. Para expor um novo campo/entidade edite `GraphQL/Types/EntityTypes.cs` (tipo) e `FilterSortTypes.cs` (filtro/ordenação).
   - Os tipos usam `BindFieldsExplicitly()`: **só aparece o que for listado**. É deliberado: custo, margem e comissão ficam fora (sem autenticação ainda) e nomes técnicos (`IdEntidadeNavigation`, `FtPedidoItem`) são renomeados (`entidade`, `itens`). Filtro/ordenação também são explícitos, senão o HotChocolate inferiria da CLR e permitiria filtrar por campo oculto.
   - Nunca exponha navegações inversas (ex.: `Entidade.pedidos`) sem paginação: geram ciclos e consultas pesadas.
2. **Relatórios em SQL (`relacaoPedidos`, `relacaoItensPedidos`)** — ver seção abaixo; usados quando a regra vive no banco (devoluções, custo/margem/comissão por usuário).

Regerar entities após mudança no banco (ferramenta local `dotnet-ef` 9.x em `.config/dotnet-tools.json`; sobrescreve `Entities/` e `AppDbContext.cs`, então não edite esses arquivos à mão; para uma tabela nova acrescente `--table`):

```
dotnet dotnet-ef dbcontext scaffold "<connection string>" Microsoft.EntityFrameworkCore.SqlServer --project src/ErpWebAnalytics.Infrastructure --startup-project src/ErpWebAnalytics.Api --context AppDbContext --context-dir Persistence --output-dir ../ErpWebAnalytics.Domain/Entities --namespace ErpWebAnalytics.Domain.Entities --context-namespace ErpWebAnalytics.Infrastructure.Persistence --no-onconfiguring --no-pluralize --force --table ftPedido --table ftPedidoItem ... (todas as tabelas já mapeadas)
```

Tabelas hoje mapeadas: ftPedido, ftPedidoItem, ftPedidoVendedor, ftVendedor, ftCondPagto, ftOperacao, tbEntidade, tbCidade, prProduto, fiNotaFiscal, fiNotaFiscalPedido e as views ViewEstoqueProduto e ViewPedidosComDevolucao.

## Consultas de pedidos (padrão para novas consultas legadas)

O banco é o de um ERP legado (views e funções como `viewPedidosComDevolucao`, `dbo.CalcularMargemLucroPedidoNF`), então as consultas de relatório são **T-SQL parametrizado**, não LINQ:

- SQL em `Infrastructure/Pedidos/Sql/*.sql` (EmbeddedResource), executado por `PedidoQueries` via `Database.SqlQueryRaw<T>`. Listas de inteiros entram por placeholders `{{EMPRESAS}}`/`{{TIPOS_MOVIMENTO}}`; todo o resto é `SqlParameter`.
- O SQL deve ser um `SELECT` simples (sem `DECLARE`/CTE): o EF o envolve em subquery. Cada coluna é convertida com `CAST` explícito para o tipo do DTO; sem isso o EF falha com tinyint/smallint/varchar inesperados. Alias da coluna = nome da propriedade do DTO.
- Contrato (`IPedidoQueries`, `FiltroPedidos`, DTOs) fica em `Application/Pedidos`; GraphQL expõe `relacaoPedidos` e `relacaoItensPedidos` (mesmo filtro). Datas do filtro são `DateOnly` (`"yyyy-MM-dd"`).
- Tipos reais no banco: `IdEntidade` é **varchar** (ex.: `C000429`); `StatusPedido`/`TipoPedido` são smallint; demais ids são int. Confira em `INFORMATION_SCHEMA.COLUMNS` antes de mapear novas colunas.
- `idUsuario` do filtro controla a visibilidade de custo/margem/comissão (`tbUsuario.TipoCustoMargem`).

## Desempenho mensal (painel de vendas)

`desempenhoMensal(ano, mes, idsEmpresa, idUsuario)` devolve o mês já agregado (loja, ranking de vendedores internos, série diária, últimas vendas). Implementado em `Application/Desempenho/DesempenhoService`, que agrega em memória as linhas de `relacaoPedidos` (herda devoluções, custo e permissão do usuário; não duplica SQL). Definições: realizado = venda bruta − devoluções; ticket = venda bruta ÷ pedidos; margem = (realizado − custo) ÷ realizado, onde o custo das devoluções já vem negativo. O painel (artefato "Painel de vendas") consome um instantâneo JSON dessa query; para atualizá-lo é preciso consultar a API de novo e reenviar o arquivo.

## Conector MCP e chave de API

A API também é um **servidor MCP** (HTTP sem estado, `ModelContextProtocol.AspNetCore`) em `/mcp`, para o Claude e artefatos consumirem os indicadores como conector. Ferramenta única, somente leitura: `desempenho_periodo(ano, mes, quantidade=4)` (`Api/Mcp/PainelTools.cs`), que devolve o mesmo formato do instantâneo do painel (listas planas `loja`, `vendedores`, `porDia`, `ultimasVendas` com coluna `mes`) e guarda o resultado em cache por `Painel:CacheMinutos`.

- Empresas e usuário do ERP usados pelo conector ficam **no servidor** (`Painel:IdsEmpresa`, `Painel:IdUsuario`): quem chama não escolhe, porque `IdUsuario` define se custo/margem aparecem.
- `Seguranca:ApiKey` protege `/mcp` **e** `/graphql` (`Security/ApiKeyMiddleware`), por `Authorization: Bearer <chave>` ou `X-Api-Key`; no `/mcp` também por `?apiKey=<chave>` (o diálogo de conector do claude.ai não tem campo de cabeçalho e, ao receber 401, tenta OAuth e falha). Vazia = sem proteção (só para uso local). Em qualquer ambiente exposto defina-a por variável de ambiente (`Seguranca__ApiKey`) ou user-secrets.
- `Seguranca:GraphQLAnonimo=true` libera **só `/graphql`** (nunca `/mcp`) para abrir o IDE no navegador numa demonstração local. Está ligado em `appsettings.Development.json` (só vale com `ASPNETCORE_ENVIRONMENT=Development`; nunca coloque no `appsettings.json`) e nunca deve ficar ativo com a API exposta por túnel/servidor: o GraphQL dá acesso a pedidos e entidades (com CPF/CNPJ).
- Testar sem o Claude: POST em `/mcp` com `Accept: application/json, text/event-stream` e corpo JSON-RPC (`tools/list`, `tools/call`). Usar o conector no Claude Code: `claude mcp add --transport http painel http://localhost:5099/mcp --header "Authorization: Bearer <chave>"`.
- Limite conhecido: um artefato só alcança a API por conector MCP **remoto** registrado na conta claude.ai (a página bloqueia `fetch`). `localhost` não é alcançável por lá; para demonstrar com o artefato, expor a API por um túnel HTTPS. Não confirmado: se o claude.ai aceita chave estática em conector personalizado ou exige OAuth.

## Comandos SQL dinâmicos (catálogo)

Consultas novas **sem deploy**: ficam em `Analytics.ComandoSQL` (script em `Infrastructure/Comandos/Analytics.ComandoSQL.sql`, rodado à mão: a API só lê) e o Claude as usa pelo MCP com `listar_comandos` e `executar_comando(id, parametros, resultado?)` (`Api/Mcp/ComandosTools.cs`). Versão simplificada do `fwRelatorio` do ERP legado, com JSON no lugar de XML.

- Colunas: `ChaveEmpresa`+`Id` (PK), `Descricao`, `Parametros` (JSON), `Regras` (JSON), `SQL`, `Ativa`, `MaxLinhas`, `TimeoutSeg`. O servidor só enxerga a `ChaveEmpresa` de `Comandos:ChaveEmpresa` (vazia = ferramentas recusam); o chamador não escolhe empresa, usuário nem SQL. Empresas e usuário vêm de `Painel:IdsEmpresa`/`Painel:IdUsuario`, por parâmetros com `"origem":"Servidor"` (`chave`: `idsEmpresa` ou `idUsuario`) que o chamador não consegue informar.
- Interpretador (`Application/Comandos/InterpretadorSql.cs`, lógica pura): `;` comentário, `%Regra%` trecho escolhido por regra, `[Param]` valor (`[Param:1]` = 2º valor do between), `{Param}` condição pronta (`{Param as p.Col}` troca a coluna), linha com `#` só entra se os parâmetros citados tiverem valor. Tudo vira `SqlParameter` (`@p1..`): **ao contrário do legado, nenhum valor entra no texto do SQL**. Só nomes declarados são substituídos (`[dbo].[T]` e `LIKE '%a%'` passam intactos).
- Parâmetro: `id`, `campo`, `descricao`, `tipo` (String|Inteiro|Decimal|Date|DateTime|Checkbox), `valorUnico`, `obrigatorio`, `padrao`/`padrao2`, `condicaoFixa`, `origem`, `chave`. Datas ISO (`yyyy-MM-dd`); `Date` compara o dia inteiro (coluna pode ter hora). Strings vão como `varchar` (colunas do ERP são varchar).
- Opções (combobox): `opcoes: [{"valor":"P.IdGrupo","rotulo":"Grupo"}]` no parâmetro (valor `""` = "sem valor"). A tela renderiza `<select>` na aba Testar, no padrão e nas condições das regras; o servidor **só aceita valores da lista** (vale também para o Claude, que as vê em `listar_comandos`). A importação do legado converte `ListaPesquisa` (`''=Sem Quebra; P.IdGrupo=Grupo`) em opções.
- Regra: `{"id":..., "casos":[{"se":[{"param":..,"op":"preenchido|vazio|igual|diferente|maior|maiorIgual|menor|menorIgual","valor":..}],"sql":"..."}], "senao":"..."}`. Condições estruturadas: não há avaliador de expressões C# como no legado. O trecho de uma regra é texto livre, expandido em qualquer ponto da linha: serve para listas de colunas (`select a, b, %Colunas%`) e pode usar outras regras (`%Outra%`, até 10 níveis) e parâmetros (`[P]`, `{P}`). Referência circular é recusada ao salvar (`ValidadorComando`). As condições (`se`) só leem parâmetros.
- Passos: a coluna `SQL` é um SELECT único em texto **ou** uma lista JSON `[{"ordem":1,"nome":"loja","sql":"...","somenteExecucao":false}, ...]` (`PassoSql`). Passos rodam por `ordem` (empate = ordem de aparição), na mesma conexão (`#temp` de um passo vale nos seguintes) e dividem os parâmetros e regras do comando. Passo que devolve dados tem `nome` (vazio = `resultado1`, `resultado2`...); `somenteExecucao:true` roda sem devolver nada. Um comando = um propósito (ex.: `painel` tem 4 resultados), em vez de uma linha por consulta.
- Chamada: valor simples = igual; `{"op":"between","valor":..,"valor2":..}`; lista = itens combinados com OU. Resultado `{resultados:[{nome, colunas, linhas[][], truncado}]}`, um por passo que devolve dados (1º conjunto de cada um; teto de 5000 linhas / 120 s imposto pela API). `executar_comando(id, parametros, resultado?)`: com `resultado` roda só esse conjunto (mais os passos de execução), o que evita pagar pelos 4 SELECTs quando o painel pede um.
- Escrita: comandos com passo `somenteExecucao` são recusados por `executar_comando` (somente leitura). Só `executar_comando_com_escrita` (ReadOnly=false, Destructive) os roda, numa transação (rollback se algo falhar), e só se `Comandos:PermitirEscrita=true` (padrão false). O limite real continua sendo o login do banco.
- Erros de uso (`ComandoInvalidoException`) viram `McpException` para a mensagem chegar ao chamador; qualquer outra exceção do MCP vira "An error occurred" (a causa só aparece no log do servidor).
- O SQL do catálogo é código executável: quem edita o catálogo é de confiança, e o login do banco deve ter só leitura. O SQL não é exposto pelo MCP.

## Tela de manutenção do catálogo (frontend React)

Pasta `frontend/` (React 19 + Vite + TypeScript, sem biblioteca de UI): CRUD de `Analytics.ComandoSQL` com abas Geral, Parâmetros, Regras, SQL (passos) e **Testar**. Responsiva (lista e editor viram telas separadas no celular), tema claro/escuro.

- Desenvolvimento: `cd frontend && npm install && npm run dev` (proxy de `/api` para `VITE_API_URL`, padrão `http://localhost:5094`). Produção: `npm run build` grava em `src/ErpWebAnalytics.Api/wwwroot/admin`, servido pela API em `/admin/` (caminhos relativos, funciona sob caminho virtual como `/Homologacao/ErpWebAnalytics/admin/`). Rode o build antes do `dotnet publish`.
- Listas longas (abas Parâmetros, Regras e SQL): barra fixa no topo da lista (`frontend/src/tabs/navegacao.tsx`) com filtro por texto, seletor "Ir para…", anterior/próximo e recolher/expandir tudo; cartões recolhíveis (listas com mais de 8 itens abrem recolhidas, com resumo de uma linha) e itens novos rolam até a vista. No celular a barra para logo abaixo do cabeçalho e das abas (`--nav-top`, calculado em `App.tsx`).
- API em `/admin/api` (`Api/Admin/ComandosAdminEndpoints.cs`): `GET /info`, `GET/POST /comandos`, `PUT/DELETE /comandos/{id}`, `POST /testar`. Protegida por `Seguranca:AdminKey` no cabeçalho `X-Admin-Key`; **sem a chave configurada o módulo fica desligado (404)**, nunca aberto. A empresa é sempre `Comandos:ChaveEmpresa`.
- **É a única escrita da API**: o login do banco precisa de INSERT/UPDATE/DELETE em `Analytics.ComandoSQL`. `ValidadorComando` valida tudo antes de gravar (ids, tipos, padrões, regras, nomes de resultados, ao menos um passo que devolve dados) para o MCP nunca achar uma linha ilegível; a coluna `SQL` é sempre gravada como lista JSON de passos.
- Testar (`IComandoSqlService.TestarAsync`) usa o **rascunho** sem gravar, com `empresa/usuário` do servidor. "Prévia" só gera o SQL final (com `@pN` e valores); "Executar" roda. Comando com passo de execução só executa se `Comandos:PermitirEscrita=true`, e então numa transação **sempre desfeita** (`ModoTransacao.Reverter`).
- **Importação do ERP legado** (botão "Importar do ERP legado"): lê `fwRelatorio` e `fwRelatorioDinamico` (`Importacao:Tabelas` troca a lista) **sem entities**, por SQL puro em `Infrastructure/Comandos/LegadoRelatorioLeitor.cs`, para funcionar em outro banco. Cada tabela só é oferecida se existir com as colunas `IdRelatorio, Titulo, XmlParametros, XmlRegras, XmlComandos` (senão `/admin/api/legado` devolve `[]` e o botão some). A conversão é pura (`Application/Comandos/Legado/ConversorRelatorioLegado.cs`) e gera um **rascunho inativo** que a tela abre como comando novo, com a lista de avisos; nada grava até o usuário salvar. Traduz: parâmetros (tipos, padrões, `SelecaoEmpresas`/`UsuarioLogado` -> origem servidor), regras `$SE/$SENAOSE/$SENAO/$FIMSE` -> casos (condições simples com `&&`; `||`, aninhamento e texto fora do bloco ficam como texto original no 'senão' + aviso), `'[X]'` -> `[X]`, `col in [X]` -> `{X as col}`, nome de coluna colado (`[Ordem]`) -> regra com lista fechada, comandos -> passos (`FlagExecucao` -> somente execução). Dos 71 relatórios do banco de teste, 66 convertem, validam e geram SQL; os demais pedem ajuste manual (aviso).
- `ValidadorComando`/`DetectorEscrita`: passo que devolve dados (e as regras que ele usa) não pode conter insert/update/delete/drop/create/alter/merge/truncate, senão `executar_comando` (somente leitura) escreveria; escrita em `#temp`/`@tabela` é permitida. Exige marcar o passo como somente execução.
- O SQL digitado na tela roda no banco como o do catálogo: quem tem a chave de administração é de confiança, e o login deve ter o mínimo de permissões. Troque a chave padrão e não a exponha na URL.

## Perfil do GraphQL por sistema (ERPWEB, RSYS2003, CHAMADOS)

A API serve **um sistema por instalação**, escolhido em `Sistema:Perfil`. Os sistemas são ERPs diferentes (tabelas e colunas distintas, todos SQL Server), então o que o GraphQL expõe é um **perfil** salvo em `Analytics.PerfilGraphQL` (script `Infrastructure/Perfis/Analytics.PerfilGraphQL.sql`, rodado à mão), em vez de entities compiladas. Schema **nativo** do sistema: os nomes são os que o perfil define.

- Perfil (`Application/Perfis/PerfilGraphQL.cs`): tabelas (`tabela` = `schema.nome`, `nome` do tipo, `consulta` na raiz), colunas (`coluna` -> `campo`, `filtravel`, `ordenavel`) e relações (`paraUm`/`paraMuitos`, por pares de colunas, alvo precisa estar exposto). **Nada é exposto por padrão**; colunas com nome de dado sensível (cpf, cnpj, senha, token, custo, margem...) começam desmarcadas e geram aviso se expostas.
- O esquema real vem do próprio banco (`Infrastructure/Perfis/EsquemaBancoLeitor.cs`, `sys.*`, sem entities); `ValidadorPerfil` confere o perfil contra ele (colunas inexistentes, tipos não suportados como timestamp/binário, nomes GraphQL inválidos ou repetidos, relação para tabela não exposta).
- Na subida (`PerfilInicializador`) o perfil é carregado e validado; problemas viram log e o `PerfilAtivo` os guarda, mas **a API nunca deixa de subir** por causa do perfil (o GraphQL EF atual e os comandos seguem funcionando). Sem `Sistema:Perfil` o recurso fica desligado.
- Tela: aba **GraphQL** (alternador Comandos | GraphQL, só aparece com `Sistema:Perfil` configurado). API em `/admin/api/graphql/` (`banco/tabelas`, `banco/tabela?chave=`, `perfil` GET/PUT, `perfil/validar`), mesma chave de administração. O perfil editado é sempre o do sistema configurado.
- **GraphQL dinâmico** (`Api/GraphQL/Dinamico/PerfilTypeModule.cs`): um `TypeModule` do HotChocolate monta tipos e consultas a partir do perfil. Por tabela exposta: tipo `<Nome>`, `<Nome>Pagina {itens, temMais, total}`, `<Nome>Filtro` (só colunas `filtravel`; operadores `igual, diferente, maior, maiorOuIgual, menor, menorOuIgual, contem, comecaCom, em, nulo`; agrupar com `E`/`OU`) e `<Nome>Ordem` (só colunas `ordenavel`); consulta raiz `<consulta>(primeiro<=500, pular, onde, ordenarPor)`. Relações: `paraUm` e `paraMuitos(primeiro<=100, pular)`.
  - SQL em `Application/Perfis/Dinamico/ConstrutorConsulta.cs` (função pura): todo valor é parâmetro tipado como a coluna; todo nome de coluna é conferido contra a estrutura real e citado `[col]`; `like` escapa `% _ [`. `total` só é contado se pedido. Execução em `Infrastructure/Perfis/ExecutorDinamico.cs` (ADO, uma conexão por consulta, timeout 30 s).
  - Relações usam o **escopo de DataLoaders do HotChocolate** (`IDataLoaderScope`, agendador `BatchDispatcher`) para juntar os pedidos num só `where ... in (...)`: 20 estados com cidades = 2 consultas. Não use `IBatchScheduler` direto do `ctx.Services`: é o `AutoBatchScheduler`, que não espera os resolvers irmãos e volta ao N+1.
  - `Program.cs`: com `Sistema:Perfil` vazio ou `ERPWEB` vale o GraphQL clássico (EF) em `/graphql`, e o do perfil, se houver, é um **schema à parte em `/graphql/perfil`** (o clássico já tem tipos `Cidade`, `Entidade`...; no mesmo schema os nomes colidiriam, o HotChocolate falharia ao remontar e o perfil nunca apareceria). Com outro sistema (RSYS2003, CHAMADOS...) só o dinâmico, em `/graphql`, pois as tabelas do ERPWEB não existem lá. `AddTypeModule<T>` exige `T` no DI.
  - **Recarga ao vivo**: `PerfilAtivo.Definir` (na subida e ao salvar na tela) dispara `TypesChanged` e o schema é remontado sem reiniciar. Profundidade máxima de execução 8.
  - **Mutations de atualização** (`atualizar<Tipo>`): o perfil marca a tabela como `atualizavel` e cada coluna como `editavel` (nunca chave primária/identity; só tabela com chave primária, não view; exige ao menos uma coluna `filtravel`). Só existem no schema com `Comandos:PermitirEscrita=true` **e** `Seguranca:ApiKeyEscrita` configurada (a chave de escrita, **separada** da de leitura, vai em `X-Write-Key`; `mutation { validarChaveEscrita }` confere). Argumentos: `onde` (obrigatório, com ao menos uma condição), `valores` (só os campos informados mudam; `null` limpa coluna que aceita nulo), `limite` (padrão 1, máximo 100) e `simular` (executa tudo e desfaz). `Infrastructure/Perfis/ExecutorDinamico.AtualizarAsync`: numa transação, lê as chaves que o filtro atinge (limite + 1), recusa e desfaz se passar do limite, atualiza **por chave primária** (nunca pelo filtro do cliente) e relê as linhas. Recusas viram erros GraphQL com `extensions.code` (`ESCRITA_NAO_AUTORIZADA`, `FILTRO_OBRIGATORIO`, `ATUALIZACAO_RECUSADA`, `NADA_A_ATUALIZAR`, `VALOR_INVALIDO`, `ERRO_BANCO`); cada atualização gera log (tabela, colunas, linhas; sem valores). A raiz `Mutation` é registrada no builder (`AddPerfilDinamico`); o módulo só a estende. O login do banco precisa de UPDATE nas tabelas.
  - Introspecção: o HotChocolate a bloqueia fora de Development (`Introspection is not allowed`). Para o Claude ler o schema em produção é preciso liberá-la (decisão de segurança ainda em aberto).

## Configuração

A connection string fica em `ConnectionStrings:DefaultConnection` de `src/ErpWebAnalytics.Api/appsettings.json`, hoje com a string real (contém credenciais; mover para user-secrets antes de versionar). A API não sobe sem essa chave. O servidor usa certificado SSL não confiável, por isso a string inclui `TrustServerCertificate=True`. Não versione credenciais reais: use user-secrets (`dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..." --project src/ErpWebAnalytics.Api`) ou variável de ambiente.

## Comandos

```
dotnet build
dotnet run --project src/ErpWebAnalytics.Api
dotnet test                                              # ainda não há projeto de testes
dotnet test --filter "FullyQualifiedName~NomeDoTeste"    # um único teste
```

O banco é de um ERP existente (banco-primeiro): **não use migrations**; o esquema muda no ERP e as entities são regeneradas por scaffold (ver acima). A API só lê.

## Versões

Fixar pacotes EF Core em **9.x**: o `dotnet add package` sem versão traz EF Core 11, incompatível com net9.0. HotChocolate está na 16.x.

## Ainda em aberto

Autenticação/autorização, formato e armazenamento dos artefatos exportados (HTML/PDF/XLSX) e framework de testes.

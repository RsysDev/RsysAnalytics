using System.Text.Json;
using System.Text.Json.Serialization;
using ErpWebAnalytics.Api.Admin;
using ErpWebAnalytics.Api.GraphQL;
using ErpWebAnalytics.Api.GraphQL.Dinamico;
using ErpWebAnalytics.Api.GraphQL.Types;
using ErpWebAnalytics.Api.Mcp;
using ErpWebAnalytics.Api.Perfis;
using ErpWebAnalytics.Api.Security;
using ErpWebAnalytics.Infrastructure;
using ErpWebAnalytics.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

// JSON dos endpoints /admin/api: enums como texto camelCase ("maiorIgual", "date"), igual ao gravado no catálogo.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<PainelOptions>(builder.Configuration.GetSection(PainelOptions.Secao));
builder.Services.Configure<ComandosOptions>(builder.Configuration.GetSection(ComandosOptions.Secao));
builder.Services.Configure<SistemaOptions>(builder.Configuration.GetSection(SistemaOptions.Secao));
builder.Services.AddSingleton<PerfilAtivo>();
builder.Services.AddHostedService<PerfilInicializador>();

// ERPWEB (e instalações sem Sistema:Perfil) mantêm o GraphQL clássico, tipado, em EF. Os demais sistemas (RSYS2003, CHAMADOS...)
// têm só o GraphQL dinâmico, montado a partir do perfil gravado em Analytics.PerfilGraphQL: as tabelas do ERPWEB nem existem lá.
var sistemaPerfil = builder.Configuration[$"{SistemaOptions.Secao}:Perfil"] ?? "";
var classico = new SistemaOptions { Perfil = sistemaPerfil }.Classico;

var graphql = builder.Services.AddGraphQLServer();
if (classico)
{
    graphql
        .AddQueryType<Query>()
        .AddTypes(typeof(PedidoType).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, Namespace: "ErpWebAnalytics.Api.GraphQL.Types" } && t.Name.EndsWith("Type"))
            .ToArray())
        .AddProjections()
        .AddFiltering()
        .AddSorting()
        .RegisterDbContextFactory<AppDbContext>();
}
else
{
    graphql.AddQueryType(d => d.Name("Query"));
}

const string SchemaPerfil = "perfil";
// Mutations (atualização) só existem com escrita liberada no servidor E uma chave de escrita própria configurada.
var mutacoes = PerfilTypeModule.MutacoesAtivas(
    new ComandosOptions { PermitirEscrita = builder.Configuration.GetValue<bool>("Comandos:PermitirEscrita") }, builder.Configuration);
if (!string.IsNullOrWhiteSpace(sistemaPerfil))
{
    builder.Services.AddSingleton<PerfilTypeModule>(); // o HotChocolate resolve o módulo pelo DI
    if (classico)
    {
        // ERPWEB já tem tipos com nomes de tabelas (Cidade, Entidade...): tipos do perfil no mesmo schema colidiriam e o HotChocolate
        // deixaria de remontar o schema (e o perfil nunca apareceria). Por isso o GraphQL do perfil é um schema à parte: /graphql/perfil.
        builder.Services.AddGraphQLServer(SchemaPerfil)
            .AddQueryType(d => d.Name("Query"))
            .AddPerfilDinamico(mutacoes);
    }
    else
    {
        // Sem GraphQL clássico (RSYS2003, CHAMADOS...): o dinâmico é o /graphql.
        graphql.AddPerfilDinamico(mutacoes);
    }
}

// Servidor MCP (HTTP sem estado): permite que o Claude e artefatos consumam os indicadores como conector.
builder.Services
    .AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithTools<PainelTools>()
    .WithTools<ComandosTools>();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Seguranca:GraphQLAnonimo"))
    app.Logger.LogWarning("Seguranca:GraphQLAnonimo está ativo: /graphql aceita acesso anônimo. Use apenas em demonstração local, nunca exposto a terceiros.");

app.UseMiddleware<ApiKeyMiddleware>();

// Tela de manutenção do catálogo (React, publicada em wwwroot/admin). Sem Seguranca:AdminKey a API /admin/api fica desligada.
// "/admin" sem a barra final não serve o index.html (e os caminhos relativos do app quebrariam): redireciona para "/admin/".
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.Equals("/admin", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.Redirect($"{ctx.Request.PathBase}/admin/{ctx.Request.QueryString}");
        return;
    }
    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapComandosAdmin();

app.MapGraphQL();
if (classico && !string.IsNullOrWhiteSpace(sistemaPerfil)) app.MapGraphQL("/graphql/perfil", SchemaPerfil);
app.MapMcp("/mcp");

app.Run();

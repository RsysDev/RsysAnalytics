using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErpWebAnalytics.Api.Mcp;
using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Application.Comandos.Legado;
using Microsoft.Extensions.Options;

namespace ErpWebAnalytics.Api.Admin;

/// <summary>Corpo de POST /admin/api/legado/{tabela}/converter (o id vai no corpo: pode ter caracteres que a URL não aceita).</summary>
public sealed record ConverterLegadoRequest(string IdRelatorio);

/// <summary>Corpo de POST /admin/api/testar: o rascunho do comando (ainda não gravado) e os valores de teste.</summary>
public sealed record TesteRequest(
    ComandoSql Comando, Dictionary<string, JsonElement>? Parametros, string? Resultado, bool SoPrevia);

/// <summary>
/// API da tela de manutenção do catálogo (<c>/admin/api</c>). Protegida por <c>Seguranca:AdminKey</c> no cabeçalho
/// <c>X-Admin-Key</c>; sem a chave configurada o módulo inteiro fica desligado (404), nunca aberto.
/// A empresa vem de <c>Comandos:ChaveEmpresa</c>: quem chama não escolhe.
/// </summary>
public static class ComandosAdminEndpoints
{
    public static void MapComandosAdmin(this WebApplication app)
    {
        var grupo = app.MapGroup("/admin/api").AddEndpointFilter(ExigirChave);
        grupo.MapPerfilAdmin();

        grupo.MapGet("/info", (IOptions<ComandosOptions> o, IOptions<ErpWebAnalytics.Api.Perfis.SistemaOptions> sistema, IConfiguration cfg) => new
        {
            mutacoesAtivas = ErpWebAnalytics.Api.GraphQL.Dinamico.PerfilTypeModule.MutacoesAtivas(o.Value, cfg),
            chaveEmpresa = o.Value.ChaveEmpresa,
            sistema = sistema.Value.Perfil,
            permitirEscrita = o.Value.PermitirEscrita,
            chavesServidor = new[] { "idsEmpresa", "idUsuario" },
        });

        grupo.MapGet("/comandos", (IComandoSqlAdminService admin, IOptions<ComandosOptions> o, CancellationToken ct) =>
            Executar(async () => Results.Ok(await admin.ListarAsync(Chave(o), ct))));

        grupo.MapPost("/comandos", (ComandoSql comando, IComandoSqlAdminService admin, IOptions<ComandosOptions> o, CancellationToken ct) =>
            Executar(async () =>
            {
                await admin.SalvarAsync(Chave(o), comando, criar: true, ct);
                return Results.Created($"/admin/api/comandos/{Uri.EscapeDataString(comando.Id)}", comando);
            }));

        grupo.MapPut("/comandos/{id}", (string id, ComandoSql comando, IComandoSqlAdminService admin, IOptions<ComandosOptions> o, CancellationToken ct) =>
            Executar(async () =>
            {
                if (!string.Equals(id, comando.Id, StringComparison.Ordinal))
                    throw new ComandoInvalidoException("O id da URL é diferente do id do corpo. Para renomear, crie o novo comando e exclua o antigo.");
                await admin.SalvarAsync(Chave(o), comando, criar: false, ct);
                return Results.Ok(comando);
            }));

        grupo.MapDelete("/comandos/{id}", (string id, IComandoSqlAdminService admin, IOptions<ComandosOptions> o, CancellationToken ct) =>
            Executar(async () =>
            {
                await admin.ExcluirAsync(Chave(o), id, ct);
                return Results.NoContent();
            }));

        // Importação do ERP legado: só responde com dados se as tabelas existirem neste banco (lista vazia = nada a oferecer).
        grupo.MapGet("/legado", (IImportacaoLegadoService legado, CancellationToken ct) =>
            Executar(async () => Results.Ok(await legado.TabelasAsync(ct))));

        grupo.MapGet("/legado/{tabela}", (string tabela, IImportacaoLegadoService legado, IOptions<ComandosOptions> o, CancellationToken ct) =>
            Executar(async () => Results.Ok(await legado.ListarAsync(Chave(o), tabela, ct))));

        grupo.MapPost("/legado/{tabela}/converter", (string tabela, ConverterLegadoRequest req, IImportacaoLegadoService legado, CancellationToken ct) =>
            Executar(async () => Results.Ok(await legado.ConverterAsync(tabela, req.IdRelatorio, ct))));

        grupo.MapPost("/testar", (TesteRequest req, IComandoSqlService servico, IOptions<ComandosOptions> o, IOptions<PainelOptions> painel, CancellationToken ct) =>
            Executar(async () =>
            {
                var contexto = new ContextoComando(Chave(o), painel.Value.IdsEmpresa, painel.Value.IdUsuario);
                var teste = await servico.TestarAsync(
                    contexto, req.Comando, req.Parametros, req.Resultado, req.SoPrevia, o.Value.PermitirEscrita, ct);
                return Results.Ok(teste);
            }));
    }

    private static string Chave(IOptions<ComandosOptions> o) =>
        string.IsNullOrWhiteSpace(o.Value.ChaveEmpresa)
            ? throw new ComandoInvalidoException("Comandos:ChaveEmpresa não está configurada no servidor.")
            : o.Value.ChaveEmpresa;

    /// <summary>Erros de uso viram 400 com a mensagem; erros de SQL (o texto do SQL é do administrador) viram 422 com a mensagem do banco.</summary>
    private static async Task<IResult> Executar(Func<Task<IResult>> acao)
    {
        try { return await acao(); }
        catch (ComandoInvalidoException e) { return Results.BadRequest(new { erro = e.Message }); }
        catch (Microsoft.Data.SqlClient.SqlException e) { return Results.UnprocessableEntity(new { erro = e.Message }); }
    }

    private static async ValueTask<object?> ExigirChave(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var esperada = ctx.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Seguranca:AdminKey"];
        if (string.IsNullOrEmpty(esperada)) return Results.NotFound(); // módulo desligado

        var enviada = ctx.HttpContext.Request.Headers["X-Admin-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(enviada) ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(enviada)), SHA256.HashData(Encoding.UTF8.GetBytes(esperada))))
            return Results.Json(new { erro = "Chave de administração inválida." }, statusCode: StatusCodes.Status401Unauthorized);

        return await next(ctx);
    }
}

using ErpWebAnalytics.Api.Mcp;
using ErpWebAnalytics.Api.Perfis;
using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Application.Perfis;
using Microsoft.Extensions.Options;

namespace ErpWebAnalytics.Api.Admin;

/// <summary>
/// Manutenção do perfil GraphQL do sistema configurado (<c>Sistema:Perfil</c>). Mesma proteção do restante de /admin/api
/// (chave de administração). O perfil editado é sempre o do sistema desta instalação: quem chama não escolhe.
/// </summary>
public static class PerfilAdminEndpoints
{
    public static void MapPerfilAdmin(this RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/graphql");

        g.MapGet("/banco/tabelas", (string? busca, IEsquemaBancoLeitor esquema, CancellationToken ct) =>
            Executar(async () => Results.Ok(await esquema.ListarAsync(busca, ct))));

        // A chave tem ponto (dbo.tabela): vai na query string.
        g.MapGet("/banco/tabela", (string chave, IEsquemaBancoLeitor esquema, CancellationToken ct) =>
            Executar(async () => await esquema.ObterAsync(chave, ct) is { } t
                ? Results.Ok(t)
                : Results.NotFound(new { erro = $"A tabela '{chave}' não existe neste banco." })));

        g.MapGet("/perfil", (IPerfilService perfis, IOptions<SistemaOptions> sistema, IOptions<ComandosOptions> comandos, CancellationToken ct) =>
            Executar(async () =>
            {
                var s = Sistema(sistema);
                var perfil = await perfis.ObterAsync(Chave(comandos), s, ct);
                return Results.Ok(new { sistema = s, perfil, problemas = await perfis.ValidarAsync(perfil, ct) });
            }));

        g.MapPost("/perfil/validar", (PerfilGraphQL perfil, IPerfilService perfis, IOptions<SistemaOptions> sistema, CancellationToken ct) =>
            Executar(async () => Results.Ok(await perfis.ValidarAsync(ComSistema(perfil, Sistema(sistema)), ct))));

        // Ao salvar, o schema dinâmico é remontado na hora (PerfilAtivo avisa o módulo de tipos): sem reiniciar a API.
        g.MapPut("/perfil", (PerfilGraphQL perfil, IPerfilService perfis, PerfilAtivo ativo, IOptions<SistemaOptions> sistema, IOptions<ComandosOptions> comandos, CancellationToken ct) =>
            Executar(async () =>
            {
                var completo = ComSistema(perfil, Sistema(sistema));
                var problemas = await perfis.SalvarAsync(Chave(comandos), completo, ct);
                var r = await perfis.CompilarAsync(completo, ct);
                ativo.Definir(completo, r.Compilado, r.Problemas);
                return Results.Ok(problemas);
            }));
    }

    private static string Sistema(IOptions<SistemaOptions> o) =>
        string.IsNullOrWhiteSpace(o.Value.Perfil) ? throw new ComandoInvalidoException("Sistema:Perfil não está configurado no servidor.") : o.Value.Perfil;

    private static string Chave(IOptions<ComandosOptions> o) =>
        string.IsNullOrWhiteSpace(o.Value.ChaveEmpresa) ? throw new ComandoInvalidoException("Comandos:ChaveEmpresa não está configurada no servidor.") : o.Value.ChaveEmpresa;

    private static PerfilGraphQL ComSistema(PerfilGraphQL p, string sistema) => new() { Sistema = sistema, Tabelas = p.Tabelas };

    private static async Task<IResult> Executar(Func<Task<IResult>> acao)
    {
        try { return await acao(); }
        catch (ComandoInvalidoException e) { return Results.BadRequest(new { erro = e.Message }); }
        catch (Microsoft.Data.SqlClient.SqlException e) { return Results.UnprocessableEntity(new { erro = e.Message }); }
    }
}

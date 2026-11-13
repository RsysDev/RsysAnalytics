using System.Security.Cryptography;
using System.Text;

namespace ErpWebAnalytics.Api.Security;

/// <summary>
/// Exige a chave de API nos endpoints /graphql e /mcp quando <c>Seguranca:ApiKey</c> está configurada.
/// A chave segue em <c>Authorization: Bearer &lt;chave&gt;</c> ou <c>X-Api-Key</c>. Sem chave configurada (desenvolvimento local), não exige nada.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var esperada = config["Seguranca:ApiKey"];
        // Interruptor só para demonstração local no navegador: libera /graphql, nunca /mcp.
        var graphQlAnonimo = config.GetValue<bool>("Seguranca:GraphQLAnonimo");
        var protegido = ctx.Request.Path.StartsWithSegments("/mcp")
            || (!graphQlAnonimo && ctx.Request.Path.StartsWithSegments("/graphql"));

        if (!string.IsNullOrEmpty(esperada) && protegido)
        {
            var enviada = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
            // Conectores do claude.ai não enviam cabeçalho fixo: no /mcp a chave também pode ir na URL (?apiKey=...).
            // Menos seguro (a URL pode aparecer em logs); troque a chave ao sair da homologação.
            if (string.IsNullOrEmpty(enviada) && ctx.Request.Path.StartsWithSegments("/mcp"))
                enviada = ctx.Request.Query["apiKey"].FirstOrDefault();

            if (string.IsNullOrEmpty(enviada))
            {
                var auth = ctx.Request.Headers.Authorization.FirstOrDefault();
                if (auth is not null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    enviada = auth["Bearer ".Length..].Trim();
            }

            if (string.IsNullOrEmpty(enviada) || !IguaisEmTempoConstante(enviada, esperada))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.Headers.WWWAuthenticate = "Bearer";
                return;
            }
        }

        await next(ctx);
    }

    private static bool IguaisEmTempoConstante(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)), SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}

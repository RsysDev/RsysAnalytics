using System.ComponentModel;
using ErpWebAnalytics.Application.Desempenho;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace ErpWebAnalytics.Api.Mcp;

/// <summary>Empresas e usuário usados pelo conector MCP. Ficam no servidor: quem chama não escolhe o que pode ver.</summary>
public class PainelOptions
{
    public const string Secao = "Painel";

    /// <summary>Empresas (IdEmpresa) incluídas nos indicadores.</summary>
    public List<int> IdsEmpresa { get; set; } = [1];

    /// <summary>Usuário do ERP cujas permissões de custo/margem valem para o conector.</summary>
    public int IdUsuario { get; set; } = 1;

    /// <summary>Minutos que o resultado de um mês fica em cache.</summary>
    public int CacheMinutos { get; set; } = 5;
}

/// <summary>Ferramentas MCP (somente leitura) para painéis de vendas.</summary>
[McpServerToolType]
public sealed class PainelTools(IDesempenhoService desempenho, IMemoryCache cache, IOptions<PainelOptions> options)
{
    [McpServerTool(Name = "desempenho_periodo", Title = "Desempenho de vendas por período", ReadOnly = true, Idempotent = true)]
    [Description(
        "Indicadores de vendas dos últimos N meses até o mês informado, em listas planas com a coluna 'mes' (yyyy-MM): " +
        "loja (venda bruta, devoluções, realizado, pedidos, ticket médio, lucro bruto, margem %), " +
        "vendedores (os mesmos indicadores por vendedor interno), porDia (realizado e pedidos por dia) e " +
        "ultimasVendas (15 pedidos mais recentes do mês final). Realizado = venda bruta - devoluções. " +
        "Margem e lucro vêm zerados se o usuário do conector não puder ver custo.")]
    public Task<DesempenhoPeriodo> DesempenhoPeriodo(
        [Description("Ano do mês mais recente, ex.: 2026")] int ano,
        [Description("Mês mais recente, de 1 a 12")] int mes,
        [Description("Quantos meses trazer, de 1 a 12 (padrão 4)")] int quantidade = 4,
        CancellationToken ct = default)
    {
        if (ano is < 2000 or > 2100) throw new ArgumentOutOfRangeException(nameof(ano), "Ano inválido.");
        if (mes is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(mes), "Mês deve estar entre 1 e 12.");

        var o = options.Value;
        var chave = $"periodo:{ano}-{mes}:{quantidade}:{string.Join(",", o.IdsEmpresa)}:{o.IdUsuario}";
        return cache.GetOrCreateAsync(chave, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(0, o.CacheMinutos));
            return desempenho.ObterPeriodoAsync(ano, mes, quantidade, o.IdsEmpresa, o.IdUsuario, ct);
        })!;
    }
}

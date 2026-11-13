using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Application.Comandos.Legado;
using ErpWebAnalytics.Application.Perfis;
using ErpWebAnalytics.Application.Desempenho;
using ErpWebAnalytics.Infrastructure.Comandos;
using ErpWebAnalytics.Application.Pedidos;
using ErpWebAnalytics.Infrastructure.Pedidos;
using ErpWebAnalytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ErpWebAnalytics.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

        // Pool de DbContext: requerido pelo HotChocolate (RegisterDbContextFactory) para consultas paralelas.
        services.AddPooledDbContextFactory<AppDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        services.AddScoped<IPedidoQueries, PedidoQueries>();
        services.AddScoped<IDesempenhoService, DesempenhoService>();

        services.AddScoped<IComandoSqlCatalogo, ComandoSqlCatalogo>();
        services.AddScoped<IComandoSqlExecutor, ComandoSqlExecutor>();
        services.AddScoped<IComandoSqlService, ComandoSqlService>();
        services.AddScoped<IComandoSqlRepositorio, ComandoSqlRepositorio>();
        services.AddScoped<IComandoSqlAdminService, ComandoSqlAdminService>();
        services.AddScoped<ILegadoRelatorioLeitor, LegadoRelatorioLeitor>();
        services.AddScoped<IImportacaoLegadoService, ImportacaoLegadoService>();

        services.AddScoped<IEsquemaBancoLeitor, ErpWebAnalytics.Infrastructure.Perfis.EsquemaBancoLeitor>();
        services.AddScoped<IPerfilRepositorio, ErpWebAnalytics.Infrastructure.Perfis.PerfilRepositorio>();
        services.AddScoped<IPerfilService, PerfilService>();
        services.AddSingleton<ErpWebAnalytics.Application.Perfis.Dinamico.IExecutorDinamico, ErpWebAnalytics.Infrastructure.Perfis.ExecutorDinamico>();

        return services;
    }
}

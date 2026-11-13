using ErpWebAnalytics.Api.Mcp;
using ErpWebAnalytics.Application.Perfis;
using ErpWebAnalytics.Application.Perfis.Dinamico;
using Microsoft.Extensions.Options;

namespace ErpWebAnalytics.Api.Perfis;

/// <summary>Qual sistema esta instalação serve. Uma instalação serve sempre um só (ERPWEB, RSYS2003, CHAMADOS...).</summary>
public class SistemaOptions
{
    public const string Secao = "Sistema";

    /// <summary>Nome do perfil GraphQL a carregar de Analytics.PerfilGraphQL. Vazio = nenhum perfil dinâmico.</summary>
    public string Perfil { get; set; } = "";

    /// <summary>ERPWEB (e instalações sem perfil) mantêm o GraphQL clássico, tipado, em EF. Os demais sistemas só têm o dinâmico.</summary>
    public bool Classico => string.IsNullOrWhiteSpace(Perfil) || Perfil.Equals("ERPWEB", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Perfil carregado e compilado (cruzado com o banco). O GraphQL dinâmico lê daqui; ao salvar um perfil novo, <see cref="Definir"/>
/// avisa o módulo de tipos, que remonta o schema sem reiniciar a API.
/// </summary>
public sealed class PerfilAtivo
{
    private readonly TaskCompletionSource _pronto = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string Sistema { get; set; } = "";
    public PerfilGraphQL? Perfil { get; private set; }
    public PerfilCompilado Compilado { get; private set; } = new();
    public IReadOnlyList<ProblemaPerfil> Problemas { get; private set; } = [];

    /// <summary>Mensagem quando o perfil não pôde ser carregado (tabela ausente, JSON inválido...).</summary>
    public string? Falha { get; private set; }

    /// <summary>Conclui quando a primeira tentativa de carga terminou (com sucesso ou não). O schema espera por isto.</summary>
    public Task Pronto => _pronto.Task;

    public event EventHandler? Alterado;

    public void Definir(PerfilGraphQL perfil, PerfilCompilado compilado, IReadOnlyList<ProblemaPerfil> problemas)
    {
        Perfil = perfil; Compilado = compilado; Problemas = problemas; Falha = null;
        _pronto.TrySetResult();
        Alterado?.Invoke(this, EventArgs.Empty);
    }

    public void Falhou(string mensagem)
    {
        Falha = mensagem;
        _pronto.TrySetResult();
    }
}

/// <summary>
/// Na subida, carrega o perfil do sistema configurado e o valida contra o banco. Problemas viram log: a API nunca
/// deixa de subir por causa de um perfil (o GraphQL clássico e os comandos continuam funcionando).
/// </summary>
public sealed class PerfilInicializador(
    IServiceScopeFactory escopos, PerfilAtivo ativo, IOptions<SistemaOptions> sistema, IOptions<ComandosOptions> comandos,
    ILogger<PerfilInicializador> log) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        ativo.Sistema = sistema.Value.Perfil;
        if (string.IsNullOrWhiteSpace(sistema.Value.Perfil)) { log.LogInformation("Sistema:Perfil vazio: GraphQL dinâmico desligado."); ativo.Falhou("Sistema:Perfil não configurado."); return; }
        if (string.IsNullOrWhiteSpace(comandos.Value.ChaveEmpresa)) { ativo.Falhou("Comandos:ChaveEmpresa não está configurada."); log.LogWarning("Perfil não carregado: {Falha}", ativo.Falha); return; }

        try
        {
            await using var escopo = escopos.CreateAsyncScope();
            var servico = escopo.ServiceProvider.GetRequiredService<IPerfilService>();
            var perfil = await servico.ObterAsync(comandos.Value.ChaveEmpresa, sistema.Value.Perfil, ct);
            var r = await servico.CompilarAsync(perfil, ct);
            ativo.Definir(perfil, r.Compilado, r.Problemas);

            log.LogInformation("Perfil '{Sistema}': {Tabelas} tabela(s) exposta(s), {Ativas} no GraphQL, {Erros} erro(s), {Avisos} aviso(s).",
                perfil.Sistema, perfil.Tabelas.Count, r.Compilado.Tabelas.Count, r.Problemas.Count(p => p.Nivel == ProblemaPerfil.Erro), r.Problemas.Count(p => p.Nivel == ProblemaPerfil.Aviso));
            foreach (var p in r.Problemas) log.LogWarning("Perfil '{Sistema}' [{Nivel}]: {Mensagem}", perfil.Sistema, p.Nivel, p.Mensagem);
        }
        catch (Exception e)
        {
            ativo.Falhou(e.Message);
            log.LogWarning("Perfil '{Sistema}' não carregado: {Falha}", sistema.Value.Perfil, e.Message);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

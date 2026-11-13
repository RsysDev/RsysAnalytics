using System.ComponentModel;
using System.Text.Json;
using ErpWebAnalytics.Application.Comandos;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ErpWebAnalytics.Api.Mcp;

/// <summary>Chave da empresa dona do catálogo (Analytics.ComandoSQL.ChaveEmpresa). Fica no servidor: quem chama não escolhe.</summary>
public class ComandosOptions
{
    public const string Secao = "Comandos";
    public string ChaveEmpresa { get; set; } = "";

    /// <summary>
    /// Habilita executar_comando_com_escrita (comandos com passos "somente execução", que podem alterar dados).
    /// Desligado por padrão: com ele a API deixa de ser só leitura. O login do banco continua sendo o limite real.
    /// </summary>
    public bool PermitirEscrita { get; set; }
}

/// <summary>
/// Ferramentas MCP (somente leitura) sobre o catálogo de comandos SQL. Empresas e usuário vêm de <see cref="PainelOptions"/>,
/// os mesmos do painel, porque <c>IdUsuario</c> define o que custo/margem mostram.
/// </summary>
[McpServerToolType]
public sealed class ComandosTools(IComandoSqlService servico, IOptions<ComandosOptions> comandos, IOptions<PainelOptions> painel)
{
    [McpServerTool(Name = "listar_comandos", Title = "Consultas disponíveis", ReadOnly = true, Idempotent = true)]
    [Description(
        "Lista as consultas SQL do catálogo, com os parâmetros de cada uma (id, tipo, obrigatório, valor padrão). " +
        "Chame antes de executar_comando para descobrir o que existe e como preencher os parâmetros.")]
    public Task<IReadOnlyList<ComandoResumo>> ListarComandos(CancellationToken ct = default) =>
        servico.ListarAsync(Contexto(), ct);

    [McpServerTool(Name = "executar_comando", Title = "Executar consulta do catálogo", ReadOnly = true, Idempotent = true)]
    [Description(
        "Executa uma consulta do catálogo e devolve {resultados:[{nome, colunas, linhas, truncado}]}, um item por conjunto de dados; " +
        "cada linha é uma lista na ordem das colunas. 'resultado' (opcional) pede só um conjunto, pelo nome em listar_comandos. " +
        "Comandos com alteracaoDados=true só rodam em executar_comando_com_escrita. " +
        "'parametros' é um objeto cujas chaves são os ids de listar_comandos. Valor simples = igual (\"C000429\", 10, true). " +
        "Datas em ISO (yyyy-MM-dd). Intervalo: {\"op\":\"between\",\"valor\":\"2026-11-01\",\"valor2\":\"2026-11-30\"}. " +
        "Outros operadores (op): igual, diferente, maior, maiorIgual, menor, menorIgual, like, notLike. " +
        "Uma lista [..] combina os itens com OU. Parâmetros omitidos usam o padrão, se houver. " +
        "'truncado' = true indica que o limite de linhas foi atingido: filtre mais.")]
    public Task<ResultadoComando> ExecutarComando(
        [Description("Id da consulta, como em listar_comandos")] string id,
        [Description("Valores dos parâmetros, por id")] Dictionary<string, JsonElement>? parametros = null,
        [Description("Nome de um conjunto de dados do comando; omita para receber todos")] string? resultado = null,
        CancellationToken ct = default) =>
        Executar(id, parametros, resultado, permitirEscrita: false, ct);

    [McpServerTool(Name = "executar_comando_com_escrita", Title = "Executar comando que altera dados", ReadOnly = false, Destructive = true, Idempotent = false)]
    [Description(
        "Como executar_comando, mas aceita comandos com passos de execução (update/insert etc.), todos numa única transação. " +
        "ALTERA DADOS: use só quando o usuário pediu explicitamente e confirme antes. Desabilitada no servidor por padrão.")]
    public Task<ResultadoComando> ExecutarComandoComEscrita(
        [Description("Id do comando, como em listar_comandos")] string id,
        [Description("Valores dos parâmetros, por id")] Dictionary<string, JsonElement>? parametros = null,
        [Description("Nome de um conjunto de dados do comando; omita para receber todos")] string? resultado = null,
        CancellationToken ct = default)
    {
        if (!comandos.Value.PermitirEscrita)
            throw new McpException("Comandos com escrita estão desabilitados neste servidor (Comandos:PermitirEscrita).");
        return Executar(id, parametros, resultado, permitirEscrita: true, ct);
    }

    private async Task<ResultadoComando> Executar(
        string id, Dictionary<string, JsonElement>? parametros, string? resultado, bool permitirEscrita, CancellationToken ct)
    {
        try
        {
            return await servico.ExecutarAsync(Contexto(), id, parametros, resultado, permitirEscrita, ct);
        }
        catch (ComandoInvalidoException e)
        {
            // McpException leva a mensagem ao chamador; qualquer outra exceção vira um erro genérico.
            throw new McpException(e.Message, e);
        }
    }

    private ContextoComando Contexto()
    {
        var chave = comandos.Value.ChaveEmpresa;
        if (string.IsNullOrWhiteSpace(chave))
            throw new McpException("Comandos:ChaveEmpresa não está configurada no servidor.");
        return new ContextoComando(chave, painel.Value.IdsEmpresa, painel.Value.IdUsuario);
    }
}

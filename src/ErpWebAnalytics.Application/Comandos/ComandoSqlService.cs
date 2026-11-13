using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace ErpWebAnalytics.Application.Comandos;

public interface IComandoSqlService
{
    Task<IReadOnlyList<ComandoResumo>> ListarAsync(ContextoComando contexto, CancellationToken ct);

    /// <summary>
    /// <c>resultado</c>: nome de um conjunto de dados; roda só ele (mais os passos de execução). Null = todos.
    /// <c>permitirEscrita</c>: sem isso, comandos com passos "somente execução" são recusados antes de rodar qualquer coisa.
    /// </summary>
    Task<ResultadoComando> ExecutarAsync(
        ContextoComando contexto, string id, IReadOnlyDictionary<string, JsonElement>? parametros,
        string? resultado, bool permitirEscrita, CancellationToken ct);

    /// <summary>
    /// Testa um comando ainda não gravado (rascunho da tela de manutenção). <c>soPrevia</c> só gera o SQL, sem executar.
    /// Comandos com passos de execução rodam numa transação que é sempre desfeita no fim.
    /// </summary>
    Task<TesteComando> TestarAsync(
        ContextoComando contexto, ComandoSql rascunho, IReadOnlyDictionary<string, JsonElement>? parametros,
        string? resultado, bool soPrevia, bool permitirEscrita, CancellationToken ct);
}

/// <summary>
/// Caso de uso: o chamador escolhe o comando e os parâmetros de origem "chamador"; o servidor impõe empresa,
/// usuário, limites e o filtro por ChaveEmpresa. O SQL nunca é exposto nem aceito do chamador.
/// </summary>
public sealed class ComandoSqlService(IComandoSqlCatalogo catalogo, IComandoSqlExecutor executor) : IComandoSqlService
{
    private const int MaxLinhasPadrao = 1000, MaxLinhasLimite = 5000;
    private const int TimeoutPadrao = 30, TimeoutLimite = 120;

    public async Task<IReadOnlyList<ComandoResumo>> ListarAsync(ContextoComando contexto, CancellationToken ct)
    {
        var comandos = await catalogo.ListarAsync(contexto.ChaveEmpresa, ct);
        return comandos.Select(c => new ComandoResumo(
            c.Id, c.Descricao,
            c.Parametros.Where(p => p.Origem == OrigemParametro.Chamador).Select(p => new ParametroResumo(
                p.Id, p.Descricao, p.Tipo.ToString(), p.ValorUnico, p.Obrigatorio, p.Padrao, p.Padrao2, p.CondicaoFixa?.ToString(),
                p.Opcoes is { Count: > 0 } ? p.Opcoes : null)).ToList(),
            c.Passos.Where(p => !p.SomenteExecucao).Select(p => p.Nome!).ToList(),
            c.Passos.Any(p => p.SomenteExecucao)))
            .ToList();
    }

    public async Task<ResultadoComando> ExecutarAsync(
        ContextoComando contexto, string id, IReadOnlyDictionary<string, JsonElement>? parametros,
        string? resultado, bool permitirEscrita, CancellationToken ct)
    {
        var comando = await catalogo.ObterAsync(contexto.ChaveEmpresa, id, ct)
            ?? throw new ComandoInvalidoException($"Comando '{id}' não encontrado. Use listar_comandos para ver os disponíveis.");

        var altera = comando.Passos.Any(p => p.SomenteExecucao);
        if (altera && !permitirEscrita)
            throw new ComandoInvalidoException(
                $"O comando '{id}' tem passos de execução (podem alterar dados). Use executar_comando_com_escrita.");

        var passos = Preparar(contexto, comando, parametros, resultado);
        return await executor.ExecutarAsync(
            passos, Timeout(comando), MaxLinhas(comando), altera ? ModoTransacao.Confirmar : ModoTransacao.Nenhuma, ct);
    }

    public async Task<TesteComando> TestarAsync(
        ContextoComando contexto, ComandoSql rascunho, IReadOnlyDictionary<string, JsonElement>? parametros,
        string? resultado, bool soPrevia, bool permitirEscrita, CancellationToken ct)
    {
        var comando = new ComandoSql
        {
            Id = string.IsNullOrWhiteSpace(rascunho.Id) ? "(rascunho)" : rascunho.Id,
            Descricao = rascunho.Descricao,
            Parametros = rascunho.Parametros,
            Regras = rascunho.Regras,
            MaxLinhas = rascunho.MaxLinhas,
            TimeoutSeg = rascunho.TimeoutSeg,
            Passos = ValidadorComando.NormalizarPassos(string.IsNullOrWhiteSpace(rascunho.Id) ? "(rascunho)" : rascunho.Id, rascunho.Passos),
        };

        var altera = comando.Passos.Any(p => p.SomenteExecucao);
        if (altera && !soPrevia && !permitirEscrita)
            throw new ComandoInvalidoException(
                "Este comando tem passos de execução. Para testá-los habilite Comandos:PermitirEscrita no servidor " +
                "(o teste roda numa transação que é desfeita no fim) ou use a prévia, que só gera o SQL.");

        var passos = Preparar(contexto, comando, parametros, resultado);
        var previa = passos.Select(p => new PassoPrevia(
            p.Nome, p.SomenteExecucao, p.Sql.Texto,
            p.Sql.Parametros.Select(x => new ParametroPrevia(x.Nome, x.Tipo.ToString(), Convert.ToString(x.Valor, CultureInfo.InvariantCulture))).ToList())).ToList();
        if (soPrevia) return new TesteComando(previa, null, 0);

        var cron = Stopwatch.StartNew();
        var dados = await executor.ExecutarAsync(
            passos, Timeout(comando), MaxLinhas(comando), altera ? ModoTransacao.Reverter : ModoTransacao.Nenhuma, ct);
        return new TesteComando(previa, dados, cron.ElapsedMilliseconds);
    }

    private static int MaxLinhas(ComandoSql c) => c.MaxLinhas > 0 ? Math.Min(c.MaxLinhas, MaxLinhasLimite) : MaxLinhasPadrao;
    private static int Timeout(ComandoSql c) => c.TimeoutSeg > 0 ? Math.Min(c.TimeoutSeg, TimeoutLimite) : TimeoutPadrao;

    /// <summary>Junta valores do servidor e do chamador, escolhe os passos a rodar e gera o SQL final de cada um.</summary>
    private static List<PassoGerado> Preparar(
        ContextoComando contexto, ComandoSql comando, IReadOnlyDictionary<string, JsonElement>? parametros, string? resultado)
    {
        IEnumerable<PassoSql> escolhidos = comando.Passos;
        if (!string.IsNullOrWhiteSpace(resultado))
        {
            var alvo = comando.Passos.FirstOrDefault(p => !p.SomenteExecucao && string.Equals(p.Nome, resultado, StringComparison.OrdinalIgnoreCase))
                ?? throw new ComandoInvalidoException(
                    $"O comando '{comando.Id}' não tem o resultado '{resultado}'. Disponíveis: " +
                    string.Join(", ", comando.Passos.Where(p => !p.SomenteExecucao).Select(p => p.Nome)) + ".");
            // Passos de execução (preparação) rodam sempre, na ordem; dos que devolvem dados roda só o pedido.
            escolhidos = comando.Passos.Where(p => p.SomenteExecucao || ReferenceEquals(p, alvo));
        }

        var valores = new Dictionary<string, IReadOnlyList<ValorParametro>>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in comando.Parametros.Where(p => p.Origem == OrigemParametro.Servidor))
            valores[p.Id] = ValoresDoServidor(p, contexto);

        foreach (var (nome, json) in parametros ?? new Dictionary<string, JsonElement>())
        {
            var p = comando.Parametros.FirstOrDefault(x => x.Id.Equals(nome, StringComparison.OrdinalIgnoreCase))
                ?? throw new ComandoInvalidoException(
                    $"Parâmetro '{nome}' desconhecido no comando '{comando.Id}'. Válidos: " +
                    string.Join(", ", comando.Parametros.Where(x => x.Origem == OrigemParametro.Chamador).Select(x => x.Id)) + ".");
            if (p.Origem == OrigemParametro.Servidor)
                throw new ComandoInvalidoException($"O parâmetro '{p.Id}' é definido pelo servidor e não pode ser informado.");
            valores[p.Id] = ConferirOpcoes(p, LerValores(p.Id, json));
        }

        return escolhidos
            .Select(p => new PassoGerado(p.SomenteExecucao ? null : p.Nome, p.SomenteExecucao, InterpretadorSql.Gerar(comando, p.Sql, valores)))
            .ToList();
    }

    /// <summary>Parâmetro com lista fechada (combobox) só aceita os valores da lista: vale para a tela e para o Claude.</summary>
    private static IReadOnlyList<ValorParametro> ConferirOpcoes(ParametroComando p, IReadOnlyList<ValorParametro> valores)
    {
        if (p.Opcoes is not { Count: > 0 }) return valores;

        // O que vai ao SQL é sempre o valor da opção; aceita-se também o rótulo ("Codigo" -> L.IdProduto).
        string? Traduzir(string? texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            var opcao = p.Opcoes.FirstOrDefault(o => string.Equals(o.Valor, texto, StringComparison.OrdinalIgnoreCase))
                ?? p.Opcoes.FirstOrDefault(o => string.Equals(o.Rotulo, texto, StringComparison.OrdinalIgnoreCase))
                ?? throw new ComandoInvalidoException(
                    $"Valor '{texto}' não é uma opção de '{p.Id}'. Opções: " + string.Join(", ", p.Opcoes.Select(o => $"{o.Rotulo} ('{o.Valor}')")) + ".");
            return opcao.Valor;
        }

        return valores.Select(v => v with { Valor1 = Traduzir(v.Valor1), Valor2 = Traduzir(v.Valor2) }).ToList();
    }

    private static IReadOnlyList<ValorParametro> ValoresDoServidor(ParametroComando p, ContextoComando ctx) =>
        p.Chave?.ToLowerInvariant() switch
        {
            "idsempresa" => ctx.IdsEmpresa.Select(i => new ValorParametro(OperadorComparacao.Igual, Texto(i))).ToList(),
            "idusuario" => [new ValorParametro(OperadorComparacao.Igual, Texto(ctx.IdUsuario))],
            _ => throw new ComandoInvalidoException(
                $"O parâmetro '{p.Id}' tem origem 'Servidor' com chave '{p.Chave}' desconhecida (use idsEmpresa ou idUsuario)."),
        };

    private static string Texto(int i) => i.ToString(CultureInfo.InvariantCulture);

    // ---------- entrada do chamador ----------
    // valor                       -> igual
    // {"op":"maior","valor":"x"}  -> um item (valor2 só em between)
    // [valor, {...}, ...]         -> vários itens, combinados com "or"
    // null / "" / []              -> não informado

    internal static IReadOnlyList<ValorParametro> LerValores(string id, JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.Array)
            return json.EnumerateArray().SelectMany(e => LerValores(id, e)).ToList();

        if (json.ValueKind == JsonValueKind.Object)
        {
            var op = OperadorComparacao.Igual;
            if (Propriedade(json, "op") is { } o && o.ValueKind == JsonValueKind.String)
            {
                if (!Enum.TryParse(o.GetString(), ignoreCase: true, out op))
                    throw new ComandoInvalidoException(
                        $"Operador '{o.GetString()}' inválido em '{id}'. Use: {string.Join(", ", Enum.GetNames<OperadorComparacao>())}.");
            }
            var v1 = Escalar(id, Propriedade(json, "valor"));
            var v2 = Escalar(id, Propriedade(json, "valor2"));
            return v1 == null && v2 == null ? [] : [new ValorParametro(op, v1, v2)];
        }

        var valor = Escalar(id, json);
        return valor == null ? [] : [new ValorParametro(OperadorComparacao.Igual, valor)];
    }

    private static JsonElement? Propriedade(JsonElement obj, string nome)
    {
        foreach (var p in obj.EnumerateObject())
            if (p.Name.Equals(nome, StringComparison.OrdinalIgnoreCase)) return p.Value;
        return null;
    }

    private static string? Escalar(string id, JsonElement? e) => e is not { } v ? null : v.ValueKind switch
    {
        JsonValueKind.String => v.GetString(),
        JsonValueKind.Number => v.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => throw new ComandoInvalidoException($"Valor inválido em '{id}': use texto, número ou booleano."),
    };
}

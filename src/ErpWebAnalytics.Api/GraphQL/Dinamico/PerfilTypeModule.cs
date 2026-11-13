using System.Collections;
using System.Security.Cryptography;
using System.Text;
using ErpWebAnalytics.Api.Mcp;
using ErpWebAnalytics.Api.Perfis;
using ErpWebAnalytics.Application.Perfis;
using ErpWebAnalytics.Application.Perfis.Dinamico;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.Options;

namespace ErpWebAnalytics.Api.GraphQL.Dinamico;

/// <summary>Resultado de uma mutation de atualização: as linhas já alteradas (releitura) e se foi só simulação.</summary>
public sealed record ResultadoAtualizacao(IReadOnlyList<Linha> Itens, bool Simulado);

/// <summary>Página de uma consulta raiz: os itens, se há mais, e o total (só é contado se o cliente pedir).</summary>
public sealed record Pagina(IReadOnlyList<Linha> Itens, bool TemMais, Func<Task<long>> Total);

/// <summary>
/// Monta os tipos e consultas do GraphQL a partir do perfil do sistema (tabelas, colunas e relações escolhidas na tela).
/// Nada existe no schema sem estar no perfil. Quando o perfil é salvo, o módulo avisa o HotChocolate, que remonta o schema
/// sem reiniciar a API. Os valores vêm do SQL gerado por <see cref="ConstrutorConsulta"/> (sempre parametrizado).
/// </summary>
public static class PerfilDinamicoExtensions
{
    /// <summary>
    /// Liga o GraphQL do perfil neste schema: o módulo de tipos, o limite de profundidade e, com mutations ativas, a raiz Mutation
    /// (o HotChocolate só aceita a raiz registrada no builder; o módulo acrescenta os campos <c>atualizar&lt;Tipo&gt;</c> a ela).
    /// </summary>
    public static IRequestExecutorBuilder AddPerfilDinamico(this IRequestExecutorBuilder builder, bool mutacoesAtivas)
    {
        builder.AddTypeModule<PerfilTypeModule>().AddMaxExecutionDepthRule(8);
        if (mutacoesAtivas)
        {
            builder.AddMutationType(d =>
            {
                d.Name("Mutation");
                d.Description("Atualizações. Exigem a chave de escrita (cabeçalho X-Write-Key).");
                d.Field("validarChaveEscrita").Type(new NonNullTypeNode(new NamedTypeNode("Boolean")))
                    .Description("Confere a chave de escrita enviada em X-Write-Key (não altera nada).")
                    .Resolve(ctx => { PerfilTypeModule.ExigirChaveEscrita(ctx); return true; });
            });
        }
        return builder;
    }
}

public sealed class PerfilTypeModule : TypeModule
{
    public const int MaxPagina = 500;
    public const int MaxRelacao = 100;

    public const int MaxLimiteAtualizacao = 100;

    private readonly PerfilAtivo _ativo;
    private readonly bool _mutacoes;

    /// <summary>
    /// As mutations só existem no schema com <c>Comandos:PermitirEscrita=true</c> <b>e</b> <c>Seguranca:ApiKeyEscrita</c> configurada
    /// (a chave separada, enviada em <c>X-Write-Key</c>). Mudar essas opções vale na próxima remontagem do schema (reinício).
    /// </summary>
    public static bool MutacoesAtivas(ComandosOptions comandos, IConfiguration config) =>
        comandos.PermitirEscrita && !string.IsNullOrEmpty(config["Seguranca:ApiKeyEscrita"]);

    public PerfilTypeModule(PerfilAtivo ativo, IOptions<ComandosOptions> comandos, IConfiguration config)
    {
        _ativo = ativo;
        _mutacoes = MutacoesAtivas(comandos.Value, config);
        _ativo.Alterado += (_, _) => OnTypesChanged();
    }

    public override async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(IDescriptorContext context, CancellationToken cancellationToken)
    {
        // Espera a primeira carga do perfil (feita na subida). Com limite: se não vier, o schema nasce só com o campo `sistema`
        // e é remontado assim que o perfil chegar (PerfilAtivo.Definir avisa este módulo).
        await Task.WhenAny(_ativo.Pronto, Task.Delay(TimeSpan.FromSeconds(30), cancellationToken));
        var perfil = _ativo.Compilado;
        var tipos = new List<ITypeSystemMember>();

        tipos.AddRange(TiposDeFiltro());
        tipos.Add(new EnumType(d => { d.Name("Direcao"); d.Description("Sentido da ordenação."); d.Value("ASC"); d.Value("DESC"); }));

        var atualizaveis = _mutacoes ? perfil.Tabelas.Where(t => t.Atualizavel && t.Campos.Any(c => c.Filtravel)).ToList() : [];

        foreach (var t in perfil.Tabelas)
        {
            tipos.Add(TipoObjeto(t));
            tipos.Add(TipoPagina(t));
            if (t.Campos.Any(c => c.Filtravel)) tipos.Add(TipoFiltro(t));
            if (t.Campos.Any(c => c.Ordenavel)) tipos.Add(TipoOrdem(t));
        }

        foreach (var t in atualizaveis)
        {
            tipos.Add(TipoAtualizacao(t));
            tipos.Add(TipoResultadoAtualizacao(t));
        }
        if (atualizaveis.Count > 0)
        {
            tipos.Add(new ObjectTypeExtension(d =>
            {
                d.Name("Mutation");
                foreach (var t in atualizaveis) CampoAtualizar(d, t);
            }));
        }

        tipos.Add(new ObjectTypeExtension(d =>
        {
            d.Name("Query");
            d.Field("sistema").Type(Nn("String")).Description("Sistema servido por esta instalação.").Resolve(_ => _ativo.Sistema);
            foreach (var t in perfil.Tabelas) CampoRaiz(d, t);
        }));

        return tipos;
    }

    // ---------- tipos ----------

    private static ITypeNode Nn(string nome) => new NonNullTypeNode(new NamedTypeNode(nome));
    private static ITypeNode N(string nome) => new NamedTypeNode(nome);
    private static ITypeNode Lista(string nome) => new NonNullTypeNode(new ListTypeNode(new NonNullTypeNode(new NamedTypeNode(nome))));

    private static string Escalar(TipoCampo t) => t switch
    {
        TipoCampo.Inteiro => "Int", TipoCampo.Longo => "Long", TipoCampo.Decimal => "Decimal", TipoCampo.Booleano => "Boolean",
        TipoCampo.DataHora or TipoCampo.Data => "DateTime", TipoCampo.Guid => "UUID", _ => "String",
    };

    private static string NomeFiltro(TipoCampo t) => t switch
    {
        TipoCampo.Inteiro => "FiltroInteiro", TipoCampo.Longo => "FiltroLongo", TipoCampo.Decimal => "FiltroDecimal", TipoCampo.Booleano => "FiltroBooleano",
        TipoCampo.DataHora or TipoCampo.Data => "FiltroData", TipoCampo.Guid => "FiltroUuid", _ => "FiltroTexto",
    };

    private static IEnumerable<ITypeSystemMember> TiposDeFiltro()
    {
        InputObjectType Filtro(string nome, string escalar, params string[] operadores) => new(d =>
        {
            d.Name(nome);
            d.Description($"Condições sobre um campo ({escalar}). Vários operadores no mesmo campo se combinam com E.");
            foreach (var op in operadores)
                d.Field(op).Type(op == "em" ? new ListTypeNode(new NonNullTypeNode(new NamedTypeNode(escalar))) : op == "nulo" ? N("Boolean") : N(escalar));
        });

        yield return Filtro("FiltroTexto", "String", "igual", "diferente", "contem", "comecaCom", "em", "nulo");
        yield return Filtro("FiltroInteiro", "Int", "igual", "diferente", "maior", "maiorOuIgual", "menor", "menorOuIgual", "em", "nulo");
        yield return Filtro("FiltroLongo", "Long", "igual", "diferente", "maior", "maiorOuIgual", "menor", "menorOuIgual", "em", "nulo");
        yield return Filtro("FiltroDecimal", "Decimal", "igual", "diferente", "maior", "maiorOuIgual", "menor", "menorOuIgual", "em", "nulo");
        yield return Filtro("FiltroData", "DateTime", "igual", "diferente", "maior", "maiorOuIgual", "menor", "menorOuIgual", "nulo");
        yield return Filtro("FiltroBooleano", "Boolean", "igual", "diferente", "nulo");
        yield return Filtro("FiltroUuid", "UUID", "igual", "diferente", "em", "nulo");
    }

    private static ObjectType TipoObjeto(TabelaCompilada t) => new(d =>
    {
        d.Name(t.Nome);
        if (!string.IsNullOrWhiteSpace(t.Descricao)) d.Description(t.Descricao);

        foreach (var c in t.Campos)
        {
            var coluna = c.Coluna;
            var f = d.Field(c.Campo).Type(N(Escalar(c.Tipo))).Resolve(ctx => ctx.Parent<Linha>().GetValueOrDefault(coluna));
            if (!string.IsNullOrWhiteSpace(c.Descricao)) f.Description(c.Descricao);
        }

        foreach (var r in t.Relacoes)
        {
            var relacao = r;
            if (r.Cardinalidade == CardinalidadeRelacao.ParaUm)
            {
                var f = d.Field(r.Campo).Type(N(r.Alvo.Nome)).Resolve(async ctx => (await Carregar(ctx, relacao, 0, 1)).FirstOrDefault());
                if (!string.IsNullOrWhiteSpace(r.Descricao)) f.Description(r.Descricao);
            }
            else
            {
                var f = d.Field(r.Campo).Type(Lista(r.Alvo.Nome))
                    .Argument("primeiro", a => a.Type(N("Int")).DefaultValue(20).Description($"Quantas linhas (máximo {MaxRelacao})."))
                    .Argument("pular", a => a.Type(N("Int")).DefaultValue(0))
                    .Resolve(async ctx =>
                    {
                        var primeiro = Math.Clamp(ctx.ArgumentValue<int>("primeiro"), 1, MaxRelacao);
                        return (IReadOnlyList<Linha>)await Carregar(ctx, relacao, Math.Max(0, ctx.ArgumentValue<int>("pular")), primeiro);
                    });
                if (!string.IsNullOrWhiteSpace(r.Descricao)) f.Description(r.Descricao);
            }
        }
    });

    private static ObjectType TipoPagina(TabelaCompilada t) => new(d =>
    {
        d.Name(t.Nome + "Pagina");
        d.Description($"Uma página de {t.Nome}.");
        d.Field("itens").Type(Lista(t.Nome)).Resolve(ctx => ctx.Parent<Pagina>().Itens);
        d.Field("temMais").Type(Nn("Boolean")).Description("Há mais linhas depois desta página.").Resolve(ctx => ctx.Parent<Pagina>().TemMais);
        d.Field("total").Type(Nn("Long")).Description("Total de linhas que atendem ao filtro (só é contado se pedido).")
            .Resolve(async ctx => await ctx.Parent<Pagina>().Total());
    });

    private static InputObjectType TipoFiltro(TabelaCompilada t) => new(d =>
    {
        d.Name(t.Nome + "Filtro");
        d.Description($"Filtro de {t.Nome}. Campos no mesmo nível se combinam com E; use E/OU para agrupar.");
        foreach (var c in t.Campos.Where(c => c.Filtravel)) d.Field(c.Campo).Type(N(NomeFiltro(c.Tipo)));
        d.Field("E").Type(new ListTypeNode(new NonNullTypeNode(new NamedTypeNode(t.Nome + "Filtro")))).Description("Todos precisam valer.");
        d.Field("OU").Type(new ListTypeNode(new NonNullTypeNode(new NamedTypeNode(t.Nome + "Filtro")))).Description("Basta um valer.");
    });

    private static InputObjectType TipoOrdem(TabelaCompilada t) => new(d =>
    {
        d.Name(t.Nome + "Ordem");
        d.Description($"Ordenação de {t.Nome}: informe um campo por item da lista.");
        foreach (var c in t.Campos.Where(c => c.Ordenavel)) d.Field(c.Campo).Type(N("Direcao"));
    });

    private static InputObjectType TipoAtualizacao(TabelaCompilada t) => new(d =>
    {
        d.Name(t.Nome + "Atualizacao");
        d.Description($"Valores a gravar em {t.Nome}: só os campos informados são alterados (informe null para limpar uma coluna que aceita nulo).");
        foreach (var c in t.Campos.Where(c => c.Editavel)) d.Field(c.Campo).Type(N(Escalar(c.Tipo)));
    });

    private static ObjectType TipoResultadoAtualizacao(TabelaCompilada t) => new(d =>
    {
        d.Name(t.Nome + "ResultadoAtualizacao");
        d.Field("afetadas").Type(Nn("Int")).Description("Quantas linhas foram alteradas.").Resolve(ctx => ctx.Parent<ResultadoAtualizacao>().Itens.Count);
        d.Field("simulado").Type(Nn("Boolean")).Description("Verdadeiro se foi só simulação (nada foi gravado).").Resolve(ctx => ctx.Parent<ResultadoAtualizacao>().Simulado);
        d.Field("itens").Type(Lista(t.Nome)).Description("As linhas depois da alteração.").Resolve(ctx => ctx.Parent<ResultadoAtualizacao>().Itens);
    });

    private static void CampoAtualizar(IObjectTypeDescriptor d, TabelaCompilada t)
    {
        var nome = "atualizar" + char.ToUpperInvariant(t.Nome[0]) + t.Nome[1..];
        d.Field(nome).Type(Nn(t.Nome + "ResultadoAtualizacao"))
            .Description($"Atualiza linhas de {t.Nome}. O filtro é obrigatório; passando de limite linhas nada é alterado.")
            .Argument("onde", a => a.Type(Nn(t.Nome + "Filtro")).Description("Quais linhas. Precisa ter ao menos uma condição."))
            .Argument("valores", a => a.Type(Nn(t.Nome + "Atualizacao")))
            .Argument("limite", a => a.Type(N("Int")).DefaultValue(1).Description($"Máximo de linhas que o filtro pode atingir (1 a {MaxLimiteAtualizacao})."))
            .Argument("simular", a => a.Type(N("Boolean")).DefaultValue(false).Description("Executa tudo e desfaz no fim: mostra o que mudaria."))
            .Resolve(ctx => Atualizar(ctx, t));
    }

    private static void CampoRaiz(IObjectTypeDescriptor d, TabelaCompilada t)
    {
        var f = d.Field(t.Consulta).Type(Nn(t.Nome + "Pagina"))
            .Argument("primeiro", a => a.Type(N("Int")).DefaultValue(50).Description($"Quantas linhas (máximo {MaxPagina})."))
            .Argument("pular", a => a.Type(N("Int")).DefaultValue(0));
        if (t.Campos.Any(c => c.Filtravel)) f.Argument("onde", a => a.Type(N(t.Nome + "Filtro")));
        if (t.Campos.Any(c => c.Ordenavel)) f.Argument("ordenarPor", a => a.Type(new ListTypeNode(new NonNullTypeNode(new NamedTypeNode(t.Nome + "Ordem")))));
        if (!string.IsNullOrWhiteSpace(t.Descricao)) f.Description(t.Descricao);
        f.Resolve(ctx => Raiz(ctx, t));
    }

    // ---------- resolvers ----------

    private static async ValueTask<object?> Raiz(IResolverContext ctx, TabelaCompilada t)
    {
        var pular = Math.Max(0, ctx.ArgumentValue<int>("pular"));
        var primeiro = Math.Clamp(ctx.ArgumentValue<int>("primeiro"), 1, MaxPagina);
        var filtro = t.Campos.Any(c => c.Filtravel) ? ConverterFiltro(ctx.ArgumentValue<object?>("onde"), t) : null;
        var ordem = t.Campos.Any(c => c.Ordenavel) ? ConverterOrdem(ctx.ArgumentValue<object?>("ordenarPor"), t) : [];
        if (ordem.Count == 0) ordem = t.OrdemPadrao;

        var executor = ctx.Services.GetRequiredService<IExecutorDinamico>();
        var linhas = await executor.LerAsync(ConstrutorConsulta.Listar(t.Banco, t.Colunas, filtro, ordem, pular, primeiro + 1), t.Banco, ctx.RequestAborted);
        var temMais = linhas.Count > primeiro;
        return new Pagina(temMais ? linhas.Take(primeiro).ToList() : linhas, temMais,
            () => executor.ContarAsync(ConstrutorConsulta.Contar(t.Banco, filtro), ctx.RequestAborted));
    }

    private static readonly Dictionary<string, OperadorFiltro> Operadores = new()
    {
        ["igual"] = OperadorFiltro.Igual, ["diferente"] = OperadorFiltro.Diferente, ["maior"] = OperadorFiltro.Maior, ["maiorOuIgual"] = OperadorFiltro.MaiorOuIgual,
        ["menor"] = OperadorFiltro.Menor, ["menorOuIgual"] = OperadorFiltro.MenorOuIgual, ["contem"] = OperadorFiltro.Contem, ["comecaCom"] = OperadorFiltro.ComecaCom,
        ["em"] = OperadorFiltro.Em, ["nulo"] = OperadorFiltro.Nulo,
    };

    internal static NoFiltro? ConverterFiltro(object? entrada, TabelaCompilada t)
    {
        if (Dicionario(entrada) is not { } d) return null;
        var no = new NoFiltro();
        foreach (var (chave, valor) in d)
        {
            // O HotChocolate entrega todos os campos do input, os não informados com valor nulo: nulo aqui significa "não pedido".
            // (Para filtrar por nulo existe o operador `nulo`.)
            if (valor is null) continue;
            if (chave == "E") { foreach (var item in Itens(valor)) if (ConverterFiltro(item, t) is { } f) no.E.Add(f); continue; }
            if (chave == "OU") { foreach (var item in Itens(valor)) if (ConverterFiltro(item, t) is { } f) no.Ou.Add(f); continue; }
            var campo = t.Campos.FirstOrDefault(c => c.Filtravel && c.Campo == chave);
            if (campo is null || Dicionario(valor) is not { } ops) continue;
            foreach (var (nomeOp, v) in ops)
                if (v is not null && Operadores.TryGetValue(nomeOp, out var op)) no.Condicoes.Add(new CondicaoFiltro(campo.Coluna, op, op == OperadorFiltro.Em ? Itens(v).ToList() : v));
        }
        return no;
    }

    internal static List<OrdemColuna> ConverterOrdem(object? entrada, TabelaCompilada t)
    {
        var ordem = new List<OrdemColuna>();
        foreach (var item in Itens(entrada))
            if (Dicionario(item) is { } d)
                foreach (var (chave, valor) in d)
                    if (valor is not null && t.Campos.FirstOrDefault(c => c.Ordenavel && c.Campo == chave) is { } campo)
                        ordem.Add(new OrdemColuna(campo.Coluna, string.Equals(valor?.ToString(), "DESC", StringComparison.OrdinalIgnoreCase)));
        return ordem;
    }

    private static IEnumerable<KeyValuePair<string, object?>>? Dicionario(object? o) => o switch
    {
        IReadOnlyDictionary<string, object?> d => d,
        IDictionary<string, object?> d => d,
        IDictionary d => d.Cast<DictionaryEntry>().Select(e => new KeyValuePair<string, object?>(e.Key.ToString()!, e.Value)),
        _ => null,
    };

    private static IEnumerable<object?> Itens(object? o) => o is IEnumerable e and not string ? e.Cast<object?>() : [];

    // ---------- atualização ----------

    private static GraphQLException Recusa(string mensagem, string codigo) =>
        new(ErrorBuilder.New().SetMessage(mensagem).SetCode(codigo).Build());

    internal static void ExigirChaveEscrita(IResolverContext ctx)
    {
        var esperada = ctx.Services.GetRequiredService<IConfiguration>()["Seguranca:ApiKeyEscrita"];
        var enviada = ctx.Services.GetRequiredService<IHttpContextAccessor>().HttpContext?.Request.Headers["X-Write-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(esperada) || string.IsNullOrEmpty(enviada) ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(enviada)), SHA256.HashData(Encoding.UTF8.GetBytes(esperada))))
            throw Recusa("Chave de escrita ausente ou inválida (cabeçalho X-Write-Key).", "ESCRITA_NAO_AUTORIZADA");
    }

    private static bool TemCondicao(NoFiltro f) => f.Condicoes.Count > 0 || f.E.Any(TemCondicao) || f.Ou.Any(TemCondicao);

    private static async ValueTask<object?> Atualizar(IResolverContext ctx, TabelaCompilada t)
    {
        ExigirChaveEscrita(ctx);

        var filtro = ConverterFiltro(ctx.ArgumentValue<object?>("onde"), t);
        if (filtro is null || !TemCondicao(filtro)) throw Recusa("O argumento onde precisa ter ao menos uma condição (não é permitido atualizar sem filtro).", "FILTRO_OBRIGATORIO");

        // Quais campos foram informados vem do próprio texto da consulta: assim "campo: null" (limpar) difere de "campo omitido".
        var informados = ctx.ArgumentLiteral<IValueNode>("valores") is ObjectValueNode obj
            ? obj.Fields.Select(f => f.Name.Value).ToHashSet(StringComparer.Ordinal)
            : [];
        var entrada = Dicionario(ctx.ArgumentValue<object?>("valores"))?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? [];

        var valores = new List<(string Coluna, object? Valor)>();
        foreach (var campo in t.Campos.Where(c => c.Editavel && informados.Contains(c.Campo)))
        {
            var v = entrada.GetValueOrDefault(campo.Campo);
            var coluna = t.Banco.Colunas.First(c => c.Nome.Equals(campo.Coluna, StringComparison.OrdinalIgnoreCase));
            if (v is null && !coluna.Nulavel) throw Recusa($"O campo '{campo.Campo}' não aceita nulo.", "VALOR_INVALIDO");
            valores.Add((campo.Coluna, v));
        }
        if (valores.Count == 0) throw Recusa("Informe ao menos um campo editável em valores.", "NADA_A_ATUALIZAR");

        var limite = Math.Clamp(ctx.ArgumentValue<int>("limite"), 1, MaxLimiteAtualizacao);
        var simular = ctx.ArgumentValue<bool>("simular");
        var log = ctx.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Perfil.Atualizacao");

        try
        {
            var linhas = await ctx.Services.GetRequiredService<IExecutorDinamico>().AtualizarAsync(t, filtro, valores, limite, simular, ctx.RequestAborted);
            log.LogInformation("Atualização em {Tabela}: {Linhas} linha(s), colunas [{Colunas}], limite {Limite}, simulada={Simulada}.",
                t.Chave, linhas.Count, string.Join(", ", valores.Select(v => v.Coluna)), limite, simular);
            return new ResultadoAtualizacao(linhas, simular);
        }
        catch (AtualizacaoRecusadaException e)
        {
            log.LogWarning("Atualização recusada em {Tabela}: {Motivo}", t.Chave, e.Message);
            throw Recusa(e.Message, "ATUALIZACAO_RECUSADA");
        }
        catch (Microsoft.Data.SqlClient.SqlException e)
        {
            log.LogWarning("Atualização falhou no banco em {Tabela}: {Erro}", t.Chave, e.Message);
            throw Recusa("O banco recusou a atualização: " + e.Message, "ERRO_BANCO");
        }
    }

    // ---------- relações (uma consulta por relação e lote, não uma por linha) ----------

    private static async Task<IReadOnlyList<Linha>> Carregar(IResolverContext ctx, RelacaoCompilada r, int pular, int tomar)
    {
        var linha = ctx.Parent<Linha>();
        var valores = r.Colunas.Select(c => linha.GetValueOrDefault(c.Origem)).ToArray();
        if (valores.Any(v => v is null)) return [];

        // O escopo de DataLoaders do HotChocolate guarda um carregador por nome e usa o agendador do próprio executor, que só dispara
        // o lote quando todos os resolvers irmãos já pediram a sua chave. (Com um agendador "automático" cada pedido virava uma consulta.)
        var escopo = ctx.Services.GetRequiredService<GreenDonut.DependencyInjection.IDataLoaderScope>();
        var executor = ctx.Services.GetRequiredService<IExecutorDinamico>();
        var carregador = escopo.GetDataLoader<CarregadorRelacao>(
            sp => new CarregadorRelacao(sp.GetRequiredService<GreenDonut.IBatchScheduler>(), new GreenDonut.DataLoaderOptions(), executor, r, pular, tomar),
            $"perfil:{r.Alvo.Chave}:{r.Campo}:{pular}:{tomar}");
        return await carregador.CarregarAsync(valores, ctx.RequestAborted);
    }
}

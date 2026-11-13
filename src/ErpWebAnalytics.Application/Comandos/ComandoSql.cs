using System.Text.Json.Serialization;

namespace ErpWebAnalytics.Application.Comandos;

/// <summary>
/// Comando SQL do catálogo (linha de Analytics.ComandoSQL). <see cref="Parametros"/> e <see cref="Regras"/> vêm de colunas JSON.
/// </summary>
public sealed class ComandoSql
{
    public string Id { get; init; } = "";
    public string Descricao { get; init; } = "";
    public bool Ativa { get; init; } = true;

    /// <summary>Preenchido só na tela de manutenção: o JSON gravado no banco não pôde ser lido.</summary>
    public string? Erro { get; init; }

    /// <summary>Instruções em ordem de execução (coluna SQL). Um comando pode devolver vários conjuntos de dados.</summary>
    public List<PassoSql> Passos { get; init; } = [];

    public List<ParametroComando> Parametros { get; init; } = [];
    public List<RegraComando> Regras { get; init; } = [];
    public int MaxLinhas { get; init; }
    public int TimeoutSeg { get; init; }
}

/// <summary>
/// Uma instrução do comando. JSON da coluna SQL: <c>[{"ordem":1,"nome":"loja","sql":"select ...","somenteExecucao":false}]</c>.
/// Instruções de mesma ordem rodam na ordem em que aparecem. Todas compartilham os parâmetros e as regras do comando
/// e a mesma conexão (tabelas <c>#temp</c> criadas num passo valem nos seguintes).
/// </summary>
public sealed class PassoSql
{
    public int Ordem { get; init; }

    /// <summary>Nome do resultado (passos que devolvem dados). Vazio = <c>resultado1</c>, <c>resultado2</c>...</summary>
    public string? Nome { get; init; }

    public string Sql { get; init; } = "";

    /// <summary>true = update/insert/preparação: roda mas não devolve dados.</summary>
    public bool SomenteExecucao { get; init; }
}

public enum TipoParametro { String, Inteiro, Decimal, Date, DateTime, Checkbox }

public enum OperadorComparacao { Igual, Diferente, Maior, MaiorIgual, Menor, MenorIgual, Between, Like, NotLike }

/// <summary>Quem fornece o valor: quem chama a ferramenta, ou o servidor (valores que o chamador não pode escolher).</summary>
public enum OrigemParametro { Chamador, Servidor }

/// <summary>Parâmetro do comando. JSON: <c>{"id":"DataVenda","campo":"p.DataMovimento","tipo":"Date","obrigatorio":true}</c>.</summary>
public sealed class ParametroComando
{
    /// <summary>Nome usado no SQL: <c>[Id]</c> (valor) ou <c>{Id}</c> (condição).</summary>
    public string Id { get; init; } = "";

    /// <summary>Coluna comparada em <c>{Id}</c>. Pode ser qualificada (<c>p.IdPedido</c>). Vazio = o próprio Id.</summary>
    public string? Campo { get; init; }

    public string? Descricao { get; init; }
    public TipoParametro Tipo { get; init; } = TipoParametro.String;

    /// <summary>Aceita um único valor (sempre comparação "igual"). Sem isso aceita lista e operadores.</summary>
    public bool ValorUnico { get; init; }

    public bool Obrigatorio { get; init; }

    /// <summary>Valores usados quando o chamador não informa (<see cref="Padrao2"/> só em Between). Formato ISO nas datas.</summary>
    public string? Padrao { get; init; }
    public string? Padrao2 { get; init; }

    /// <summary>Força o operador, ignorando o que o chamador enviar.</summary>
    public OperadorComparacao? CondicaoFixa { get; init; }

    public OrigemParametro Origem { get; init; } = OrigemParametro.Chamador;

    /// <summary>
    /// Lista fechada de valores (combobox). Com opções, o chamador só pode informar um desses valores; valor vazio
    /// (<c>""</c>) significa "sem valor". JSON: <c>[{"valor":"","rotulo":"Sem quebra"},{"valor":"P.IdGrupo","rotulo":"Grupo"}]</c>.
    /// </summary>
    public List<OpcaoParametro>? Opcoes { get; init; }

    /// <summary>Para <see cref="OrigemParametro.Servidor"/>: <c>idsEmpresa</c> ou <c>idUsuario</c>.</summary>
    public string? Chave { get; init; }

    [JsonIgnore] public string ColunaEfetiva => string.IsNullOrWhiteSpace(Campo) ? Id : Campo;
}

/// <summary>
/// Trecho de SQL escolhido conforme os parâmetros, usado no SQL como <c>%Id%</c>.
/// JSON: <c>{"id":"Periodo","casos":[{"se":[{"param":"DataIni","op":"preenchido"}],"sql":"and p.Data &gt;= [DataIni]"}],"senao":"and p.Data &gt;= dateadd(day,-30,getdate())"}</c>.
/// </summary>
public sealed class RegraComando
{
    public string Id { get; init; } = "";

    /// <summary>Avaliados em ordem; vale o primeiro cujas condições (todas, "e") forem verdadeiras.</summary>
    public List<CasoRegra> Casos { get; init; } = [];

    /// <summary>Usado se nenhum caso valer. Vazio = nada.</summary>
    public string? Senao { get; init; }
}

public sealed class CasoRegra
{
    public List<CondicaoRegra> Se { get; init; } = [];
    public string Sql { get; init; } = "";
}

public sealed class OpcaoParametro
{
    public string Valor { get; init; } = "";
    public string Rotulo { get; init; } = "";
}

public enum OperadorCondicao { Preenchido, Vazio, Igual, Diferente, Maior, MaiorIgual, Menor, MenorIgual }

public sealed class CondicaoRegra
{
    public string Param { get; init; } = "";
    public OperadorCondicao Op { get; init; } = OperadorCondicao.Preenchido;

    /// <summary>Literal comparado ao primeiro valor do parâmetro (ignorado em Preenchido/Vazio).</summary>
    public string? Valor { get; init; }
}

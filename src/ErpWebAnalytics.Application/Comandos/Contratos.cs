namespace ErpWebAnalytics.Application.Comandos;

/// <summary>Erro de uso (comando inexistente, parâmetro inválido, regra mal escrita). A mensagem é segura para o chamador.</summary>
public sealed class ComandoInvalidoException(string message) : Exception(message);

/// <summary>Um valor informado para um parâmetro. Datas em ISO (<c>yyyy-MM-dd</c>).</summary>
public sealed record ValorParametro(OperadorComparacao Op, string? Valor1, string? Valor2 = null);

/// <summary>Parâmetro SQL já tipado (long, decimal, DateTime, bool ou string).</summary>
public sealed record ParametroSql(string Nome, object Valor, TipoParametro Tipo);

/// <summary>SQL final, com <c>@p1..@pn</c>, e os valores desses parâmetros.</summary>
public sealed record SqlGerado(string Texto, IReadOnlyList<ParametroSql> Parametros);

/// <summary>Conjunto de dados de um passo que devolve linhas.</summary>
public sealed record ResultadoConsulta(string Nome, IReadOnlyList<string> Colunas, IReadOnlyList<object?[]> Linhas, bool Truncado);

/// <summary>Um resultado por passo que devolve dados, na ordem de execução.</summary>
public sealed record ResultadoComando(IReadOnlyList<ResultadoConsulta> Resultados);

/// <summary>Passo pronto para executar. <paramref name="Nome"/> só existe nos passos que devolvem dados.</summary>
public sealed record PassoGerado(string? Nome, bool SomenteExecucao, SqlGerado Sql);

/// <summary>Dados que só o servidor decide (o chamador nunca os escolhe): escopo do cliente e permissões.</summary>
public sealed record ContextoComando(string ChaveEmpresa, IReadOnlyList<int> IdsEmpresa, int IdUsuario);

public sealed record ParametroResumo(
    string Id, string? Descricao, string Tipo, bool ValorUnico, bool Obrigatorio, string? Padrao, string? Padrao2, string? OperadorFixo,
    IReadOnlyList<OpcaoParametro>? Opcoes = null);

/// <summary>
/// <c>Resultados</c>: nomes dos conjuntos de dados que o comando devolve (use em <c>resultado</c> para pedir só um).
/// <c>AlteraDados</c>: true se algum passo é "somente execução" (exige executar_comando_com_escrita).
/// </summary>
public sealed record ComandoResumo(
    string Id, string Descricao, IReadOnlyList<ParametroResumo> Parametros, IReadOnlyList<string> Resultados, bool AlteraDados);

public interface IComandoSqlCatalogo
{
    /// <summary>Comandos ativos da empresa. Linhas com JSON inválido são ignoradas.</summary>
    Task<IReadOnlyList<ComandoSql>> ListarAsync(string chaveEmpresa, CancellationToken ct);

    /// <summary>Comando ativo da empresa, ou null. Lança <see cref="ComandoInvalidoException"/> se o JSON da linha for inválido.</summary>
    Task<ComandoSql?> ObterAsync(string chaveEmpresa, string id, CancellationToken ct);
}

public interface IComandoSqlExecutor
{
    /// <summary>
    /// Executa os passos em ordem, na mesma conexão. Passos com <c>Nome</c> devolvem o primeiro conjunto de resultados,
    /// cortado em <paramref name="maxLinhas"/>. Com <paramref name="transacao"/>, tudo é confirmado junto ou desfeito se algo falhar.
    /// </summary>
    Task<ResultadoComando> ExecutarAsync(
        IReadOnlyList<PassoGerado> passos, int timeoutSeg, int maxLinhas, ModoTransacao transacao, CancellationToken ct);
}

public enum ModoTransacao { Nenhuma, Confirmar, Reverter }

/// <summary>Parâmetro SQL de um passo, em texto, para a tela de testes.</summary>
public sealed record ParametroPrevia(string Nome, string Tipo, string? Valor);

/// <summary>SQL final de um passo (depois do interpretador) e os parâmetros que o acompanham.</summary>
public sealed record PassoPrevia(string? Nome, bool SomenteExecucao, string Sql, IReadOnlyList<ParametroPrevia> Parametros);

/// <summary>Resultado de um teste: o SQL gerado e, se não foi só prévia, os dados e o tempo.</summary>
public sealed record TesteComando(IReadOnlyList<PassoPrevia> Passos, ResultadoComando? Resultado, long Milissegundos);

/// <summary>Leitura e gravação do catálogo para a tela de manutenção. Sempre restrito à ChaveEmpresa informada.</summary>
public interface IComandoSqlRepositorio
{
    /// <summary>Todos os comandos da empresa, ativos ou não, com os passos como estão gravados. JSON inválido vem em <see cref="ComandoSql.Erro"/>.</summary>
    Task<IReadOnlyList<ComandoSql>> ListarTodosAsync(string chaveEmpresa, CancellationToken ct);

    /// <summary>Insere (<paramref name="criar"/>) ou atualiza. Lança <see cref="ComandoInvalidoException"/> se já existe ou não existe.</summary>
    Task SalvarAsync(string chaveEmpresa, ComandoSql comando, bool criar, CancellationToken ct);

    Task<bool> ExcluirAsync(string chaveEmpresa, string id, CancellationToken ct);
}

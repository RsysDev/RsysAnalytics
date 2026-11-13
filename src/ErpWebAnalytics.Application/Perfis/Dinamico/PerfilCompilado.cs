namespace ErpWebAnalytics.Application.Perfis.Dinamico;

public sealed class CampoCompilado
{
    public string Campo { get; init; } = "";
    public string Coluna { get; init; } = "";
    public string? Descricao { get; init; }
    public TipoCampo Tipo { get; init; }
    public bool Filtravel { get; init; }
    public bool Ordenavel { get; init; }
    public bool Editavel { get; init; }
}

public sealed class RelacaoCompilada
{
    public string Campo { get; init; } = "";
    public string? Descricao { get; init; }
    public CardinalidadeRelacao Cardinalidade { get; init; }
    public TabelaCompilada Alvo { get; set; } = null!;
    public IReadOnlyList<ParColunas> Colunas { get; init; } = [];
}

/// <summary>Uma tabela do perfil já cruzada com a estrutura real do banco: é o que o GraphQL dinâmico usa para montar tipos e SQL.</summary>
public sealed class TabelaCompilada
{
    public string Chave { get; init; } = "";
    public string Nome { get; init; } = "";
    public string Consulta { get; init; } = "";
    public string? Descricao { get; init; }
    public TabelaBanco Banco { get; init; } = null!;
    public List<CampoCompilado> Campos { get; init; } = [];
    public List<RelacaoCompilada> Relacoes { get; init; } = [];

    /// <summary>Tem mutation de atualização (perfil pede, é tabela com chave primária e há coluna editável).</summary>
    public bool Atualizavel { get; init; }

    /// <summary>Colunas da chave primária: identificam cada linha na atualização.</summary>
    public List<string> Chaves { get; init; } = [];

    /// <summary>Colunas lidas do banco: as expostas mais as que as relações precisam (mesmo não expostas).</summary>
    public List<string> Colunas { get; init; } = [];

    /// <summary>Ordem usada quando o cliente não pede uma: chave primária, ou a primeira coluna ordenável.</summary>
    public List<OrdemColuna> OrdemPadrao { get; init; } = [];
}

public sealed class PerfilCompilado
{
    public string Sistema { get; init; } = "";
    public List<TabelaCompilada> Tabelas { get; init; } = [];
}

public static class CompiladorPerfil
{
    /// <summary>
    /// Cruza o perfil com o esquema do banco. Tabelas com erro, pausadas, ou que apontam para tabela fora do perfil, ficam de fora;
    /// o resto continua funcionando (um perfil com defeito nunca derruba o que está correto).
    /// </summary>
    public static PerfilCompilado Compilar(PerfilGraphQL perfil, IReadOnlyList<ProblemaPerfil> problemas, Func<string, TabelaBanco?> esquema)
    {
        string Chave(string t) => TiposBanco.Separar(t) is { } s ? $"{s.Schema}.{s.Nome}" : t;
        var comErro = problemas.Where(p => p.Nivel == ProblemaPerfil.Erro && p.Tabela is not null)
            .Select(p => Chave(p.Tabela!)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var resultado = new PerfilCompilado { Sistema = perfil.Sistema };
        var porChave = new Dictionary<string, TabelaCompilada>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in perfil.Tabelas.Where(t => t.Ativa && !comErro.Contains(Chave(t.Tabela))))
        {
            var banco = esquema(t.Tabela);
            if (banco is null) continue;
            var porNome = banco.Colunas.ToDictionary(c => c.Nome, StringComparer.OrdinalIgnoreCase);
            var campos = t.Colunas.Where(c => porNome.ContainsKey(c.Coluna) && porNome[c.Coluna].Tipo != TipoCampo.NaoSuportado)
                .Select(c => new CampoCompilado
                {
                    Campo = c.Campo, Coluna = porNome[c.Coluna].Nome, Descricao = c.Descricao, Tipo = porNome[c.Coluna].Tipo,
                    Filtravel = c.Filtravel, Ordenavel = c.Ordenavel,
                    Editavel = c.Editavel && !porNome[c.Coluna].Chave && !porNome[c.Coluna].Identidade,
                }).ToList();

            var ordem = banco.Colunas.Where(c => c.Chave).Select(c => new OrdemColuna(c.Nome, false)).ToList();
            if (ordem.Count == 0 && campos.FirstOrDefault(c => c.Ordenavel) is { } primeira) ordem.Add(new OrdemColuna(primeira.Coluna, false));

            var compilada = new TabelaCompilada
            {
                Chave = Chave(t.Tabela), Nome = t.Nome, Consulta = t.Consulta, Descricao = t.Descricao, Banco = banco,
                Campos = campos, Colunas = campos.Select(c => c.Coluna).ToList(), OrdemPadrao = ordem,
                Chaves = banco.Colunas.Where(c => c.Chave).Select(c => c.Nome).ToList(),
                Atualizavel = t.Atualizavel && !banco.EhView && banco.Colunas.Any(c => c.Chave) && campos.Any(c => c.Editavel),
            };
            porChave[compilada.Chave] = compilada;
            resultado.Tabelas.Add(compilada);
        }

        // Relações: só para tabelas que também foram compiladas. As colunas das chaves entram na leitura, expostas ou não.
        foreach (var t in perfil.Tabelas)
        {
            if (!porChave.TryGetValue(Chave(t.Tabela), out var origem)) continue;
            foreach (var r in t.Relacoes)
            {
                if (!porChave.TryGetValue(Chave(r.TabelaAlvo), out var alvo)) continue;
                origem.Relacoes.Add(new RelacaoCompilada
                {
                    Campo = r.Campo, Descricao = r.Descricao, Cardinalidade = r.Cardinalidade, Alvo = alvo, Colunas = r.Colunas,
                });
                foreach (var par in r.Colunas)
                {
                    Garantir(origem.Colunas, origem.Banco, par.Origem);
                    Garantir(alvo.Colunas, alvo.Banco, par.Destino);
                }
            }
        }
        return resultado;
    }

    private static void Garantir(List<string> colunas, TabelaBanco banco, string nome)
    {
        var real = banco.Colunas.FirstOrDefault(c => c.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase));
        if (real is not null && !colunas.Contains(real.Nome, StringComparer.OrdinalIgnoreCase)) colunas.Add(real.Nome);
    }
}

/// <summary>Executa as consultas geradas e devolve linhas. A implementação (SQL Server) fica na infraestrutura.</summary>
public interface IExecutorDinamico
{
    Task<IReadOnlyList<Linha>> LerAsync(ConsultaGerada consulta, TabelaBanco tabela, CancellationToken ct);
    Task<long> ContarAsync(ConsultaGerada consulta, CancellationToken ct);

    /// <summary>
    /// Atualiza as linhas que atendem ao filtro, numa só transação: acha as chaves (no máximo <paramref name="limite"/> + 1), recusa se passar
    /// do limite, atualiza por chave primária e relê as linhas. Com <paramref name="simular"/> tudo roda e é desfeito no fim.
    /// Lança <see cref="AtualizacaoRecusadaException"/> para recusas esperadas (limite, nada a alterar, concorrência).
    /// </summary>
    Task<IReadOnlyList<Linha>> AtualizarAsync(
        TabelaCompilada tabela, NoFiltro filtro, IReadOnlyList<(string Coluna, object? Valor)> valores, int limite, bool simular, CancellationToken ct);
}

/// <summary>Recusa esperada de uma atualização (mensagem segura para o cliente).</summary>
public sealed class AtualizacaoRecusadaException(string message) : Exception(message);

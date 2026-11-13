namespace ErpWebAnalytics.Application.Perfis;

/// <summary>
/// Perfil de um sistema (ERPWEB, RSYS2003, CHAMADOS): quais tabelas e colunas o GraphQL expõe, com que nomes e relações.
/// Fica em <c>Analytics.PerfilGraphQL</c> (JSON) e é editado na tela de manutenção. <b>Nada é exposto por padrão</b>:
/// só o que estiver listado aqui existe no schema.
/// </summary>
public sealed class PerfilGraphQL
{
    public string Sistema { get; init; } = "";
    public List<TabelaExposta> Tabelas { get; init; } = [];
}

public sealed class TabelaExposta
{
    /// <summary>Tabela ou view no banco, como <c>dbo.tbEntidade</c> (sem schema = dbo).</summary>
    public string Tabela { get; init; } = "";

    /// <summary>Nome do tipo GraphQL (ex.: <c>Entidade</c>).</summary>
    public string Nome { get; init; } = "";

    /// <summary>Nome do campo de consulta na raiz (ex.: <c>entidades</c>).</summary>
    public string Consulta { get; init; } = "";

    public string? Descricao { get; init; }
    public bool Ativa { get; init; } = true;

    /// <summary>
    /// Gera a mutation <c>atualizar&lt;Nome&gt;</c> (só nas colunas <see cref="ColunaExposta.Editavel"/>). Desligado por padrão e exige
    /// tabela (não view) com chave primária. A mutation só entra no schema com <c>Comandos:PermitirEscrita</c> e <c>Seguranca:ApiKeyEscrita</c>.
    /// </summary>
    public bool Atualizavel { get; init; }

    public List<ColunaExposta> Colunas { get; init; } = [];
    public List<RelacaoExposta> Relacoes { get; init; } = [];
}

public sealed class ColunaExposta
{
    /// <summary>Nome da coluna no banco.</summary>
    public string Coluna { get; init; } = "";

    /// <summary>Nome do campo no GraphQL (ex.: <c>razaoSocial</c>).</summary>
    public string Campo { get; init; } = "";

    public string? Descricao { get; init; }
    public bool Filtravel { get; init; }
    public bool Ordenavel { get; init; }

    /// <summary>Pode ser alterada pela mutation. Nunca chave primária nem identity. Desligado por padrão.</summary>
    public bool Editavel { get; init; }
}

public enum CardinalidadeRelacao { ParaUm, ParaMuitos }

/// <summary>
/// Relação entre duas tabelas expostas, por pares de colunas. <c>ParaUm</c>: a linha aponta para uma (chave estrangeira);
/// <c>ParaMuitos</c>: várias linhas da outra tabela apontam para esta (sempre paginada, nunca sem limite).
/// </summary>
public sealed class RelacaoExposta
{
    public string Campo { get; init; } = "";
    public string? Descricao { get; init; }
    public CardinalidadeRelacao Cardinalidade { get; init; } = CardinalidadeRelacao.ParaUm;

    /// <summary>Tabela alvo (mesmo formato de <see cref="TabelaExposta.Tabela"/>); precisa estar exposta no perfil.</summary>
    public string TabelaAlvo { get; init; } = "";

    public List<ParColunas> Colunas { get; init; } = [];
}

public sealed class ParColunas
{
    /// <summary>Coluna desta tabela.</summary>
    public string Origem { get; init; } = "";

    /// <summary>Coluna da tabela alvo.</summary>
    public string Destino { get; init; } = "";
}

public sealed record ProblemaPerfil(string Nivel, string Mensagem, string? Tabela = null, string? Coluna = null)
{
    public const string Erro = "erro";
    public const string Aviso = "aviso";
}

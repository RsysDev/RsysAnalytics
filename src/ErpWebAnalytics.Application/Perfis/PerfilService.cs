using System.Text.RegularExpressions;
using ErpWebAnalytics.Application.Comandos;
using ErpWebAnalytics.Application.Perfis.Dinamico;

namespace ErpWebAnalytics.Application.Perfis;

public interface IPerfilRepositorio
{
    /// <summary>Null se ainda não há perfil gravado. Lança se a tabela Analytics.PerfilGraphQL não existir.</summary>
    Task<PerfilGraphQL?> ObterAsync(string chaveEmpresa, string sistema, CancellationToken ct);
    Task SalvarAsync(string chaveEmpresa, PerfilGraphQL perfil, CancellationToken ct);
}

/// <summary>Perfil cruzado com o banco (o que o GraphQL dinâmico usa) e os problemas encontrados no caminho.</summary>
public sealed record ResultadoCompilacao(PerfilCompilado Compilado, IReadOnlyList<ProblemaPerfil> Problemas);

public interface IPerfilService
{
    Task<ResultadoCompilacao> CompilarAsync(PerfilGraphQL perfil, CancellationToken ct);

    Task<PerfilGraphQL> ObterAsync(string chaveEmpresa, string sistema, CancellationToken ct);
    Task<IReadOnlyList<ProblemaPerfil>> ValidarAsync(PerfilGraphQL perfil, CancellationToken ct);

    /// <summary>Grava se não houver erros (avisos não impedem). Devolve todos os problemas encontrados.</summary>
    Task<IReadOnlyList<ProblemaPerfil>> SalvarAsync(string chaveEmpresa, PerfilGraphQL perfil, CancellationToken ct);
}

public sealed class PerfilService(IPerfilRepositorio repositorio, IEsquemaBancoLeitor esquema) : IPerfilService
{
    public async Task<PerfilGraphQL> ObterAsync(string chaveEmpresa, string sistema, CancellationToken ct) =>
        await repositorio.ObterAsync(chaveEmpresa, sistema, ct) ?? new PerfilGraphQL { Sistema = sistema };

    public async Task<IReadOnlyList<ProblemaPerfil>> ValidarAsync(PerfilGraphQL perfil, CancellationToken ct) =>
        (await CompilarAsync(perfil, ct)).Problemas;

    public async Task<ResultadoCompilacao> CompilarAsync(PerfilGraphQL perfil, CancellationToken ct)
    {
        // Lê do banco só as tabelas que o perfil cita (e as alvo das relações).
        var chaves = perfil.Tabelas.Select(t => t.Tabela).Concat(perfil.Tabelas.SelectMany(t => t.Relacoes.Select(r => r.TabelaAlvo)))
            .Where(k => TiposBanco.Separar(k) is not null).Select(NormalizarChave).Distinct(StringComparer.OrdinalIgnoreCase);
        var lidas = new Dictionary<string, TabelaBanco?>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in chaves) lidas[k] = await esquema.ObterAsync(k, ct);
        TabelaBanco? Esquema(string k) => lidas.GetValueOrDefault(NormalizarChave(k));
        var problemas = ValidadorPerfil.Validar(perfil, Esquema);
        return new ResultadoCompilacao(CompiladorPerfil.Compilar(perfil, problemas, Esquema), problemas);
    }

    public async Task<IReadOnlyList<ProblemaPerfil>> SalvarAsync(string chaveEmpresa, PerfilGraphQL perfil, CancellationToken ct)
    {
        var problemas = await ValidarAsync(perfil, ct);
        var erros = problemas.Where(p => p.Nivel == ProblemaPerfil.Erro).ToList();
        if (erros.Count > 0)
            throw new ComandoInvalidoException("O perfil tem erros e não foi salvo: " + string.Join(" ", erros.Select(e => e.Mensagem)));
        await repositorio.SalvarAsync(chaveEmpresa, perfil, ct);
        return problemas;
    }

    private static string NormalizarChave(string k) => TiposBanco.Separar(k) is { } s ? $"{s.Schema}.{s.Nome}" : k;
}

/// <summary>Regras do perfil contra o esquema real do banco. Lógica pura: o esquema entra por uma função.</summary>
public static class ValidadorPerfil
{
    private static readonly Regex NomeGraphQL = new(@"^[_A-Za-z][_0-9A-Za-z]*$", RegexOptions.Compiled);

    private static readonly HashSet<string> Reservados = new(StringComparer.OrdinalIgnoreCase)
        { "Query", "Mutation", "Subscription", "String", "Int", "Float", "Boolean", "ID", "Long", "Decimal", "DateTime", "Date", "Guid", "UUID", "Any",
          "Direcao", "FiltroTexto", "FiltroInteiro", "FiltroLongo", "FiltroDecimal", "FiltroData", "FiltroBooleano", "FiltroUuid" };

    public static IReadOnlyList<ProblemaPerfil> Validar(PerfilGraphQL perfil, Func<string, TabelaBanco?> esquema)
    {
        var p = new List<ProblemaPerfil>();
        void Erro(string m, string? t = null, string? c = null) => p.Add(new(ProblemaPerfil.Erro, m, t, c));
        void Aviso(string m, string? t = null, string? c = null) => p.Add(new(ProblemaPerfil.Aviso, m, t, c));

        if (string.IsNullOrWhiteSpace(perfil.Sistema)) Erro("O perfil precisa do nome do sistema.");

        var tipos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var consultas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var expostas = perfil.Tabelas.Select(t => Chave(t.Tabela)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var t in perfil.Tabelas)
        {
            var rotulo = string.IsNullOrWhiteSpace(t.Nome) ? t.Tabela : t.Nome;
            if (TiposBanco.Separar(t.Tabela) is null) { Erro($"Tabela '{t.Tabela}': nome inválido (use schema.tabela).", t.Tabela); continue; }

            if (!NomeValido(t.Nome) || Reservados.Contains(t.Nome)) Erro($"'{rotulo}': nome do tipo inválido ou reservado.", t.Tabela);
            else if (!tipos.Add(t.Nome) | !tipos.Add(t.Nome + "Pagina") | !tipos.Add(t.Nome + "Filtro") | !tipos.Add(t.Nome + "Ordem"))
                Erro($"O tipo '{t.Nome}' (ou um dos gerados a partir dele: {t.Nome}Pagina, {t.Nome}Filtro, {t.Nome}Ordem) repete o nome de outra tabela.", t.Tabela);
            if (!NomeValido(t.Consulta)) Erro($"'{rotulo}': nome da consulta inválido.", t.Tabela);
            else if (!consultas.Add(t.Consulta)) Erro($"Consulta '{t.Consulta}' repetida.", t.Tabela);

            var banco = esquema(t.Tabela);
            if (banco is null) { Erro($"'{rotulo}': a tabela '{t.Tabela}' não existe neste banco.", t.Tabela); continue; }

            if (t.Colunas.Count == 0) Erro($"'{rotulo}': escolha ao menos uma coluna.", t.Tabela);
            var campos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var porNome = banco.Colunas.ToDictionary(c => c.Nome, StringComparer.OrdinalIgnoreCase);

            foreach (var c in t.Colunas)
            {
                if (!porNome.TryGetValue(c.Coluna, out var real)) { Erro($"'{rotulo}': a coluna '{c.Coluna}' não existe em {t.Tabela}.", t.Tabela, c.Coluna); continue; }
                if (!NomeValido(c.Campo)) Erro($"'{rotulo}.{c.Coluna}': nome do campo inválido.", t.Tabela, c.Coluna);
                else if (!campos.Add(c.Campo)) Erro($"'{rotulo}': campo '{c.Campo}' repetido.", t.Tabela, c.Coluna);
                if (real.Tipo == TipoCampo.NaoSuportado) Erro($"'{rotulo}.{c.Coluna}': o tipo {real.TipoSql} não pode ser exposto.", t.Tabela, c.Coluna);
                if (real.Sensivel) Aviso($"'{rotulo}.{c.Coluna}' parece um dado sensível (pessoal, segredo ou custo). Confirme que deve ser exposto.", t.Tabela, c.Coluna);
            }

            if (t.Atualizavel)
            {
                if (banco.EhView) Erro($"'{rotulo}': uma view não pode ser atualizada.", t.Tabela);
                if (!banco.Colunas.Any(c => c.Chave)) Erro($"'{rotulo}': atualização exige chave primária (para identificar cada linha com segurança).", t.Tabela);
                if (!t.Colunas.Any(c => c.Editavel)) Erro($"'{rotulo}': marque ao menos uma coluna como editável.", t.Tabela);
                if (!t.Colunas.Any(c => c.Filtravel)) Erro($"'{rotulo}': a atualização exige o argumento onde: marque ao menos uma coluna como filtrável.", t.Tabela);
            }
            foreach (var c in t.Colunas.Where(c => c.Editavel))
                if (porNome.TryGetValue(c.Coluna, out var re) && (re.Chave || re.Identidade))
                    Erro($"'{rotulo}.{c.Coluna}': chave primária e identity não podem ser editáveis.", t.Tabela, c.Coluna);

            if (!banco.Colunas.Any(c => c.Chave) && !t.Colunas.Any(c => c.Ordenavel))
                Aviso($"'{rotulo}' não tem chave primária: marque ao menos uma coluna como ordenável para a paginação ser estável.", t.Tabela);

            foreach (var r in t.Relacoes)
            {
                if (!NomeValido(r.Campo)) { Erro($"'{rotulo}': nome de relação inválido ('{r.Campo}').", t.Tabela); continue; }
                if (!campos.Add(r.Campo)) Erro($"'{rotulo}': a relação '{r.Campo}' repete o nome de um campo.", t.Tabela);
                if (!expostas.Contains(Chave(r.TabelaAlvo))) { Erro($"'{rotulo}.{r.Campo}': a tabela alvo '{r.TabelaAlvo}' não está exposta no perfil.", t.Tabela); continue; }
                var alvo = esquema(r.TabelaAlvo);
                if (alvo is null) { Erro($"'{rotulo}.{r.Campo}': a tabela alvo '{r.TabelaAlvo}' não existe.", t.Tabela); continue; }
                if (r.Colunas.Count == 0) Erro($"'{rotulo}.{r.Campo}': informe ao menos um par de colunas.", t.Tabela);
                foreach (var par in r.Colunas)
                {
                    if (!porNome.ContainsKey(par.Origem)) Erro($"'{rotulo}.{r.Campo}': a coluna '{par.Origem}' não existe em {t.Tabela}.", t.Tabela);
                    if (!alvo.Colunas.Any(c => c.Nome.Equals(par.Destino, StringComparison.OrdinalIgnoreCase)))
                        Erro($"'{rotulo}.{r.Campo}': a coluna '{par.Destino}' não existe em {r.TabelaAlvo}.", t.Tabela);
                }
            }
        }
        return p;
    }

    private static bool NomeValido(string? n) => !string.IsNullOrEmpty(n) && NomeGraphQL.IsMatch(n) && !n.StartsWith("__", StringComparison.Ordinal);
    private static string Chave(string t) => TiposBanco.Separar(t) is { } s ? $"{s.Schema}.{s.Nome}" : t;
}

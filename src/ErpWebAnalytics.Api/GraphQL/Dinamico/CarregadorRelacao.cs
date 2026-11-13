using System.Collections.Concurrent;
using System.Globalization;
using ErpWebAnalytics.Application.Perfis;
using ErpWebAnalytics.Application.Perfis.Dinamico;
using GreenDonut;

namespace ErpWebAnalytics.Api.GraphQL.Dinamico;

/// <summary>
/// Junta, numa única consulta, todas as linhas de uma relação pedidas no mesmo momento (o problema N+1): se uma lista de 200
/// cidades pede o estado de cada uma, vira um <c>where IdEstado in (...)</c>, não 200 consultas. Um carregador por relação e página.
/// </summary>
internal sealed class CarregadorRelacao : BatchDataLoader<string, IReadOnlyList<Linha>>
{
    private const int TamanhoLote = 400;

    private readonly IExecutorDinamico _executor;
    private readonly RelacaoCompilada _relacao;
    private readonly int _pular, _tomar;
    private readonly ConcurrentDictionary<string, object?[]> _valores = new();

    public CarregadorRelacao(IBatchScheduler scheduler, DataLoaderOptions options, IExecutorDinamico executor, RelacaoCompilada relacao, int pular, int tomar)
        : base(scheduler, options)
    {
        _executor = executor; _relacao = relacao; _pular = pular; _tomar = tomar;
    }

    /// <summary>Chave comparável entre colunas de tipos diferentes (int x bigint, char x varchar): texto sem espaços à direita.</summary>
    private static string Chave(IEnumerable<object?> valores) =>
        string.Join('\u001f', valores.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)?.TrimEnd().ToUpperInvariant()));

    public async Task<IReadOnlyList<Linha>> CarregarAsync(object?[] valores, CancellationToken ct)
    {
        var chave = Chave(valores);
        _valores[chave] = valores;
        return await LoadAsync(chave, ct) ?? [];
    }

    protected override async Task<IReadOnlyDictionary<string, IReadOnlyList<Linha>>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken ct)
    {
        var alvo = _relacao.Alvo;
        var destinos = _relacao.Colunas.Select(c => c.Destino).ToList();
        var resultado = keys.ToDictionary(k => k, _ => (IReadOnlyList<Linha>)[]);
        var porChave = new Dictionary<string, List<Linha>>();

        foreach (var lote in keys.Chunk(Math.Max(1, TamanhoLote / destinos.Count)))
        {
            var chaves = lote.Select(k => _valores[k]).ToList();
            var consulta = _relacao.Cardinalidade == CardinalidadeRelacao.ParaUm
                ? ConstrutorConsulta.PorChaves(alvo.Banco, alvo.Colunas, destinos, chaves)
                : ConstrutorConsulta.PorChavesPaginado(alvo.Banco, alvo.Colunas, destinos, chaves, alvo.OrdemPadrao, _pular, _tomar);

            foreach (var linha in await _executor.LerAsync(consulta, alvo.Banco, ct))
            {
                var chave = Chave(destinos.Select(d => linha.GetValueOrDefault(d)));
                if (!porChave.TryGetValue(chave, out var lista)) porChave[chave] = lista = [];
                lista.Add(linha);
            }
        }

        foreach (var k in keys) if (porChave.TryGetValue(k, out var l)) resultado[k] = l;
        return resultado;
    }
}

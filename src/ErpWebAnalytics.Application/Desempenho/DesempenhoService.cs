using ErpWebAnalytics.Application.Pedidos;

namespace ErpWebAnalytics.Application.Desempenho;

public interface IDesempenhoService
{
    Task<DesempenhoMensal> ObterMensalAsync(int ano, int mes, IReadOnlyList<int> idsEmpresa, int idUsuario, CancellationToken ct);

    /// <summary>Os últimos <paramref name="quantidade"/> meses até (e incluindo) ano/mes, em listas planas.</summary>
    Task<DesempenhoPeriodo> ObterPeriodoAsync(int ano, int mes, int quantidade, IReadOnlyList<int> idsEmpresa, int idUsuario, CancellationToken ct);
}

/// <summary>
/// Agrega a relação de pedidos (mesma regra do relatório do ERP) por mês e por vendedor interno.
/// Reaproveita o SQL de <see cref="IPedidoQueries"/> para herdar devoluções, custo e permissões do usuário.
/// </summary>
public sealed class DesempenhoService(IPedidoQueries pedidos) : IDesempenhoService
{
    public async Task<DesempenhoMensal> ObterMensalAsync(
        int ano, int mes, IReadOnlyList<int> idsEmpresa, int idUsuario, CancellationToken ct)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var linhas = await pedidos.ObterRelacaoPedidosAsync(new FiltroPedidos
        {
            IdsEmpresa = [.. idsEmpresa],
            IdUsuario = idUsuario,
            DataInicio = inicio,
            DataFim = fim,
            QuebraVendedor = QuebraVendedor.Interno,
            IncluirDevolucoes = true,
        }, ct);

        var vendedores = linhas
            .GroupBy(l => (Id: l.VendedorInterno ?? 0, Nome: l.NomeVendedorInterno ?? "Sem vendedor"))
            .Select(g => Agregar(new DesempenhoVendedor { IdVendedor = g.Key.Id, Nome = g.Key.Nome }, g))
            .OrderByDescending(v => v.Realizado)
            .ToList();

        return new DesempenhoMensal
        {
            Ano = ano,
            Mes = mes,
            DiasNoMes = DateTime.DaysInMonth(ano, mes),
            Loja = Agregar(new IndicadoresVenda(), linhas),
            Vendedores = vendedores,
            PorDia = linhas
                .Where(l => l.DataMovimento is not null)
                .GroupBy(l => DateOnly.FromDateTime(l.DataMovimento!.Value))
                .OrderBy(g => g.Key)
                .Select(g => new VendaDia
                {
                    Data = g.Key,
                    Realizado = g.Sum(l => l.ValorVendaComDevolucao),
                    Pedidos = g.Sum(l => l.CountPedido),
                }).ToList(),
            UltimasVendas = linhas
                .Where(l => l.CountPedido == 1 && l.DataMovimento is not null)
                .OrderByDescending(l => l.DataMovimento).ThenByDescending(l => l.IdPedido)
                .Take(15)
                .Select(l => new UltimaVenda
                {
                    IdPedido = l.IdPedido,
                    DataMovimento = l.DataMovimento!.Value,
                    Vendedor = l.NomeVendedorInterno,
                    Valor = l.ValorVenda,
                }).ToList(),
        };
    }

    public async Task<DesempenhoPeriodo> ObterPeriodoAsync(
        int ano, int mes, int quantidade, IReadOnlyList<int> idsEmpresa, int idUsuario, CancellationToken ct)
    {
        if (quantidade is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(quantidade), "Informe de 1 a 12 meses.");

        var periodo = new DesempenhoPeriodo { GeradoEm = DateTime.UtcNow };
        var ultimo = new DateOnly(ano, mes, 1);

        // Sequencial: o AppDbContext não suporta consultas simultâneas.
        for (var i = quantidade - 1; i >= 0; i--)
        {
            var m = ultimo.AddMonths(-i);
            var d = await ObterMensalAsync(m.Year, m.Month, idsEmpresa, idUsuario, ct);
            var chave = $"{m.Year:0000}-{m.Month:00}";

            periodo.Loja.Add(Copiar(new LojaMes { Mes = chave }, d.Loja));
            periodo.Vendedores.AddRange(d.Vendedores.Select(v =>
                Copiar(new VendedorMes { Mes = chave, IdVendedor = v.IdVendedor, Nome = v.Nome }, v)));
            periodo.PorDia.AddRange(d.PorDia.Select(x => new VendaDiaMes { Mes = chave, Data = x.Data, Realizado = x.Realizado, Pedidos = x.Pedidos }));
            if (i == 0) periodo.UltimasVendas = d.UltimasVendas;
        }

        return periodo;
    }

    private static T Copiar<T>(T alvo, IndicadoresVenda o) where T : IndicadoresVenda
    {
        alvo.VendaBruta = o.VendaBruta; alvo.ValorDevolucoes = o.ValorDevolucoes; alvo.Realizado = o.Realizado;
        alvo.Pedidos = o.Pedidos; alvo.Devolucoes = o.Devolucoes; alvo.TicketMedio = o.TicketMedio;
        alvo.LucroBruto = o.LucroBruto; alvo.MargemPercentual = o.MargemPercentual; alvo.DevolucoesPercentual = o.DevolucoesPercentual;
        return alvo;
    }

    private static T Agregar<T>(T alvo, IEnumerable<PedidoResumo> linhas) where T : IndicadoresVenda
    {
        decimal vendaBruta = 0, devolvido = 0, realizado = 0, custo = 0;
        int pedidos = 0, devolucoes = 0;

        foreach (var l in linhas)
        {
            pedidos += l.CountPedido;
            devolucoes += l.CountNota;
            vendaBruta += l.ValorVenda;
            devolvido += l.CountNota == 1 ? -l.ValorVendaComDevolucao : 0;
            realizado += l.ValorVendaComDevolucao;
            custo += l.ValorCustoFor; // devoluções já vêm com sinal negativo
        }

        var lucro = realizado - custo;
        alvo.VendaBruta = vendaBruta;
        alvo.ValorDevolucoes = devolvido;
        alvo.Realizado = realizado;
        alvo.Pedidos = pedidos;
        alvo.Devolucoes = devolucoes;
        alvo.TicketMedio = pedidos > 0 ? Math.Round(vendaBruta / pedidos, 2) : 0;
        // Sem permissão de custo o SQL zera ValorCustoFor; não exibir margem enganosa (100%).
        var temCusto = custo != 0;
        alvo.LucroBruto = temCusto ? lucro : 0;
        alvo.MargemPercentual = temCusto && realizado != 0 ? Math.Round(lucro / realizado * 100, 2) : 0;
        alvo.DevolucoesPercentual = vendaBruta != 0 ? Math.Round(devolvido / vendaBruta * 100, 2) : 0;
        return alvo;
    }
}

import { useState } from 'react'
import { ApiError, api } from '../api'
import { Icon } from '../icons'
import { OPERADORES, TIPOS, type Comando, type Info, type OperadorComparacao, type Parametro, type Teste } from '../types'
import { Chip, Field, ValorInput, Vazio } from '../ui'

export interface ItemTeste { op: OperadorComparacao; v1: string; v2: string }
export type ValoresTeste = Record<string, ItemTeste[]>

const itemVazio = (p: Parametro): ItemTeste => ({
  op: p.condicaoFixa ?? (p.padrao2 ? 'between' : 'igual'), v1: '', v2: '',
})

/** Nomes dos resultados como o servidor os calcula (vazio = resultado1, resultado2...). */
export function nomesResultados(c: Comando): string[] {
  let n = 0
  return c.passos.filter((p) => !p.somenteExecucao).map((p) => { n++; return p.nome?.trim() || `resultado${n}` })
}

function montarParametros(c: Comando, valores: ValoresTeste): Record<string, unknown> {
  const out: Record<string, unknown> = {}
  for (const p of c.parametros) {
    if (p.origem !== 'chamador' || !p.id) continue
    const itens = (valores[p.id] ?? []).filter((i) => i.v1 !== '' || i.v2 !== '')
    if (itens.length === 0) continue
    const conv = itens.map((i) =>
      p.valorUnico || (i.op === 'igual' && !i.v2) ? i.v1 : { op: i.op, valor: i.v1, ...(i.v2 ? { valor2: i.v2 } : {}) })
    out[p.id] = conv.length === 1 ? conv[0] : conv
  }
  return out
}

const fmtNum = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 6 })
const fmtData = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

// Identificadores (IdPedido, Codigo...) não levam separador de milhar; valores levam.
const ehIdentificador = (coluna: string) => /^(id|cod|codigo|ordem|numero|nota)/i.test(coluna)

function Celula({ v, coluna }: { v: unknown; coluna: string }) {
  if (v === null || v === undefined) return <td className="nulo">∅</td>
  if (typeof v === 'number') return <td className="num">{ehIdentificador(coluna) ? String(v) : fmtNum.format(v)}</td>
  if (typeof v === 'boolean') return <td>{v ? 'Sim' : 'Não'}</td>
  if (typeof v === 'string' && /^\d{4}-\d\d-\d\dT\d\d:\d\d/.test(v)) {
    const d = new Date(v)
    const meia = /T00:00:00(\.0+)?$/.test(v)
    return <td className="num">{meia ? d.toLocaleDateString('pt-BR') : fmtData.format(d)}</td>
  }
  return <td>{String(v)}</td>
}

export function Testar({ c, info, valores, setValores }: {
  c: Comando; info: Info; valores: ValoresTeste; setValores: (v: ValoresTeste) => void
}) {
  const [resultado, setResultado] = useState('')
  const [carregando, setCarregando] = useState<'previa' | 'executar' | null>(null)
  const [erro, setErro] = useState<string | null>(null)
  const [teste, setTeste] = useState<Teste | null>(null)
  const [aba, setAba] = useState(0)

  const chamadores = c.parametros.filter((p) => p.origem === 'chamador' && p.id)
  const servidores = c.parametros.filter((p) => p.origem === 'servidor' && p.id)
  const nomes = nomesResultados(c)
  const altera = c.passos.some((p) => p.somenteExecucao)

  const itens = (p: Parametro) => (valores[p.id]?.length ? valores[p.id] : [itemVazio(p)])
  const grava = (p: Parametro, l: ItemTeste[]) => setValores({ ...valores, [p.id]: l })

  const rodar = async (soPrevia: boolean) => {
    setCarregando(soPrevia ? 'previa' : 'executar')
    setErro(null)
    try {
      const r = await api.testar({ comando: c, parametros: montarParametros(c, valores), resultado: resultado || undefined, soPrevia })
      setTeste(r)
      setAba(0)
    } catch (e) {
      setTeste(null)
      setErro(e instanceof ApiError ? e.message : 'Não foi possível falar com o servidor.')
    } finally {
      setCarregando(null)
    }
  }

  const resultados = teste?.resultado?.resultados ?? []
  const atual = resultados[Math.min(aba, resultados.length - 1)]

  return (
    <div className="stack">
      <section className="card">
        <h3 className="card-t">Valores de teste</h3>
        <p className="dica">Preencha como se fosse o Claude chamando. Campo vazio usa o valor padrão do parâmetro. O teste usa o rascunho atual, mesmo sem salvar.</p>

        {servidores.length > 0 && (
          <div className="chips-linha">
            {servidores.map((p) => <Chip key={p.id} tom="aviso">{p.id} = servidor ({p.chave})</Chip>)}
          </div>
        )}

        {chamadores.length === 0 && <Vazio icone="sliders" titulo="Sem parâmetros para preencher" />}

        <div className="stack">
          {chamadores.map((p) => {
            const lista = itens(p)
            const lista_ = !!p.opcoes?.length
            const livre = !p.valorUnico && !p.condicaoFixa && !lista_
            return (
              <div className="teste-param" key={p.id}>
                <div className="teste-param-h">
                  <b className="mono">{p.id}</b>
                  <Chip tom="marca">{TIPOS.find((t) => t.valor === p.tipo)?.rotulo}</Chip>
                  {p.obrigatorio && <Chip tom="perigo">obrigatório</Chip>}
                  {(p.padrao || p.padrao2) && <Chip>padrão: {p.padrao}{p.padrao2 ? ` … ${p.padrao2}` : ''}</Chip>}
                </div>
                {p.descricao && <p className="dica">{p.descricao}</p>}

                {lista.map((it, k) => {
                  const muda = (n: Partial<ItemTeste>) => grava(p, lista.map((x, j) => (j === k ? { ...x, ...n } : x)))
                  const entre = !p.valorUnico && it.op === 'between'
                  return (
                    <div className="teste-linha" key={k}>
                      {livre ? (
                        <select className="input op" aria-label="Operador" value={it.op} onChange={(e) => muda({ op: e.target.value as OperadorComparacao })}>
                          {OPERADORES.map((o) => <option key={o.valor} value={o.valor}>{o.rotulo}</option>)}
                        </select>
                      ) : (
                        <span className="op-fixo">{p.valorUnico || lista_ ? (lista_ ? 'escolha' : 'igual a') : OPERADORES.find((o) => o.valor === p.condicaoFixa)?.rotulo}</span>
                      )}
                      <ValorInput tipo={p.tipo} opcoes={p.opcoes} valor={it.v1} onChange={(v) => muda({ v1: v })} ariaLabel={`Valor de ${p.id}`} />
                      {!lista_ && (entre || (!p.valorUnico && p.condicaoFixa === 'between')) && (
                        <>
                          <span className="ate">até</span>
                          <ValorInput tipo={p.tipo} valor={it.v2} onChange={(v) => muda({ v2: v })} ariaLabel={`Valor final de ${p.id}`} />
                        </>
                      )}
                      {lista.length > 1 && (
                        <button className="icon-btn" title="Remover valor" onClick={() => grava(p, lista.filter((_, j) => j !== k))}><Icon nome="x" size={16} /></button>
                      )}
                    </div>
                  )
                })}
                {!p.valorUnico && (
                  <button className="mini" onClick={() => grava(p, [...lista, itemVazio(p)])}><Icon nome="plus" size={14} /> outro valor (ou)</button>
                )}
              </div>
            )
          })}
        </div>

        <div className="grid g2 topo">
          <Field rotulo="O que executar" dica="Pedir só um resultado roda apenas aquele SELECT.">
            <select className="input" value={resultado} onChange={(e) => setResultado(e.target.value)}>
              <option value="">Todos os resultados</option>
              {nomes.map((n) => <option key={n} value={n}>{n}</option>)}
            </select>
          </Field>
        </div>

        {altera && (
          <div className="banner banner-aviso">
            <Icon nome="alert" />
            <div>
              Este comando tem passos de execução. {info.permitirEscrita
                ? 'O teste roda numa transação que é sempre desfeita no fim.'
                : 'O servidor está com Comandos:PermitirEscrita desligado: só a prévia do SQL funciona.'}
            </div>
          </div>
        )}

        <div className="acoes-linha">
          <button className="btn btn-marca" disabled={carregando !== null} onClick={() => rodar(false)}>
            <Icon nome="play" /> {carregando === 'executar' ? 'Executando…' : 'Executar'}
          </button>
          <button className="btn" disabled={carregando !== null} onClick={() => rodar(true)}>
            <Icon nome="eye" /> {carregando === 'previa' ? 'Gerando…' : 'Prévia do SQL'}
          </button>
        </div>
      </section>

      {erro && (
        <div className="banner banner-perigo"><Icon nome="alert" /><div><b>Não deu certo</b><p className="pre">{erro}</p></div></div>
      )}

      {teste && (
        <>
          {teste.resultado && (
            <section className="card">
              <header className="card-h">
                <div className="card-h-t">
                  <h3 className="card-t sem-margem">Resultado</h3>
                  <Chip tom="ok">{teste.milissegundos} ms</Chip>
                </div>
              </header>

              {resultados.length > 1 && (
                <div className="abas-mini" role="tablist">
                  {resultados.map((r, i) => (
                    <button key={r.nome} role="tab" aria-selected={i === aba} className={i === aba ? 'on' : ''} onClick={() => setAba(i)}>
                      {r.nome} <small>{r.linhas.length}</small>
                    </button>
                  ))}
                </div>
              )}

              {atual && (
                <>
                  <div className="chips-linha">
                    <Chip>{atual.linhas.length} {atual.linhas.length === 1 ? 'linha' : 'linhas'}</Chip>
                    <Chip>{atual.colunas.length} colunas</Chip>
                    {atual.truncado && <Chip tom="aviso">truncado no limite de linhas</Chip>}
                  </div>
                  {atual.linhas.length === 0 ? (
                    <Vazio icone="db" titulo="Nenhuma linha" texto="A consulta rodou, mas não encontrou dados com esses valores." />
                  ) : (
                    <div className="tabela-wrap">
                      <table className="tabela">
                        <thead><tr>{atual.colunas.map((n) => <th key={n}>{n}</th>)}</tr></thead>
                        <tbody>{atual.linhas.map((l, i) => <tr key={i}>{l.map((v, j) => <Celula key={j} v={v} coluna={atual.colunas[j]} />)}</tr>)}</tbody>
                      </table>
                    </div>
                  )}
                </>
              )}
              {!atual && <p className="dica">O comando rodou sem devolver conjuntos de dados.</p>}
            </section>
          )}

          <section className="card">
            <h3 className="card-t">SQL gerado</h3>
            <p className="dica">Depois do interpretador: regras escolhidas, linhas condicionais resolvidas e valores trocados por parâmetros.</p>
            {teste.passos.map((p, i) => (
              <details className="sqlgerado" key={i} open={teste.passos.length === 1}>
                <summary>
                  <span className="ordem">{i + 1}</span> {p.somenteExecucao ? 'Execução' : p.nome} <small>{p.parametros.length} parâmetros</small>
                </summary>
                <pre className="codigo">{p.sql}</pre>
                {p.parametros.length > 0 && (
                  <ul className="params-gerados">
                    {p.parametros.map((x) => <li key={x.nome}><code>{x.nome}</code> <small>{x.tipo}</small> = <b>{x.valor}</b></li>)}
                  </ul>
                )}
              </details>
            ))}
          </section>
        </>
      )}
    </div>
  )
}

import { Icon } from '../icons'
import { OPERADORES_CONDICAO, type Caso, type Comando, type Condicao, type Regra } from '../types'
import { Chip, Field, SqlEditor, ValorInput, Vazio } from '../ui'
import { Navegador, Seta, useNavegacao } from './navegacao'

export function Regras({ c, set }: { c: Comando; set: (c: Comando) => void }) {
  const regras = c.regras
  const nav = useNavegacao(regras.map((r) => `${r.id} ${r.casos.map((k) => k.sql).join(' ')} ${r.senao ?? ''}`), 'reg-')
  const paramsDe = c.parametros.filter((p) => p.id)
  const trocaRegra = (i: number, r: Regra) => set({ ...c, regras: regras.map((x, k) => (k === i ? r : x)) })

  // O trecho de uma regra pode usar parâmetros ([P], {P}) e outras regras (%Outra%), nunca ela mesma.
  const chipsDe = (idAtual: string) => [
    ...paramsDe.flatMap((x) => [
      { texto: `[${x.id}]`, dica: `valor de ${x.id}` },
      { texto: `{${x.id}}`, dica: `condição pronta com a coluna ${x.campo || x.id}` },
    ]),
    ...regras.filter((x) => x.id && x.id.toLowerCase() !== idAtual.toLowerCase()).map((x) => ({ texto: `%${x.id}%`, dica: `trecho da regra ${x.id}` })),
  ]

  const novaCondicao = (): Condicao => ({ param: paramsDe[0]?.id ?? '', op: 'preenchido', valor: '' })
  const novoCaso = (): Caso => ({ se: [novaCondicao()], sql: '' })

  return (
    <div className="stack">
      <p className="dica">
        Uma regra é um conjunto de casos: vale o <b>primeiro</b> cujas condições forem <b>todas</b> verdadeiras; se nenhum valer, usa o
        <i> senão</i>. No SQL, escreva <code>%NomeDaRegra%</code> onde o trecho escolhido deve entrar: pode ser uma condição, uma lista de colunas
        (<code>select a, b, %Colunas%</code>) ou qualquer pedaço de texto. Um trecho pode usar outras regras e parâmetros.
      </p>

      {regras.length === 0 && (
        <Vazio icone="branch" titulo="Sem regras"
          texto="Use regras para trocar um pedaço do SQL conforme os parâmetros (ex.: ordenar por data ou por valor)." />
      )}

      <Navegador nav={nav} rotulos={regras.map((r) => r.id)} plural="regras" />

      {regras.map((r, i) => (
        <section className={`card ${nav.recolhidos.has(i) ? 'fechado' : ''}`} key={i} id={`reg-${i}`} data-nav hidden={!nav.visivel(i)}>
          <header className="card-h">
            <div className="card-h-t">
              <Seta aberto={!nav.recolhidos.has(i)} onClick={() => nav.alternar(i)} rotulo={r.id || 'regra'} />
              <b className="mono">%{r.id || 'nova'}%</b>
              <Chip>{r.casos.length} {r.casos.length === 1 ? 'caso' : 'casos'}</Chip>
              {r.id && regras.some((x, k) => k !== i && x.id.toLowerCase() === r.id.toLowerCase()) && <Chip tom="perigo">id repetido</Chip>}
            </div>
            <div className="card-h-a">
              <button className="icon-btn perigo" title="Remover regra" onClick={() => set({ ...c, regras: regras.filter((_, k) => k !== i) })}><Icon nome="trash" /></button>
            </div>
          </header>
          <p className="resumo">{(r.casos[0]?.sql || r.senao || '').split(/\r?\n/).find((l) => l.trim()) || 'sem trecho'}</p>

          <Field rotulo="Id da regra" dica="Letras, números e _.">
            <input className="input mono" value={r.id} maxLength={64} placeholder="Ordenacao" onChange={(e) => trocaRegra(i, { ...r, id: e.target.value.replace(/\W/g, '') })} />
          </Field>

          {r.casos.map((caso, k) => {
            const trocaCaso = (n: Caso) => trocaRegra(i, { ...r, casos: r.casos.map((x, j) => (j === k ? n : x)) })
            return (
              <div className="caso" key={k}>
                <div className="caso-h">
                  <span className="caso-n">{k === 0 ? 'Se' : 'Senão, se'}</span>
                  <button className="icon-btn perigo" title="Remover caso" onClick={() => trocaRegra(i, { ...r, casos: r.casos.filter((_, j) => j !== k) })}><Icon nome="x" size={16} /></button>
                </div>

                {caso.se.map((cond, m) => {
                  const p = paramsDe.find((x) => x.id === cond.param)
                  const trocaCond = (n: Condicao) => trocaCaso({ ...caso, se: caso.se.map((x, j) => (j === m ? n : x)) })
                  const semValor = cond.op === 'preenchido' || cond.op === 'vazio'
                  return (
                    <div className="cond" key={m}>
                      <span className="cond-e">{m === 0 ? '' : 'e'}</span>
                      <select className="input" aria-label="Parâmetro" value={cond.param} onChange={(e) => trocaCond({ ...cond, param: e.target.value, valor: '' })}>
                        {!p && <option value={cond.param}>{cond.param || '— escolha —'}</option>}
                        {paramsDe.map((x) => <option key={x.id} value={x.id}>{x.id}</option>)}
                      </select>
                      <select className="input" aria-label="Condição" value={cond.op} onChange={(e) => trocaCond({ ...cond, op: e.target.value as Condicao['op'] })}>
                        {OPERADORES_CONDICAO.map((o) => <option key={o.valor} value={o.valor}>{o.rotulo}</option>)}
                      </select>
                      {!semValor && <ValorInput tipo={p?.tipo ?? 'string'} opcoes={p?.opcoes} valor={cond.valor ?? ''} onChange={(v) => trocaCond({ ...cond, valor: v })} ariaLabel="Valor comparado" />}
                      <button className="icon-btn" title="Remover condição" disabled={caso.se.length === 1} onClick={() => trocaCaso({ ...caso, se: caso.se.filter((_, j) => j !== m) })}><Icon nome="x" size={16} /></button>
                    </div>
                  )
                })}
                <button className="mini" onClick={() => trocaCaso({ ...caso, se: [...caso.se, novaCondicao()] })}><Icon nome="plus" size={14} /> condição (e)</button>

                <Field rotulo="então use este trecho de SQL">
                  <SqlEditor rotulo="SQL do caso" minLinhas={3} valor={caso.sql} onChange={(v) => trocaCaso({ ...caso, sql: v })} chips={chipsDe(r.id)} />
                </Field>
              </div>
            )
          })}

          <button className="btn" onClick={() => trocaRegra(i, { ...r, casos: [...r.casos, novoCaso()] })}><Icon nome="plus" /> Adicionar caso</button>

          <Field rotulo="Senão (quando nenhum caso valer)" dica="Deixe vazio para não inserir nada.">
            <SqlEditor rotulo="SQL do senão" minLinhas={2} valor={r.senao ?? ''} onChange={(v) => trocaRegra(i, { ...r, senao: v })} chips={chipsDe(r.id)} />
          </Field>
        </section>
      ))}

      <div className="acoes-linha">
        <button className="btn btn-marca" onClick={() => { nav.irParaNovo(regras.length); set({ ...c, regras: [...regras, { id: '', casos: [novoCaso()], senao: '' }] }) }}>
          <Icon nome="plus" /> Nova regra
        </button>
      </div>
    </div>
  )
}

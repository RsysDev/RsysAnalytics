import { Icon } from '../icons'
import type { Comando, Passo } from '../types'
import { Chip, Field, SqlEditor, Toggle } from '../ui'
import { Navegador, Seta, useNavegacao } from './navegacao'

export function Passos({ c, set }: { c: Comando; set: (c: Comando) => void }) {
  const passos = c.passos
  const nomeDe = (i: number) => {
    const p = passos[i]
    return p.somenteExecucao ? 'execução' : p.nome?.trim() || `resultado${passos.slice(0, i + 1).filter((x) => !x.somenteExecucao).length}`
  }
  const nav = useNavegacao(passos.map((p, i) => `${nomeDe(i)} ${p.sql}`), 'pas-')
  const renumera = (l: Passo[]) => l.map((p, i) => ({ ...p, ordem: i + 1 }))
  const troca = (i: number, p: Passo) => set({ ...c, passos: passos.map((x, k) => (k === i ? p : x)) })
  const move = (i: number, d: number) => {
    const j = i + d
    if (j < 0 || j >= passos.length) return
    const n = [...passos]
    ;[n[i], n[j]] = [n[j], n[i]]
    set({ ...c, passos: renumera(n) })
  }

  const chips = [
    ...c.parametros.filter((p) => p.id).flatMap((p) => [
      { texto: `[${p.id}]`, dica: `valor de ${p.id}` },
      { texto: `{${p.id}}`, dica: `condição pronta com a coluna ${p.campo || p.id}` },
    ]),
    ...c.regras.filter((r) => r.id).map((r) => ({ texto: `%${r.id}%`, dica: `trecho escolhido pela regra ${r.id}` })),
  ]

  return (
    <div className="stack">
      <p className="dica">
        Os passos rodam em ordem, na mesma conexão (uma tabela <code>#temp</code> criada num passo vale nos seguintes). Passos que
        <b> devolvem dados</b> têm um nome — é por ele que o painel ou o Claude pedem só um resultado. Linhas iniciadas por <code>;</code> são
        comentários e linhas iniciadas por <code>#</code> só entram se os parâmetros citados tiverem valor.
      </p>

      <Navegador nav={nav} rotulos={passos.map((_, i) => nomeDe(i))} plural="passos" />

      {passos.map((p, i) => {
        const nomeAuto = !p.somenteExecucao ? `resultado${passos.slice(0, i + 1).filter((x) => !x.somenteExecucao).length}` : ''
        return (
          <section className={`card ${p.somenteExecucao ? 'card-exec' : ''} ${nav.recolhidos.has(i) ? 'fechado' : ''}`} key={i} id={`pas-${i}`} data-nav hidden={!nav.visivel(i)}>
            <header className="card-h">
              <div className="card-h-t">
                <Seta aberto={!nav.recolhidos.has(i)} onClick={() => nav.alternar(i)} rotulo={`passo ${i + 1}`} />
                <span className="ordem">{i + 1}</span>
                {p.somenteExecucao ? <Chip tom="aviso">execução, sem retorno</Chip> : <Chip tom="ok">devolve dados</Chip>}
                {!p.somenteExecucao && <b className="mono">{p.nome?.trim() || nomeAuto}</b>}
              </div>
              <div className="card-h-a">
                <button className="icon-btn" title="Subir" disabled={i === 0} onClick={() => move(i, -1)}><Icon nome="up" /></button>
                <button className="icon-btn" title="Descer" disabled={i === passos.length - 1} onClick={() => move(i, 1)}><Icon nome="down" /></button>
                <button className="icon-btn perigo" title="Remover passo" disabled={passos.length === 1}
                  onClick={() => set({ ...c, passos: renumera(passos.filter((_, k) => k !== i)) })}><Icon nome="trash" /></button>
              </div>
            </header>
            <p className="resumo">{p.sql.split(/\r?\n/).find((l) => l.trim()) ?? 'vazio'}</p>

            <div className="grid g2">
              {!p.somenteExecucao && (
                <Field rotulo="Nome do resultado" dica={`Vazio = ${nomeAuto}. Letras, números e _.`}>
                  <input className="input mono" value={p.nome ?? ''} maxLength={64} placeholder={nomeAuto}
                    onChange={(e) => troca(i, { ...p, nome: e.target.value.replace(/\W/g, '') })} />
                </Field>
              )}
              <Toggle valor={p.somenteExecucao} onChange={(v) => troca(i, { ...p, somenteExecucao: v })} rotulo="Somente execução"
                dica="update, insert, select into #temp... Não devolve dados e só roda em executar_comando_com_escrita." />
            </div>

            <SqlEditor rotulo={`SQL do passo ${i + 1}`} valor={p.sql} onChange={(v) => troca(i, { ...p, sql: v })} chips={chips} minLinhas={9} />
          </section>
        )
      })}

      <div className="acoes-linha">
        <button className="btn btn-marca" onClick={() => { nav.irParaNovo(passos.length); set({ ...c, passos: renumera([...passos, { ordem: 0, nome: '', sql: 'select ', somenteExecucao: false }]) }) }}>
          <Icon nome="plus" /> Passo que devolve dados
        </button>
        <button className="btn" onClick={() => { nav.irParaNovo(passos.length); set({ ...c, passos: renumera([...passos, { ordem: 0, nome: '', sql: '', somenteExecucao: true }]) }) }}>
          <Icon nome="plus" /> Passo de execução
        </button>
      </div>
    </div>
  )
}

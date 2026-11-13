import { Icon } from '../icons'
import { useState } from 'react'
import { novoParametro, OPERADORES, TIPOS, type Comando, type Opcao, type Parametro } from '../types'
import { Chip, Field, ValorInput, Toggle, Vazio } from '../ui'
import { Navegador, Seta, useNavegacao } from './navegacao'

const rotuloTipo = (t: string) => TIPOS.find((x) => x.valor === t)?.rotulo ?? t

/** "''=Sem quebra; P.IdGrupo=Grupo" -> opções. '' ou "" é o valor vazio; sem '=' o rótulo é o próprio valor. */
function lerLista(texto: string): Opcao[] {
  const saida: Opcao[] = []
  for (const item of texto.split(/[;\n]/).map((x) => x.trim()).filter(Boolean)) {
    const corte = item.indexOf('=')
    let valor = (corte < 0 ? item : item.slice(0, corte)).trim()
    if (valor === "''" || valor === '""') valor = ''
    else if (valor.length >= 2 && (valor[0] === "'" || valor[0] === '"') && valor.endsWith(valor[0])) valor = valor.slice(1, -1)
    const rotulo = (corte < 0 ? valor : item.slice(corte + 1)).trim() || valor || '(vazio)'
    if (!saida.some((o) => o.valor === valor)) saida.push({ valor, rotulo })
  }
  return saida
}

/** Edita a lista fechada de um parâmetro (vira combobox na aba Testar), linha a linha ou colando "valor=Rótulo; ...". */
function Opcoes({ opcoes, onChange }: { opcoes: Opcao[]; onChange: (o: Opcao[] | null) => void }) {
  const [colar, setColar] = useState(false)
  const [texto, setTexto] = useState('')
  const grava = (l: Opcao[]) => onChange(l.length ? l : null)
  return (
    <div className="opcoes">
      <div className="opcoes-h">
        <span className="field-l">Lista de opções (combobox)</span>
        <button type="button" className="mini" onClick={() => { setColar(!colar); setTexto(opcoes.map((o) => `${o.valor === '' ? "''" : o.valor}=${o.rotulo}`).join('; ')) }}>
          {colar ? 'fechar' : 'colar lista'}
        </button>
      </div>
      {colar && (
        <div className="opcoes-colar">
          <textarea className="input mono" rows={3} value={texto} placeholder="''=Sem quebra; P.IdGrupo=Grupo; M.Sigla=Marca" onChange={(e) => setTexto(e.target.value)} />
          <button type="button" className="btn btn-marca" onClick={() => { grava(lerLista(texto)); setColar(false) }}>Aplicar lista</button>
        </div>
      )}
      {opcoes.map((o, i) => (
        <div className="opcao" key={i}>
          <input className="input mono" aria-label="Valor" placeholder="valor (vazio = sem valor)" value={o.valor}
            onChange={(e) => grava(opcoes.map((x, k) => (k === i ? { ...x, valor: e.target.value } : x)))} />
          <input className="input" aria-label="Rótulo" placeholder="Rótulo exibido" value={o.rotulo}
            onChange={(e) => grava(opcoes.map((x, k) => (k === i ? { ...x, rotulo: e.target.value } : x)))} />
          <button className="icon-btn perigo" title="Remover opção" onClick={() => grava(opcoes.filter((_, k) => k !== i))}><Icon nome="x" size={16} /></button>
        </div>
      ))}
      <button type="button" className="mini" onClick={() => onChange([...opcoes, { valor: '', rotulo: '' }])}><Icon nome="plus" size={14} /> opção</button>
      <span className="field-h">Com opções, quem chama só pode usar um desses valores (também o Claude). Sem opções, o campo segue o tipo.</span>
    </div>
  )
}

export function Parametros({ c, set }: { c: Comando; set: (c: Comando) => void }) {
  const lista = c.parametros
  const nav = useNavegacao(lista.map((p) => `${p.id} ${p.descricao ?? ''} ${p.campo ?? ''}`), 'par-')
  const troca = (i: number, p: Parametro) => set({ ...c, parametros: lista.map((x, k) => (k === i ? p : x)) })
  const remove = (i: number) => set({ ...c, parametros: lista.filter((_, k) => k !== i) })
  const move = (i: number, d: number) => {
    const j = i + d
    if (j < 0 || j >= lista.length) return
    const n = [...lista]
    ;[n[i], n[j]] = [n[j], n[i]]
    set({ ...c, parametros: n })
  }
  const add = (...novos: Parametro[]) => { nav.irParaNovo(lista.length); set({ ...c, parametros: [...lista, ...novos] }) }
  const existe = (id: string) => lista.some((p) => p.id.toLowerCase() === id.toLowerCase())

  const presets: { rotulo: string; fazer: () => void; ok: boolean }[] = [
    {
      rotulo: 'Empresas do servidor',
      ok: !existe('Empresas'),
      fazer: () => add({ ...novoParametro(), id: 'Empresas', campo: 'IdEmpresa', tipo: 'inteiro', origem: 'servidor', chave: 'idsEmpresa' }),
    },
    {
      rotulo: 'Usuário do servidor',
      ok: !existe('IdUsuario'),
      fazer: () => add({ ...novoParametro(), id: 'IdUsuario', tipo: 'inteiro', valorUnico: true, origem: 'servidor', chave: 'idUsuario' }),
    },
    {
      rotulo: 'Período (início e fim)',
      ok: !existe('DataInicio') && !existe('DataFim'),
      fazer: () => add(
        { ...novoParametro(), id: 'DataInicio', tipo: 'date', valorUnico: true, obrigatorio: true, descricao: 'Data inicial, inclusive (yyyy-MM-dd)' },
        { ...novoParametro(), id: 'DataFim', tipo: 'date', valorUnico: true, obrigatorio: true, descricao: 'Data final, inclusive (yyyy-MM-dd)' },
      ),
    },
  ]

  return (
    <div className="stack">
      <p className="dica">
        Cada parâmetro vira <code>[Id]</code> (o valor) ou <code>{'{Id}'}</code> (uma condição pronta, como <code>(Coluna = @p1)</code>) nos SQLs.
        Tudo vai ao banco como parâmetro, nunca dentro do texto.
      </p>

      {lista.length === 0 && (
        <Vazio icone="sliders" titulo="Sem parâmetros" texto="Este comando roda sempre do mesmo jeito. Adicione parâmetros para o Claude poder filtrar." />
      )}

      <Navegador nav={nav} rotulos={lista.map((p) => p.id)} plural="parâmetros" />

      {lista.map((p, i) => {
        const servidor = p.origem === 'servidor'
        const dupe = p.id && lista.some((x, k) => k !== i && x.id.toLowerCase() === p.id.toLowerCase())
        return (
          <section className={`card ${nav.recolhidos.has(i) ? 'fechado' : ''}`} key={i} id={`par-${i}`} data-nav hidden={!nav.visivel(i)}>
            <header className="card-h">
              <div className="card-h-t">
                <Seta aberto={!nav.recolhidos.has(i)} onClick={() => nav.alternar(i)} rotulo={p.id || 'parâmetro'} />
                <b className="mono">{p.id || 'novo parâmetro'}</b>
                <Chip tom="marca">{rotuloTipo(p.tipo)}</Chip>
                {servidor ? <Chip tom="aviso">definido pelo servidor</Chip> : p.obrigatorio && <Chip tom="perigo">obrigatório</Chip>}
                {dupe && <Chip tom="perigo">id repetido</Chip>}
              </div>
              <div className="card-h-a">
                <button className="icon-btn" title="Subir" disabled={i === 0} onClick={() => move(i, -1)}><Icon nome="up" /></button>
                <button className="icon-btn" title="Descer" disabled={i === lista.length - 1} onClick={() => move(i, 1)}><Icon nome="down" /></button>
                <button className="icon-btn perigo" title="Remover" onClick={() => remove(i)}><Icon nome="trash" /></button>
              </div>
            </header>
            <p className="resumo">{p.descricao || p.campo || 'sem descrição'}</p>

            <div className="grid g3">
              <Field rotulo="Id" dica="Nome usado no SQL e na chamada.">
                <input className="input mono" value={p.id} maxLength={64} placeholder="DataVenda"
                  onChange={(e) => troca(i, { ...p, id: e.target.value.replace(/\W/g, '') })} />
              </Field>
              <Field rotulo="Coluna (campo)" dica="Usada em {Id}. Pode ter alias: p.DataMovimento.">
                <input className="input mono" value={p.campo ?? ''} placeholder={p.id || 'p.Coluna'} onChange={(e) => troca(i, { ...p, campo: e.target.value })} />
              </Field>
              <Field rotulo="Tipo">
                <select className="input" value={p.tipo} onChange={(e) => troca(i, { ...p, tipo: e.target.value as Parametro['tipo'], padrao: '', padrao2: '' })}>
                  {TIPOS.map((t) => <option key={t.valor} value={t.valor}>{t.rotulo}</option>)}
                </select>
              </Field>
            </div>

            <Field rotulo="Descrição" dica="O Claude lê para saber como preencher.">
              <input className="input" value={p.descricao ?? ''} onChange={(e) => troca(i, { ...p, descricao: e.target.value })} />
            </Field>

            <div className="grid g2">
              <Field rotulo="Quem informa o valor" dica={servidor ? 'O servidor impõe o valor; o chamador não consegue alterar (use para empresa e usuário).' : 'Quem chama o comando informa.'}>
                <select className="input" value={p.origem}
                  onChange={(e) => troca(i, { ...p, origem: e.target.value as Parametro['origem'], chave: e.target.value === 'servidor' ? 'idsEmpresa' : null })}>
                  <option value="chamador">Quem chama (Claude / painel)</option>
                  <option value="servidor">Servidor</option>
                </select>
              </Field>
              {servidor && (
                <Field rotulo="Valor do servidor">
                  <select className="input" value={p.chave ?? 'idsEmpresa'} onChange={(e) => troca(i, { ...p, chave: e.target.value })}>
                    <option value="idsEmpresa">Empresas (Painel:IdsEmpresa)</option>
                    <option value="idUsuario">Usuário (Painel:IdUsuario)</option>
                  </select>
                </Field>
              )}
            </div>

            {!servidor && <Opcoes opcoes={p.opcoes ?? []} onChange={(o) => troca(i, { ...p, opcoes: o, padrao: '', padrao2: '' })} />}

            {!servidor && (
              <>
                <div className="grid g2">
                  <Toggle valor={p.obrigatorio} onChange={(v) => troca(i, { ...p, obrigatorio: v })} rotulo="Obrigatório" dica="Sem valor nem padrão, o comando recusa." />
                  <Toggle valor={p.valorUnico} onChange={(v) => troca(i, { ...p, valorUnico: v, condicaoFixa: v ? null : p.condicaoFixa })} rotulo="Valor único" dica="Só aceita um valor, sempre 'igual a'." />
                </div>
                {!p.valorUnico && (
                  <Field rotulo="Comparação" dica="Fixa ignora o que o chamador enviar; livre deixa escolher (igual, entre, contém...).">
                    <select className="input" value={p.condicaoFixa ?? ''} onChange={(e) => troca(i, { ...p, condicaoFixa: (e.target.value || null) as Parametro['condicaoFixa'] })}>
                      <option value="">Livre (o chamador escolhe)</option>
                      {OPERADORES.map((o) => <option key={o.valor} value={o.valor}>Fixa: {o.rotulo}</option>)}
                    </select>
                  </Field>
                )}
                <div className="grid g2">
                  <Field rotulo={p.valorUnico ? 'Valor padrão' : 'Valor padrão (início)'} dica="Usado quando o chamador não informa.">
                    <ValorInput tipo={p.tipo} opcoes={p.opcoes} valor={p.padrao ?? ''} onChange={(v) => troca(i, { ...p, padrao: v })} ariaLabel="Valor padrão" />
                  </Field>
                  {!p.valorUnico && p.tipo !== 'checkbox' && (
                    <Field rotulo="Valor padrão (fim)" dica="Opcional: com os dois, o padrão vira 'entre'.">
                      <ValorInput tipo={p.tipo} opcoes={p.opcoes} valor={p.padrao2 ?? ''} onChange={(v) => troca(i, { ...p, padrao2: v })} ariaLabel="Valor padrão 2" />
                    </Field>
                  )}
                </div>
              </>
            )}
          </section>
        )
      })}

      <div className="acoes-linha">
        <button className="btn btn-marca" onClick={() => add(novoParametro())}><Icon nome="plus" /> Novo parâmetro</button>
        {presets.filter((x) => x.ok).map((x) => (
          <button key={x.rotulo} className="btn" onClick={x.fazer}><Icon nome="plus" /> {x.rotulo}</button>
        ))}
      </div>
    </div>
  )
}

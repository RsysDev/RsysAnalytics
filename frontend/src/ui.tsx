import { useRef, type ReactNode } from 'react'
import { Icon } from './icons'
import type { Opcao, TipoParametro } from './types'

/** Rótulo + controle + dica, empilhados. */
export function Field({ rotulo, dica, children, className = '' }: { rotulo: string; dica?: string; children: ReactNode; className?: string }) {
  return (
    <label className={`field ${className}`}>
      <span className="field-l">{rotulo}</span>
      {children}
      {dica && <span className="field-h">{dica}</span>}
    </label>
  )
}

export function Toggle({ valor, onChange, rotulo, dica }: { valor: boolean; onChange: (v: boolean) => void; rotulo: string; dica?: string }) {
  return (
    <button type="button" role="switch" aria-checked={valor} className={`toggle ${valor ? 'on' : ''}`} onClick={() => onChange(!valor)}>
      <span className="toggle-track"><span className="toggle-knob" /></span>
      <span className="toggle-txt">
        <b>{rotulo}</b>
        {dica && <small>{dica}</small>}
      </span>
    </button>
  )
}

export function Chip({ children, tom = 'neutro' }: { children: ReactNode; tom?: 'neutro' | 'ok' | 'aviso' | 'perigo' | 'marca' }) {
  return <span className={`chip chip-${tom}`}>{children}</span>
}

export function Vazio({ icone, titulo, texto, acao }: { icone: string; titulo: string; texto?: string; acao?: ReactNode }) {
  return (
    <div className="vazio">
      <div className="vazio-ic"><Icon nome={icone} size={28} /></div>
      <h3>{titulo}</h3>
      {texto && <p>{texto}</p>}
      {acao}
    </div>
  )
}

export function Confirmar({ titulo, texto, confirmar, perigo, onSim, onNao }: {
  titulo: string; texto: string; confirmar: string; perigo?: boolean; onSim: () => void; onNao: () => void
}) {
  return (
    <div className="overlay" role="presentation" onClick={onNao}>
      <div className="modal" role="alertdialog" aria-modal="true" aria-label={titulo} onClick={(e) => e.stopPropagation()}>
        <h3>{titulo}</h3>
        <p>{texto}</p>
        <div className="modal-acoes">
          <button className="btn" onClick={onNao}>Cancelar</button>
          <button className={`btn ${perigo ? 'btn-perigo' : 'btn-marca'}`} onClick={onSim}>{confirmar}</button>
        </div>
      </div>
    </div>
  )
}

/**
 * Entrada de um valor conforme o tipo do parâmetro. O valor circula sempre como texto no formato da API
 * (datas ISO, números com ponto), então o mesmo componente serve para o valor padrão e para os testes.
 */
export function ValorInput({ tipo, valor, onChange, placeholder, disabled, ariaLabel, opcoes }: {
  tipo: TipoParametro; valor: string; onChange: (v: string) => void; placeholder?: string; disabled?: boolean; ariaLabel?: string
  /** Parâmetro com lista fechada: vira combobox, qualquer que seja o tipo. */
  opcoes?: Opcao[] | null
}) {
  const comum = { disabled, 'aria-label': ariaLabel, className: 'input' }
  if (opcoes && opcoes.length > 0) {
    const tem = (v: string) => opcoes.some((o) => o.valor === v)
    return (
      <select {...comum} value={valor} onChange={(e) => onChange(e.target.value)}>
        {!tem('') && <option value="">— sem valor —</option>}
        {valor !== '' && !tem(valor) && <option value={valor}>{valor} (fora da lista)</option>}
        {opcoes.map((o) => <option key={o.valor} value={o.valor} title={o.valor ? `SQL: ${o.valor}` : 'sem valor'}>{o.rotulo}</option>)}
      </select>
    )
  }
  switch (tipo) {
    case 'inteiro':
      return <input {...comum} type="number" step={1} inputMode="numeric" placeholder={placeholder} value={valor} onChange={(e) => onChange(e.target.value)} />
    case 'decimal':
      return <input {...comum} type="number" step="any" inputMode="decimal" placeholder={placeholder} value={valor} onChange={(e) => onChange(e.target.value)} />
    case 'date':
      return <input {...comum} type="date" value={valor.slice(0, 10)} onChange={(e) => onChange(e.target.value)} />
    case 'dateTime':
      return <input {...comum} type="datetime-local" value={valor.slice(0, 16)} onChange={(e) => onChange(e.target.value ? e.target.value + ':00' : '')} />
    case 'checkbox':
      return (
        <select {...comum} value={valor} onChange={(e) => onChange(e.target.value)}>
          <option value="">— sem valor —</option>
          <option value="true">Sim</option>
          <option value="false">Não</option>
        </select>
      )
    default:
      return <input {...comum} type="text" placeholder={placeholder} value={valor} onChange={(e) => onChange(e.target.value)} />
  }
}

/** Área de texto para SQL: fonte monoespaçada, Tab insere recuo e chips inserem [Param], {Param} ou %Regra% no cursor. */
export function SqlEditor({ valor, onChange, chips, minLinhas = 7, rotulo }: {
  valor: string; onChange: (v: string) => void; chips?: { texto: string; dica: string }[]; minLinhas?: number; rotulo: string
}) {
  const ref = useRef<HTMLTextAreaElement>(null)
  const linhas = Math.min(26, Math.max(minLinhas, valor.split('\n').length + 1))

  const inserir = (txt: string) => {
    const el = ref.current
    if (!el) return onChange(valor + txt)
    const { selectionStart: a, selectionEnd: b } = el
    onChange(valor.slice(0, a) + txt + valor.slice(b))
    requestAnimationFrame(() => { el.focus(); el.setSelectionRange(a + txt.length, a + txt.length) })
  }

  return (
    <div className="sqled">
      {chips && chips.length > 0 && (
        <div className="sqled-chips" aria-label="Inserir no SQL">
          {chips.map((c) => (
            <button key={c.texto} type="button" className="mini" title={c.dica} onClick={() => inserir(c.texto)}>{c.texto}</button>
          ))}
        </div>
      )}
      <textarea
        ref={ref} className="sql" spellCheck={false} rows={linhas} aria-label={rotulo} value={valor}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={(e) => { if (e.key === 'Tab') { e.preventDefault(); inserir('  ') } }}
      />
    </div>
  )
}

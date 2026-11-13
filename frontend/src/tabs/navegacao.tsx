import { useEffect, useRef, useState } from 'react'
import { Icon } from '../icons'

/**
 * Navegação dentro de uma lista longa de cartões (parâmetros, regras, passos): filtro por texto, salto direto para um item,
 * anterior/próximo e recolher/expandir. Os cartões usam `id={prefixo + índice}` e `data-nav`.
 */
export function useNavegacao(textos: string[], prefixo: string) {
  const n = textos.length
  const [filtro, setFiltro] = useState('')
  // Listas grandes começam recolhidas: o cabeçalho de cada item já identifica e o seletor leva até ele.
  const [recolhidos, setRecolhidos] = useState<Set<number>>(() => new Set(n > 8 ? textos.map((_, i) => i) : []))
  const [atual, setAtual] = useState<number | null>(null)
  const pendente = useRef<number | null>(null)

  const q = filtro.trim().toLowerCase()
  const visivel = (i: number) => !q || (textos[i] ?? '').toLowerCase().includes(q)
  const visiveis = textos.map((_, i) => i).filter(visivel)

  const rolar = (i: number) => {
    const el = document.getElementById(`${prefixo}${i}`)
    if (!el) return false
    el.scrollIntoView({ behavior: 'smooth', block: 'start' })
    el.classList.remove('destaque')
    void el.offsetWidth // reinicia a animação
    el.classList.add('destaque')
    return true
  }

  // Depois de renderizar, completa um salto pedido antes de o cartão existir (ex.: item recém-criado).
  useEffect(() => {
    if (pendente.current !== null && rolar(pendente.current)) pendente.current = null
  })

  const ir = (i: number) => {
    if (i < 0 || i >= n) return
    setFiltro('')
    setRecolhidos((s) => { const c = new Set(s); c.delete(i); return c })
    setAtual(i)
    pendente.current = i
  }

  const alternar = (i: number) => setRecolhidos((s) => { const c = new Set(s); if (!c.delete(i)) c.add(i); return c })
  const recolherTodos = () => setRecolhidos(new Set(textos.map((_, k) => k)))
  const expandirTodos = () => setRecolhidos(new Set())
  const passo = (d: number) => {
    const lista = visiveis.length ? visiveis : textos.map((_, i) => i)
    const pos = atual === null ? -1 : lista.indexOf(atual)
    const prox = lista[Math.min(lista.length - 1, Math.max(0, pos + d))]
    if (prox !== undefined) ir(prox)
  }
  /** Chamar ao criar um item: abre e rola até ele. */
  const irParaNovo = (i: number) => { setFiltro(''); setAtual(i); pendente.current = i }

  return { filtro, setFiltro, visivel, visiveis, recolhidos, alternar, recolherTodos, expandirTodos, ir, passo, atual, irParaNovo, n }
}

export type Navegacao = ReturnType<typeof useNavegacao>

export function Seta({ aberto, onClick, rotulo }: { aberto: boolean; onClick: () => void; rotulo: string }) {
  return (
    <button type="button" className={`icon-btn seta ${aberto ? 'aberta' : ''}`} aria-expanded={aberto} title={aberto ? 'Recolher' : 'Expandir'} aria-label={`${aberto ? 'Recolher' : 'Expandir'} ${rotulo}`} onClick={onClick}>
      <Icon nome="down" size={18} />
    </button>
  )
}

/** Barra fixa no topo da lista. Só aparece quando há mais de um item. */
export function Navegador({ nav, rotulos, plural }: { nav: Navegacao; rotulos: string[]; plural: string }) {
  if (nav.n < 2) return null
  return (
    <div className="nav-bar" role="search" aria-label={`Navegar entre ${plural}`}>
      <div className="busca nav-busca">
        <Icon nome="search" size={16} />
        <input placeholder={`Filtrar ${plural}…`} value={nav.filtro} onChange={(e) => nav.setFiltro(e.target.value)} aria-label={`Filtrar ${plural}`} />
        {nav.filtro && <button type="button" className="icon-btn nav-limpa" title="Limpar filtro" onClick={() => nav.setFiltro('')}><Icon nome="x" size={14} /></button>}
      </div>
      <select className="input nav-ir" aria-label="Ir para" value={nav.atual ?? ''} onChange={(e) => e.target.value !== '' && nav.ir(Number(e.target.value))}>
        <option value="">Ir para… ({nav.n})</option>
        {rotulos.map((r, i) => <option key={i} value={i}>{i + 1}. {r || '(sem nome)'}</option>)}
      </select>
      <div className="nav-botoes">
        <button type="button" className="icon-btn" title="Anterior" onClick={() => nav.passo(-1)}><Icon nome="up" /></button>
        <button type="button" className="icon-btn" title="Próximo" onClick={() => nav.passo(1)}><Icon nome="down" /></button>
        <button type="button" className="mini" onClick={nav.recolhidos.size >= nav.n ? nav.expandirTodos : nav.recolherTodos}>
          {nav.recolhidos.size >= nav.n ? 'expandir tudo' : 'recolher tudo'}
        </button>
        <span className="nav-conta">{nav.filtro ? `${nav.visiveis.length} de ${nav.n}` : nav.n}</span>
      </div>
    </div>
  )
}

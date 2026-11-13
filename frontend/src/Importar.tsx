import { useEffect, useMemo, useState } from 'react'
import { ApiError, api } from './api'
import { Icon } from './icons'
import type { Comando, RelatorioLegadoItem, TabelaLegado } from './types'
import { Chip, Vazio } from './ui'

/**
 * Folha de importação do ERP legado (fwRelatorio / fwRelatorioDinamico). Escolher um relatório converte no servidor
 * e abre o resultado no editor como rascunho novo: nada é gravado até o usuário revisar, testar e salvar.
 */
export function Importar({ tabelas, aoFechar, aoEscolher }: {
  tabelas: TabelaLegado[]
  aoFechar: () => void
  aoEscolher: (comando: Comando, avisos: string[]) => void
}) {
  const [tabela, setTabela] = useState(tabelas[0]?.nome ?? '')
  const [itens, setItens] = useState<RelatorioLegadoItem[] | null>(null)
  const [busca, setBusca] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const [convertendo, setConvertendo] = useState<string | null>(null)

  useEffect(() => {
    if (!tabela) return
    let ativo = true
    api.legadoLista(tabela)
      .then((l) => { if (ativo) { setItens(l); setErro(null) } })
      .catch((e) => { if (ativo) setErro(e instanceof ApiError ? e.message : 'Não foi possível ler a lista.') })
    return () => { ativo = false }
  }, [tabela])

  const trocaTabela = (nome: string) => { setItens(null); setErro(null); setTabela(nome) }

  const visiveis = useMemo(() => {
    const q = busca.trim().toLowerCase()
    return (itens ?? []).filter((i) => !q || i.idRelatorio.toLowerCase().includes(q) || i.titulo.toLowerCase().includes(q) || (i.tag ?? '').toLowerCase().includes(q))
  }, [itens, busca])

  const escolher = async (item: RelatorioLegadoItem) => {
    setConvertendo(item.idRelatorio)
    setErro(null)
    try {
      const r = await api.converterLegado(tabela, item.idRelatorio)
      // O id do legado já existe no catálogo: o rascunho nasce com outro id (editável) para não colidir ao salvar.
      const comando = item.existe ? { ...r.comando, id: `${r.comando.id}_importado` } : r.comando
      const avisos = item.existe ? [`Já existe um comando '${item.idComando}'; este rascunho usa o id '${comando.id}'.`, ...r.avisos] : r.avisos
      aoEscolher(comando, avisos)
    } catch (e) {
      setErro(e instanceof ApiError ? e.message : 'Não foi possível converter este relatório.')
      setConvertendo(null)
    }
  }

  return (
    <div className="overlay" role="presentation" onClick={aoFechar}>
      <div className="sheet" role="dialog" aria-modal="true" aria-label="Importar do ERP legado" onClick={(e) => e.stopPropagation()}>
        <header className="sheet-h">
          <div>
            <h3>Importar do ERP legado</h3>
            <p className="dica">Converte o relatório em um rascunho de comando. Nada é gravado até você salvar.</p>
          </div>
          <button className="icon-btn" title="Fechar" onClick={aoFechar}><Icon nome="x" /></button>
        </header>

        {tabelas.length > 1 && (
          <div className="abas-mini" role="tablist">
            {tabelas.map((t) => (
              <button key={t.nome} role="tab" aria-selected={t.nome === tabela} className={t.nome === tabela ? 'on' : ''} onClick={() => trocaTabela(t.nome)}>
                {t.nome} <small>{t.total}</small>
              </button>
            ))}
          </div>
        )}

        <div className="busca">
          <Icon nome="search" size={16} />
          <input autoFocus placeholder="Buscar por título, id ou tag…" value={busca} onChange={(e) => setBusca(e.target.value)} aria-label="Buscar relatório" />
        </div>

        {erro && <div className="banner banner-perigo"><Icon nome="alert" /><div className="pre">{erro}</div></div>}

        <div className="sheet-lista">
          {itens === null && !erro && <div className="esqueleto" />}
          {itens !== null && visiveis.length === 0 && <Vazio icone="search" titulo="Nada encontrado" texto="Ajuste a busca ou troque de tabela." />}
          {visiveis.map((i) => (
            <button key={i.idRelatorio} className="item" disabled={convertendo !== null} onClick={() => escolher(i)}>
              <div className="item-l1">
                <b>{i.titulo || i.idRelatorio}</b>
                {i.existe && <Chip tom="ok">já no catálogo</Chip>}
                {convertendo === i.idRelatorio && <Chip tom="marca">convertendo…</Chip>}
              </div>
              <div className="item-l3">
                <Chip><span className="mono">{i.idRelatorio}</span></Chip>
                {i.tag && <Chip>{i.tag}</Chip>}
              </div>
            </button>
          ))}
        </div>
      </div>
    </div>
  )
}

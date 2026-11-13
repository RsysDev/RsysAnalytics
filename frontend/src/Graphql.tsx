import { useCallback, useEffect, useMemo, useState } from 'react'
import { ApiError, api } from './api'
import { Icon } from './icons'
import type { ColunaExposta, Info, PerfilGraphQL, ProblemaPerfil, RelacaoExposta, TabelaBanco, TabelaBancoResumo, TabelaExposta } from './types'
import { Chip, Confirmar, Field, Toggle, Vazio } from './ui'

/** "tbEntidade" -> "Entidade"; "prProduto" -> "Produto". Só uma sugestão: o nome é editável. */
const sugerirTipo = (tabela: string) => {
  const base = tabela.replace(/^(tb|pr|ft|fi|ce|fw)(?=[A-Z])/, '').replace(/\W/g, '')
  return base ? base[0].toUpperCase() + base.slice(1) : 'Tabela'
}
const sugerirConsulta = (tipo: string) => { const c = tipo[0].toLowerCase() + tipo.slice(1); return c.endsWith('s') ? c : c + 's' }
const sugerirCampo = (coluna: string) => { const c = coluna.replace(/\W/g, ''); const r = c ? c[0].toLowerCase() + c.slice(1) : 'campo'; return /^\d/.test(r) ? 'c' + r : r }
const assinatura = (p: PerfilGraphQL | null) => (p ? JSON.stringify(p) : '')

/**
 * Perfil do GraphQL do sistema configurado (Sistema:Perfil): o usuário escolhe quais tabelas e colunas do banco aparecem no
 * schema, com que nomes e relações. Nada aparece sem ser marcado aqui; colunas com cara de dado sensível começam desmarcadas.
 */
export function Graphql({ info, comandosAtivos, aoTrocarModo, aoSair, tema, aoTrocarTema }: {
  info: Info; comandosAtivos: boolean; aoTrocarModo: () => void; aoSair: () => void; tema: 'claro' | 'escuro'; aoTrocarTema: () => void
}) {
  const [perfil, setPerfil] = useState<PerfilGraphQL | null>(null)
  const [original, setOriginal] = useState('')
  const [banco, setBanco] = useState<TabelaBancoResumo[]>([])
  const [busca, setBusca] = useState('')
  const [soExpostas, setSoExpostas] = useState(false)
  const [selecionada, setSelecionada] = useState<string | null>(null)
  const [detalhes, setDetalhes] = useState<Record<string, TabelaBanco>>({})
  const [problemas, setProblemas] = useState<ProblemaPerfil[]>([])
  const [erro, setErro] = useState<string | null>(null)
  const [aviso, setAviso] = useState<string | null>(null)
  const [salvando, setSalvando] = useState(false)
  const [filtroCol, setFiltroCol] = useState('')
  const [remover, setRemover] = useState(false)

  const falha = useCallback((e: unknown) => {
    if (e instanceof ApiError && e.status === 401) { aoSair(); return }
    setErro(e instanceof ApiError ? e.message : 'Não foi possível falar com o servidor.')
  }, [aoSair])

  const carregarPerfil = useCallback(async () => {
    try {
      const r = await api.graphqlPerfil()
      setPerfil(r.perfil); setOriginal(assinatura(r.perfil)); setProblemas(r.problemas); setErro(null)
    } catch (e) {
      // Tabela do perfil ainda não criada: dá para montar o perfil, mas só salvar depois de rodar o script.
      setPerfil({ sistema: info.sistema ?? '', tabelas: [] }); setOriginal(''); falha(e)
    }
  }, [falha, info.sistema])

  useEffect(() => { void carregarPerfil() }, [carregarPerfil])
  useEffect(() => {
    const t = setTimeout(() => { api.graphqlTabelas(busca).then(setBanco).catch(falha) }, busca ? 250 : 0)
    return () => clearTimeout(t)
  }, [busca, falha])
  useEffect(() => { if (!aviso) return; const t = setTimeout(() => setAviso(null), 3500); return () => clearTimeout(t) }, [aviso])

  const exposta = (chave: string) => perfil?.tabelas.find((t) => t.tabela.toLowerCase() === chave.toLowerCase())
  const sujo = perfil !== null && assinatura(perfil) !== original
  const detalhe = selecionada ? detalhes[selecionada.toLowerCase()] : undefined
  const entrada = selecionada ? exposta(selecionada) : undefined

  const abrir = async (chave: string) => {
    setSelecionada(chave); setFiltroCol(''); setErro(null)
    if (!detalhes[chave.toLowerCase()]) {
      try { const t = await api.graphqlTabela(chave); setDetalhes((d) => ({ ...d, [chave.toLowerCase()]: t })) } catch (e) { falha(e) }
    }
  }

  const trocaTabela = (chave: string, f: (t: TabelaExposta) => TabelaExposta) =>
    setPerfil((p) => (p ? { ...p, tabelas: p.tabelas.map((t) => (t.tabela.toLowerCase() === chave.toLowerCase() ? f(t) : t)) } : p))

  /** Expor uma tabela: nasce com as colunas suportadas que não parecem sensíveis marcadas. */
  const expor = () => {
    if (!detalhe || !perfil) return
    const tipo = sugerirTipo(detalhe.nome)
    const usados = new Set(perfil.tabelas.map((t) => t.nome.toLowerCase()))
    let nome = tipo
    for (let n = 2; usados.has(nome.toLowerCase()); n++) nome = tipo + n
    const colunas: ColunaExposta[] = detalhe.colunas.filter((c) => c.tipo !== 'naoSuportado' && !c.sensivel)
      .map((c) => ({ coluna: c.nome, campo: sugerirCampo(c.nome), filtravel: c.chave, ordenavel: c.chave }))
    setPerfil({ ...perfil, tabelas: [...perfil.tabelas, { tabela: detalhe.chave, nome, consulta: sugerirConsulta(nome), ativa: true, colunas, relacoes: [] }] })
  }

  const marcaColuna = (c: ColunaBanco_, ligada: boolean) => {
    if (!detalhe) return
    trocaTabela(detalhe.chave, (t) => {
      const sem = t.colunas.filter((x) => x.coluna !== c.nome)
      if (!ligada) return { ...t, colunas: sem }
      const nova: ColunaExposta = { coluna: c.nome, campo: sugerirCampo(c.nome), filtravel: false, ordenavel: false }
      const ordem = (n: string) => detalhe.colunas.findIndex((x) => x.nome === n)
      return { ...t, colunas: [...sem, nova].sort((a, b) => ordem(a.coluna) - ordem(b.coluna)) }
    })
  }

  const salvar = async () => {
    if (!perfil) return
    setSalvando(true); setErro(null)
    try {
      setProblemas(await api.graphqlSalvar(perfil))
      setOriginal(assinatura(perfil)); setAviso('Perfil salvo.')
    } catch (e) { falha(e) } finally { setSalvando(false) }
  }

  const validar = async () => {
    if (!perfil) return
    try { setProblemas(await api.graphqlValidar(perfil)); setAviso('Validado contra o banco.') } catch (e) { falha(e) }
  }

  const visiveis = useMemo(() => banco.filter((t) => !soExpostas || exposta(t.chave)), [banco, soExpostas, perfil]) // eslint-disable-line react-hooks/exhaustive-deps
  const expostasN = perfil?.tabelas.length ?? 0
  const errosN = problemas.filter((p) => p.nivel === 'erro').length

  // Relações possíveis (chaves estrangeiras reais do banco), ainda não adicionadas.
  const sugestoes = useMemo(() => {
    if (!detalhe || !entrada) return []
    const jaTem = (alvo: string, pares: string) => entrada.relacoes.some((r) => r.tabelaAlvo.toLowerCase() === alvo.toLowerCase() && r.colunas.map((c) => `${c.origem}>${c.destino}`).join() === pares)
    const montar = (fks: TabelaBanco['foreignKeys'], card: RelacaoExposta['cardinalidade']) => fks.map((fk) => {
      const alvo = `${fk.schemaAlvo}.${fk.tabelaAlvo}`
      const pares = fk.colunas.map((c) => `${c.origem}>${c.destino}`).join()
      return { fk, alvo, card, pares, exposto: !!exposta(alvo), existe: jaTem(alvo, pares) }
    })
    return [...montar(detalhe.foreignKeys, 'paraUm'), ...montar(detalhe.referenciadas, 'paraMuitos')].filter((s) => !s.existe)
  }, [detalhe, entrada, perfil]) // eslint-disable-line react-hooks/exhaustive-deps

  const colunasFiltradas = (detalhe?.colunas ?? []).filter((c) => !filtroCol || c.nome.toLowerCase().includes(filtroCol.toLowerCase()))

  return (
    <div className={`app ${selecionada ? 'com-editor' : ''}`}>
      <aside className="lista">
        <header className="topo">
          <div className="marca"><span className="logo pequeno"><Icon nome="db" size={18} /></span><div><b>GraphQL</b><small>{info.sistema}</small></div></div>
          <div className="topo-a">
            <button className="icon-btn" title="Alternar tema" onClick={aoTrocarTema}><Icon nome={tema === 'escuro' ? 'sun' : 'moon'} /></button>
            <button className="icon-btn" title="Sair" onClick={aoSair}><Icon nome="logout" /></button>
          </div>
        </header>

        {comandosAtivos && (
          <div className="modo" role="tablist" aria-label="Seção">
            <button role="tab" aria-selected={false} onClick={aoTrocarModo}>Comandos</button>
            <button role="tab" aria-selected className="on">GraphQL</button>
          </div>
        )}

        <div className="busca">
          <Icon nome="search" size={16} />
          <input placeholder="Buscar tabela ou view…" value={busca} onChange={(e) => setBusca(e.target.value)} aria-label="Buscar tabela" />
        </div>

        <Toggle valor={soExpostas} onChange={setSoExpostas} rotulo={`Só as expostas (${expostasN})`} />

        <nav className="itens" aria-label="Tabelas do banco">
          {visiveis.length === 0 && <Vazio icone="db" titulo="Nenhuma tabela" texto={busca ? `Nada com “${busca}”.` : 'Nada para listar.'} />}
          {visiveis.map((t) => {
            const e = exposta(t.chave)
            return (
              <button key={t.chave} className={`item ${selecionada?.toLowerCase() === t.chave.toLowerCase() ? 'ativo' : ''}`} onClick={() => abrir(t.chave)}>
                <div className="item-l1">
                  <b className="mono">{t.schema === 'dbo' ? t.nome : t.chave}</b>
                  {e && <Chip tom="ok">{e.ativa ? 'exposta' : 'pausada'}</Chip>}
                  {t.ehView && <Chip>view</Chip>}
                </div>
                <div className="item-l3">
                  <Chip>{t.colunas} colunas</Chip>
                  {e && <Chip tom="marca">{e.nome} · {e.colunas.length} campos</Chip>}
                </div>
              </button>
            )
          })}
        </nav>
      </aside>

      <main className="editor">
        {!selecionada ? (
          <Vazio icone="db" titulo="Escolha uma tabela"
            texto="Selecione uma tabela ou view do banco para decidir o que o GraphQL expõe dela. Nada aparece no schema sem ser marcado aqui." />
        ) : (
          <>
            <header className="editor-h">
              <button className="icon-btn so-mobile" title="Voltar à lista" onClick={() => setSelecionada(null)}><Icon nome="back" /></button>
              <div className="editor-t">
                <h2 className="mono">{selecionada}</h2>
                <div className="chips-linha">
                  {entrada ? <Chip tom="ok">exposta como {entrada.nome}</Chip> : <Chip>não exposta</Chip>}
                  {sujo && <Chip tom="aviso">alterações não salvas</Chip>}
                  {errosN > 0 && <Chip tom="perigo">{errosN} erro(s) no perfil</Chip>}
                </div>
              </div>
              <div className="editor-a">
                <button className="btn btn-icone-mobile" onClick={validar} title="Validar contra o banco"><Icon nome="check" /><span>Validar</span></button>
                <button className="btn btn-marca" disabled={salvando || !sujo} onClick={salvar}><Icon nome="save" /><span>{salvando ? 'Salvando…' : 'Salvar perfil'}</span></button>
              </div>
            </header>

            <div className="conteudo">
              {erro && (
                <div className="banner banner-perigo"><Icon nome="alert" /><div><b>Atenção</b><p className="pre">{erro}</p></div>
                  <button className="icon-btn" onClick={() => setErro(null)} title="Fechar"><Icon nome="x" size={16} /></button></div>
              )}

              {problemas.length > 0 && (
                <div className={`banner ${errosN ? 'banner-perigo' : 'banner-aviso'}`}>
                  <Icon nome="alert" />
                  <div><b>{errosN ? 'O perfil tem erros (não salva até corrigir)' : 'Avisos do perfil'}</b>
                    <ul className="avisos">{problemas.map((p, i) => <li key={i}>{p.nivel === 'erro' ? '✕ ' : '⚠ '}{p.mensagem}</li>)}</ul></div>
                </div>
              )}

              {!detalhe && <div className="esqueleto" />}

              {detalhe && !entrada && (
                <section className="card">
                  <h3 className="card-t">Expor esta {detalhe.ehView ? 'view' : 'tabela'}</h3>
                  <p className="dica">
                    {detalhe.colunas.length} colunas, {detalhe.colunas.filter((c) => c.chave).length} de chave primária, {detalhe.foreignKeys.length} chave(s) estrangeira(s).
                    Ao expor, as colunas de tipos suportados que não parecem dados sensíveis já vêm marcadas; você ajusta em seguida.
                  </p>
                  <div className="acoes-linha"><button className="btn btn-marca" onClick={expor}><Icon nome="plus" /> Expor no GraphQL</button></div>
                </section>
              )}

              {detalhe && entrada && (
                <>
                  <section className="card">
                    <h3 className="card-t">Tipo e consulta</h3>
                    <div className="grid g3">
                      <Field rotulo="Nome do tipo" dica="Como aparece no schema."><input className="input mono" value={entrada.nome} maxLength={64}
                        onChange={(e) => trocaTabela(entrada.tabela, (t) => ({ ...t, nome: e.target.value.replace(/\W/g, '') }))} /></Field>
                      <Field rotulo="Nome da consulta" dica="Campo na raiz do Query."><input className="input mono" value={entrada.consulta} maxLength={64}
                        onChange={(e) => trocaTabela(entrada.tabela, (t) => ({ ...t, consulta: e.target.value.replace(/\W/g, '') }))} /></Field>
                      <Field rotulo="Tabela"><input className="input mono" value={entrada.tabela} disabled /></Field>
                    </div>
                    <Field rotulo="Descrição" dica="Aparece na introspecção: é o que o Claude lê para entender a tabela.">
                      <input className="input" value={entrada.descricao ?? ''} onChange={(e) => trocaTabela(entrada.tabela, (t) => ({ ...t, descricao: e.target.value }))} /></Field>
                    <div className="grid g2">
                      <Toggle valor={entrada.ativa} onChange={(v) => trocaTabela(entrada.tabela, (t) => ({ ...t, ativa: v }))} rotulo="Ativa" dica="Pausada, some do schema sem perder a configuração." />
                      <button className="btn btn-perigo-suave" onClick={() => setRemover(true)}><Icon nome="trash" /> Remover do perfil</button>
                    </div>
                    {!detalhe.ehView && detalhe.colunas.some((c) => c.chave) && (
                      <Toggle valor={!!entrada.atualizavel} onChange={(v) => trocaTabela(entrada.tabela, (t) => ({ ...t, atualizavel: v }))}
                        rotulo="Permitir atualização (mutation)"
                        dica={`Cria atualizar${entrada.nome}: só as colunas marcadas como editáveis, com filtro obrigatório, limite de linhas e simulação.`} />
                    )}
                    {entrada.atualizavel && !info.mutacoesAtivas && (
                      <div className="banner banner-aviso"><Icon nome="alert" /><div>
                        A mutation <b>não aparece no schema</b> até o servidor ter <code>Comandos:PermitirEscrita=true</code> e <code>Seguranca:ApiKeyEscrita</code> configurados
                        (a chave de escrita vai no cabeçalho <code>X-Write-Key</code>). O login do banco também precisa de permissão de UPDATE.
                      </div></div>
                    )}
                  </section>

                  <section className="card">
                    <header className="card-h">
                      <h3 className="card-t sem-margem">Colunas ({entrada.colunas.length} de {detalhe.colunas.length} expostas)</h3>
                      <div className="card-h-a">
                        <button className="mini" onClick={() => trocaTabela(entrada.tabela, (t) => ({ ...t, colunas: [] }))}>nenhuma</button>
                        <button className="mini" onClick={() => detalhe.colunas.filter((c) => c.tipo !== 'naoSuportado' && !c.sensivel).forEach((c) => !entrada.colunas.some((x) => x.coluna === c.nome) && marcaColuna(c, true))}>todas seguras</button>
                      </div>
                    </header>
                    <div className="busca"><Icon nome="search" size={16} /><input placeholder="Filtrar colunas…" value={filtroCol} onChange={(e) => setFiltroCol(e.target.value)} aria-label="Filtrar colunas" /></div>
                    <div className="cols">
                      {colunasFiltradas.map((c) => {
                        const e = entrada.colunas.find((x) => x.coluna === c.nome)
                        const nao = c.tipo === 'naoSuportado'
                        return (
                          <div key={c.nome} className={`col ${e ? 'on' : ''}`}>
                            <label className="col-chk">
                              <input type="checkbox" checked={!!e} disabled={nao} onChange={(ev) => marcaColuna(c, ev.target.checked)} aria-label={`Expor ${c.nome}`} />
                              <span className="mono col-nome">{c.nome}</span>
                            </label>
                            <div className="chips-linha col-chips">
                              <Chip>{c.tipoSql}</Chip>
                              {c.chave && <Chip tom="marca">chave</Chip>}
                              {c.sensivel && <Chip tom="perigo">sensível?</Chip>}
                              {nao && <Chip tom="aviso">tipo não suportado</Chip>}
                            </div>
                            {e && (
                              <div className="col-campos">
                                <input className="input mono" aria-label="Nome do campo" value={e.campo}
                                  onChange={(ev) => trocaTabela(entrada.tabela, (t) => ({ ...t, colunas: t.colunas.map((x) => (x.coluna === c.nome ? { ...x, campo: ev.target.value.replace(/\W/g, '') } : x)) }))} />
                                <label className="check"><input type="checkbox" checked={e.filtravel} onChange={(ev) => trocaTabela(entrada.tabela, (t) => ({ ...t, colunas: t.colunas.map((x) => (x.coluna === c.nome ? { ...x, filtravel: ev.target.checked } : x)) }))} /> filtrar</label>
                                {entrada.atualizavel && (
                                  <label className="check" title={c.chave || c.identidade ? 'Chave primária e identity não podem ser editadas' : 'Pode ser alterada pela mutation'}>
                                    <input type="checkbox" disabled={c.chave || c.identidade} checked={!!e.editavel && !c.chave && !c.identidade}
                                      onChange={(ev) => trocaTabela(entrada.tabela, (t) => ({ ...t, colunas: t.colunas.map((x) => (x.coluna === c.nome ? { ...x, editavel: ev.target.checked } : x)) }))} /> editar
                                  </label>
                                )}
                                <label className="check"><input type="checkbox" checked={e.ordenavel} onChange={(ev) => trocaTabela(entrada.tabela, (t) => ({ ...t, colunas: t.colunas.map((x) => (x.coluna === c.nome ? { ...x, ordenavel: ev.target.checked } : x)) }))} /> ordenar</label>
                              </div>
                            )}
                          </div>
                        )
                      })}
                    </div>
                  </section>

                  <section className="card">
                    <h3 className="card-t">Relações</h3>
                    <p className="dica">Vêm das chaves estrangeiras do banco. “Para muitos” é sempre paginada. A tabela do outro lado precisa estar exposta.</p>
                    {entrada.relacoes.map((r, i) => (
                      <div className="rel" key={i}>
                        <Chip tom="marca">{r.cardinalidade === 'paraUm' ? 'para um' : 'para muitos'}</Chip>
                        <input className="input mono" aria-label="Nome da relação" value={r.campo}
                          onChange={(ev) => trocaTabela(entrada.tabela, (t) => ({ ...t, relacoes: t.relacoes.map((x, k) => (k === i ? { ...x, campo: ev.target.value.replace(/\W/g, '') } : x)) }))} />
                        <span className="dica">→ {r.tabelaAlvo} ({r.colunas.map((c) => `${c.origem}=${c.destino}`).join(', ')})</span>
                        <button className="icon-btn perigo" title="Remover relação" onClick={() => trocaTabela(entrada.tabela, (t) => ({ ...t, relacoes: t.relacoes.filter((_, k) => k !== i) }))}><Icon nome="trash" /></button>
                      </div>
                    ))}
                    {sugestoes.length === 0 && entrada.relacoes.length === 0 && <p className="dica">Esta tabela não tem chaves estrangeiras no banco.</p>}
                    {sugestoes.map((s, i) => (
                      <div className="rel sugestao" key={i}>
                        <Chip>{s.card === 'paraUm' ? 'para um' : 'para muitos'}</Chip>
                        <span className="mono">{s.alvo}</span>
                        <span className="dica">({s.fk.colunas.map((c) => `${c.origem}=${c.destino}`).join(', ')})</span>
                        {s.exposto
                          ? <button className="mini" onClick={() => trocaTabela(entrada.tabela, (t) => ({ ...t, relacoes: [...t.relacoes, { campo: sugerirCampo(s.card === 'paraUm' ? s.fk.tabelaAlvo.replace(/^(tb|pr|ft|fi|ce|fw)(?=[A-Z])/, '') : sugerirConsulta(sugerirTipo(s.fk.tabelaAlvo))), cardinalidade: s.card, tabelaAlvo: s.alvo, colunas: s.fk.colunas }] }))}><Icon nome="plus" size={14} /> adicionar</button>
                          : <span className="dica">exponha {s.alvo} primeiro</span>}
                      </div>
                    ))}
                  </section>
                </>
              )}
            </div>
          </>
        )}
      </main>

      {aviso && <div className="toast" role="status"><Icon nome="check" size={16} /> {aviso}</div>}
      {remover && entrada && (
        <Confirmar titulo={`Remover ${entrada.nome} do perfil?`} texto="A tabela deixa de existir no schema. As colunas e relações configuradas aqui serão perdidas ao salvar." confirmar="Remover" perigo
          onNao={() => setRemover(false)}
          onSim={() => { setRemover(false); setPerfil((p) => p && { ...p, tabelas: p.tabelas.filter((t) => t.tabela.toLowerCase() !== entrada.tabela.toLowerCase()) }) }} />
      )}
    </div>
  )
}

type ColunaBanco_ = TabelaBanco['colunas'][number]

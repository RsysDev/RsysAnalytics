import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { ApiError, api, chaveSalva, esquecerChave, guardarChave } from './api'
import { Icon } from './icons'
import { Graphql } from './Graphql'
import { Importar } from './Importar'
import { Geral } from './tabs/Geral'
import { Parametros } from './tabs/Parametros'
import { Passos } from './tabs/Passos'
import { Regras } from './tabs/Regras'
import { Testar, type ValoresTeste } from './tabs/Testar'
import { novoComando, type Comando, type Info, type TabelaLegado } from './types'
import { Chip, Confirmar, Vazio } from './ui'

type Aba = 'geral' | 'parametros' | 'regras' | 'sql' | 'testar'

/** Garante campos preenchidos e passos em ordem 1..n (a ordem gravada pode ter empates ou saltos). */
function normalizar(c: Comando): Comando {
  const passos = [...c.passos].sort((a, b) => a.ordem - b.ordem).map((p, i) => ({ ...p, ordem: i + 1, nome: p.nome ?? '' }))
  return {
    ...c,
    erro: c.erro ?? null,
    parametros: c.parametros.map((p) => ({ ...p, campo: p.campo ?? '', descricao: p.descricao ?? '', padrao: p.padrao ?? '', padrao2: p.padrao2 ?? '' })),
    regras: c.regras.map((r) => ({ ...r, senao: r.senao ?? '', casos: r.casos.map((k) => ({ ...k, se: k.se.map((s) => ({ ...s, valor: s.valor ?? '' })) })) })),
    passos: passos.length ? passos : novoComando().passos,
  }
}

const assinatura = (c: Comando | null) => (c ? JSON.stringify(c) : '')

function Login({ aoEntrar }: { aoEntrar: (info: Info) => void }) {
  const [chave, setChave] = useState('')
  const [lembrar, setLembrar] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const [carregando, setCarregando] = useState(false)

  const entrar = async (e: FormEvent) => {
    e.preventDefault()
    setCarregando(true)
    setErro(null)
    guardarChave(chave.trim(), lembrar)
    try {
      aoEntrar(await api.info())
    } catch (err) {
      esquecerChave()
      setErro(err instanceof ApiError ? err.message : 'Não foi possível falar com o servidor.')
    } finally {
      setCarregando(false)
    }
  }

  return (
    <main className="login">
      <form className="login-card" onSubmit={entrar}>
        <div className="logo"><Icon nome="db" size={26} /></div>
        <h1>Comandos SQL</h1>
        <p>Catálogo de consultas que o Claude executa pelo MCP. Entre com a chave de administração.</p>
        <label className="field">
          <span className="field-l">Chave de administração</span>
          <input className="input" type="password" autoFocus autoComplete="current-password" value={chave} onChange={(e) => setChave(e.target.value)} />
        </label>
        <label className="check"><input type="checkbox" checked={lembrar} onChange={(e) => setLembrar(e.target.checked)} /> Lembrar neste aparelho</label>
        {erro && <div className="banner banner-perigo"><Icon nome="alert" /><div>{erro}</div></div>}
        <button className="btn btn-marca btn-grande" disabled={!chave.trim() || carregando}>{carregando ? 'Entrando…' : 'Entrar'}</button>
      </form>
    </main>
  )
}

export default function App() {
  const [info, setInfo] = useState<Info | null>(null)
  const [tentando, setTentando] = useState(() => !!chaveSalva())
  const [comandos, setComandos] = useState<Comando[]>([])
  const [carregandoLista, setCarregandoLista] = useState(false)
  const [busca, setBusca] = useState('')
  const [rascunho, setRascunho] = useState<Comando | null>(null)
  const [original, setOriginal] = useState('')
  const [novo, setNovo] = useState(false)
  const [aba, setAba] = useState<Aba>('geral')
  const [modo, setModo] = useState<'comandos' | 'graphql'>('comandos')
  const [legado, setLegado] = useState<TabelaLegado[]>([])
  const [importando, setImportando] = useState(false)
  const [avisos, setAvisos] = useState<string[]>([])
  const [valores, setValores] = useState<ValoresTeste>({})
  const [salvando, setSalvando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const [aviso, setAviso] = useState<string | null>(null)
  const [pendente, setPendente] = useState<null | { titulo: string; texto: string; confirmar: string; perigo?: boolean; acao: () => void }>(null)
  const [tema, setTema] = useState<'claro' | 'escuro'>(() =>
    (localStorage.getItem('tema') as 'claro' | 'escuro' | null) ?? (matchMedia('(prefers-color-scheme: dark)').matches ? 'escuro' : 'claro'))

  useEffect(() => {
    document.documentElement.dataset.theme = tema
    localStorage.setItem('tema', tema)
  }, [tema])

  // O erro aparece no topo do conteúdo: leva a tela até lá (no celular é a página que rola).
  useEffect(() => {
    if (!erro) return
    document.querySelector('.conteudo')?.scrollTo({ top: 0, behavior: 'smooth' })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }, [erro])

  useEffect(() => {
    if (!aviso) return
    const t = setTimeout(() => setAviso(null), 3500)
    return () => clearTimeout(t)
  }, [aviso])

  // No celular a página inteira rola e o cabeçalho + abas ficam fixos: a barra de navegação das listas precisa
  // parar logo abaixo deles. No desktop o conteúdo tem a própria rolagem e o topo é 0.
  useEffect(() => {
    if (!rascunho) return
    const ajusta = () => {
      const h = document.querySelector<HTMLElement>('.editor-h'); const t = document.querySelector<HTMLElement>('.abas')
      const mobile = matchMedia('(max-width: 900px)').matches
      document.documentElement.style.setProperty('--nav-top', mobile && h && t ? `${h.offsetHeight + t.offsetHeight}px` : '0px')
    }
    ajusta()
    window.addEventListener('resize', ajusta)
    return () => window.removeEventListener('resize', ajusta)
  }, [rascunho !== null])

  const sair = useCallback(() => {
    esquecerChave()
    setInfo(null)
    setRascunho(null)
    setComandos([])
  }, [])

  const falha = useCallback((e: unknown) => {
    if (e instanceof ApiError && e.status === 401) { sair(); return }
    setErro(e instanceof ApiError ? e.message : 'Não foi possível falar com o servidor.')
  }, [sair])

  const carregar = useCallback(async () => {
    setCarregandoLista(true)
    try {
      setComandos((await api.listar()).map(normalizar))
    } catch (e) {
      falha(e)
    } finally {
      setCarregandoLista(false)
    }
  }, [falha])

  // Chave já guardada: tenta entrar sozinho.
  useEffect(() => {
    if (!tentando) return
    api.info().then(setInfo).catch(() => esquecerChave()).finally(() => setTentando(false))
  }, [tentando])

  useEffect(() => { if (info) void carregar() }, [info, carregar])

  // A importação só é oferecida se o banco tiver as tabelas do ERP legado (a API devolve lista vazia senão).
  useEffect(() => {
    if (!info) { setLegado([]); return }
    api.legado().then(setLegado).catch(() => setLegado([]))
  }, [info])

  const sujo = rascunho !== null && assinatura(rascunho) !== original
  const protegido = (acao: () => void) => {
    if (!sujo) return acao()
    setPendente({ titulo: 'Descartar alterações?', texto: 'Há alterações não salvas neste comando.', confirmar: 'Descartar', perigo: true, acao })
  }

  const abrir = (c: Comando, ehNovo = false, aoAbrir: Aba = 'geral', avisosDaImportacao: string[] = []) => {
    const n = normalizar(c)
    setRascunho(n)
    setOriginal(ehNovo ? '' : assinatura(n))
    setNovo(ehNovo)
    setAba(aoAbrir)
    setValores({})
    setErro(null)
    setAvisos(avisosDaImportacao)
  }

  const fechar = () => { setRascunho(null); setOriginal(''); setNovo(false); setErro(null); setAvisos([]) }

  const salvar = async () => {
    if (!rascunho) return
    setSalvando(true)
    setErro(null)
    try {
      const gravado = { ...rascunho, erro: null }
      if (novo) await api.criar(gravado)
      else await api.atualizar(gravado)
      const lista = (await api.listar()).map(normalizar)
      setComandos(lista)
      const volta = lista.find((x) => x.id === rascunho.id) ?? normalizar(gravado)
      setRascunho(volta)
      setOriginal(assinatura(volta))
      setNovo(false)
      setAvisos([])
      setAviso('Comando salvo.')
    } catch (e) {
      falha(e)
    } finally {
      setSalvando(false)
    }
  }

  const excluir = () => {
    if (!rascunho) return
    const id = rascunho.id
    setPendente({
      titulo: `Excluir “${id}”?`, texto: 'O comando some do catálogo e o Claude deixa de conseguir executá-lo. Não dá para desfazer.', confirmar: 'Excluir', perigo: true,
      acao: async () => {
        try {
          await api.excluir(id)
          fechar()
          await carregar()
          setAviso('Comando excluído.')
        } catch (e) { falha(e) }
      },
    })
  }

  const duplicar = () => {
    if (!rascunho) return
    protegido(() => abrir({ ...rascunho, id: `${rascunho.id}_copia`, ativa: false, erro: null }, true))
  }

  const visiveis = useMemo(() => {
    const q = busca.trim().toLowerCase()
    return q ? comandos.filter((c) => c.id.toLowerCase().includes(q) || c.descricao.toLowerCase().includes(q)) : comandos
  }, [comandos, busca])

  if (tentando) return <div className="carregando"><div className="spinner" /></div>
  if (!info) return <Login aoEntrar={setInfo} />
  if (modo === 'graphql' && info.sistema) {
    return <Graphql info={info} comandosAtivos aoTrocarModo={() => setModo('comandos')} aoSair={() => { setModo('comandos'); sair() }}
      tema={tema} aoTrocarTema={() => setTema(tema === 'escuro' ? 'claro' : 'escuro')} />
  }

  const abas: { id: Aba; rotulo: string; icone: string; n?: number }[] = rascunho ? [
    { id: 'geral', rotulo: 'Geral', icone: 'gear' },
    { id: 'parametros', rotulo: 'Parâmetros', icone: 'sliders', n: rascunho.parametros.length },
    { id: 'regras', rotulo: 'Regras', icone: 'branch', n: rascunho.regras.length },
    { id: 'sql', rotulo: 'SQL', icone: 'code', n: rascunho.passos.length },
    { id: 'testar', rotulo: 'Testar', icone: 'flask' },
  ] : []

  return (
    <div className={`app ${rascunho ? 'com-editor' : ''}`}>
      <aside className="lista">
        <header className="topo">
          <div className="marca"><span className="logo pequeno"><Icon nome="db" size={18} /></span><div><b>Comandos SQL</b><small>{info.chaveEmpresa || 'sem empresa'}</small></div></div>
          <div className="topo-a">
            <button className="icon-btn" title="Alternar tema" onClick={() => setTema(tema === 'escuro' ? 'claro' : 'escuro')}><Icon nome={tema === 'escuro' ? 'sun' : 'moon'} /></button>
            <button className="icon-btn" title="Sair" onClick={sair}><Icon nome="logout" /></button>
          </div>
        </header>

        {info.sistema && (
          <div className="modo" role="tablist" aria-label="Seção">
            <button role="tab" aria-selected className="on">Comandos</button>
            <button role="tab" aria-selected={false} onClick={() => protegido(() => { fechar(); setModo('graphql') })}>GraphQL</button>
          </div>
        )}

        {!info.chaveEmpresa && (
          <div className="banner banner-aviso"><Icon nome="alert" /><div>Defina <code>Comandos:ChaveEmpresa</code> no servidor para listar e gravar comandos.</div></div>
        )}

        <div className="busca">
          <Icon nome="search" size={16} />
          <input placeholder="Buscar comando…" value={busca} onChange={(e) => setBusca(e.target.value)} aria-label="Buscar comando" />
        </div>

        <button className="btn btn-marca btn-bloco" onClick={() => protegido(() => abrir(novoComando(), true))}><Icon nome="plus" /> Novo comando</button>
        {legado.length > 0 && (
          <button className="btn btn-bloco" onClick={() => protegido(() => setImportando(true))}><Icon nome="download" /> Importar do ERP legado</button>
        )}

        <nav className="itens" aria-label="Comandos">
          {carregandoLista && comandos.length === 0 && <div className="esqueleto" />}
          {visiveis.map((c) => (
            <button key={c.id} className={`item ${rascunho && !novo && rascunho.id === c.id ? 'ativo' : ''}`} onClick={() => protegido(() => abrir(c))}>
              <div className="item-l1">
                <b className="mono">{c.id}</b>
                {!c.ativa && <Chip tom="neutro">inativo</Chip>}
                {c.erro && <Chip tom="perigo">JSON inválido</Chip>}
              </div>
              <p>{c.descricao || 'Sem descrição'}</p>
              <div className="item-l3">
                <Chip>{c.passos.length} {c.passos.length === 1 ? 'passo' : 'passos'}</Chip>
                <Chip>{c.parametros.filter((p) => p.origem === 'chamador').length} param.</Chip>
                {c.regras.length > 0 && <Chip>{c.regras.length} regras</Chip>}
                {c.passos.some((p) => p.somenteExecucao) && <Chip tom="aviso">altera dados</Chip>}
              </div>
            </button>
          ))}
          {!carregandoLista && comandos.length > 0 && visiveis.length === 0 && <p className="dica centro">Nenhum comando com “{busca}”.</p>}
          {!carregandoLista && comandos.length === 0 && info.chaveEmpresa && (
            <Vazio icone="db" titulo="Catálogo vazio" texto="Crie o primeiro comando com o botão acima." />
          )}
        </nav>
      </aside>

      <main className="editor">
        {!rascunho ? (
          <Vazio icone="code" titulo="Escolha um comando" texto="Selecione um comando na lista ou crie um novo para editar parâmetros, regras e SQL, e testar tudo aqui mesmo." />
        ) : (
          <>
            <header className="editor-h">
              <button className="icon-btn so-mobile" title="Voltar à lista" onClick={() => protegido(fechar)}><Icon nome="back" /></button>
              <div className="editor-t">
                <h2 className="mono">{rascunho.id || 'Novo comando'}</h2>
                <div className="chips-linha">
                  {novo && <Chip tom="marca">novo</Chip>}
                  {sujo && !novo && <Chip tom="aviso">alterações não salvas</Chip>}
                  {!rascunho.ativa && <Chip>inativo</Chip>}
                </div>
              </div>
              <div className="editor-a">
                {!novo && <button className="btn btn-icone-mobile" onClick={duplicar} title="Duplicar"><Icon nome="copy" /><span>Duplicar</span></button>}
                {!novo && <button className="btn btn-perigo-suave btn-icone-mobile" onClick={excluir} title="Excluir"><Icon nome="trash" /><span>Excluir</span></button>}
                <button className="btn btn-marca" disabled={salvando || (!novo && !sujo)} onClick={salvar}><Icon nome="save" /><span>{salvando ? 'Salvando…' : 'Salvar'}</span></button>
              </div>
            </header>

            <div className="abas" role="tablist">
              {abas.map((a) => (
                <button key={a.id} role="tab" aria-selected={aba === a.id} className={aba === a.id ? 'on' : ''} onClick={() => setAba(a.id)}>
                  <Icon nome={a.icone} size={16} /> {a.rotulo}{a.n !== undefined && a.n > 0 && <small>{a.n}</small>}
                </button>
              ))}
            </div>

            <div className="conteudo">
              {erro && (
                <div className="banner banner-perigo">
                  <Icon nome="alert" /><div><b>Não foi possível salvar</b><p className="pre">{erro}</p></div>
                  <button className="icon-btn" onClick={() => setErro(null)} title="Fechar"><Icon nome="x" size={16} /></button>
                </div>
              )}
              {avisos.length > 0 && (
                <div className="banner banner-aviso">
                  <Icon nome="alert" />
                  <div>
                    <b>Importado do ERP legado: revise antes de salvar</b>
                    <ul className="avisos">{avisos.map((a, i) => <li key={i}>{a}</li>)}</ul>
                  </div>
                  <button className="icon-btn" onClick={() => setAvisos([])} title="Dispensar avisos"><Icon nome="x" size={16} /></button>
                </div>
              )}
              {aba === 'geral' && <Geral c={rascunho} set={setRascunho} novo={novo} chaveEmpresa={info.chaveEmpresa} />}
              {aba === 'parametros' && <Parametros c={rascunho} set={setRascunho} />}
              {aba === 'regras' && <Regras c={rascunho} set={setRascunho} />}
              {aba === 'sql' && <Passos c={rascunho} set={setRascunho} />}
              {aba === 'testar' && <Testar c={rascunho} info={info} valores={valores} setValores={setValores} />}
            </div>
          </>
        )}
      </main>

      {importando && (
        <Importar
          tabelas={legado}
          aoFechar={() => setImportando(false)}
          aoEscolher={(comando, lista) => { setImportando(false); abrir(comando, true, 'geral', lista) }}
        />
      )}
      {aviso && <div className="toast" role="status"><Icon nome="check" size={16} /> {aviso}</div>}
      {pendente && (
        <Confirmar {...pendente} onNao={() => setPendente(null)} onSim={() => { const a = pendente.acao; setPendente(null); a() }} />
      )}
    </div>
  )
}

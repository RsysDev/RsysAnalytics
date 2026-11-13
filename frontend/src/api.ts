import type { Comando, ConversaoLegado, Info, PerfilGraphQL, ProblemaPerfil, RelatorioLegadoItem, TabelaBanco, TabelaBancoResumo, TabelaLegado, Teste } from './types'

const BASE = import.meta.env.DEV ? '/api' : './api'
const CHAVE = 'erpanalytics.adminKey'

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

export const chaveSalva = () => sessionStorage.getItem(CHAVE) ?? localStorage.getItem(CHAVE) ?? ''

export function guardarChave(chave: string, lembrar: boolean) {
  sessionStorage.setItem(CHAVE, chave)
  if (lembrar) localStorage.setItem(CHAVE, chave)
  else localStorage.removeItem(CHAVE)
}

export function esquecerChave() {
  sessionStorage.removeItem(CHAVE)
  localStorage.removeItem(CHAVE)
}

async function chamar<T>(metodo: string, caminho: string, corpo?: unknown): Promise<T> {
  const r = await fetch(`${BASE}${caminho}`, {
    method: metodo,
    headers: { 'X-Admin-Key': chaveSalva(), ...(corpo !== undefined ? { 'Content-Type': 'application/json' } : {}) },
    body: corpo !== undefined ? JSON.stringify(corpo) : undefined,
  })
  if (r.status === 204) return undefined as T
  const texto = await r.text()
  let json: unknown = null
  try { json = texto ? JSON.parse(texto) : null } catch { /* resposta que não é JSON */ }
  if (!r.ok) {
    const msg = (json as { erro?: string } | null)?.erro
      ?? (r.status === 404 ? 'Administração desligada no servidor (defina Seguranca:AdminKey).' : `Erro ${r.status}`)
    throw new ApiError(r.status, msg)
  }
  return json as T
}

export const api = {
  info: () => chamar<Info>('GET', '/info'),
  listar: () => chamar<Comando[]>('GET', '/comandos'),
  criar: (c: Comando) => chamar<Comando>('POST', '/comandos', c),
  atualizar: (c: Comando) => chamar<Comando>('PUT', `/comandos/${encodeURIComponent(c.id)}`, c),
  excluir: (id: string) => chamar<void>('DELETE', `/comandos/${encodeURIComponent(id)}`),
  // Importação do ERP legado: a lista de tabelas vem vazia quando o banco não tem as tabelas esperadas.
  legado: () => chamar<TabelaLegado[]>('GET', '/legado'),
  legadoLista: (tabela: string) => chamar<RelatorioLegadoItem[]>('GET', `/legado/${encodeURIComponent(tabela)}`),
  converterLegado: (tabela: string, idRelatorio: string) =>
    chamar<ConversaoLegado>('POST', `/legado/${encodeURIComponent(tabela)}/converter`, { idRelatorio }),
  // Perfil do GraphQL: tabelas e colunas do banco, e o que está exposto.
  graphqlTabelas: (busca: string) => chamar<TabelaBancoResumo[]>('GET', `/graphql/banco/tabelas?busca=${encodeURIComponent(busca)}`),
  graphqlTabela: (chave: string) => chamar<TabelaBanco>('GET', `/graphql/banco/tabela?chave=${encodeURIComponent(chave)}`),
  graphqlPerfil: () => chamar<{ sistema: string; perfil: PerfilGraphQL; problemas: ProblemaPerfil[] }>('GET', '/graphql/perfil'),
  graphqlValidar: (p: PerfilGraphQL) => chamar<ProblemaPerfil[]>('POST', '/graphql/perfil/validar', p),
  graphqlSalvar: (p: PerfilGraphQL) => chamar<ProblemaPerfil[]>('PUT', '/graphql/perfil', p),
  testar: (req: { comando: Comando; parametros: Record<string, unknown>; resultado?: string; soPrevia: boolean }) =>
    chamar<Teste>('POST', '/testar', req),
}

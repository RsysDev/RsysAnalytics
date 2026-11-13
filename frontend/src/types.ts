// Espelha o JSON de /admin/api (enums como texto camelCase).

export type TipoParametro = 'string' | 'inteiro' | 'decimal' | 'date' | 'dateTime' | 'checkbox'
export type OperadorComparacao =
  | 'igual' | 'diferente' | 'maior' | 'maiorIgual' | 'menor' | 'menorIgual' | 'between' | 'like' | 'notLike'
export type OperadorCondicao =
  | 'preenchido' | 'vazio' | 'igual' | 'diferente' | 'maior' | 'maiorIgual' | 'menor' | 'menorIgual'
export type OrigemParametro = 'chamador' | 'servidor'

export interface Opcao { valor: string; rotulo: string }

export interface Parametro {
  id: string
  campo?: string | null
  descricao?: string | null
  tipo: TipoParametro
  valorUnico: boolean
  obrigatorio: boolean
  padrao?: string | null
  padrao2?: string | null
  condicaoFixa?: OperadorComparacao | null
  origem: OrigemParametro
  chave?: string | null
  /** Lista fechada: vira combobox na aba Testar. Valor vazio = "sem valor". */
  opcoes?: Opcao[] | null
}

export interface Condicao { param: string; op: OperadorCondicao; valor?: string | null }
export interface Caso { se: Condicao[]; sql: string }
export interface Regra { id: string; casos: Caso[]; senao?: string | null }
export interface Passo { ordem: number; nome?: string | null; sql: string; somenteExecucao: boolean }

export interface Comando {
  id: string
  descricao: string
  ativa: boolean
  maxLinhas: number
  timeoutSeg: number
  parametros: Parametro[]
  regras: Regra[]
  passos: Passo[]
  erro?: string | null
}

export interface Info {
  chaveEmpresa: string
  mutacoesAtivas?: boolean
  sistema?: string
  permitirEscrita: boolean
  chavesServidor: string[]
}

export interface ResultadoConsulta { nome: string; colunas: string[]; linhas: unknown[][]; truncado: boolean }
export interface ParametroPrevia { nome: string; tipo: string; valor: string | null }
export interface PassoPrevia { nome: string | null; somenteExecucao: boolean; sql: string; parametros: ParametroPrevia[] }
export interface Teste {
  passos: PassoPrevia[]
  resultado: { resultados: ResultadoConsulta[] } | null
  milissegundos: number
}

export const TIPOS: { valor: TipoParametro; rotulo: string }[] = [
  { valor: 'string', rotulo: 'Texto' },
  { valor: 'inteiro', rotulo: 'Inteiro' },
  { valor: 'decimal', rotulo: 'Decimal' },
  { valor: 'date', rotulo: 'Data' },
  { valor: 'dateTime', rotulo: 'Data e hora' },
  { valor: 'checkbox', rotulo: 'Sim / Não' },
]

export const OPERADORES: { valor: OperadorComparacao; rotulo: string }[] = [
  { valor: 'igual', rotulo: 'igual a' },
  { valor: 'diferente', rotulo: 'diferente de' },
  { valor: 'maior', rotulo: 'maior que' },
  { valor: 'maiorIgual', rotulo: 'maior ou igual' },
  { valor: 'menor', rotulo: 'menor que' },
  { valor: 'menorIgual', rotulo: 'menor ou igual' },
  { valor: 'between', rotulo: 'entre' },
  { valor: 'like', rotulo: 'contém (like)' },
  { valor: 'notLike', rotulo: 'não contém' },
]

export const OPERADORES_CONDICAO: { valor: OperadorCondicao; rotulo: string }[] = [
  { valor: 'preenchido', rotulo: 'está preenchido' },
  { valor: 'vazio', rotulo: 'está vazio' },
  { valor: 'igual', rotulo: '=' },
  { valor: 'diferente', rotulo: '≠' },
  { valor: 'maior', rotulo: '>' },
  { valor: 'maiorIgual', rotulo: '≥' },
  { valor: 'menor', rotulo: '<' },
  { valor: 'menorIgual', rotulo: '≤' },
]

export const novoParametro = (): Parametro => ({
  id: '', campo: '', descricao: '', tipo: 'string', valorUnico: false, obrigatorio: false,
  padrao: '', padrao2: '', condicaoFixa: null, origem: 'chamador', chave: null,
})

export const novoComando = (): Comando => ({
  id: '', descricao: '', ativa: true, maxLinhas: 1000, timeoutSeg: 30,
  parametros: [], regras: [],
  passos: [{ ordem: 1, nome: '', sql: 'select 1 as exemplo', somenteExecucao: false }],
})

export interface TabelaLegado { nome: string; total: number }
export interface RelatorioLegadoItem { idRelatorio: string; titulo: string; tag: string | null; idComando: string; existe: boolean }
export interface ConversaoLegado { comando: Comando; avisos: string[] }

// ---- Perfil do GraphQL (tabelas e colunas expostas) ----
export interface ColunaExposta { coluna: string; campo: string; descricao?: string | null; filtravel: boolean; ordenavel: boolean; editavel?: boolean }
export interface ParColunas { origem: string; destino: string }
export interface RelacaoExposta { campo: string; descricao?: string | null; cardinalidade: 'paraUm' | 'paraMuitos'; tabelaAlvo: string; colunas: ParColunas[] }
export interface TabelaExposta { tabela: string; nome: string; consulta: string; descricao?: string | null; ativa: boolean; atualizavel?: boolean; colunas: ColunaExposta[]; relacoes: RelacaoExposta[] }
export interface PerfilGraphQL { sistema: string; tabelas: TabelaExposta[] }
export interface ProblemaPerfil { nivel: 'erro' | 'aviso'; mensagem: string; tabela?: string | null; coluna?: string | null }

export interface TabelaBancoResumo { schema: string; nome: string; ehView: boolean; colunas: number; chave: string }
export interface ColunaBanco { nome: string; tipoSql: string; tipo: string; nulavel: boolean; chave: boolean; identidade: boolean; sensivel: boolean }
export interface ForeignKeyBanco { nome: string; schemaAlvo: string; tabelaAlvo: string; colunas: ParColunas[] }
export interface TabelaBanco { schema: string; nome: string; ehView: boolean; chave: string; colunas: ColunaBanco[]; foreignKeys: ForeignKeyBanco[]; referenciadas: ForeignKeyBanco[] }

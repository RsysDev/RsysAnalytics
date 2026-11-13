import { Icon } from '../icons'
import type { Comando } from '../types'
import { Field, Toggle } from '../ui'

export function Geral({ c, set, novo, chaveEmpresa }: { c: Comando; set: (c: Comando) => void; novo: boolean; chaveEmpresa: string }) {
  return (
    <div className="stack">
      {c.erro && (
        <div className="banner banner-perigo">
          <Icon nome="alert" />
          <div>
            <b>O JSON gravado no banco não pôde ser lido.</b>
            <p>{c.erro}</p>
            <p>Ao salvar, os parâmetros e regras abaixo substituem o que está gravado.</p>
          </div>
        </div>
      )}

      <section className="card">
        <h3 className="card-t">Identificação</h3>
        <div className="grid g2">
          <Field rotulo="Id do comando" dica={novo ? 'É o nome que o Claude usa em executar_comando. Não pode mudar depois: para renomear, duplique.' : 'Não pode ser alterado. Para renomear, use Duplicar e exclua o antigo.'}>
            <input className="input mono" value={c.id} disabled={!novo} placeholder="vendas_por_cliente" maxLength={100}
              onChange={(e) => set({ ...c, id: e.target.value.replace(/[^A-Za-z0-9_.-]/g, '') })} />
          </Field>
          <Field rotulo="Empresa (chave)" dica="Definida no servidor (Comandos:ChaveEmpresa); ninguém escolhe aqui.">
            <input className="input mono" value={chaveEmpresa} disabled />
          </Field>
        </div>
        <Field rotulo="Descrição" dica="O Claude lê este texto para decidir quando usar o comando: diga o que devolve e como filtrar.">
          <textarea className="input" rows={3} maxLength={1000} value={c.descricao} onChange={(e) => set({ ...c, descricao: e.target.value })} />
        </Field>
        <Toggle valor={c.ativa} onChange={(v) => set({ ...c, ativa: v })} rotulo="Ativo" dica="Desativado, o comando deixa de aparecer e de executar no MCP." />
      </section>

      <section className="card">
        <h3 className="card-t">Limites</h3>
        <div className="grid g2">
          <Field rotulo="Máximo de linhas por resultado" dica="De 1 a 5000. Acima disso o resultado vem marcado como truncado.">
            <input className="input" type="number" min={1} max={5000} inputMode="numeric" value={c.maxLinhas}
              onChange={(e) => set({ ...c, maxLinhas: Number(e.target.value) })} />
          </Field>
          <Field rotulo="Tempo limite (segundos)" dica="De 1 a 120, por passo.">
            <input className="input" type="number" min={1} max={120} inputMode="numeric" value={c.timeoutSeg}
              onChange={(e) => set({ ...c, timeoutSeg: Number(e.target.value) })} />
          </Field>
        </div>
      </section>
    </div>
  )
}

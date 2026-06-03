import type { ArquivoBuscaResultado } from '../types'

interface Props {
  itens: ArquivoBuscaResultado[]
  termo: string
}

function formatarData(data?: string | null): string {
  if (!data) return '-'
  const d = new Date(data)
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleDateString('pt-BR')
}

export function ListaResultados({ itens, termo }: Props) {
  if (termo.trim().length < 2) {
    return <p className="dica">Digite ao menos 2 caracteres para buscar.</p>
  }

  if (itens.length === 0) {
    return <p className="dica">Nenhum resultado para "{termo}".</p>
  }

  return (
    <ul className="resultados">
      {itens.map((item) => (
        <li key={`${item.tipo}-${item.externalKey}`} className="resultado">
          <span className="icone" aria-hidden>
            {item.tipo === 'Pasta' ? '📁' : '📄'}
          </span>
          <div className="resultado-corpo">
            <div className="resultado-linha1">
              <span className="resultado-nome">{item.nome}</span>
              <span className={`badge badge-${item.tipo.toLowerCase()}`}>{item.tipo}</span>
              {item.statusEnvio && <span className="badge badge-status">{item.statusEnvio}</span>}
            </div>
            <div className="resultado-caminho" title={item.caminho}>
              {item.caminho}
            </div>
            {item.tipo === 'Envio' && (
              <div className="resultado-meta">
                {item.nomeDestinatarios && <span>👥 {item.nomeDestinatarios}</span>}
                <span>Criado: {formatarData(item.dataCriacao)}</span>
                {item.dataExpiracao && <span>Expira: {formatarData(item.dataExpiracao)}</span>}
              </div>
            )}
          </div>
        </li>
      ))}
    </ul>
  )
}

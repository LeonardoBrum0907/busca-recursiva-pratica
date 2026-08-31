import type { ItemPasta } from '../types'

interface Props {
  itens: ItemPasta[]
  termo: string
  onPastaClick?: (pasta: ItemPasta) => void
  onEnvioClick?: (envio: ItemPasta) => void
}

function formatarData(data?: string | null): string {
  if (!data) return '-'
  const d = new Date(data)
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleDateString('pt-BR')
}

export function ListaResultados({ itens, termo, onPastaClick, onEnvioClick }: Props) {
  const termoLimpo = termo.trim()

  if (itens.length === 0) {
    return (
      <p className="dica">
        {termoLimpo.length === 0
          ? 'Esta pasta esta vazia.'
          : `Nenhum item deste nivel corresponde a "${termo}".`}
      </p>
    )
  }

  return (
    <ul className="resultados">
      {itens.map((item) => {
        const ehPastaClicavel = item.tipo === 'Pasta' && onPastaClick
        const ehEnvioClicavel = item.tipo === 'Envio' && onEnvioClick
        const ehClicavel = ehPastaClicavel || ehEnvioClicavel

        return (
          <li
            key={`${item.tipo}-${item.externalKey}`}
            className={`resultado${ehClicavel ? ' resultado-clicavel' : ''}`}
          >
            <span className="icone" aria-hidden>
              {item.tipo === 'Pasta' ? '📁' : '📄'}
            </span>
            <div className="resultado-corpo">
              {ehPastaClicavel ? (
                <button
                  type="button"
                  className="resultado-item-btn"
                  onClick={() => onPastaClick(item)}
                >
                  <div className="resultado-linha1">
                    <span className="resultado-nome">{item.nome}</span>
                    <span className={`badge badge-${item.tipo.toLowerCase()}`}>{item.tipo}</span>
                  </div>
                </button>
              ) : ehEnvioClicavel ? (
                <button
                  type="button"
                  className="resultado-item-btn"
                  onClick={() => onEnvioClick(item)}
                >
                  <div className="resultado-linha1">
                    <span className="resultado-nome">{item.nome}</span>
                    <span className={`badge badge-${item.tipo.toLowerCase()}`}>{item.tipo}</span>
                    {item.statusEnvio && (
                      <span className="badge badge-status">{item.statusEnvio}</span>
                    )}
                  </div>
                </button>
              ) : (
                <div className="resultado-linha1">
                  <span className="resultado-nome">{item.nome}</span>
                  <span className={`badge badge-${item.tipo.toLowerCase()}`}>{item.tipo}</span>
                  {item.statusEnvio && (
                    <span className="badge badge-status">{item.statusEnvio}</span>
                  )}
                </div>
              )}
              {item.tipo === 'Envio' && (
                <div className="resultado-meta">
                  {item.nomeDestinatarios && <span>👥 {item.nomeDestinatarios}</span>}
                  <span>Criado: {formatarData(item.dataCriacao)}</span>
                  {item.dataExpiracao && <span>Expira: {formatarData(item.dataExpiracao)}</span>}
                  {ehEnvioClicavel && <span className="resultado-dica-clique">Clique para visualizar</span>}
                </div>
              )}
            </div>
          </li>
        )
      })}
    </ul>
  )
}

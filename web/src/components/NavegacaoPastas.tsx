import type { PastaNavegacao } from '../types'

interface Props {
  pilha: PastaNavegacao[]
  onNavegar: (indice: number) => void
}

export function NavegacaoPastas({ pilha, onNavegar }: Props) {
  if (pilha.length === 0) return null

  return (
    <nav className="navegacao-pastas" aria-label="Localizacao atual">
      <button type="button" className="navegacao-link" onClick={() => onNavegar(-1)}>
        Inicio
      </button>
      {pilha.map((pasta, indice) => (
        <span key={pasta.externalKey} className="navegacao-segmento">
          <span className="navegacao-separador" aria-hidden>
            /
          </span>
          {indice < pilha.length - 1 ? (
            <button
              type="button"
              className="navegacao-link"
              onClick={() => onNavegar(indice)}
            >
              {pasta.nome}
            </button>
          ) : (
            <span className="navegacao-atual">{pasta.nome}</span>
          )}
        </span>
      ))}
    </nav>
  )
}

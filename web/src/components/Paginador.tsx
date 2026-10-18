interface Props {
  pagina: number
  totalPaginas: number
  onMudar: (pagina: number) => void
}

// Cada troca de pagina e uma nova requisicao: o front nunca segura mais que uma pagina.
export function Paginador({ pagina, totalPaginas, onMudar }: Props) {
  if (totalPaginas <= 1) return null

  return (
    <nav className="paginador" aria-label="Paginas">
      <button
        type="button"
        className="paginador-botao"
        disabled={pagina <= 1}
        onClick={() => onMudar(pagina - 1)}
      >
        Anterior
      </button>
      <span className="paginador-posicao">
        Pagina {pagina} de {totalPaginas}
      </span>
      <button
        type="button"
        className="paginador-botao"
        disabled={pagina >= totalPaginas}
        onClick={() => onMudar(pagina + 1)}
      >
        Proxima
      </button>
    </nav>
  )
}

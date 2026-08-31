import { useEffect, useState } from 'react'
import type { ArquivoBuscaResultado } from '../types'
import { URL_PDF_DEMO, urlPdfEnvio } from '../config'

interface Props {
  envio: ArquivoBuscaResultado
  onFechar: () => void
}

function formatarData(data?: string | null): string {
  if (!data) return '-'
  const d = new Date(data)
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleDateString('pt-BR')
}

async function resolverUrlPdf(externalKey: string): Promise<string> {
  const urlVolume = urlPdfEnvio(externalKey)
  try {
    const resposta = await fetch(urlVolume, { method: 'HEAD' })
    const contentType = resposta.headers.get('content-type') ?? ''
    // O Vite devolve index.html (200) para caminhos inexistentes — checar o content-type.
    if (resposta.ok && contentType.includes('application/pdf')) {
      return urlVolume
    }
  } catch {
    // fallback abaixo
  }
  return URL_PDF_DEMO
}

export function PreviewDocumento({ envio, onFechar }: Props) {
  const [urlPdf, setUrlPdf] = useState<string | null>(null)

  useEffect(() => {
    let cancelado = false
    setUrlPdf(null)

    resolverUrlPdf(envio.externalKey).then((url) => {
      if (!cancelado) setUrlPdf(url)
    })

    return () => {
      cancelado = true
    }
  }, [envio.externalKey])

  const urlPdfViewer = urlPdf ? `${urlPdf}#toolbar=1&navpanes=0` : null

  useEffect(() => {
    function aoPressionarEsc(evento: KeyboardEvent) {
      if (evento.key === 'Escape') onFechar()
    }

    document.body.style.overflow = 'hidden'
    window.addEventListener('keydown', aoPressionarEsc)

    return () => {
      document.body.style.overflow = ''
      window.removeEventListener('keydown', aoPressionarEsc)
    }
  }, [onFechar])

  return (
    <div className="preview-overlay" role="presentation" onClick={onFechar}>
      <div
        className="preview-painel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="preview-titulo"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="preview-cabecalho">
          <div className="preview-cabecalho-info">
            <h2 id="preview-titulo" className="preview-titulo">
              {envio.nome}
            </h2>
            <p className="preview-caminho" title={envio.caminho}>
              {envio.caminho}
            </p>
            <div className="preview-meta">
              {envio.statusEnvio && (
                <span className="badge badge-status">{envio.statusEnvio}</span>
              )}
              <span>Criado em {formatarData(envio.dataCriacao)}</span>
              {envio.nomeDestinatarios && <span>Destinatarios: {envio.nomeDestinatarios}</span>}
            </div>
          </div>
          <div className="preview-acoes">
            {urlPdf && (
              <>
                <a
                  className="preview-btn preview-btn-secundario"
                  href={urlPdf}
                  target="_blank"
                  rel="noreferrer"
                >
                  Abrir em nova aba
                </a>
                <a className="preview-btn preview-btn-secundario" href={urlPdf} download>
                  Baixar
                </a>
              </>
            )}
            <button type="button" className="preview-btn preview-btn-fechar" onClick={onFechar}>
              Fechar
            </button>
          </div>
        </header>

        <div className="preview-corpo">
          {urlPdfViewer ? (
            <iframe
              className="preview-iframe"
              src={urlPdfViewer}
              title={`Visualizacao de ${envio.nome}`}
            />
          ) : (
            <p className="preview-carregando">Carregando documento...</p>
          )}
        </div>
      </div>
    </div>
  )
}

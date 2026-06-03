import { EXTERNAL_KEY_CONTA } from '../config'
import type { ArquivoBuscaResultado, Paginacao } from '../types'

export interface BuscarArquivosOpcoes {
  externalKeyPasta?: string | null
  pagina?: number
  tamanhoPagina?: number
  signal?: AbortSignal
}

/**
 * Chama o endpoint de busca recursiva da API.
 * O usuario corrente e identificado pelo header X-Auth-Key (auth simulada).
 */
export async function buscarArquivos(
  termo: string,
  authKey: string,
  opcoes: BuscarArquivosOpcoes = {},
): Promise<Paginacao<ArquivoBuscaResultado>> {
  const { externalKeyPasta, pagina = 1, tamanhoPagina = 50, signal } = opcoes

  const params = new URLSearchParams()
  params.set('termo', termo)
  params.set('pagina', String(pagina))
  params.set('tamanhoPagina', String(tamanhoPagina))
  if (externalKeyPasta) {
    params.set('externalKeyPasta', externalKeyPasta)
  }

  const url = `/api/envio/conta/${EXTERNAL_KEY_CONTA}/buscar?${params.toString()}`

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      'X-Auth-Key': authKey,
    },
    signal,
  })

  if (!response.ok) {
    throw new Error(`Falha na busca (HTTP ${response.status})`)
  }

  return (await response.json()) as Paginacao<ArquivoBuscaResultado>
}

import { EXTERNAL_KEY_CONTA } from '../config'
import type { ItemPasta } from '../types'

export interface ListarItensOpcoes {
  externalKeyPasta?: string | null
  signal?: AbortSignal
}

/**
 * Carrega o conteudo de uma pasta (filhos diretos + envios daquele nivel).
 * O termo digitado pelo usuario nao e enviado: a filtragem acontece no navegador.
 */
export async function listarItens(
  authKey: string,
  opcoes: ListarItensOpcoes = {},
): Promise<ItemPasta[]> {
  const { externalKeyPasta, signal } = opcoes

  const params = new URLSearchParams()
  if (externalKeyPasta) {
    params.set('externalKeyPasta', externalKeyPasta)
  }

  const queryString = params.toString()
  const url = `/api/envio/conta/${EXTERNAL_KEY_CONTA}/itens${queryString ? `?${queryString}` : ''}`

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      'X-Auth-Key': authKey,
    },
    signal,
  })

  if (!response.ok) {
    throw new Error(`Falha ao carregar a pasta (HTTP ${response.status})`)
  }

  return (await response.json()) as ItemPasta[]
}

import { EXTERNAL_KEY_CONTA } from '../config'
import type { ItemPasta, Paginacao } from '../types'

export interface ListarItensOpcoes {
  externalKeyPasta?: string | null
  signal?: AbortSignal
}

export interface BuscarArquivosOpcoes {
  externalKeyPasta?: string | null
  pagina?: number
  tamanhoPagina?: number
  signal?: AbortSignal
}

function montarUrl(caminho: string, params: URLSearchParams): string {
  const queryString = params.toString()
  return `/api/envio/conta/${EXTERNAL_KEY_CONTA}/${caminho}${queryString ? `?${queryString}` : ''}`
}

async function chamar<T>(url: string, authKey: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, {
    method: 'GET',
    headers: { 'X-Auth-Key': authKey },
    signal,
  })

  if (!response.ok) {
    throw new Error(`Falha na requisicao (HTTP ${response.status})`)
  }

  return (await response.json()) as T
}

/** Navegacao: conteudo de UM nivel (filhos diretos + envios da pasta). */
export async function listarItens(
  authKey: string,
  opcoes: ListarItensOpcoes = {},
): Promise<ItemPasta[]> {
  const params = new URLSearchParams()
  if (opcoes.externalKeyPasta) {
    params.set('externalKeyPasta', opcoes.externalKeyPasta)
  }

  return chamar<ItemPasta[]>(montarUrl('itens', params), authKey, opcoes.signal)
}

/** Busca: percorre a subarvore inteira a partir da pasta atual (ou da raiz). */
export async function buscarArquivos(
  termo: string,
  authKey: string,
  opcoes: BuscarArquivosOpcoes = {},
): Promise<Paginacao<ItemPasta>> {
  const { externalKeyPasta, pagina = 1, tamanhoPagina = 50, signal } = opcoes

  const params = new URLSearchParams()
  params.set('termo', termo)
  params.set('pagina', String(pagina))
  params.set('tamanhoPagina', String(tamanhoPagina))
  if (externalKeyPasta) {
    params.set('externalKeyPasta', externalKeyPasta)
  }

  return chamar<Paginacao<ItemPasta>>(montarUrl('buscar', params), authKey, signal)
}

export interface PastaNavegacao {
  externalKey: string
  nome: string
}

export interface ArquivoBuscaResultado {
  externalKey: string
  tipo: 'Pasta' | 'Envio'
  nome: string
  caminho: string
  externalKeyPastaPai?: string | null
  statusEnvio?: string | null
  dataCriacao?: string | null
  dataExpiracao?: string | null
  nomeDestinatarios?: string | null
}

export interface Paginacao<T> {
  tamanhoPagina: number
  paginaSelecionada: number
  total: number
  dados: T[]
}

export interface PastaNavegacao {
  externalKey: string
  nome: string
}

export interface ItemPasta {
  externalKey: string
  tipo: 'Pasta' | 'Envio'
  nome: string
  statusEnvio?: string | null
  dataCriacao?: string | null
  dataExpiracao?: string | null
  nomeDestinatarios?: string | null
  // Preenchidos apenas em resultados de busca: o item pode estar em qualquer
  // profundidade, entao precisa dizer onde fica.
  caminho?: string | null
  externalKeyPastaPai?: string | null
}

export interface Paginacao<T> {
  tamanhoPagina: number
  paginaSelecionada: number
  total: number
  dados: T[]
}

export interface TotaisConta {
  totalArquivos: number
}

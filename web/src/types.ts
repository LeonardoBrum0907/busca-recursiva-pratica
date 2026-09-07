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
}

export interface TotaisConta {
  totalArquivos: number
}

// ExternalKey da conta semeada em db/02_seed.sql
export const EXTERNAL_KEY_CONTA = '11111111-1111-1111-1111-111111111111'

// PDF de demonstracao servido estaticamente (envios do seed original).
export const URL_PDF_DEMO = '/documentos/teste.pdf'

export function urlPdfEnvio(externalKey: string): string {
  return `/documentos/volume/${externalKey}.pdf`
}

export interface UsuarioDemo {
  nome: string
  authKey: string
  descricao: string
}

// AuthKeys correspondem aos usuarios semeados em db/02_seed.sql.
export const USUARIOS_DEMO: UsuarioDemo[] = [
  {
    nome: 'Ana (admin)',
    authKey: 'a0000000-0000-0000-0000-000000000001',
    descricao: 'Enxerga todas as pastas da conta',
  },
  {
    nome: 'Maria (Financeiro)',
    authKey: 'a0000000-0000-0000-0000-000000000002',
    descricao: 'So enxerga o ramo Financeiro (permissao via rlcusuariopasta)',
  },
]

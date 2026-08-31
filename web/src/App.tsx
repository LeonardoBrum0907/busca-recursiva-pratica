import { useEffect, useRef, useState } from 'react'
import { USUARIOS_DEMO } from './config'
import { useDebounce } from './hooks/useDebounce'
import { buscarArquivos } from './services/envioService'
import type { ArquivoBuscaResultado, PastaNavegacao } from './types'
import { ListaResultados } from './components/ListaResultados'
import { NavegacaoPastas } from './components/NavegacaoPastas'
import { PreviewDocumento } from './components/PreviewDocumento'
import './App.css'

const MIN_CARACTERES = 2
const DEBOUNCE_MS = 300

export default function App() {
  const [authKey, setAuthKey] = useState(USUARIOS_DEMO[0].authKey)
  const [termo, setTermo] = useState('')
  const [pilhaPastas, setPilhaPastas] = useState<PastaNavegacao[]>([])
  const [itens, setItens] = useState<ArquivoBuscaResultado[]>([])
  const [total, setTotal] = useState(0)
  const [buscando, setBuscando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const [envioPreview, setEnvioPreview] = useState<ArquivoBuscaResultado | null>(null)

  const termoDebounced = useDebounce(termo, DEBOUNCE_MS)
  const abortRef = useRef<AbortController | null>(null)

  const pastaAtual = pilhaPastas.length > 0 ? pilhaPastas[pilhaPastas.length - 1] : null
  const navegando = termo.trim().length === 0

  useEffect(() => {
    setPilhaPastas([])
  }, [authKey])

  useEffect(() => {
    const termoLimpo = termoDebounced.trim()

    abortRef.current?.abort()

    if (termoLimpo.length > 0 && termoLimpo.length < MIN_CARACTERES) {
      setBuscando(false)
      return
    }

    const controller = new AbortController()
    abortRef.current = controller

    setBuscando(true)
    setErro(null)

    buscarArquivos(termoLimpo, authKey, {
      externalKeyPasta: pastaAtual?.externalKey ?? null,
      signal: controller.signal,
    })
      .then((resultado) => {
        setItens(resultado.dados)
        setTotal(resultado.total)
      })
      .catch((e: unknown) => {
        if (e instanceof DOMException && e.name === 'AbortError') return
        setErro(e instanceof Error ? e.message : 'Erro desconhecido')
        setItens([])
        setTotal(0)
      })
      .finally(() => {
        if (!controller.signal.aborted) setBuscando(false)
      })

    return () => controller.abort()
  }, [termoDebounced, authKey, pastaAtual?.externalKey])

  function entrarNaPasta(pasta: ArquivoBuscaResultado) {
    setPilhaPastas((atual) => [
      ...atual,
      { externalKey: pasta.externalKey, nome: pasta.nome },
    ])
  }

  function navegarPara(indice: number) {
    if (indice < 0) {
      setPilhaPastas([])
      return
    }
    setPilhaPastas((atual) => atual.slice(0, indice + 1))
  }

  const contadorPastas = itens.filter((i) => i.tipo === 'Pasta').length
  const contadorEnvios = itens.filter((i) => i.tipo === 'Envio').length

  function textoContador(): string {
    if (!navegando) return `${total} resultado(s)`

    if (contadorEnvios === 0) return `${contadorPastas} pasta(s)`

    const partes: string[] = []
    if (contadorPastas > 0) partes.push(`${contadorPastas} pasta(s)`)
    if (contadorEnvios > 0) partes.push(`${contadorEnvios} envio(s)`)
    return partes.join(', ')
  }

  return (
    <div className="container">
      <header>
        <h1>Pastas e busca por nivel</h1>
        <p className="subtitulo">
          Navegue pelas pastas permitidas. A busca filtra apenas o nivel atual
          (nao percorre subpastas) e a arvore inteira e carregada em memoria.
        </p>
      </header>

      <section className="painel-usuario">
        <label htmlFor="usuario">Usuario:</label>
        <select
          id="usuario"
          value={authKey}
          onChange={(e) => setAuthKey(e.target.value)}
        >
          {USUARIOS_DEMO.map((u) => (
            <option key={u.authKey} value={u.authKey}>
              {u.nome}
            </option>
          ))}
        </select>
        <span className="usuario-descricao">
          {USUARIOS_DEMO.find((u) => u.authKey === authKey)?.descricao}
        </span>
      </section>

      <section className="busca">
        <div className="campo-busca">
          <span className="lupa" aria-hidden>
            🔍
          </span>
          <input
            type="text"
            placeholder="Pesquisar neste nivel..."
            value={termo}
            onChange={(e) => setTermo(e.target.value)}
            autoFocus
          />
          {buscando && <span className="spinner" title="Buscando..." />}
        </div>
        {!buscando && !erro && !(termo.trim().length > 0 && termo.trim().length < MIN_CARACTERES) && (
          <p className="contador">{textoContador()}</p>
        )}
        {!navegando && termo.trim().length >= MIN_CARACTERES && (
          <p className="dica">
            A busca vale so para esta pasta. Entre em uma subpasta para procurar mais fundo.
          </p>
        )}
      </section>

      <NavegacaoPastas pilha={pilhaPastas} onNavegar={navegarPara} />

      {erro && <p className="erro">{erro}</p>}

      <ListaResultados
        itens={itens}
        termo={termo}
        navegando={navegando}
        onPastaClick={navegando ? entrarNaPasta : undefined}
        onEnvioClick={setEnvioPreview}
      />

      {envioPreview && (
        <PreviewDocumento envio={envioPreview} onFechar={() => setEnvioPreview(null)} />
      )}
    </div>
  )
}

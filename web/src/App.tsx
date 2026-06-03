import { useEffect, useRef, useState } from 'react'
import { USUARIOS_DEMO } from './config'
import { useDebounce } from './hooks/useDebounce'
import { buscarArquivos } from './services/envioService'
import type { ArquivoBuscaResultado } from './types'
import { ListaResultados } from './components/ListaResultados'
import './App.css'

const MIN_CARACTERES = 2
const DEBOUNCE_MS = 300

export default function App() {
  const [authKey, setAuthKey] = useState(USUARIOS_DEMO[0].authKey)
  const [termo, setTermo] = useState('')
  const [itens, setItens] = useState<ArquivoBuscaResultado[]>([])
  const [total, setTotal] = useState(0)
  const [buscando, setBuscando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  const termoDebounced = useDebounce(termo, DEBOUNCE_MS)
  const abortRef = useRef<AbortController | null>(null)

  useEffect(() => {
    const termoLimpo = termoDebounced.trim()

    // Aborta a busca anterior ainda em voo
    abortRef.current?.abort()

    if (termoLimpo.length < MIN_CARACTERES) {
      setItens([])
      setTotal(0)
      setBuscando(false)
      setErro(null)
      return
    }

    const controller = new AbortController()
    abortRef.current = controller

    setBuscando(true)
    setErro(null)

    buscarArquivos(termoLimpo, authKey, { signal: controller.signal })
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
  }, [termoDebounced, authKey])

  return (
    <div className="container">
      <header>
        <h1>Busca recursiva de arquivos</h1>
        <p className="subtitulo">
          Pratica de busca textual recursiva (Pasta &gt; Envio) com filtro de permissoes.
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
          <span className="lupa" aria-hidden>🔍</span>
          <input
            type="text"
            placeholder="Pesquisar pastas e envios..."
            value={termo}
            onChange={(e) => setTermo(e.target.value)}
            autoFocus
          />
          {buscando && <span className="spinner" title="Buscando..." />}
        </div>
        {termo.trim().length >= MIN_CARACTERES && !buscando && !erro && (
          <p className="contador">{total} resultado(s)</p>
        )}
      </section>

      {erro && <p className="erro">{erro}</p>}

      <ListaResultados itens={itens} termo={termo} />
    </div>
  )
}

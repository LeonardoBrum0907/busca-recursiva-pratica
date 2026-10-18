import { useEffect, useRef, useState } from 'react'
import { USUARIOS_DEMO } from './config'
import { useDebounce } from './hooks/useDebounce'
import { buscarArquivos, listarItens, obterTotais } from './services/envioService'
import type { ItemPasta, PastaNavegacao } from './types'
import { ListaResultados } from './components/ListaResultados'
import { NavegacaoPastas } from './components/NavegacaoPastas'
import { Paginador } from './components/Paginador'
import { PreviewDocumento } from './components/PreviewDocumento'
import './App.css'

const MIN_CARACTERES = 2
const DEBOUNCE_MS = 300
const TAMANHO_PAGINA = 50

export default function App() {
  const [authKey, setAuthKey] = useState(USUARIOS_DEMO[0].authKey)
  const [termo, setTermo] = useState('')
  const [pilhaPastas, setPilhaPastas] = useState<PastaNavegacao[]>([])
  const [itens, setItens] = useState<ItemPasta[]>([])
  const [total, setTotal] = useState(0)
  const [pagina, setPagina] = useState(1)
  const [carregando, setCarregando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const [envioPreview, setEnvioPreview] = useState<ItemPasta | null>(null)
  const [durationMs, setDurationMs] = useState<number | null>(null)
  const [totalArquivos, setTotalArquivos] = useState<number | null>(null)

  const termoDebounced = useDebounce(termo, DEBOUNCE_MS)
  const abortRef = useRef<AbortController | null>(null)

  const pastaAtual = pilhaPastas.length > 0 ? pilhaPastas[pilhaPastas.length - 1] : null
  const termoLimpo = termoDebounced.trim()
  const buscando = termoLimpo.length >= MIN_CARACTERES
  const aguardandoMinimo = termo.trim().length > 0 && termo.trim().length < MIN_CARACTERES
  const totalPaginas = Math.ceil(total / TAMANHO_PAGINA)

  useEffect(() => {
    setPilhaPastas([])
  }, [authKey])

  useEffect(() => {
    const controller = new AbortController()

    obterTotais(authKey, controller.signal)
      .then((totais) => setTotalArquivos(totais.totalArquivos))
      .catch((e: unknown) => {
        if (e instanceof DOMException && e.name === 'AbortError') return
        // Totais sao informativos; nao bloqueiam a navegacao.
      })

    return () => controller.abort()
  }, [authKey])

  useEffect(() => {
    abortRef.current?.abort()

    // Um caractere nao dispara nada: com milhares de arquivos, quase tudo casaria.
    if (termoLimpo.length > 0 && termoLimpo.length < MIN_CARACTERES) {
      setCarregando(false)
      return
    }

    const controller = new AbortController()
    abortRef.current = controller

    setCarregando(true)
    setErro(null)

    const inicio = performance.now()

    const opcoes = {
      externalKeyPasta: pastaAtual?.externalKey ?? null,
      pagina,
      tamanhoPagina: TAMANHO_PAGINA,
      signal: controller.signal,
    }
    const requisicao = buscando
      ? buscarArquivos(termoLimpo, authKey, opcoes)
      : listarItens(authKey, opcoes)

    requisicao
      .then((resultado) => {
        setItens(resultado.dados)
        setTotal(resultado.total)
        setDurationMs(Math.round(performance.now() - inicio))
      })
      .catch((e: unknown) => {
        if (e instanceof DOMException && e.name === 'AbortError') return
        setErro(e instanceof Error ? e.message : 'Erro desconhecido')
        setItens([])
        setTotal(0)
      })
      .finally(() => {
        if (!controller.signal.aborted) setCarregando(false)
      })

    return () => controller.abort()
  }, [termoLimpo, buscando, authKey, pastaAtual?.externalKey, pagina])

  function entrarNaPasta(pasta: ItemPasta) {
    setTermo('')
    setPagina(1)
    setPilhaPastas((atual) => [
      ...atual,
      { externalKey: pasta.externalKey, nome: pasta.nome },
    ])
  }

  function navegarPara(indice: number) {
    setTermo('')
    setPagina(1)
    if (indice < 0) {
      setPilhaPastas([])
      return
    }
    setPilhaPastas((atual) => atual.slice(0, indice + 1))
  }

  const contadorPastas = itens.filter((i) => i.tipo === 'Pasta').length
  const contadorEnvios = itens.filter((i) => i.tipo === 'Envio').length

  function textoItensEncontrados(): string {
    if (buscando) return `${total} resultado(s) na subarvore`
    // Com mais de uma pagina, contar so a pagina na tela enganaria.
    if (totalPaginas > 1) return `${total} item(ns) nesta pasta`

    const partes: string[] = []
    if (contadorPastas > 0) partes.push(`${contadorPastas} pasta(s)`)
    if (contadorEnvios > 0) partes.push(`${contadorEnvios} envio(s)`)
    return partes.length > 0 ? partes.join(', ') : 'nenhum item'
  }

  function textoContador(): string {
    const partes: string[] = []
    if (durationMs !== null) partes.push(`${durationMs}ms`)
    partes.push(textoItensEncontrados())
    if (totalArquivos !== null) partes.push(`${totalArquivos} arquivos totais`)
    return partes.join(' - ')
  }

  return (
    <div className="container">
      <header>
        <h1>Pastas e busca recursiva</h1>
        <p className="subtitulo">
          Navegue pelas pastas permitidas ou busque em qualquer profundidade a partir
          da pasta atual — a varredura acontece no banco, respeitando as permissoes.
        </p>
      </header>

      <section className="painel-usuario">
        <label htmlFor="usuario">Usuario:</label>
        <select
          id="usuario"
          value={authKey}
          onChange={(e) => {
            setAuthKey(e.target.value)
            setPagina(1)
          }}
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
            placeholder="Pesquisar pastas e envios..."
            value={termo}
            onChange={(e) => {
              setTermo(e.target.value)
              setPagina(1)
            }}
            autoFocus
          />
          {carregando && <span className="spinner" title="Buscando..." />}
        </div>
        {!carregando && !erro && !aguardandoMinimo && (
          <p className="contador">{textoContador()}</p>
        )}
      </section>

      {!buscando && <NavegacaoPastas pilha={pilhaPastas} onNavegar={navegarPara} />}

      {erro && <p className="erro">{erro}</p>}

      <ListaResultados
        itens={itens}
        termo={termo}
        buscando={buscando}
        aguardandoMinimo={aguardandoMinimo}
        onPastaClick={buscando ? undefined : entrarNaPasta}
        onEnvioClick={setEnvioPreview}
      />

      {!carregando && !aguardandoMinimo && (
        <Paginador pagina={pagina} totalPaginas={totalPaginas} onMudar={setPagina} />
      )}

      {envioPreview && (
        <PreviewDocumento envio={envioPreview} onFechar={() => setEnvioPreview(null)} />
      )}
    </div>
  )
}

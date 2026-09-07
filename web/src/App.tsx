import { useEffect, useMemo, useRef, useState } from 'react'
import { USUARIOS_DEMO } from './config'
import { listarItens, obterTotais } from './services/envioService'
import type { ItemPasta, PastaNavegacao } from './types'
import { ListaResultados } from './components/ListaResultados'
import { NavegacaoPastas } from './components/NavegacaoPastas'
import { PreviewDocumento } from './components/PreviewDocumento'
import './App.css'

export default function App() {
  const [authKey, setAuthKey] = useState(USUARIOS_DEMO[0].authKey)
  const [termo, setTermo] = useState('')
  const [pilhaPastas, setPilhaPastas] = useState<PastaNavegacao[]>([])
  // Espelha o ItensUnificadosAux do ArecoSign: so o conteudo do nivel aberto.
  const [itensUnificados, setItensUnificados] = useState<ItemPasta[]>([])
  const [carregando, setCarregando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)
  const [envioPreview, setEnvioPreview] = useState<ItemPasta | null>(null)
  const [durationMs, setDurationMs] = useState<number | null>(null)
  const [totalArquivos, setTotalArquivos] = useState<number | null>(null)

  const abortRef = useRef<AbortController | null>(null)

  const pastaAtual = pilhaPastas.length > 0 ? pilhaPastas[pilhaPastas.length - 1] : null

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

  // A pasta so e recarregada quando o usuario navega ou troca de perfil.
  // O termo digitado nunca chega ate aqui.
  useEffect(() => {
    abortRef.current?.abort()

    const controller = new AbortController()
    abortRef.current = controller

    setCarregando(true)
    setErro(null)

    const inicio = performance.now()

    listarItens(authKey, {
      externalKeyPasta: pastaAtual?.externalKey ?? null,
      signal: controller.signal,
    })
      .then((itens) => {
        setItensUnificados(itens)
        setDurationMs(Math.round(performance.now() - inicio))
      })
      .catch((e: unknown) => {
        if (e instanceof DOMException && e.name === 'AbortError') return
        setErro(e instanceof Error ? e.message : 'Erro desconhecido')
        setItensUnificados([])
      })
      .finally(() => {
        if (!controller.signal.aborted) setCarregando(false)
      })

    return () => controller.abort()
  }, [authKey, pastaAtual?.externalKey])

  // Filtro 100% client-side, em memoria, sobre o nivel ja carregado.
  // Roda de novo a cada tecla e nunca alcanca itens de subpastas.
  const termoLimpo = termo.trim().toLowerCase()
  const { itensFiltrados, filtroMs } = useMemo(() => {
    if (termoLimpo.length === 0) {
      return { itensFiltrados: itensUnificados, filtroMs: null as number | null }
    }

    const inicio = performance.now()
    const filtrados = itensUnificados.filter((item) =>
      item.nome.toLowerCase().includes(termoLimpo),
    )
    return {
      itensFiltrados: filtrados,
      filtroMs: Math.round(performance.now() - inicio),
    }
  }, [itensUnificados, termoLimpo])

  const durationExibida = termoLimpo.length > 0 ? (filtroMs ?? 0) : durationMs

  function entrarNaPasta(pasta: ItemPasta) {
    setTermo('')
    setPilhaPastas((atual) => [
      ...atual,
      { externalKey: pasta.externalKey, nome: pasta.nome },
    ])
  }

  function navegarPara(indice: number) {
    setTermo('')
    if (indice < 0) {
      setPilhaPastas([])
      return
    }
    setPilhaPastas((atual) => atual.slice(0, indice + 1))
  }

  const contadorPastas = itensFiltrados.filter((i) => i.tipo === 'Pasta').length
  const contadorEnvios = itensFiltrados.filter((i) => i.tipo === 'Envio').length

  function textoItensEncontrados(): string {
    const partes: string[] = []
    if (contadorPastas > 0) partes.push(`${contadorPastas} pasta(s)`)
    if (contadorEnvios > 0) partes.push(`${contadorEnvios} envio(s)`)
    return partes.length > 0 ? partes.join(', ') : 'nenhum item'
  }

  function textoContador(): string {
    const partes: string[] = []
    if (durationExibida !== null) partes.push(`${durationExibida}ms`)
    partes.push(textoItensEncontrados())
    if (totalArquivos !== null) partes.push(`${totalArquivos} arquivos totais`)
    return partes.join(' - ')
  }

  return (
    <div className="container">
      <header>
        <h1>Pastas e busca por nivel</h1>
        <p className="subtitulo">
          Navegue pelas pastas permitidas. A busca filtra em memoria apenas os itens
          ja carregados do nivel atual — a API nunca recebe o termo digitado.
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
          {carregando && <span className="spinner" title="Carregando pasta..." />}
        </div>
        {!carregando && !erro && <p className="contador">{textoContador()}</p>}
        {termoLimpo.length > 0 && (
          <p className="dica">
            A busca vale so para esta pasta. Entre em uma subpasta para procurar mais fundo.
          </p>
        )}
      </section>

      <NavegacaoPastas pilha={pilhaPastas} onNavegar={navegarPara} />

      {erro && <p className="erro">{erro}</p>}

      <ListaResultados
        itens={itensFiltrados}
        termo={termo}
        onPastaClick={entrarNaPasta}
        onEnvioClick={setEnvioPreview}
      />

      {envioPreview && (
        <PreviewDocumento envio={envioPreview} onFechar={() => setEnvioPreview(null)} />
      )}
    </div>
  )
}

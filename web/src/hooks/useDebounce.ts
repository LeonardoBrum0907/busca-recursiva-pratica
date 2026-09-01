import { useEffect, useState } from 'react'

/**
 * Retorna o valor apenas apos `delayMs` sem alteracoes.
 * Usado para nao disparar uma chamada HTTP a cada tecla digitada.
 */
export function useDebounce<T>(valor: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(valor)

  useEffect(() => {
    const id = setTimeout(() => setDebounced(valor), delayMs)
    return () => clearTimeout(id)
  }, [valor, delayMs])

  return debounced
}

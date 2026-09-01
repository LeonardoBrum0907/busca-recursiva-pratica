import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Porta 5174 para poder rodar lado a lado com a branch main (5173).
// O proxy encaminha /api para a API .NET, evitando CORS no desenvolvimento.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:5081',
        changeOrigin: true,
      },
    },
  },
})

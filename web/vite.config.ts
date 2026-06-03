import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// O proxy encaminha /api para a API .NET (http://localhost:5080),
// evitando problemas de CORS durante o desenvolvimento.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
})

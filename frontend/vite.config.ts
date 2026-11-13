import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// Produção: o build vai para a pasta wwwroot/admin da API (servida em /admin/), com caminhos relativos
// para funcionar também sob um caminho virtual (ex.: /Homologacao/ErpWebAnalytics/admin/).
// Desenvolvimento: `npm run dev` faz proxy de /api para a API local (VITE_API_URL, padrão http://localhost:5094).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  return {
    base: './',
    plugins: [react()],
    build: {
      outDir: '../src/ErpWebAnalytics.Api/wwwroot/admin',
      emptyOutDir: true,
    },
    server: {
      proxy: {
        '/api': {
          target: env.VITE_API_URL || 'http://localhost:5094',
          changeOrigin: true,
          rewrite: (p: string) => '/admin' + p,
        },
      },
    },
  }
})

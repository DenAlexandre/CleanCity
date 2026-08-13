import { existsSync, readFileSync } from 'node:fs'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ command }) => {
  const certsAvailable =
    existsSync('.certs/localhost.key') && existsSync('.certs/localhost.pem')

  return {
    plugins: [react()],
    server: {
      // Certificat dotnet dev-certs (celui de l'API, deja approuve par Windows/le navigateur) reutilise
      // ici pour eviter l'avertissement "Non securise" d'un certificat auto-signe non approuve.
      // Uniquement en dev local : absent (et inutile) sur les environnements de build (Render/Cloudflare Pages).
      https:
        command === 'serve' && certsAvailable
          ? {
              key: readFileSync('.certs/localhost.key'),
              cert: readFileSync('.certs/localhost.pem'),
            }
          : undefined,
      proxy: {
        '/api': {
          target: 'https://localhost:7085',
          changeOrigin: true,
          secure: false,
        },
      },
    },
  }
})

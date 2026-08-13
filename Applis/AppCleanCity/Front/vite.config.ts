import { readFileSync } from 'node:fs'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Certificat dotnet dev-certs (celui de l'API, deja approuve par Windows/le navigateur) reutilise
    // ici pour eviter l'avertissement "Non securise" d'un certificat auto-signe non approuve.
    https: {
      key: readFileSync('.certs/localhost.key'),
      cert: readFileSync('.certs/localhost.pem'),
    },
    proxy: {
      '/api': {
        target: 'https://localhost:7085',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})

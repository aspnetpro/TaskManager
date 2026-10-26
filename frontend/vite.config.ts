/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Em dev a SPA e a Api ficam na mesma origem via proxy: o cookie de refresh
// (SameSite=Strict, Path=/api/v1/auth) funciona sem CORS.
const apiTarget = process.env.API_PROXY_TARGET ?? 'http://localhost:5000'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': { target: apiTarget },
    },
  },
  test: {
    environment: 'jsdom',
    environmentOptions: { jsdom: { url: 'http://localhost:5173' } },
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    testTimeout: 15_000,
  },
})

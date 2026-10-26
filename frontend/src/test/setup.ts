import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterAll, afterEach, beforeAll } from 'vitest'
import { useAuthStore } from '@/stores/authStore'
import { db, server } from './server'

// Rotas são lazy: o primeiro import de cada página é transformado na hora e passa de 1s.
configure({ asyncUtilTimeout: 5_000 })

// Request sem handler = teste quebrado, não um 404 silencioso.
beforeAll(() => server.listen({ onUnhandledFrame: 'error' }))

afterEach(() => {
  cleanup()
  server.resetHandlers()
  db.reset()
  useAuthStore.setState({ status: 'unknown', session: null })
})

afterAll(() => server.close())

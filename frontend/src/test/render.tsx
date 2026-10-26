import { QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactNode } from 'react'
import { createMemoryRouter } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { AppProviders } from '@/App'
import { createQueryClient } from '@/lib/queryClient'
import { createRoutes } from '@/routes'
import { useAuthStore } from '@/stores/authStore'
import type { WorkspaceRole } from '@/types/api'
import { sessionFor } from './fixtures'
import { db } from './server'

/** Loga como `role` no store e na Api fake (o token passa a ser aceito). */
export function signInAs(role: WorkspaceRole) {
  const session = sessionFor(role)
  db.validToken = session.accessToken
  db.refreshSession = session
  useAuthStore.getState().setSession(session)
  return session
}

/** App inteiro (rotas, guards, providers) numa URL — o teste mais próximo do usuário real. */
export function renderApp(path: string) {
  const queryClient = createQueryClient()
  const router = createMemoryRouter(createRoutes(), { initialEntries: [path] })
  render(
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>,
  )
  return { router, queryClient }
}

export function createWrapper() {
  const queryClient = createQueryClient()
  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  )
  return { queryClient, wrapper }
}

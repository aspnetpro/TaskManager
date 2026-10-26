import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { createBrowserRouter } from 'react-router'
import { RouterProvider } from 'react-router/dom'
import { bootstrapSession } from '@/api/http'
import { AppProviders } from '@/App'
import { clearCacheOnSessionChange, createQueryClient } from '@/lib/queryClient'
import { createRoutes } from '@/routes'
import './index.css'

const queryClient = createQueryClient()
clearCacheOnSessionChange(queryClient)

// Fora do React de propósito: o StrictMode roda efeitos duas vezes em dev, e
// um refresh duplicado reapresentaria um token rotacionado (= sessão revogada).
void bootstrapSession()

const router = createBrowserRouter(createRoutes())

const root = document.getElementById('root')
if (!root)
  throw new Error('#root não encontrado no index.html')

createRoot(root).render(
  <StrictMode>
    <AppProviders queryClient={queryClient}>
      <RouterProvider router={router} />
    </AppProviders>
  </StrictMode>,
)

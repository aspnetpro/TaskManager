import { Navigate, Outlet, useLocation } from 'react-router'
import { FullPageSpinner } from '@/components/ui/Spinner'
import { useAuthStore } from '@/stores/authStore'

export interface RedirectState {
  from?: string
}

/**
 * Sessão expirou no meio do uso (refresh voltou 401)? O store vira 'anonymous'
 * e este guard redireciona sozinho — sem window.location, sem perder o SPA.
 */
export function RequireAuth() {
  const status = useAuthStore((s) => s.status)
  const location = useLocation()

  if (status === 'unknown')
    return <FullPageSpinner />

  if (status === 'anonymous') {
    const state: RedirectState = { from: location.pathname + location.search }
    return <Navigate to="/login" state={state} replace />
  }

  return <Outlet />
}

export function PublicOnly() {
  const status = useAuthStore((s) => s.status)
  const location = useLocation()

  if (status === 'unknown')
    return <FullPageSpinner />

  if (status === 'authenticated') {
    const from = (location.state as RedirectState | null)?.from
    return <Navigate to={from ?? '/board'} replace />
  }

  return <Outlet />
}

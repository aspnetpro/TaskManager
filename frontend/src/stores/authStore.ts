import { create } from 'zustand'
import type { AuthSession } from '@/types/api'

/**
 * 'unknown' = ainda não sabemos (o bootstrap está tentando o refresh).
 * Sem esse estado, um F5 mandaria o usuário logado para /login por um frame.
 */
export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous'

interface AuthState {
  status: AuthStatus
  session: AuthSession | null
  setSession: (session: AuthSession) => void
  clear: () => void
}

// Só memória. Nada de localStorage/sessionStorage: XSS lê os dois.
// A sessão sobrevive ao F5 porque o cookie httpOnly sobrevive, não o token.
export const useAuthStore = create<AuthState>()((set) => ({
  status: 'unknown',
  session: null,
  setSession: (session) => set({ session, status: 'authenticated' }),
  clear: () => set({ session: null, status: 'anonymous' }),
}))

export const useSession = () => useAuthStore((s) => s.session)

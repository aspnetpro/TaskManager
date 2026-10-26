import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ApiError, NetworkError } from '@/api/http'
import { useAuthStore } from '@/stores/authStore'
import { describeError } from './errors'

declare module '@tanstack/react-query' {
  interface Register {
    mutationMeta: {
      /** A própria mutation trata o erro (ex.: formulário mostra inline). */
      silentError?: boolean
    }
  }
}

/** Rede cai e volta: vale tentar. 4xx é determinístico: repetir não muda nada. */
function shouldRetry(failureCount: number, error: unknown): boolean {
  if (error instanceof NetworkError)
    return failureCount < 3
  if (error instanceof ApiError && error.status >= 500)
    return failureCount < 1
  return false
}

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { staleTime: 30_000, retry: shouldRetry },
      mutations: { retry: false },
    },
    queryCache: new QueryCache({
      // Primeira carga mostra o erro inline (ErrorBanner); refetch em background
      // que falha com dados na tela vira toast, sem apagar o que o usuário vê.
      onError: (error, query) => {
        if (query.state.data !== undefined)
          toast.error(describeError(error))
      },
    }),
    mutationCache: new MutationCache({
      onError: (error, _variables, _context, mutation) => {
        if (!mutation.meta?.silentError)
          toast.error(describeError(error))
      },
    }),
  })
}

/**
 * O cache do front também é multi-tenant: trocou de usuário ou de workspace
 * (logout, sessão expirada, outro login na mesma aba), o cache inteiro sai.
 * É o equivalente do IWorkspaceCache no backend.
 */
export function clearCacheOnSessionChange(queryClient: QueryClient): () => void {
  return useAuthStore.subscribe((state, previous) => {
    const scope = (s: typeof state) => `${s.session?.user.id}:${s.session?.workspace.workspaceId}`
    if (scope(state) !== scope(previous))
      queryClient.clear()
  })
}

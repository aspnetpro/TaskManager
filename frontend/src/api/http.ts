import { useAuthStore } from '@/stores/authStore'
import type { AuthSession, ProblemDetails } from '@/types/api'

// Vazio = mesma origem (proxy do Vite em dev, nginx no container).
const API_BASE = `${import.meta.env.VITE_API_URL ?? ''}/api/v1`

/** Resposta HTTP de erro da Api, já com o ProblemDetails lido. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string | undefined
  readonly fieldErrors: Record<string, string[]>
  readonly retryAfterSeconds: number | undefined

  constructor(status: number, problem: ProblemDetails | null, retryAfterSeconds?: number) {
    super(problem?.detail ?? problem?.title ?? `HTTP ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.code = problem?.code
    this.fieldErrors = problem?.errors ?? {}
    this.retryAfterSeconds = retryAfterSeconds
  }
}

/** A request nem chegou na Api (offline, DNS, Api fora do ar). */
export class NetworkError extends Error {
  constructor(cause: unknown) {
    super('Network request failed', { cause })
    this.name = 'NetworkError'
  }
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  query?: object
  headers?: Record<string, string>
  signal?: AbortSignal
  /** Endpoints de auth: sem Bearer e sem tentar refresh em 401. */
  anonymous?: boolean
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const tokenUsed = options.anonymous ? null : currentToken()
  let response = await send(path, options, tokenUsed)

  if (response.status === 401 && !options.anonymous) {
    // Se outra request já renovou enquanto esta estava no ar, só repete com o token novo.
    const latest = currentToken()
    const token = latest && latest !== tokenUsed ? latest : (await refreshSession())?.accessToken
    if (token)
      response = await send(path, options, token)
  }

  return parse<T>(response)
}

let refreshing: Promise<AuthSession | null> | null = null

/**
 * Single-flight: N requests com 401 ao mesmo tempo disparam UM refresh.
 * Não é otimização — o backend rotaciona o refresh token e trata reuso como
 * roubo. Dois refreshes em paralelo = o segundo reapresenta um token já
 * rotacionado = a Api revoga a família inteira e o usuário cai para o login.
 */
export function refreshSession(): Promise<AuthSession | null> {
  refreshing ??= withCrossTabLock(doRefresh).finally(() => {
    refreshing = null
  })
  return refreshing
}

/** F5 ou primeira visita: tenta recuperar a sessão pelo cookie. */
export async function bootstrapSession(): Promise<void> {
  try {
    await refreshSession()
  } catch {
    useAuthStore.getState().clear()
  }
}

async function doRefresh(): Promise<AuthSession | null> {
  const response = await send('/auth/refresh', { method: 'POST' }, null)
  if (response.status === 401) {
    useAuthStore.getState().clear()
    return null
  }

  // 429/5xx/rede: propaga sem deslogar — instabilidade não é sessão inválida.
  const session = await parse<AuthSession>(response)
  useAuthStore.getState().setSession(session)
  return session
}

// Duas abas com o token vencido também disputariam o mesmo cookie. O Web Lock
// serializa entre abas; a segunda já sai com o cookie que a primeira rotacionou.
function withCrossTabLock<T>(fn: () => Promise<T>): Promise<T> {
  const locks = typeof navigator === 'undefined' ? undefined : navigator.locks
  return locks ? locks.request('taskmanager-refresh', fn) : fn()
}

function currentToken(): string | null {
  return useAuthStore.getState().session?.accessToken ?? null
}

async function send(path: string, options: RequestOptions, token: string | null): Promise<Response> {
  const headers = new Headers(options.headers)
  headers.set('Accept', 'application/json')
  if (options.body !== undefined)
    headers.set('Content-Type', 'application/json')
  if (token)
    headers.set('Authorization', `Bearer ${token}`)

  try {
    return await fetch(buildUrl(path, options.query), {
      method: options.method ?? 'GET',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      // Necessário só quando a Api está em outra origem (VITE_API_URL); inofensivo na mesma.
      credentials: 'include',
      signal: options.signal,
    })
  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError')
      throw error
    throw new NetworkError(error)
  }
}

function buildUrl(path: string, query?: object): URL {
  const url = new URL(`${API_BASE}${path}`, window.location.origin)
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== null && value !== '')
      url.searchParams.set(key, String(value))
  }
  return url
}

async function parse<T>(response: Response): Promise<T> {
  if (response.ok) {
    const isJson = response.headers.get('Content-Type')?.includes('json') ?? false
    return (response.status === 204 || !isJson ? undefined : await response.json()) as T
  }

  const retryAfter = Number(response.headers.get('Retry-After')) || undefined
  throw new ApiError(response.status, await readProblem(response), retryAfter)
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  try {
    return (await response.json()) as ProblemDetails
  } catch {
    return null
  }
}

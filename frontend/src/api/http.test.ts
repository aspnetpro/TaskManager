import { http, HttpResponse } from 'msw/http'
import { describe, expect, it } from 'vitest'
import { useAuthStore } from '@/stores/authStore'
import { sessionFor } from '@/test/fixtures'
import { db, server } from '@/test/server'
import { ApiError, NetworkError, bootstrapSession, request } from './http'

describe('request', () => {
  it('sends the access token as a Bearer header', async () => {
    let authorization: string | null = null
    server.use(
      http.get('*/api/v1/ping', ({ request }) => {
        authorization = request.headers.get('Authorization')
        return HttpResponse.json({ ok: true })
      }),
    )
    useAuthStore.getState().setSession(sessionFor('Developer', 'abc'))

    await request('/ping')

    expect(authorization).toBe('Bearer abc')
  })

  it('refreshes once and retries every request when several get 401 at the same time', async () => {
    // Token velho no store; a Api só aceita o que vem do refresh.
    useAuthStore.getState().setSession(sessionFor('Developer', 'expired'))
    db.refreshSession = sessionFor('Developer', 'fresh')
    db.validToken = 'not-yet'

    const results = await Promise.all([
      request('/tasks'),
      request('/tasks'),
      request('/workspace/members'),
    ])

    expect(results).toHaveLength(3)
    // O ponto do teste: dois refreshes em paralelo = reuso de token = sessão revogada.
    expect(db.refreshCalls).toBe(1)
    expect(useAuthStore.getState().session?.accessToken).toBe('fresh')
  })

  it('clears the session and surfaces the 401 when the refresh cookie is no longer valid', async () => {
    useAuthStore.getState().setSession(sessionFor('Developer', 'expired'))
    db.refreshSession = null

    const error = await request('/tasks').catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect((error as ApiError).status).toBe(401)
    expect(useAuthStore.getState().status).toBe('anonymous')
  })

  it('keeps the session when the refresh fails for a reason other than 401', async () => {
    useAuthStore.getState().setSession(sessionFor('Developer', 'expired'))
    server.use(http.post('*/api/v1/auth/refresh', () => HttpResponse.json({}, { status: 503 })))

    await expect(request('/tasks')).rejects.toMatchObject({ status: 503 })
    expect(useAuthStore.getState().status).toBe('authenticated')
  })

  it('maps ProblemDetails into ApiError with code, field errors and Retry-After', async () => {
    server.use(
      http.post('*/api/v1/things', () =>
        HttpResponse.json(
          { status: 400, detail: 'Invalid', code: 'Validation', errors: { Title: ['Title is required.'] } },
          { status: 400, headers: { 'Retry-After': '12' } },
        ),
      ),
    )

    const error = (await request('/things', { method: 'POST', body: {}, anonymous: true }).catch((e: unknown) => e)) as ApiError

    expect(error.code).toBe('Validation')
    expect(error.fieldErrors).toEqual({ Title: ['Title is required.'] })
    expect(error.retryAfterSeconds).toBe(12)
  })

  it('turns a failed fetch into NetworkError', async () => {
    server.use(http.get('*/api/v1/offline', () => HttpResponse.error()))

    await expect(request('/offline', { anonymous: true })).rejects.toBeInstanceOf(NetworkError)
  })

  it('skips empty query values when building the URL', async () => {
    let url = ''
    server.use(
      http.get('*/api/v1/search', ({ request }) => {
        url = request.url
        return HttpResponse.json([])
      }),
    )

    await request('/search', { query: { status: 'Done', search: '', assigneeId: undefined, page: 2 }, anonymous: true })

    expect(new URL(url).search).toBe('?status=Done&page=2')
  })
})

describe('bootstrapSession', () => {
  it('restores the session from the refresh cookie', async () => {
    db.refreshSession = sessionFor('Admin')

    await bootstrapSession()

    expect(useAuthStore.getState().status).toBe('authenticated')
    expect(useAuthStore.getState().session?.workspace.role).toBe('Admin')
  })

  it('ends anonymous when there is no cookie or the Api is down', async () => {
    server.use(http.post('*/api/v1/auth/refresh', () => HttpResponse.error()))

    await bootstrapSession()

    expect(useAuthStore.getState().status).toBe('anonymous')
  })
})

import { http, HttpResponse } from 'msw/http'
import { setupServer } from 'msw/node'
import { canTransition } from '@/lib/transitions'
import type { AuthSession, ProblemDetails, TaskItem, TaskStatus } from '@/types/api'
import { members, sessionFor } from './fixtures'

/**
 * "Banco" da Api fake. Cada teste monta o estado de que precisa; o setup zera
 * tudo entre testes. As regras imitam a Api real (transições, 401 sem token,
 * 422, If-Match) — o suficiente para o front ser testado contra o contrato.
 */
export const db = {
  tasks: [] as TaskItem[],
  /** Sessão que o cookie de refresh "representa". null = cookie ausente/inválido. */
  refreshSession: null as AuthSession | null,
  /** Token aceito pela Api. Mudar simula expiração do access token. */
  validToken: null as string | null,
  refreshCalls: 0,
  reset() {
    this.tasks = []
    this.refreshSession = null
    this.validToken = null
    this.refreshCalls = 0
  },
}

const API = '*/api/v1'

const problem = (status: number, body: ProblemDetails = {}) =>
  HttpResponse.json({ status, ...body }, { status, headers: { 'Content-Type': 'application/problem+json' } })

const authorized = (request: Request) =>
  db.validToken !== null && request.headers.get('Authorization') === `Bearer ${db.validToken}`

export const handlers = [
  http.post(`${API}/auth/refresh`, () => {
    db.refreshCalls += 1
    if (!db.refreshSession)
      return problem(401, { code: 'Auth.InvalidRefreshToken' })
    db.validToken = db.refreshSession.accessToken
    return HttpResponse.json(db.refreshSession)
  }),

  http.post(`${API}/auth/login`, async ({ request }) => {
    const body = (await request.json()) as { email: string; password: string }
    if (body.password !== 'demo-password-123')
      return problem(401, { code: 'Auth.InvalidCredentials', detail: 'Invalid email or password.' })
    const session = sessionFor('TechLead')
    db.validToken = session.accessToken
    db.refreshSession = session
    return HttpResponse.json(session)
  }),

  http.post(`${API}/auth/logout`, () => new HttpResponse(null, { status: 204 })),

  http.get(`${API}/workspace/members`, ({ request }) =>
    authorized(request) ? HttpResponse.json(members) : problem(401)),

  http.get(`${API}/notifications`, ({ request }) =>
    authorized(request)
      ? HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 10, totalPages: 0 })
      : problem(401)),

  http.get(`${API}/tasks`, ({ request }) => {
    if (!authorized(request))
      return problem(401)
    const query = new URL(request.url).searchParams
    const status = query.get('status')
    const search = query.get('search')?.toLowerCase()
    const pageSize = Number(query.get('pageSize') ?? 20)
    const items = db.tasks.filter(
      (t) => (!status || t.status === status) && (!search || t.title.toLowerCase().includes(search)),
    )
    return HttpResponse.json({
      items: items.slice(0, pageSize),
      totalCount: items.length,
      page: 1,
      pageSize,
      totalPages: Math.ceil(items.length / pageSize),
    })
  }),

  http.get(`${API}/tasks/:id`, ({ request, params }) => {
    if (!authorized(request))
      return problem(401)
    const task = db.tasks.find((t) => t.id === params.id)
    return task ? HttpResponse.json({ ...task, statusHistory: [] }) : problem(404, { code: 'Task.NotFound' })
  }),

  http.patch(`${API}/tasks/:id/status`, async ({ request, params }) => {
    if (!authorized(request))
      return problem(401)
    const { status } = (await request.json()) as { status: TaskStatus }
    const task = db.tasks.find((t) => t.id === params.id)
    if (!task)
      return problem(404)
    if (!canTransition(task.status, status))
      return problem(422, { code: 'Task.InvalidTransition' })
    task.status = status
    task.version += 1
    return HttpResponse.json(task)
  }),
]

export const server = setupServer(...handlers)

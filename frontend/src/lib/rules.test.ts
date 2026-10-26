import { describe, expect, it } from 'vitest'
import { ApiError, NetworkError } from '@/api/http'
import { aTask, sessionFor, userIds } from '@/test/fixtures'
import type { TaskStatus } from '@/types/api'
import { formatDateOnly, utcToday } from './dates'
import { describeError, fieldErrorsOf } from './errors'
import { canAssign, canEditTask, canWriteTasks, isAdmin } from './permissions'
import { canTransition } from './transitions'

describe('canTransition (espelho de TaskItem.AllowedTransitions)', () => {
  it.each<[TaskStatus, TaskStatus, boolean]>([
    ['Pending', 'InProgress', true],
    ['Pending', 'Cancelled', true],
    ['Pending', 'Done', false],
    ['InProgress', 'Done', true],
    ['InProgress', 'Pending', false],
    ['Done', 'InProgress', false],
    ['Cancelled', 'Pending', false],
  ])('%s → %s = %s', (from, to, expected) => {
    expect(canTransition(from, to)).toBe(expected)
  })
})

describe('permissions', () => {
  it('lets only Admin, TechLead and Developer write tasks', () => {
    expect(canWriteTasks(sessionFor('Developer'))).toBe(true)
    expect(canWriteTasks(sessionFor('Viewer'))).toBe(false)
    expect(isAdmin(sessionFor('TechLead'))).toBe(false)
  })

  it('lets only the reporter or the assignee edit a task', () => {
    const task = aTask({ reporterId: userIds.TechLead, assigneeId: userIds.Developer })

    expect(canEditTask(sessionFor('TechLead'), task)).toBe(true)
    expect(canEditTask(sessionFor('Developer'), task)).toBe(true)
    expect(canEditTask(sessionFor('Admin'), task)).toBe(false)
  })

  it('lets a Developer assign only to themselves or unassign themselves', () => {
    const dev = sessionFor('Developer')
    const mine = aTask({ assigneeId: userIds.Developer })
    const someoneElses = aTask({ assigneeId: userIds.TechLead })

    expect(canAssign(dev, someoneElses, userIds.Developer)).toBe(true)
    expect(canAssign(dev, someoneElses, userIds.Admin)).toBe(false)
    expect(canAssign(dev, mine, null)).toBe(true)
    expect(canAssign(dev, someoneElses, null)).toBe(false)
  })

  it('blocks reassigning closed tasks for everyone', () => {
    expect(canAssign(sessionFor('Admin'), aTask({ status: 'Done' }), userIds.Developer)).toBe(false)
  })
})

describe('describeError', () => {
  it('translates known error codes before falling back to the HTTP status', () => {
    expect(describeError(new ApiError(422, { code: 'Task.InvalidTransition' }))).toBe('Essa mudança de status não é permitida.')
    expect(describeError(new ApiError(403, { code: 'Unknown.Code' }))).toBe('Você não tem permissão para esta ação.')
  })

  it('includes Retry-After on 429 and hides server details on 5xx', () => {
    expect(describeError(new ApiError(429, null, 30))).toContain('30s')
    expect(describeError(new ApiError(500, { detail: 'NullReferenceException at line 42' }))).not.toContain('Null')
  })

  it('tells an unavailable Api (proxy 502-504) apart from a server bug', () => {
    expect(describeError(new ApiError(502, null))).toContain('indisponível')
    expect(describeError(new ApiError(500, null))).toContain('Erro interno')
  })

  it('has a dedicated message for network failures', () => {
    expect(describeError(new NetworkError(new TypeError('fetch failed')))).toContain('Sem conexão')
  })

  it('maps FluentValidation PascalCase keys to form field names', () => {
    const error = new ApiError(400, { errors: { DueDate: ['Due date cannot be in the past.'] } })

    expect(fieldErrorsOf(error)).toEqual({ dueDate: 'Due date cannot be in the past.' })
  })
})

describe('dates', () => {
  it('formats DateOnly without going through the local timezone', () => {
    expect(formatDateOnly('2026-01-01')).toBe('01/01/2026')
  })

  it('uses the UTC day, like TimeProvider.GetUtcToday', () => {
    // 22h em Brasília (UTC-3) já é o dia seguinte em UTC.
    expect(utcToday(new Date('2026-10-26T22:00:00-03:00'))).toBe('2026-10-27')
  })
})

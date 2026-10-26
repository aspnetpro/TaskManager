import { act, renderHook, waitFor } from '@testing-library/react'
import { http, HttpResponse } from 'msw/http'
import { describe, expect, it } from 'vitest'
import { taskKeys } from '@/lib/queryKeys'
import { aTask, userIds } from '@/test/fixtures'
import { createWrapper, signInAs } from '@/test/render'
import { db, server } from '@/test/server'
import type { PagedResponse, TaskFilters, TaskItem } from '@/types/api'
import { moveTaskInList } from './moveTaskInList'
import { useTransitionTask } from './useTaskMutations'

const page = (items: TaskItem[]): PagedResponse<TaskItem> => ({
  items,
  totalCount: items.length,
  page: 1,
  pageSize: 100,
  totalPages: 1,
})

const key = (filters: TaskFilters) => taskKeys.list(filters)

describe('moveTaskInList', () => {
  const task = aTask({ status: 'Pending' })
  const moved = { ...task, status: 'InProgress' as const }

  it('removes the task from the source column', () => {
    const result = moveTaskInList(page([task]), key({ status: 'Pending' }), moved)
    expect(result.items).toEqual([])
    expect(result.totalCount).toBe(0)
  })

  it('adds the task to the target column', () => {
    const result = moveTaskInList(page([]), key({ status: 'InProgress' }), moved)
    expect(result.items).toEqual([moved])
    expect(result.totalCount).toBe(1)
  })

  it('updates in place when the list has no status filter', () => {
    const result = moveTaskInList(page([task]), key({}), moved)
    expect(result.items[0]?.status).toBe('InProgress')
  })
})

describe('useTransitionTask', () => {
  it('moves the card before the Api answers and assigns it to whoever started it', async () => {
    signInAs('Developer')
    const task = aTask({ status: 'Pending', assigneeId: null })
    db.tasks = [task]
    const { queryClient, wrapper } = createWrapper()
    queryClient.setQueryData(key({ status: 'Pending' }), page([task]))
    queryClient.setQueryData(key({ status: 'InProgress' }), page([]))
    // Api lenta: dá tempo de ver o estado otimista.
    server.use(http.patch('*/api/v1/tasks/:id/status', () => new Promise<never>(() => {})))

    const { result } = renderHook(() => useTransitionTask(), { wrapper })
    act(() => result.current.mutate({ task, status: 'InProgress' }))

    await waitFor(() => {
      const target = queryClient.getQueryData<PagedResponse<TaskItem>>(key({ status: 'InProgress' }))
      expect(target?.items[0]?.assigneeId).toBe(userIds.Developer)
    })
    expect(queryClient.getQueryData<PagedResponse<TaskItem>>(key({ status: 'Pending' }))?.items).toEqual([])
  })

  it('rolls every list back when the Api rejects the transition', async () => {
    signInAs('Developer')
    const task = aTask({ status: 'Pending' })
    const { queryClient, wrapper } = createWrapper()
    queryClient.setQueryData(key({ status: 'Pending' }), page([task]))
    queryClient.setQueryData(key({ status: 'InProgress' }), page([]))
    server.use(
      http.patch('*/api/v1/tasks/:id/status', () =>
        HttpResponse.json({ status: 422, code: 'Task.InvalidTransition' }, { status: 422 })),
      // Bloqueia o refetch do onSettled para o teste enxergar o rollback, não o dado novo.
      http.get('*/api/v1/tasks', () => new Promise<never>(() => {})),
    )

    const { result } = renderHook(() => useTransitionTask(), { wrapper })
    act(() => result.current.mutate({ task, status: 'InProgress' }))

    await waitFor(() => expect(result.current.isError).toBe(true))
    expect(queryClient.getQueryData<PagedResponse<TaskItem>>(key({ status: 'Pending' }))?.items).toEqual([task])
    expect(queryClient.getQueryData<PagedResponse<TaskItem>>(key({ status: 'InProgress' }))?.items).toEqual([])
  })
})

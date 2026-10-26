import { useCallback, useMemo } from 'react'
import { useSearchParams } from 'react-router'
import { PRIORITIES, STATUSES } from '@/lib/labels'
import type { SortDirection, TaskFilters, TaskPriority, TaskSortField, TaskStatus } from '@/types/api'

const SORT_FIELDS: readonly TaskSortField[] = ['CreatedAt', 'UpdatedAt', 'Priority', 'DueDate']
const DIRECTIONS: readonly SortDirection[] = ['Asc', 'Desc']

const oneOf = <T extends string>(allowed: readonly T[], value: string | null): T | undefined =>
  allowed.includes(value as T) ? (value as T) : undefined

/** URL inválida (?priority=Urgente) vira "sem filtro", não um 400 da Api. */
function parse(params: URLSearchParams): TaskFilters {
  const page = Number(params.get('page'))
  return {
    status: oneOf<TaskStatus>(STATUSES, params.get('status')),
    priority: oneOf<TaskPriority>(PRIORITIES, params.get('priority')),
    assigneeId: params.get('assigneeId') || undefined,
    search: params.get('search') || undefined,
    overdue: params.get('overdue') === 'true' || undefined,
    includeDeleted: params.get('includeDeleted') === 'true' || undefined,
    sortBy: oneOf(SORT_FIELDS, params.get('sortBy')),
    sortDirection: oneOf(DIRECTIONS, params.get('sortDirection')),
    page: Number.isInteger(page) && page > 1 ? page : undefined,
  }
}

/**
 * Filtros moram na URL: F5 mantém, link compartilhado abre igual, voltar desfaz.
 * Qualquer mudança que não seja de página volta para a página 1.
 */
export function useUrlFilters(): [TaskFilters, (patch: Partial<TaskFilters>) => void] {
  const [params, setParams] = useSearchParams()
  const filters = useMemo(() => parse(params), [params])

  const update = useCallback(
    (patch: Partial<TaskFilters>) =>
      setParams(
        (previous) => {
          const next = new URLSearchParams(previous)
          for (const [key, value] of Object.entries(patch)) {
            if (value === undefined || value === '' || value === false)
              next.delete(key)
            else
              next.set(key, String(value))
          }
          if (!('page' in patch))
            next.delete('page')
          return next
        },
        { replace: true },
      ),
    [setParams],
  )

  return [filters, update]
}

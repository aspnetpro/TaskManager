import { keepPreviousData, useQueries, useQuery } from '@tanstack/react-query'
import { tasksApi } from '@/api/tasks'
import { STATUSES } from '@/lib/labels'
import { taskKeys } from '@/lib/queryKeys'
import type { PagedResponse, TaskFilters, TaskItem, TaskStatus } from '@/types/api'

export function useTaskList(filters: TaskFilters) {
  return useQuery({
    queryKey: taskKeys.list(filters),
    queryFn: ({ signal }) => tasksApi.list(filters, signal),
    // Trocar de página mantém a tabela anterior na tela até a próxima chegar.
    placeholderData: keepPreviousData,
  })
}

export function useTask(id: string | undefined) {
  return useQuery({
    queryKey: taskKeys.detail(id ?? ''),
    queryFn: ({ signal }) => tasksApi.get(id ?? '', signal),
    enabled: id !== undefined,
  })
}

/** Máximo da Api (GetTasksQuery.MaxPageSize). Coluna com mais que isso mostra "N de M". */
export const BOARD_COLUMN_SIZE = 100

export type BoardFilters = Omit<TaskFilters, 'status' | 'page' | 'pageSize' | 'includeDeleted'>

export interface BoardColumnData {
  status: TaskStatus
  tasks: TaskItem[]
  totalCount: number
  isPending: boolean
  isFetching: boolean
  error: Error | null
  refetch: () => void
}

/**
 * Uma query por coluna, não uma query para o board inteiro: cada coluna
 * pagina sozinha e uma coluna lotada de "Concluída" não esconde "A fazer".
 */
export function useBoardColumns(filters: BoardFilters): BoardColumnData[] {
  return useQueries({
    queries: STATUSES.map((status) => {
      const columnFilters: TaskFilters = {
        sortBy: 'Priority',
        sortDirection: 'Desc',
        ...filters,
        status,
        pageSize: BOARD_COLUMN_SIZE,
      }
      return {
        queryKey: taskKeys.list(columnFilters),
        queryFn: ({ signal }: { signal: AbortSignal }) => tasksApi.list(columnFilters, signal),
        // Filtrar não pisca o board em skeleton: os cards atuais ficam até os novos chegarem.
        placeholderData: keepPreviousData,
      }
    }),
    combine: (results) =>
      results.map((result, index): BoardColumnData => ({
        status: STATUSES[index] ?? 'Pending',
        tasks: (result.data as PagedResponse<TaskItem> | undefined)?.items ?? [],
        totalCount: result.data?.totalCount ?? 0,
        isPending: result.isPending,
        isFetching: result.isFetching,
        error: result.error,
        refetch: () => void result.refetch(),
      })),
  })
}

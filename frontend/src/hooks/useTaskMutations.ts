import { useMutation, useQueryClient, type QueryClient } from '@tanstack/react-query'
import { tasksApi } from '@/api/tasks'
import { isStatus } from '@/lib/errors'
import { taskKeys } from '@/lib/queryKeys'
import { useSession } from '@/stores/authStore'
import type { CreateTaskInput, PagedResponse, TaskDetails, TaskItem, TaskStatus, UpdateTaskInput } from '@/types/api'
import { moveTaskInList } from './moveTaskInList'

/** Response de escrita é TaskResponse (sem histórico): mescla no detalhe e refaz as listas. */
function applyWrite(queryClient: QueryClient, task: TaskItem) {
  queryClient.setQueryData<TaskDetails>(taskKeys.detail(task.id), (old) => (old ? { ...old, ...task } : old))
  return queryClient.invalidateQueries({ queryKey: taskKeys.lists() })
}

export function useCreateTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: CreateTaskInput) => tasksApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: taskKeys.lists() }),
    meta: { silentError: true },
  })
}

export function useUpdateTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ task, input }: { task: TaskItem; input: UpdateTaskInput }) =>
      tasksApi.update(task.id, task.version, input),
    onSuccess: (task) => applyWrite(queryClient, task),
    onError: (error, { task }) => {
      // 409: a versão local ficou velha. Recarrega para o próximo submit ir com a atual.
      if (isStatus(error, 409))
        return queryClient.invalidateQueries({ queryKey: taskKeys.detail(task.id) })
    },
    meta: { silentError: true },
  })
}

export function useAssignTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, assigneeId }: { id: string; assigneeId: string | null }) => tasksApi.assign(id, assigneeId),
    onSuccess: (task) => applyWrite(queryClient, task),
  })
}

/**
 * Optimistic update do board: o card muda de coluna antes da Api responder.
 * Falhou (422, rede)? Volta o snapshot de todas as listas e o toast global explica.
 */
export function useTransitionTask() {
  const queryClient = useQueryClient()
  const session = useSession()

  return useMutation({
    mutationFn: ({ task, status }: { task: TaskItem; status: TaskStatus }) => tasksApi.transition(task.id, status),

    onMutate: async ({ task, status }) => {
      // Um refetch em voo chegaria depois e sobrescreveria o otimista com dado velho.
      await queryClient.cancelQueries({ queryKey: taskKeys.lists() })
      const snapshot = queryClient.getQueriesData<PagedResponse<TaskItem>>({ queryKey: taskKeys.lists() })

      // US-005: iniciar sem responsável atribui a quem iniciou — o card já aparece com o avatar certo.
      const assigneeId = status === 'InProgress' ? (task.assigneeId ?? session?.user.id ?? null) : task.assigneeId
      const moved: TaskItem = { ...task, status, assigneeId }

      for (const [key, data] of snapshot) {
        if (data)
          queryClient.setQueryData(key, moveTaskInList(data, key, moved))
      }
      return { snapshot }
    },

    onError: (_error, _variables, context) => {
      for (const [key, data] of context?.snapshot ?? [])
        queryClient.setQueryData(key, data)
    },

    onSettled: (_data, _error, { task }) =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: taskKeys.lists() }),
        queryClient.invalidateQueries({ queryKey: taskKeys.detail(task.id) }),
      ]),
  })
}

export function useDeleteTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => tasksApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: taskKeys.all }),
  })
}

export function useRestoreTask() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => tasksApi.restore(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: taskKeys.all }),
  })
}

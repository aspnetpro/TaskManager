import type { QueryKey } from '@tanstack/react-query'
import type { PagedResponse, TaskFilters, TaskItem } from '@/types/api'

/**
 * Aplica a nova versão de uma tarefa a UMA lista em cache, respeitando o
 * filtro de status da lista: sai da coluna de origem, entra na de destino.
 * Os outros filtros (busca, prioridade) não são reavaliados aqui — o refetch
 * do onSettled corrige qualquer diferença em milissegundos.
 */
export function moveTaskInList(
  data: PagedResponse<TaskItem>,
  queryKey: QueryKey,
  moved: TaskItem,
): PagedResponse<TaskItem> {
  const filters = (queryKey[2] ?? {}) as TaskFilters
  const contains = data.items.some((t) => t.id === moved.id)

  if (filters.status === undefined)
    return contains ? { ...data, items: data.items.map((t) => (t.id === moved.id ? moved : t)) } : data

  if (filters.status === moved.status)
    return contains ? data : { ...data, items: [moved, ...data.items], totalCount: data.totalCount + 1 }

  return contains
    ? { ...data, items: data.items.filter((t) => t.id !== moved.id), totalCount: data.totalCount - 1 }
    : data
}

import type { NotificationFilters } from '@/api/workspace'
import type { TaskFilters } from '@/types/api'

// Hierarquia de chaves: invalidar taskKeys.all derruba listas e detalhes;
// taskKeys.lists() só as listas. Chave montada à mão em cada hook vira bug.
export const taskKeys = {
  all: ['tasks'] as const,
  lists: () => [...taskKeys.all, 'list'] as const,
  list: (filters: TaskFilters) => [...taskKeys.lists(), filters] as const,
  details: () => [...taskKeys.all, 'detail'] as const,
  detail: (id: string) => [...taskKeys.details(), id] as const,
}

export const memberKeys = {
  all: ['members'] as const,
}

export const notificationKeys = {
  all: ['notifications'] as const,
  list: (filters: NotificationFilters) => [...notificationKeys.all, filters] as const,
}

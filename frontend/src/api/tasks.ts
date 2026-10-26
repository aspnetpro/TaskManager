import type {
  CreateTaskInput,
  PagedResponse,
  TaskDetails,
  TaskFilters,
  TaskItem,
  TaskStatus,
  UpdateTaskInput,
} from '@/types/api'
import { request } from './http'

export const tasksApi = {
  list: (filters: TaskFilters, signal?: AbortSignal) =>
    request<PagedResponse<TaskItem>>('/tasks', { query: filters, signal }),

  get: (id: string, signal?: AbortSignal) => request<TaskDetails>(`/tasks/${id}`, { signal }),

  create: (input: CreateTaskInput) => request<TaskItem>('/tasks', { method: 'POST', body: input }),

  /** Concorrência otimista: sem If-Match a Api responde 428; versão velha, 409. */
  update: (id: string, version: number, input: UpdateTaskInput) =>
    request<TaskItem>(`/tasks/${id}`, {
      method: 'PATCH',
      body: input,
      headers: { 'If-Match': `"${version}"` },
    }),

  transition: (id: string, status: TaskStatus) =>
    request<TaskItem>(`/tasks/${id}/status`, { method: 'PATCH', body: { status } }),

  assign: (id: string, assigneeId: string | null) =>
    request<TaskItem>(`/tasks/${id}/assignee`, { method: 'PUT', body: { assigneeId } }),

  remove: (id: string) => request<void>(`/tasks/${id}`, { method: 'DELETE' }),

  restore: (id: string) => request<TaskItem>(`/tasks/${id}/restore`, { method: 'POST' }),
}

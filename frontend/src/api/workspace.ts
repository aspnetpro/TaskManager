import type { AppNotification, Member, PagedResponse } from '@/types/api'
import { request } from './http'

export const membersApi = {
  list: (signal?: AbortSignal) => request<Member[]>('/workspace/members', { signal }),
}

export interface NotificationFilters {
  unreadOnly?: boolean
  page?: number
  pageSize?: number
}

export const notificationsApi = {
  list: (filters: NotificationFilters, signal?: AbortSignal) =>
    request<PagedResponse<AppNotification>>('/notifications', { query: filters, signal }),

  markRead: (id: string) => request<void>(`/notifications/${id}/read`, { method: 'POST' }),
}

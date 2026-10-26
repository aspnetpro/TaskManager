import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo } from 'react'
import { membersApi, notificationsApi } from '@/api/workspace'
import { memberKeys, notificationKeys } from '@/lib/queryKeys'
import type { Member } from '@/types/api'

export function useMembers() {
  return useQuery({
    queryKey: memberKeys.all,
    queryFn: ({ signal }) => membersApi.list(signal),
    // Membros mudam raramente; o board consulta isso em todo card.
    staleTime: 5 * 60_000,
  })
}

/** userId → Member, para resolver assigneeId/reporterId em nome. */
export function useMemberLookup(): Map<string, Member> {
  const { data } = useMembers()
  return useMemo(() => new Map((data ?? []).map((m) => [m.userId, m])), [data])
}

const unreadFilters = { unreadOnly: true, pageSize: 10 }

export function useUnreadNotifications() {
  return useQuery({
    queryKey: notificationKeys.list(unreadFilters),
    queryFn: ({ signal }) => notificationsApi.list(unreadFilters, signal),
    // Polling simples. SignalR entra quando "tempo real" virar requisito, não antes.
    refetchInterval: 30_000,
  })
}

export function useMarkNotificationRead() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => notificationsApi.markRead(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: notificationKeys.all }),
  })
}

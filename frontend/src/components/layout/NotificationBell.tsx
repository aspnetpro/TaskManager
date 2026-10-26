import { Bell } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { useMarkNotificationRead, useMemberLookup, useUnreadNotifications } from '@/hooks/useWorkspace'
import { formatDateTime } from '@/lib/dates'
import { statusLabel } from '@/lib/labels'
import type { AppNotification, TaskStatus } from '@/types/api'

function describe(n: AppNotification, actor: string): string {
  if (n.type === 'TaskAssigned')
    return `${actor} atribuiu “${n.taskTitle}” a você`
  const status = statusLabel[n.detail as TaskStatus] ?? n.detail
  return `${actor} moveu “${n.taskTitle}” para ${status}`
}

export function NotificationBell() {
  const [open, setOpen] = useState(false)
  const { data } = useUnreadNotifications()
  const markRead = useMarkNotificationRead()
  const members = useMemberLookup()
  const navigate = useNavigate()

  const unread = data?.totalCount ?? 0

  function openTask(n: AppNotification) {
    markRead.mutate(n.id)
    setOpen(false)
    void navigate(`/board/tasks/${n.taskId}`)
  }

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-expanded={open}
        aria-label={unread ? `Notificações: ${unread} não lidas` : 'Notificações'}
        className="relative rounded-md p-2 text-navy-100 hover:bg-navy-700"
      >
        <Bell className="size-5" aria-hidden />
        {unread > 0 && (
          <span className="absolute -right-0.5 -top-0.5 min-w-4 rounded-full bg-red-500 px-1 text-center text-[10px] font-bold text-white">
            {unread > 9 ? '9+' : unread}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 z-20 mt-2 w-80 overflow-hidden rounded-lg border border-slate-200 bg-white text-slate-800 shadow-lg">
          <p className="border-b border-slate-100 px-4 py-2 text-sm font-semibold">Não lidas</p>
          {unread === 0 ? (
            <p className="px-4 py-6 text-center text-sm text-slate-500">Nada novo por aqui.</p>
          ) : (
            <ul className="max-h-96 divide-y divide-slate-100 overflow-y-auto">
              {data?.items.map((n) => (
                <li key={n.id}>
                  <button type="button" onClick={() => openTask(n)} className="w-full px-4 py-3 text-left hover:bg-slate-50">
                    <p className="text-sm">{describe(n, members.get(n.actorId)?.fullName ?? 'Alguém')}</p>
                    <p className="mt-0.5 text-xs text-slate-500">{formatDateTime(n.createdAt)}</p>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}

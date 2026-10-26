import type { TaskPriority, TaskStatus } from '@/types/api'

export const priorityColor: Record<TaskPriority, string> = {
  Low: 'bg-slate-100 text-slate-700',
  Medium: 'bg-sky-100 text-sky-800',
  High: 'bg-amber-100 text-amber-800',
  Critical: 'bg-red-100 text-red-800',
}

export const statusColor: Record<TaskStatus, string> = {
  Pending: 'bg-slate-400',
  InProgress: 'bg-accent-500',
  Done: 'bg-emerald-500',
  Cancelled: 'bg-slate-300',
}

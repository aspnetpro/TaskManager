import { priorityLabel, statusLabel } from '@/lib/labels'
import { cn } from '@/lib/cn'
import { priorityColor, statusColor } from '@/lib/styles'
import type { TaskPriority, TaskStatus } from '@/types/api'

const base = 'inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium'

export function PriorityBadge({ priority }: { priority: TaskPriority }) {
  return <span className={cn(base, priorityColor[priority])}>{priorityLabel[priority]}</span>
}

export function StatusBadge({ status }: { status: TaskStatus }) {
  return (
    <span className={cn(base, 'bg-slate-100 text-slate-700')}>
      <span className={cn('size-2 rounded-full', statusColor[status])} aria-hidden />
      {statusLabel[status]}
    </span>
  )
}

export function Avatar({ name, className }: { name: string; className?: string }) {
  const initials = name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('')

  return (
    <span
      title={name}
      className={cn(
        'inline-flex size-6 items-center justify-center rounded-full bg-navy-100 text-[10px] font-semibold text-navy-800',
        className,
      )}
    >
      {initials}
    </span>
  )
}

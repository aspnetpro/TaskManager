import { useDraggable } from '@dnd-kit/core'
import { CalendarClock } from 'lucide-react'
import { Link, useLocation } from 'react-router'
import { Avatar, PriorityBadge } from '@/components/ui/Badge'
import { useMemberLookup } from '@/hooks/useWorkspace'
import { cn } from '@/lib/cn'
import { formatDateOnly } from '@/lib/dates'
import { statusLabel } from '@/lib/labels'
import { nextStatuses } from '@/lib/transitions'
import type { TaskItem, TaskStatus } from '@/types/api'

interface TaskCardProps {
  task: TaskItem
  canMove: boolean
  onMove: (task: TaskItem, to: TaskStatus) => void
}

export function TaskCard({ task, canMove, onMove }: TaskCardProps) {
  const { attributes, listeners, setNodeRef, isDragging } = useDraggable({
    id: task.id,
    data: { task },
    disabled: !canMove || nextStatuses(task.status).length === 0,
  })

  // O card original fica parado e esmaecido; quem anda com o mouse é o DragOverlay.
  return (
    <article
      ref={setNodeRef}
      {...attributes}
      {...listeners}
      aria-roledescription="card arrastável"
      className={cn(
        'rounded-lg border border-slate-200 bg-white p-3 shadow-xs transition-shadow hover:shadow-md',
        canMove && 'cursor-grab active:cursor-grabbing',
        isDragging && 'opacity-40',
      )}
    >
      <TaskCardBody task={task} />
      {canMove && <MobileMoveMenu task={task} onMove={onMove} />}
    </article>
  )
}

export function TaskCardBody({ task }: { task: TaskItem }) {
  const members = useMemberLookup()
  const assignee = task.assigneeId ? members.get(task.assigneeId) : undefined
  // Mantém a query string: os filtros do board moram nela.
  const { search } = useLocation()

  return (
    <>
      <Link to={{ pathname: `/board/tasks/${task.id}`, search }} className="line-clamp-2 text-sm font-medium text-slate-900 hover:underline">
        {task.title}
      </Link>
      <div className="mt-2 flex items-center gap-2">
        <PriorityBadge priority={task.priority} />
        {task.dueDate && (
          <span
            className={cn(
              'inline-flex items-center gap-1 text-xs',
              task.isOverdue ? 'font-semibold text-red-600' : task.isDueSoon ? 'text-amber-700' : 'text-slate-500',
            )}
          >
            <CalendarClock className="size-3.5" aria-hidden />
            {formatDateOnly(task.dueDate)}
            {task.isOverdue && <span className="sr-only">(atrasada)</span>}
          </span>
        )}
        {assignee && <Avatar name={assignee.fullName} className="ml-auto" />}
      </div>
    </>
  )
}

/** Mobile não tem drag and drop confiável: um select com as transições válidas no lugar. */
function MobileMoveMenu({ task, onMove }: { task: TaskItem; onMove: TaskCardProps['onMove'] }) {
  const targets = nextStatuses(task.status)
  if (targets.length === 0)
    return null

  return (
    <label className="mt-2 block md:hidden">
      <span className="sr-only">Mover “{task.title}” para</span>
      <select
        value=""
        onChange={(e) => onMove(task, e.target.value as TaskStatus)}
        // O select fica dentro do card arrastável: não deixa o toque virar drag.
        onPointerDown={(e) => e.stopPropagation()}
        className="h-8 w-full rounded-md border border-slate-300 bg-white px-2 text-xs"
      >
        <option value="" disabled>
          Mover para…
        </option>
        {targets.map((s) => (
          <option key={s} value={s}>
            {statusLabel[s]}
          </option>
        ))}
      </select>
    </label>
  )
}

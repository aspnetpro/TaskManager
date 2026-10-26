import { useDroppable } from '@dnd-kit/core'
import { EmptyState, ErrorBanner, Skeleton } from '@/components/ui/Feedback'
import type { BoardColumnData } from '@/hooks/useTasks'
import { cn } from '@/lib/cn'
import { statusLabel } from '@/lib/labels'
import { statusColor } from '@/lib/styles'
import { canTransition } from '@/lib/transitions'
import type { TaskItem, TaskStatus } from '@/types/api'
import { TaskCard } from './TaskCard'

interface BoardColumnProps {
  column: BoardColumnData
  dragging: TaskItem | null
  canMove: boolean
  onMove: (task: TaskItem, to: TaskStatus) => void
  /** Mobile: só a coluna da aba selecionada aparece. */
  hiddenOnMobile: boolean
}

export function BoardColumn({ column, dragging, canMove, onMove, hiddenOnMobile }: BoardColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: column.status })
  const { status, tasks, totalCount } = column

  // Durante o drag, coluna que não aceita a transição fica apagada.
  const isTarget = dragging !== null && dragging.status !== status
  const accepts = isTarget && canTransition(dragging.status, status)

  return (
    <section
      ref={setNodeRef}
      aria-labelledby={`column-${status}`}
      className={cn(
        'flex min-h-64 snap-start flex-col rounded-xl bg-slate-100 p-3 transition-colors',
        hiddenOnMobile ? 'hidden md:flex' : 'flex',
        isOver && accepts && 'bg-accent-500/15 ring-2 ring-accent-500',
        isTarget && !accepts && 'opacity-50',
      )}
    >
      <header className="mb-3 flex items-center gap-2">
        <span className={cn('size-2.5 rounded-full', statusColor[status])} aria-hidden />
        <h2 id={`column-${status}`} className="text-sm font-semibold text-slate-700">
          {statusLabel[status]}
        </h2>
        <span className="ml-auto rounded-full bg-white px-2 text-xs font-medium text-slate-600" aria-label={`${totalCount} tarefas`}>
          {totalCount}
        </span>
      </header>

      <div className="flex flex-1 flex-col gap-2">
        {column.isPending ? (
          [0, 1, 2].map((i) => <Skeleton key={i} className="h-20" />)
        ) : column.error ? (
          <ErrorBanner error={column.error} onRetry={column.refetch} />
        ) : tasks.length === 0 ? (
          <EmptyState title="Nenhuma tarefa aqui" />
        ) : (
          tasks.map((task) => <TaskCard key={task.id} task={task} canMove={canMove} onMove={onMove} />)
        )}
      </div>

      {totalCount > tasks.length && (
        <p className="mt-2 text-center text-xs text-slate-500">
          Mostrando {tasks.length} de {totalCount}. Use os filtros ou a Lista.
        </p>
      )}
    </section>
  )
}

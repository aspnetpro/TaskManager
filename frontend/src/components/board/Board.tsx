import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from '@dnd-kit/core'
import { useState } from 'react'
import { toast } from 'sonner'
import type { BoardColumnData } from '@/hooks/useTasks'
import { useTransitionTask } from '@/hooks/useTaskMutations'
import { cn } from '@/lib/cn'
import { statusLabel } from '@/lib/labels'
import { canTransition } from '@/lib/transitions'
import type { TaskItem, TaskStatus } from '@/types/api'
import { boardAnnouncements, screenReaderInstructions } from './announcements'
import { BoardColumn } from './BoardColumn'
import { TaskCardBody } from './TaskCard'

const taskOf = (event: DragStartEvent | DragEndEvent) => event.active.data.current?.task as TaskItem | undefined

export function Board({ columns, canMove }: { columns: BoardColumnData[]; canMove: boolean }) {
  const transition = useTransitionTask()
  const [dragging, setDragging] = useState<TaskItem | null>(null)
  const [mobileStatus, setMobileStatus] = useState<TaskStatus>('Pending')

  const sensors = useSensors(
    // 6px de tolerância: clique no título abre o detalhe em vez de iniciar um drag.
    useSensor(PointerSensor, { activationConstraint: { distance: 6 } }),
    useSensor(KeyboardSensor),
  )

  function move(task: TaskItem, to: TaskStatus) {
    if (task.status === to)
      return
    if (!canTransition(task.status, to)) {
      toast.error(`Não dá para mover de “${statusLabel[task.status]}” para “${statusLabel[to]}”.`)
      return
    }
    transition.mutate({ task, status: to })
  }

  function handleDragEnd(event: DragEndEvent) {
    setDragging(null)
    const task = taskOf(event)
    if (task && event.over)
      move(task, event.over.id as TaskStatus)
  }

  return (
    <DndContext
      sensors={sensors}
      onDragStart={(event) => setDragging(taskOf(event) ?? null)}
      onDragEnd={handleDragEnd}
      onDragCancel={() => setDragging(null)}
      accessibility={{ announcements: boardAnnouncements, screenReaderInstructions }}
    >
      <div role="tablist" aria-label="Status" className="mb-3 flex gap-1 overflow-x-auto md:hidden">
        {columns.map(({ status, totalCount }) => (
          <button
            key={status}
            type="button"
            role="tab"
            aria-selected={mobileStatus === status}
            onClick={() => setMobileStatus(status)}
            className={cn(
              'shrink-0 rounded-full px-3 py-1 text-sm',
              mobileStatus === status ? 'bg-navy-800 text-white' : 'bg-white text-slate-700',
            )}
          >
            {statusLabel[status]} ({totalCount})
          </button>
        ))}
      </div>

      {/* Desktop: 4 colunas. Tablet: scroll horizontal com snap. Mobile: uma coluna por aba. */}
      <div className="grid gap-4 md:snap-x md:auto-cols-[minmax(17rem,1fr)] md:grid-flow-col md:overflow-x-auto md:pb-2 lg:grid-flow-row lg:grid-cols-4 lg:overflow-visible">
        {columns.map((column) => (
          <BoardColumn
            key={column.status}
            column={column}
            dragging={dragging}
            canMove={canMove}
            onMove={move}
            hiddenOnMobile={column.status !== mobileStatus}
          />
        ))}
      </div>

      <DragOverlay>
        {dragging && (
          <div className="rotate-2 rounded-lg border border-slate-200 bg-white p-3 shadow-xl">
            <TaskCardBody task={dragging} />
          </div>
        )}
      </DragOverlay>
    </DndContext>
  )
}

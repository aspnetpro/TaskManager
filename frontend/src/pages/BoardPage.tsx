import { Plus } from 'lucide-react'
import { useState } from 'react'
import { Outlet } from 'react-router'
import { Board } from '@/components/board/Board'
import { CreateTaskDialog } from '@/components/tasks/CreateTaskDialog'
import { TaskFilterBar } from '@/components/tasks/TaskFilterBar'
import { Button } from '@/components/ui/Button'
import { Spinner } from '@/components/ui/Spinner'
import { useBoardColumns, type BoardFilters } from '@/hooks/useTasks'
import { useUrlFilters } from '@/hooks/useUrlFilters'
import { canWriteTasks } from '@/lib/permissions'
import { useSession } from '@/stores/authStore'

export function BoardPage() {
  const session = useSession()
  const [filters, setFilters] = useUrlFilters()
  const [creating, setCreating] = useState(false)

  const boardFilters: BoardFilters = {
    priority: filters.priority,
    assigneeId: filters.assigneeId,
    search: filters.search,
    overdue: filters.overdue,
  }
  const columns = useBoardColumns(boardFilters)
  // Refetch em background não troca o board por skeleton: só um spinner discreto.
  const refreshing = columns.some((c) => c.isFetching && !c.isPending)
  const canWrite = canWriteTasks(session)

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-xl font-semibold">Board</h1>
        {refreshing && <Spinner className="size-4 text-slate-400" label="Atualizando" />}
        <div className="ml-auto flex flex-wrap items-center gap-2">
          <TaskFilterBar filters={filters} onChange={setFilters} />
          {canWrite && (
            <Button onClick={() => setCreating(true)}>
              <Plus className="size-4" aria-hidden />
              Nova tarefa
            </Button>
          )}
        </div>
      </div>

      <Board columns={columns} canMove={canWrite} />

      {creating && <CreateTaskDialog onClose={() => setCreating(false)} />}
      <Outlet />
    </div>
  )
}

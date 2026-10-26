import { Link, Outlet, useLocation } from 'react-router'
import { TaskFilterBar } from '@/components/tasks/TaskFilterBar'
import { Avatar, PriorityBadge, StatusBadge } from '@/components/ui/Badge'
import { Button } from '@/components/ui/Button'
import { EmptyState, ErrorBanner, Skeleton } from '@/components/ui/Feedback'
import { useTaskList } from '@/hooks/useTasks'
import { useUrlFilters } from '@/hooks/useUrlFilters'
import { useMemberLookup } from '@/hooks/useWorkspace'
import { formatDateOnly } from '@/lib/dates'
import { STATUSES, statusLabel } from '@/lib/labels'
import { isAdmin } from '@/lib/permissions'
import { useSession } from '@/stores/authStore'
import type { TaskSortField, TaskStatus } from '@/types/api'

const PAGE_SIZE = 20

const sortOptions: { value: TaskSortField; label: string }[] = [
  { value: 'CreatedAt', label: 'Mais recentes' },
  { value: 'UpdatedAt', label: 'Atualizadas' },
  { value: 'Priority', label: 'Prioridade' },
  { value: 'DueDate', label: 'Entrega' },
]

export function TaskListPage() {
  const session = useSession()
  const members = useMemberLookup()
  const { search } = useLocation()
  const [filters, setFilters] = useUrlFilters()
  const { data, error, isPending, isPlaceholderData, refetch } = useTaskList({ ...filters, pageSize: PAGE_SIZE })
  const page = filters.page ?? 1

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-3">
        <h1 className="text-xl font-semibold">Tarefas</h1>
        <div className="ml-auto">
          <TaskFilterBar filters={filters} onChange={setFilters}>
            <select
              aria-label="Status"
              className="h-9 rounded-md border border-slate-300 bg-white px-2 text-sm"
              value={filters.status ?? ''}
              onChange={(e) => setFilters({ status: (e.target.value || undefined) as TaskStatus | undefined })}
            >
              <option value="">Todos os status</option>
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {statusLabel[s]}
                </option>
              ))}
            </select>
            <select
              aria-label="Ordenar por"
              className="h-9 rounded-md border border-slate-300 bg-white px-2 text-sm"
              value={filters.sortBy ?? 'CreatedAt'}
              onChange={(e) => setFilters({ sortBy: e.target.value as TaskSortField })}
            >
              {sortOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
            {isAdmin(session) && (
              <label className="flex items-center gap-1.5 text-sm text-slate-700">
                <input
                  type="checkbox"
                  checked={filters.includeDeleted ?? false}
                  onChange={(e) => setFilters({ includeDeleted: e.target.checked || undefined })}
                />
                Incluir lixeira
              </label>
            )}
          </TaskFilterBar>
        </div>
      </div>

      {isPending ? (
        <Skeleton className="h-64" />
      ) : error ? (
        <ErrorBanner error={error} onRetry={() => void refetch()} />
      ) : data.items.length === 0 ? (
        <EmptyState title="Nenhuma tarefa com esses filtros." />
      ) : (
        <div className={isPlaceholderData ? 'opacity-60 transition-opacity' : undefined}>
          <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                <tr>
                  <th className="px-4 py-2 font-medium">Título</th>
                  <th className="px-4 py-2 font-medium">Status</th>
                  <th className="px-4 py-2 font-medium">Prioridade</th>
                  <th className="px-4 py-2 font-medium">Responsável</th>
                  <th className="px-4 py-2 font-medium">Entrega</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {data.items.map((task) => {
                  const assignee = task.assigneeId ? members.get(task.assigneeId) : undefined
                  return (
                    <tr key={task.id} className={task.isDeleted ? 'text-slate-400 line-through' : undefined}>
                      <td className="px-4 py-2">
                        <Link to={{ pathname: `/tasks/${task.id}`, search }} className="font-medium hover:underline">
                          {task.title}
                        </Link>
                      </td>
                      <td className="px-4 py-2"><StatusBadge status={task.status} /></td>
                      <td className="px-4 py-2"><PriorityBadge priority={task.priority} /></td>
                      <td className="px-4 py-2">
                        {assignee ? (
                          <span className="inline-flex items-center gap-2"><Avatar name={assignee.fullName} />{assignee.fullName}</span>
                        ) : '—'}
                      </td>
                      <td className={task.isOverdue ? 'px-4 py-2 font-semibold text-red-600' : 'px-4 py-2'}>
                        {task.dueDate ? formatDateOnly(task.dueDate) : '—'}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <nav aria-label="Paginação" className="mt-3 flex items-center justify-between text-sm text-slate-600">
            <span>{data.totalCount} tarefas · página {data.page} de {Math.max(data.totalPages, 1)}</span>
            <div className="flex gap-2">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setFilters({ page: page - 1 })}>
                Anterior
              </Button>
              <Button variant="secondary" size="sm" disabled={page >= data.totalPages} onClick={() => setFilters({ page: page + 1 })}>
                Próxima
              </Button>
            </div>
          </nav>
        </div>
      )}

      <Outlet />
    </div>
  )
}

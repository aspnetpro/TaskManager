import { Search } from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { useMembers } from '@/hooks/useWorkspace'
import { PRIORITIES, priorityLabel } from '@/lib/labels'
import type { TaskFilters, TaskPriority } from '@/types/api'

interface TaskFilterBarProps {
  filters: TaskFilters
  onChange: (patch: Partial<TaskFilters>) => void
  children?: ReactNode
}

const selectClass = 'h-9 rounded-md border border-slate-300 bg-white px-2 text-sm'

export function TaskFilterBar({ filters, onChange, children }: TaskFilterBarProps) {
  const { data: members } = useMembers()
  const [search, setSearch] = useState(filters.search ?? '')

  // Debounce: digitar "refactor" não pode disparar 8 requests.
  useEffect(() => {
    if (search === (filters.search ?? ''))
      return
    const timer = setTimeout(() => onChange({ search: search.trim() || undefined }), 300)
    return () => clearTimeout(timer)
  }, [search, filters.search, onChange])

  return (
    <search className="flex flex-wrap items-center gap-2">
      <label className="relative">
        <span className="sr-only">Buscar tarefas</span>
        <Search className="pointer-events-none absolute left-2.5 top-2.5 size-4 text-slate-400" aria-hidden />
        <input
          type="search"
          value={search}
          maxLength={200}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por título ou descrição"
          className="h-9 w-64 rounded-md border border-slate-300 bg-white pl-8 pr-2 text-sm"
        />
      </label>

      <label>
        <span className="sr-only">Prioridade</span>
        <select
          className={selectClass}
          value={filters.priority ?? ''}
          onChange={(e) => onChange({ priority: (e.target.value || undefined) as TaskPriority | undefined })}
        >
          <option value="">Todas as prioridades</option>
          {PRIORITIES.map((p) => (
            <option key={p} value={p}>
              {priorityLabel[p]}
            </option>
          ))}
        </select>
      </label>

      <label>
        <span className="sr-only">Responsável</span>
        <select
          className={selectClass}
          value={filters.assigneeId ?? ''}
          onChange={(e) => onChange({ assigneeId: e.target.value || undefined })}
        >
          <option value="">Todos os responsáveis</option>
          {members?.map((m) => (
            <option key={m.userId} value={m.userId}>
              {m.fullName}
            </option>
          ))}
        </select>
      </label>

      <label className="flex items-center gap-1.5 text-sm text-slate-700">
        <input
          type="checkbox"
          checked={filters.overdue ?? false}
          onChange={(e) => onChange({ overdue: e.target.checked || undefined })}
        />
        Só atrasadas
      </label>

      {children}
    </search>
  )
}

import { useState } from 'react'
import { toast } from 'sonner'
import { Button } from '@/components/ui/Button'
import { useAssignTask, useDeleteTask, useRestoreTask, useTransitionTask } from '@/hooks/useTaskMutations'
import { useMembers } from '@/hooks/useWorkspace'
import { statusLabel } from '@/lib/labels'
import { canAssign, canWriteTasks, isAdmin } from '@/lib/permissions'
import { nextStatuses } from '@/lib/transitions'
import { useSession } from '@/stores/authStore'
import type { TaskItem } from '@/types/api'

export function StatusActions({ task }: { task: TaskItem }) {
  const session = useSession()
  const transition = useTransitionTask()
  const targets = nextStatuses(task.status)

  if (!canWriteTasks(session) || task.isDeleted || targets.length === 0)
    return null

  return (
    <div className="flex flex-wrap gap-2">
      {targets.map((status) => (
        <Button
          key={status}
          size="sm"
          variant={status === 'Cancelled' ? 'ghost' : 'secondary'}
          loading={transition.isPending && transition.variables.status === status}
          onClick={() => transition.mutate({ task, status })}
        >
          Mover para {statusLabel[status]}
        </Button>
      ))}
    </div>
  )
}

export function AssigneePicker({ task }: { task: TaskItem }) {
  const session = useSession()
  const { data: members } = useMembers()
  const assign = useAssignTask()

  // Só oferece quem a regra do backend aceitaria (Developer vê apenas a si mesmo).
  const options = (members ?? []).filter((m) => m.isActive && canAssign(session, task, m.userId))
  const canUnassign = canAssign(session, task, null)
  const current = members?.find((m) => m.userId === task.assigneeId)

  if (options.length === 0 && !canUnassign)
    return <p className="text-sm text-slate-700">{current?.fullName ?? 'Sem responsável'}</p>

  return (
    <label className="block">
      <span className="sr-only">Responsável</span>
      <select
        value={task.assigneeId ?? ''}
        disabled={assign.isPending}
        onChange={(e) => assign.mutate({ id: task.id, assigneeId: e.target.value || null })}
        className="h-9 w-full rounded-md border border-slate-300 bg-white px-2 text-sm"
      >
        <option value="" disabled={!canUnassign}>
          Sem responsável
        </option>
        {current && !options.includes(current) && (
          <option value={current.userId} disabled>
            {current.fullName}
          </option>
        )}
        {options.map((m) => (
          <option key={m.userId} value={m.userId}>
            {m.fullName}
          </option>
        ))}
      </select>
    </label>
  )
}

/** Exclusão em dois cliques em vez de window.confirm: não bloqueia a thread e é testável. */
export function AdminActions({ task, onDeleted }: { task: TaskItem; onDeleted: () => void }) {
  const session = useSession()
  const remove = useDeleteTask()
  const restore = useRestoreTask()
  const [confirming, setConfirming] = useState(false)

  if (!isAdmin(session))
    return null

  if (task.isDeleted) {
    return (
      <Button
        variant="secondary"
        size="sm"
        loading={restore.isPending}
        onClick={() => restore.mutate(task.id, { onSuccess: () => toast.success('Tarefa restaurada.') })}
      >
        Restaurar tarefa
      </Button>
    )
  }

  return confirming ? (
    <div className="flex items-center gap-2">
      <Button
        variant="danger"
        size="sm"
        loading={remove.isPending}
        onClick={() =>
          remove.mutate(task.id, {
            onSuccess: () => {
              toast.success('Tarefa excluída. Dá para restaurar em até 30 dias.')
              onDeleted()
            },
          })
        }
      >
        Confirmar exclusão
      </Button>
      <Button variant="ghost" size="sm" onClick={() => setConfirming(false)}>
        Cancelar
      </Button>
    </div>
  ) : (
    <Button variant="ghost" size="sm" className="text-red-600" onClick={() => setConfirming(true)}>
      Excluir tarefa
    </Button>
  )
}

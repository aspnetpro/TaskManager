import { useLocation, useNavigate, useParams } from 'react-router'
import { toast } from 'sonner'
import { PriorityBadge, StatusBadge } from '@/components/ui/Badge'
import { Dialog } from '@/components/ui/Dialog'
import { ErrorBanner, Skeleton } from '@/components/ui/Feedback'
import { useUpdateTask } from '@/hooks/useTaskMutations'
import { useTask } from '@/hooks/useTasks'
import { useMemberLookup } from '@/hooks/useWorkspace'
import { formatDateOnly, formatDateTime } from '@/lib/dates'
import { isStatus } from '@/lib/errors'
import { canEditTask } from '@/lib/permissions'
import { useSession } from '@/stores/authStore'
import type { TaskDetails, UpdateTaskInput } from '@/types/api'
import { StatusHistory } from './StatusHistory'
import { AdminActions, AssigneePicker, StatusActions } from './TaskActions'
import { TaskForm } from './TaskForm'
import type { TaskFormValues } from './taskSchema'

/** Detalhe como rota filha (/board/tasks/:id): o link é compartilhável e o board continua atrás. */
export function TaskDrawer({ backTo }: { backTo: string }) {
  const { taskId } = useParams()
  const navigate = useNavigate()
  const { search } = useLocation()
  const { data: task, error, isPending, refetch } = useTask(taskId)

  const close = () => void navigate({ pathname: backTo, search })

  return (
    <Dialog variant="drawer" title={task?.title ?? 'Tarefa'} onClose={close}>
      {isPending ? (
        <div className="space-y-3">
          <Skeleton className="h-6 w-2/3" />
          <Skeleton className="h-24" />
        </div>
      ) : error ? (
        isStatus(error, 404) ? (
          <p className="text-sm text-slate-600">Tarefa não encontrada. Ela pode ter sido excluída ou ser de outro workspace.</p>
        ) : (
          <ErrorBanner error={error} onRetry={() => void refetch()} />
        )
      ) : (
        <TaskDetailsView task={task} onDeleted={close} />
      )}
    </Dialog>
  )
}

/** Só o que mudou vai no PATCH. Data apagada = clearDueDate (null no JSON significa "não mexe"). */
function toUpdateInput(task: TaskDetails, values: TaskFormValues): UpdateTaskInput {
  const input: UpdateTaskInput = {}
  if (values.title !== task.title)
    input.title = values.title
  if (values.description !== (task.description ?? ''))
    input.description = values.description
  if (values.priority !== task.priority)
    input.priority = values.priority
  if (values.dueDate === '' && task.dueDate)
    input.clearDueDate = true
  else if (values.dueDate && values.dueDate !== task.dueDate)
    input.dueDate = values.dueDate
  return input
}

function TaskDetailsView({ task, onDeleted }: { task: TaskDetails; onDeleted: () => void }) {
  const session = useSession()
  const members = useMemberLookup()
  const update = useUpdateTask()
  const nameOf = (id: string) => members.get(id)?.fullName ?? 'Membro removido'

  return (
    <div className="space-y-6">
      {task.isDeleted && (
        <p className="rounded-md bg-amber-50 px-3 py-2 text-sm text-amber-800">Esta tarefa está na lixeira.</p>
      )}

      <div className="flex flex-wrap items-center gap-2">
        <StatusBadge status={task.status} />
        <PriorityBadge priority={task.priority} />
        {task.dueDate && (
          <span className={task.isOverdue ? 'text-sm font-semibold text-red-600' : 'text-sm text-slate-600'}>
            Entrega {formatDateOnly(task.dueDate)}
            {task.isOverdue && ' · atrasada'}
          </span>
        )}
      </div>

      <StatusActions task={task} />

      <section className="space-y-1">
        <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500">Responsável</h3>
        <AssigneePicker task={task} />
      </section>

      {canEditTask(session, task) ? (
        <TaskForm
          // Versão nova = form remontado com os valores atuais (depois de salvar ou de um 409).
          key={task.version}
          submitLabel="Salvar alterações"
          originalDueDate={task.dueDate}
          defaultValues={{
            title: task.title,
            description: task.description ?? '',
            priority: task.priority,
            dueDate: task.dueDate ?? '',
          }}
          onSubmit={async (values) => {
            const input = toUpdateInput(task, values)
            if (Object.keys(input).length === 0)
              return
            await update.mutateAsync({ task, input })
            toast.success('Tarefa atualizada.')
          }}
        />
      ) : (
        <p className="whitespace-pre-wrap text-sm text-slate-700">{task.description || 'Sem descrição.'}</p>
      )}

      <dl className="grid grid-cols-2 gap-2 text-sm">
        <dt className="text-slate-500">Criada por</dt>
        <dd>{nameOf(task.reporterId)}</dd>
        <dt className="text-slate-500">Criada em</dt>
        <dd>{formatDateTime(task.createdAt)}</dd>
        {task.completedAt && (
          <>
            <dt className="text-slate-500">Concluída em</dt>
            <dd>{formatDateTime(task.completedAt)}</dd>
          </>
        )}
      </dl>

      <StatusHistory history={task.statusHistory} nameOf={nameOf} />

      <AdminActions task={task} onDeleted={onDeleted} />
    </div>
  )
}

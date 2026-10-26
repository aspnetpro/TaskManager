import { toast } from 'sonner'
import { Dialog } from '@/components/ui/Dialog'
import { useCreateTask } from '@/hooks/useTaskMutations'
import { TaskForm } from './TaskForm'

export function CreateTaskDialog({ onClose }: { onClose: () => void }) {
  const createTask = useCreateTask()

  return (
    <Dialog title="Nova tarefa" onClose={onClose}>
      <TaskForm
        submitLabel="Criar tarefa"
        onCancel={onClose}
        onSubmit={async (values) => {
          await createTask.mutateAsync({
            title: values.title,
            description: values.description.trim() || null,
            priority: values.priority,
            dueDate: values.dueDate || null,
          })
          toast.success('Tarefa criada.')
          onClose()
        }}
      />
    </Dialog>
  )
}

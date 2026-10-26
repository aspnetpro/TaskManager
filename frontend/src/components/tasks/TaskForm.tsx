import { zodResolver } from '@hookform/resolvers/zod'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { FormError } from '@/components/auth/AuthCard'
import { Button } from '@/components/ui/Button'
import { Input, Select, Textarea } from '@/components/ui/Field'
import { describeError, fieldErrorsOf } from '@/lib/errors'
import { PRIORITIES, priorityLabel } from '@/lib/labels'
import { taskSchema, type TaskFormValues } from './taskSchema'

interface TaskFormProps {
  defaultValues?: Partial<TaskFormValues>
  originalDueDate?: string | null
  submitLabel: string
  onSubmit: (values: TaskFormValues) => Promise<unknown>
  onCancel?: () => void
}

const priorityOptions = PRIORITIES.map((p) => ({ value: p, label: priorityLabel[p] }))

export function TaskForm({ defaultValues, originalDueDate, submitLabel, onSubmit, onCancel }: TaskFormProps) {
  const schema = useMemo(() => taskSchema(originalDueDate), [originalDueDate])
  const [formError, setFormError] = useState<string | null>(null)
  const { register, handleSubmit, setError, formState: { errors, isSubmitting } } = useForm<TaskFormValues>({
    resolver: zodResolver(schema),
    defaultValues: { title: '', description: '', priority: 'Medium', dueDate: '', ...defaultValues },
  })

  const submit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await onSubmit(values)
    } catch (error) {
      // Validação da Api volta para o campo; o resto vira mensagem no topo do form.
      const fields = fieldErrorsOf(error)
      for (const [field, message] of Object.entries(fields))
        setError(field as keyof TaskFormValues, { message })
      if (Object.keys(fields).length === 0)
        setFormError(describeError(error))
    }
  })

  return (
    <form onSubmit={submit} className="space-y-4" noValidate>
      <FormError message={formError} />
      <Input label="Título" error={errors.title?.message} {...register('title')} />
      <Textarea label="Descrição" error={errors.description?.message} {...register('description')} />
      <div className="grid gap-4 sm:grid-cols-2">
        <Select label="Prioridade" options={priorityOptions} error={errors.priority?.message} {...register('priority')} />
        <Input label="Entrega" type="date" error={errors.dueDate?.message} {...register('dueDate')} />
      </div>
      <div className="flex justify-end gap-2">
        {onCancel && (
          <Button variant="secondary" onClick={onCancel}>
            Cancelar
          </Button>
        )}
        <Button type="submit" loading={isSubmitting}>
          {submitLabel}
        </Button>
      </div>
    </form>
  )
}

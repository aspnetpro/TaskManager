import { z } from 'zod'
import { utcToday } from '@/lib/dates'
import { PRIORITIES } from '@/lib/labels'

/**
 * Mesmas regras do Create/UpdateTaskCommandValidator (Title 200, Description 2000,
 * DueDate >= hoje UTC). Duplicar é intencional: o front dá feedback na hora,
 * o back garante a regra quando alguém chama a Api sem passar por aqui.
 */
export function taskSchema(originalDueDate?: string | null) {
  return z.object({
    title: z.string().trim().min(1, 'Título é obrigatório.').max(200, 'Máximo de 200 caracteres.'),
    description: z.string().max(2000, 'Máximo de 2000 caracteres.'),
    priority: z.enum(PRIORITIES),
    // Editar uma tarefa já atrasada sem mexer na data não pode falhar por causa dela.
    dueDate: z
      .string()
      .refine((v) => v === '' || v === originalDueDate || v >= utcToday(), 'A data não pode estar no passado.'),
  })
}

export type TaskFormValues = z.infer<ReturnType<typeof taskSchema>>

import type { TaskStatus } from '@/types/api'

/**
 * Cópia de TaskItem.AllowedTransitions (Tasks.Domain) — só para UX: apagar
 * colunas inválidas durante o drag e esconder botões. A regra de verdade
 * continua no domínio; a Api responde 422 se o front estiver desatualizado.
 */
const allowed: Record<TaskStatus, readonly TaskStatus[]> = {
  Pending: ['InProgress', 'Cancelled'],
  InProgress: ['Done', 'Cancelled'],
  Done: [],
  Cancelled: [],
}

export const nextStatuses = (from: TaskStatus): readonly TaskStatus[] => allowed[from]

export const canTransition = (from: TaskStatus, to: TaskStatus): boolean => allowed[from].includes(to)

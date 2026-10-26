import type { AuthSession, TaskItem, WorkspaceRole } from '@/types/api'

// Espelho das policies da Api (CanWriteTasks, AdminOnly) e das regras dos handlers.
// Esconder botão é UX, não segurança: quem decide é a Api.

const writers: readonly WorkspaceRole[] = ['Admin', 'TechLead', 'Developer']

export const canWriteTasks = (session: AuthSession | null) =>
  session !== null && writers.includes(session.workspace.role)

export const isAdmin = (session: AuthSession | null) => session?.workspace.role === 'Admin'

/** US-004: só reporter ou assignee editam (UpdateTaskHandler). */
export const canEditTask = (session: AuthSession | null, task: TaskItem) =>
  canWriteTasks(session) &&
  !task.isDeleted &&
  (task.reporterId === session?.user.id || task.assigneeId === session?.user.id)

/** US-006: Admin/TechLead atribuem a qualquer um; Developer só a si ou remove a si (AssignTaskHandler). */
export function canAssign(session: AuthSession | null, task: TaskItem, assigneeId: string | null): boolean {
  if (!session || task.isDeleted || task.status === 'Done' || task.status === 'Cancelled')
    return false

  switch (session.workspace.role) {
    case 'Admin':
    case 'TechLead':
      return true
    case 'Developer':
      return assigneeId === session.user.id || (assigneeId === null && task.assigneeId === session.user.id)
    default:
      return false
  }
}

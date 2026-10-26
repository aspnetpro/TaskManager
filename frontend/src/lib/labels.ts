import type { TaskPriority, TaskStatus, WorkspaceRole } from '@/types/api'

export const STATUSES: readonly TaskStatus[] = ['Pending', 'InProgress', 'Done', 'Cancelled']
export const PRIORITIES: readonly TaskPriority[] = ['Low', 'Medium', 'High', 'Critical']

export const statusLabel: Record<TaskStatus, string> = {
  Pending: 'A fazer',
  InProgress: 'Em andamento',
  Done: 'Concluída',
  Cancelled: 'Cancelada',
}

export const priorityLabel: Record<TaskPriority, string> = {
  Low: 'Baixa',
  Medium: 'Média',
  High: 'Alta',
  Critical: 'Crítica',
}

export const roleLabel: Record<WorkspaceRole, string> = {
  Viewer: 'Leitor',
  Developer: 'Developer',
  TechLead: 'Tech Lead',
  Admin: 'Admin',
}

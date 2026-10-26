import type { AuthSession, Member, TaskItem, WorkspaceRole } from '@/types/api'

export const WORKSPACE_ID = '0190a000-0000-7000-8000-000000000001'

export const userIds: Record<WorkspaceRole, string> = {
  Admin: '0190a000-0000-7000-8000-0000000000a1',
  TechLead: '0190a000-0000-7000-8000-0000000000b2',
  Developer: '0190a000-0000-7000-8000-0000000000c3',
  Viewer: '0190a000-0000-7000-8000-0000000000d4',
}

const names: Record<WorkspaceRole, string> = {
  Admin: 'Ana Admin',
  TechLead: 'Tiago Lead',
  Developer: 'Diana Dev',
  Viewer: 'Vitor Viewer',
}

export const members: Member[] = (Object.keys(userIds) as WorkspaceRole[]).map((role) => ({
  userId: userIds[role],
  email: `${role.toLowerCase()}@demo.local`,
  fullName: names[role],
  role,
  isActive: true,
}))

export function sessionFor(role: WorkspaceRole, accessToken = `token-${role}`): AuthSession {
  return {
    accessToken,
    expiresAt: '2030-01-01T00:00:00Z',
    user: { id: userIds[role], email: `${role.toLowerCase()}@demo.local`, fullName: names[role] },
    workspace: { workspaceId: WORKSPACE_ID, workspaceName: 'Demo', role },
  }
}

let sequence = 0

/** Equivalente ao TaskItemBuilder dos testes .NET: padrão válido, sobrescreve o que importa. */
export function aTask(overrides: Partial<TaskItem> = {}): TaskItem {
  sequence += 1
  return {
    id: `0190b000-0000-7000-8000-${String(sequence).padStart(12, '0')}`,
    title: `Tarefa ${sequence}`,
    description: null,
    status: 'Pending',
    priority: 'Medium',
    reporterId: userIds.TechLead,
    assigneeId: null,
    dueDate: null,
    isOverdue: false,
    isDueSoon: false,
    completedAt: null,
    createdAt: '2026-10-20T12:00:00Z',
    updatedAt: null,
    version: 1,
    isDeleted: false,
    ...overrides,
  }
}

// Espelho dos records C# da Api. Enums viram union types de string porque a Api
// serializa enum como string (JsonStringEnumConverter, inteiro é rejeitado).
// Mudou um record no backend? Mude aqui no mesmo commit.

export type TaskStatus = 'Pending' | 'InProgress' | 'Done' | 'Cancelled'
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical'
export type WorkspaceRole = 'Viewer' | 'Developer' | 'TechLead' | 'Admin'
export type TaskSortField = 'CreatedAt' | 'UpdatedAt' | 'Priority' | 'DueDate'
export type SortDirection = 'Asc' | 'Desc'
export type NotificationType = 'TaskAssigned' | 'TaskStatusChanged'

/** Datas: DateOnly chega como "2026-10-26"; DateTimeOffset como ISO 8601. */
export type DateOnly = string
export type IsoDateTime = string

/** TaskResponse */
export interface TaskItem {
  id: string
  title: string
  description: string | null
  status: TaskStatus
  priority: TaskPriority
  reporterId: string
  assigneeId: string | null
  dueDate: DateOnly | null
  isOverdue: boolean
  isDueSoon: boolean
  completedAt: IsoDateTime | null
  createdAt: IsoDateTime
  updatedAt: IsoDateTime | null
  /** Concurrency token: vai no If-Match do PATCH. */
  version: number
  isDeleted: boolean
}

/** StatusChangeResponse */
export interface StatusChange {
  from: TaskStatus
  to: TaskStatus
  changedBy: string
  changedAt: IsoDateTime
}

/** TaskDetailsResponse */
export interface TaskDetails extends TaskItem {
  statusHistory: StatusChange[]
}

/** PagedResponse<T> */
export interface PagedResponse<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

/** CreateTaskRequest */
export interface CreateTaskInput {
  title: string
  description: string | null
  priority: TaskPriority
  dueDate: DateOnly | null
}

/** UpdateTaskRequest: campo ausente = não muda; description "" limpa. */
export interface UpdateTaskInput {
  title?: string
  description?: string
  priority?: TaskPriority
  dueDate?: DateOnly | null
  clearDueDate?: boolean
}

/** Query string de GET /tasks (ListTasksRequest) */
export interface TaskFilters {
  status?: TaskStatus
  priority?: TaskPriority
  assigneeId?: string
  search?: string
  overdue?: boolean
  includeDeleted?: boolean
  sortBy?: TaskSortField
  sortDirection?: SortDirection
  page?: number
  pageSize?: number
}

/** SessionUser */
export interface SessionUser {
  id: string
  email: string
  fullName: string
}

/** MembershipSummary */
export interface WorkspaceMembership {
  workspaceId: string
  workspaceName: string
  role: WorkspaceRole
}

/** AuthResponse — o refresh token nunca vem aqui: mora no cookie httpOnly. */
export interface AuthSession {
  accessToken: string
  expiresAt: IsoDateTime
  user: SessionUser
  workspace: WorkspaceMembership
}

export interface LoginInput {
  email: string
  password: string
  workspaceId?: string
}

export interface RegisterInput {
  email: string
  password: string
  fullName: string
  workspaceName: string
}

/** MemberResponse */
export interface Member {
  userId: string
  email: string
  fullName: string
  role: WorkspaceRole
  isActive: boolean
}

/** NotificationResponse */
export interface AppNotification {
  id: string
  type: NotificationType
  taskId: string
  taskTitle: string
  actorId: string
  detail: string | null
  createdAt: IsoDateTime
  readAt: IsoDateTime | null
}

/** ProblemDetails (RFC 9457) com a extensão `code` da Api. */
export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  code?: string
  errors?: Record<string, string[]>
}

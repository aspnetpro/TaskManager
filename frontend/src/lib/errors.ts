import { ApiError, NetworkError } from '@/api/http'

// O `code` do ProblemDetails é o contrato estável; o `detail` é texto em inglês
// para quem depura. O usuário vê a tradução do code — ou a genérica do status.
const messagesByCode: Record<string, string> = {
  'Auth.InvalidCredentials': 'Email ou senha incorretos.',
  'Auth.EmailAlreadyRegistered': 'Já existe uma conta com este email.',
  'Auth.NoWorkspaceAccess': 'Sua conta não tem acesso a nenhum workspace ativo.',
  'Auth.AlreadyMember': 'Essa pessoa já é membro do workspace.',
  'Auth.UserNotFound': 'Nenhuma conta com este email.',
  'Concurrency.Conflict': 'Alguém alterou esta tarefa enquanto você editava. Recarregue e tente de novo.',
  'Task.InvalidTransition': 'Essa mudança de status não é permitida.',
  'Task.EditForbidden': 'Só quem criou ou o responsável pela tarefa pode editá-la.',
  'Task.AssignForbidden': 'Developers só podem atribuir tarefas a si mesmos.',
  'Task.ClosedTaskAssignment': 'Tarefas concluídas ou canceladas não mudam de responsável.',
  'Task.AssigneeNotMember': 'Essa pessoa não é membro ativo do workspace.',
  'Task.RestoreWindowExpired': 'Tarefas só podem ser restauradas em até 30 dias.',
  'Task.NotFound': 'Tarefa não encontrada. Ela pode ter sido excluída.',
}

const messagesByStatus: Record<number, string> = {
  400: 'Alguns dados estão inválidos. Revise o formulário.',
  401: 'Sua sessão expirou. Entre novamente.',
  403: 'Você não tem permissão para esta ação.',
  404: 'Não encontrado.',
  409: 'Alguém alterou este item enquanto você editava. Recarregue e tente de novo.',
  422: 'A operação viola uma regra de negócio.',
  428: 'A versão da tarefa não foi enviada. Recarregue a página.',
}

export function describeError(error: unknown): string {
  if (error instanceof NetworkError)
    return 'Sem conexão com o servidor. Verifique sua rede e tente de novo.'

  if (!(error instanceof ApiError))
    return 'Algo deu errado. Tente de novo.'

  if (error.status === 429) {
    const wait = error.retryAfterSeconds ? ` em ${error.retryAfterSeconds}s` : ' em instantes'
    return `Muitas requisições. Tente de novo${wait}.`
  }

  // 502/503/504 vêm do proxy: a Api está fora do ar ou reiniciando, não quebrou.
  if (error.status >= 502 && error.status <= 504)
    return 'O servidor está indisponível no momento. Tente de novo em instantes.'

  if (error.status >= 500)
    return 'Erro interno no servidor. Tente de novo em instantes.'

  return (error.code && messagesByCode[error.code]) ?? messagesByStatus[error.status] ?? error.message
}

/**
 * Erros de validação do FluentValidation vêm com a chave em PascalCase
 * ("Title", "DueDate"). Converte para o nome do campo no formulário.
 */
export function fieldErrorsOf(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || error.status !== 400)
    return {}

  const result: Record<string, string> = {}
  for (const [key, messages] of Object.entries(error.fieldErrors)) {
    const field = key.charAt(0).toLowerCase() + key.slice(1)
    if (messages[0])
      result[field] = messages[0]
  }
  return result
}

export const isStatus = (error: unknown, status: number) => error instanceof ApiError && error.status === status

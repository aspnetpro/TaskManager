import { useMutation } from '@tanstack/react-query'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/stores/authStore'
import type { LoginInput, RegisterInput } from '@/types/api'

// Erros de login/registro aparecem no formulário, não em toast.
export function useLogin() {
  const setSession = useAuthStore((s) => s.setSession)
  return useMutation({
    mutationFn: (input: LoginInput) => authApi.login(input),
    onSuccess: setSession,
    meta: { silentError: true },
  })
}

export function useRegister() {
  const setSession = useAuthStore((s) => s.setSession)
  return useMutation({
    mutationFn: (input: RegisterInput) => authApi.register(input),
    onSuccess: setSession,
    meta: { silentError: true },
  })
}

/** Sai localmente mesmo se a Api falhar: o cookie expira sozinho, a sessão no front não pode ficar. */
export function useLogout() {
  const clear = useAuthStore((s) => s.clear)
  return useMutation({
    mutationFn: () => authApi.logout(),
    onSettled: clear,
    meta: { silentError: true },
  })
}

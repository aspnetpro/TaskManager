import type { AuthSession, LoginInput, RegisterInput } from '@/types/api'
import { request } from './http'

// O refresh mora em http.ts (refreshSession): ele precisa do single-flight.
export const authApi = {
  login: (input: LoginInput) =>
    request<AuthSession>('/auth/login', { method: 'POST', body: input, anonymous: true }),

  register: (input: RegisterInput) =>
    request<AuthSession>('/auth/register', { method: 'POST', body: input, anonymous: true }),

  logout: () => request<void>('/auth/logout', { method: 'POST', anonymous: true }),
}

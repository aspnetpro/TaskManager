import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { z } from 'zod'
import { AuthCard, FormError } from '@/components/auth/AuthCard'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Field'
import { useLogin } from '@/hooks/useAuthActions'
import { describeError } from '@/lib/errors'

const loginSchema = z.object({
  email: z.email('Informe um email válido.'),
  password: z.string().min(1, 'Informe a senha.'),
})

type LoginForm = z.infer<typeof loginSchema>

// O redirect pós-login é do PublicOnly: o store vira 'authenticated' e ele navega.
export function LoginPage() {
  const login = useLogin()
  const { register, handleSubmit, formState: { errors } } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  })

  return (
    <AuthCard
      title="Entre na sua conta"
      footer={
        <>
          Ainda não tem conta? <Link to="/register" className="font-medium text-accent-600 hover:underline">Criar conta</Link>
        </>
      }
    >
      <form onSubmit={handleSubmit((data) => login.mutate(data))} className="space-y-4" noValidate>
        <FormError message={login.error ? describeError(login.error) : null} />
        <Input label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <Input
          label="Senha"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...register('password')}
        />
        <Button type="submit" className="w-full" loading={login.isPending}>
          Entrar
        </Button>
      </form>

      {import.meta.env.DEV && (
        <p className="mt-4 text-xs text-slate-500">
          Dev: <code>techlead@demo.local</code> / <code>demo-password-123</code> (seed do workspace demo).
        </p>
      )}
    </AuthCard>
  )
}

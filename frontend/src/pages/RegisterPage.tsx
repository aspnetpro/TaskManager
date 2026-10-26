import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { z } from 'zod'
import { AuthCard, FormError } from '@/components/auth/AuthCard'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Field'
import { useRegister } from '@/hooks/useAuthActions'
import { describeError, fieldErrorsOf } from '@/lib/errors'

// Mesmos limites do RegisterCommandValidator / PasswordRules no backend.
const registerSchema = z
  .object({
    fullName: z.string().trim().min(1, 'Informe seu nome.').max(100, 'Máximo de 100 caracteres.'),
    email: z.email('Informe um email válido.').max(256, 'Máximo de 256 caracteres.'),
    password: z.string().min(12, 'A senha precisa de pelo menos 12 caracteres.').max(128, 'Máximo de 128 caracteres.'),
    confirmPassword: z.string(),
    workspaceName: z.string().trim().min(1, 'Dê um nome ao workspace.').max(100, 'Máximo de 100 caracteres.'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    path: ['confirmPassword'],
    message: 'As senhas não conferem.',
  })

type RegisterForm = z.infer<typeof registerSchema>

export function RegisterPage() {
  const registerAccount = useRegister()
  const { register, handleSubmit, setError, formState: { errors } } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
  })

  const onSubmit = handleSubmit(({ fullName, email, password, workspaceName }) =>
    registerAccount.mutate({ fullName, email, password, workspaceName }, {
      // A Api é a fonte de verdade: o que ela rejeitar por campo volta para o campo.
      onError: (error) => {
        for (const [field, message] of Object.entries(fieldErrorsOf(error)))
          setError(field as keyof RegisterForm, { message })
      },
    }),
  )

  return (
    <AuthCard
      title="Crie sua conta e seu primeiro workspace"
      footer={
        <>
          Já tem conta? <Link to="/login" className="font-medium text-accent-600 hover:underline">Entrar</Link>
        </>
      }
    >
      <form onSubmit={onSubmit} className="space-y-4" noValidate>
        <FormError message={registerAccount.error ? describeError(registerAccount.error) : null} />
        <Input label="Nome completo" autoComplete="name" error={errors.fullName?.message} {...register('fullName')} />
        <Input label="Email" type="email" autoComplete="email" error={errors.email?.message} {...register('email')} />
        <Input
          label="Senha"
          type="password"
          autoComplete="new-password"
          hint="Mínimo de 12 caracteres."
          error={errors.password?.message}
          {...register('password')}
        />
        <Input
          label="Confirme a senha"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...register('confirmPassword')}
        />
        <Input label="Nome do workspace" error={errors.workspaceName?.message} {...register('workspaceName')} />
        <Button type="submit" className="w-full" loading={registerAccount.isPending}>
          Criar conta
        </Button>
      </form>
    </AuthCard>
  )
}

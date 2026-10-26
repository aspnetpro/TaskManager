import { useId, type ComponentProps, type ReactNode } from 'react'
import { cn } from '@/lib/cn'

const control =
  'block w-full rounded-md border bg-white px-3 py-2 text-sm shadow-xs placeholder:text-slate-400 disabled:bg-slate-100'

const controlState = (error?: string) =>
  error ? 'border-red-500 focus:outline-red-500' : 'border-slate-300'

interface FieldProps {
  label: string
  error?: string | undefined
  hint?: string | undefined
  children: (props: { id: string; 'aria-invalid'?: true; 'aria-describedby'?: string }) => ReactNode
}

/** Label + controle + erro com os ids/aria ligados — leitor de tela anuncia o erro do campo. */
export function Field({ label, error, hint, children }: FieldProps) {
  const id = useId()
  const messageId = `${id}-message`
  const message = error ?? hint

  return (
    <div className="space-y-1">
      <label htmlFor={id} className="block text-sm font-medium text-slate-700">
        {label}
      </label>
      {children({
        id,
        'aria-invalid': error ? true : undefined,
        'aria-describedby': message ? messageId : undefined,
      })}
      {message && (
        <p id={messageId} className={cn('text-xs', error ? 'text-red-600' : 'text-slate-500')}>
          {message}
        </p>
      )}
    </div>
  )
}

// React 19: `ref` é prop comum — o register() do React Hook Form passa direto, sem forwardRef.
type InputProps = ComponentProps<'input'> & { label: string; error?: string | undefined; hint?: string }

export function Input({ label, error, hint, className, ...props }: InputProps) {
  return (
    <Field label={label} error={error} hint={hint}>
      {(aria) => <input className={cn(control, controlState(error), className)} {...aria} {...props} />}
    </Field>
  )
}

type TextareaProps = ComponentProps<'textarea'> & { label: string; error?: string | undefined }

export function Textarea({ label, error, className, ...props }: TextareaProps) {
  return (
    <Field label={label} error={error}>
      {(aria) => <textarea rows={4} className={cn(control, controlState(error), className)} {...aria} {...props} />}
    </Field>
  )
}

type SelectProps = ComponentProps<'select'> & {
  label: string
  error?: string | undefined
  options: readonly { value: string; label: string }[]
}

/** Select nativo: acessível de graça, funciona no mobile. Dropdown customizado só com motivo. */
export function Select({ label, error, options, className, ...props }: SelectProps) {
  return (
    <Field label={label} error={error}>
      {(aria) => (
        <select className={cn(control, controlState(error), className)} {...aria} {...props}>
          {options.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
      )}
    </Field>
  )
}

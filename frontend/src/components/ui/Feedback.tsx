import { CircleAlert, Inbox } from 'lucide-react'
import type { ReactNode } from 'react'
import { describeError } from '@/lib/errors'
import { cn } from '@/lib/cn'
import { Button } from './Button'

export function Skeleton({ className }: { className?: string }) {
  return <div className={cn('animate-pulse rounded-md bg-slate-200', className)} aria-hidden />
}

export function EmptyState({ title, action }: { title: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-2 rounded-lg border border-dashed border-slate-300 p-6 text-center">
      <Inbox className="size-6 text-slate-400" aria-hidden />
      <p className="text-sm text-slate-500">{title}</p>
      {action}
    </div>
  )
}

/** Erro de primeira carga: sem dado para mostrar, então o erro ocupa o lugar dele. */
export function ErrorBanner({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <div role="alert" className="flex items-center gap-3 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">
      <CircleAlert className="size-5 shrink-0" aria-hidden />
      <p className="flex-1">{describeError(error)}</p>
      {onRetry && (
        <Button variant="secondary" size="sm" onClick={onRetry}>
          Tentar de novo
        </Button>
      )}
    </div>
  )
}

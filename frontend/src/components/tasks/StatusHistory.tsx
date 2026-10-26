import { formatDateTime } from '@/lib/dates'
import { statusLabel } from '@/lib/labels'
import type { StatusChange } from '@/types/api'

export function StatusHistory({ history, nameOf }: { history: StatusChange[]; nameOf: (userId: string) => string }) {
  if (history.length === 0)
    return null

  return (
    <section className="space-y-2">
      <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500">Histórico</h3>
      <ol className="space-y-2 border-l border-slate-200 pl-4">
        {history.map((change) => (
          <li key={change.changedAt} className="text-sm">
            <span className="font-medium">{nameOf(change.changedBy)}</span> moveu de {statusLabel[change.from]} para{' '}
            {statusLabel[change.to]}
            <span className="block text-xs text-slate-500">{formatDateTime(change.changedAt)}</span>
          </li>
        ))}
      </ol>
    </section>
  )
}

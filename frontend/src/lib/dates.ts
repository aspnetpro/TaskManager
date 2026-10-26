import type { DateOnly, IsoDateTime } from '@/types/api'

/**
 * DateOnly ("2026-10-26") NÃO passa por new Date(): ele interpreta como UTC
 * meia-noite e, em UTC-3, a data exibida vira o dia anterior.
 */
export function formatDateOnly(value: DateOnly): string {
  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}

const dateTime = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

export const formatDateTime = (value: IsoDateTime): string => dateTime.format(new Date(value))

/** Hoje em UTC, no mesmo critério do backend (TimeProvider.GetUtcToday). */
export const utcToday = (now: Date = new Date()): DateOnly => now.toISOString().slice(0, 10)

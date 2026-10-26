import type { Announcements, ScreenReaderInstructions } from '@dnd-kit/core'
import { statusLabel } from '@/lib/labels'
import type { TaskItem, TaskStatus } from '@/types/api'

// Leitor de tela ouve o drag em português, via aria-live do próprio dnd-kit.
const title = (data: Record<string, unknown> | undefined) => (data?.task as TaskItem | undefined)?.title ?? 'tarefa'
const column = (id: string | number | undefined) => (id ? statusLabel[id as TaskStatus] : undefined)

export const screenReaderInstructions: ScreenReaderInstructions = {
  draggable: 'Para mover a tarefa, pressione espaço. Use as setas para escolher a coluna e espaço de novo para soltar. Esc cancela.',
}

export const boardAnnouncements: Announcements = {
  onDragStart: ({ active }) => `Movendo “${title(active.data.current)}”.`,
  onDragOver: ({ active, over }) =>
    over ? `“${title(active.data.current)}” sobre ${column(over.id)}.` : `“${title(active.data.current)}” fora das colunas.`,
  onDragEnd: ({ active, over }) =>
    over ? `“${title(active.data.current)}” solta em ${column(over.id)}.` : `“${title(active.data.current)}” voltou ao lugar.`,
  onDragCancel: ({ active }) => `Movimento de “${title(active.data.current)}” cancelado.`,
}

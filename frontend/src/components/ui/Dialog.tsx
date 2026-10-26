import { X } from 'lucide-react'
import { useEffect, useRef, type ReactNode } from 'react'
import { cn } from '@/lib/cn'

interface DialogProps {
  title: string
  onClose: () => void
  children: ReactNode
  /** 'drawer' = painel lateral direito; 'modal' = centralizado. */
  variant?: 'modal' | 'drawer'
}

/**
 * <dialog> nativo com showModal(): foco preso dentro, Esc fecha, fundo inerte
 * para leitor de tela. É o que uma lib de modal faz — sem a lib.
 * Montado = aberto. Para fechar, o pai desmonta.
 */
export function Dialog({ title, onClose, children, variant = 'modal' }: DialogProps) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (!dialog || dialog.open)
      return
    // jsdom ainda não implementa showModal; o atributo open basta para os testes.
    if (typeof dialog.showModal === 'function')
      dialog.showModal()
    else
      dialog.setAttribute('open', '')
  }, [])

  return (
    // Teclado já é coberto pelo Esc nativo (onCancel); o clique é só o atalho do backdrop.
    // oxlint-disable-next-line jsx-a11y/click-events-have-key-events, jsx-a11y/no-noninteractive-element-interactions
    <dialog
      ref={ref}
      aria-label={title}
      onCancel={(event) => {
        event.preventDefault()
        onClose()
      }}
      onClick={(event) => {
        // Clique no backdrop chega com target = o próprio <dialog>.
        if (event.target === event.currentTarget)
          onClose()
      }}
      className={cn(
        'bg-white p-0 shadow-xl',
        variant === 'drawer'
          ? 'm-0 ml-auto h-dvh max-h-dvh w-full max-w-xl'
          : 'm-auto w-[calc(100%-2rem)] max-w-lg rounded-lg',
      )}
    >
      <div className="flex h-full flex-col">
        <header className="flex items-center justify-between border-b border-slate-200 px-5 py-3">
          <h2 className="text-base font-semibold">{title}</h2>
          <button type="button" onClick={onClose} className="rounded p-1 text-slate-500 hover:bg-slate-100" aria-label="Fechar">
            <X className="size-5" aria-hidden />
          </button>
        </header>
        <div className="flex-1 overflow-y-auto px-5 py-4">{children}</div>
      </div>
    </dialog>
  )
}

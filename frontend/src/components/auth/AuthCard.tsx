import type { ReactNode } from 'react'

export function AuthCard({ title, children, footer }: { title: string; children: ReactNode; footer: ReactNode }) {
  return (
    <main className="flex min-h-screen items-center justify-center bg-navy-50 px-4 py-10">
      <div className="w-full max-w-sm space-y-6">
        <div className="text-center">
          <p className="text-2xl font-bold text-navy-800">TaskManager</p>
          <h1 className="mt-1 text-sm text-slate-600">{title}</h1>
        </div>
        <div className="rounded-lg bg-white p-6 shadow-sm">{children}</div>
        <p className="text-center text-sm text-slate-600">{footer}</p>
      </div>
    </main>
  )
}

export function FormError({ message }: { message: string | null }) {
  if (!message)
    return null
  return (
    <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
      {message}
    </p>
  )
}

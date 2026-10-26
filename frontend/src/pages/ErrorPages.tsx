import { isRouteErrorResponse, Link, useRouteError } from 'react-router'

function ErrorScreen({ title, message }: { title: string; message: string }) {
  return (
    <main className="flex min-h-[60vh] flex-col items-center justify-center gap-3 px-4 text-center">
      <h1 className="text-2xl font-semibold text-navy-800">{title}</h1>
      <p className="max-w-md text-sm text-slate-600">{message}</p>
      <Link to="/board" className="text-sm font-medium text-accent-600 hover:underline">
        Voltar para o board
      </Link>
    </main>
  )
}

export function NotFoundPage() {
  return <ErrorScreen title="Página não encontrada" message="O endereço não existe ou mudou." />
}

/**
 * Error boundary de rota: um erro de renderização derruba só a rota, não o app.
 * O detalhe técnico vai para o console (e, na Fase 4, para o Application Insights).
 */
export function RouteErrorPage() {
  const error = useRouteError()
  console.error(error)

  if (isRouteErrorResponse(error) && error.status === 404)
    return <NotFoundPage />

  return (
    <ErrorScreen
      title="Algo quebrou nesta tela"
      message="O erro foi registrado. Recarregue a página; se continuar, avise o time."
    />
  )
}

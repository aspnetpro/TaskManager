import { Navigate, type RouteObject } from 'react-router'
import { PublicOnly, RequireAuth } from '@/components/auth/RouteGuards'
import { AppLayout } from '@/components/layout/AppLayout'
import { NotFoundPage, RouteErrorPage } from '@/pages/ErrorPages'

// Cada página é um chunk: quem abre o login não baixa dnd-kit nem o board.
// Factory, não constante: o router muta os objetos ao resolver o `lazy`, então
// cada router (o do app, cada createMemoryRouter dos testes) precisa dos seus.
export const createRoutes = (): RouteObject[] => [
  {
    ErrorBoundary: RouteErrorPage,
    children: [
      { index: true, element: <Navigate to="/board" replace /> },
      {
        Component: PublicOnly,
        children: [
          { path: 'login', lazy: { Component: async () => (await import('@/pages/LoginPage')).LoginPage } },
          { path: 'register', lazy: { Component: async () => (await import('@/pages/RegisterPage')).RegisterPage } },
        ],
      },
      {
        Component: RequireAuth,
        children: [
          {
            Component: AppLayout,
            children: [
              {
                path: 'board',
                lazy: { Component: async () => (await import('@/pages/BoardPage')).BoardPage },
                children: [{ path: 'tasks/:taskId', lazy: { Component: async () => (await import('@/pages/TaskDrawerRoute')).BoardTaskDrawer } }],
              },
              {
                path: 'tasks',
                lazy: { Component: async () => (await import('@/pages/TaskListPage')).TaskListPage },
                children: [{ path: ':taskId', lazy: { Component: async () => (await import('@/pages/TaskDrawerRoute')).ListTaskDrawer } }],
              },
            ],
          },
        ],
      },
      { path: '*', Component: NotFoundPage },
    ],
  },
]

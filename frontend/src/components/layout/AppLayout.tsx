import { LogOut } from 'lucide-react'
import { NavLink, Outlet } from 'react-router'
import { useLogout } from '@/hooks/useAuthActions'
import { cn } from '@/lib/cn'
import { roleLabel } from '@/lib/labels'
import { useSession } from '@/stores/authStore'
import { NotificationBell } from './NotificationBell'

const navItem = ({ isActive }: { isActive: boolean }) =>
  cn('rounded-md px-3 py-1.5 text-sm font-medium', isActive ? 'bg-navy-700 text-white' : 'text-navy-100 hover:bg-navy-700')

export function AppLayout() {
  const session = useSession()
  const logout = useLogout()

  return (
    <div className="flex min-h-screen flex-col">
      <header className="bg-navy-800 text-white">
        <div className="mx-auto flex h-14 max-w-screen-2xl items-center gap-4 px-4">
          <span className="font-bold">TaskManager</span>
          <span className="hidden truncate text-sm text-navy-100 sm:inline">{session?.workspace.workspaceName}</span>

          <nav aria-label="Principal" className="ml-2 flex gap-1">
            <NavLink to="/board" className={navItem}>
              Board
            </NavLink>
            <NavLink to="/tasks" className={navItem}>
              Lista
            </NavLink>
          </nav>

          <div className="ml-auto flex items-center gap-2">
            <NotificationBell />
            <div className="hidden text-right text-xs leading-tight md:block">
              <p className="font-medium">{session?.user.fullName}</p>
              <p className="text-navy-100">{session && roleLabel[session.workspace.role]}</p>
            </div>
            <button
              type="button"
              onClick={() => logout.mutate()}
              className="rounded-md p-2 text-navy-100 hover:bg-navy-700"
              aria-label="Sair"
              title="Sair"
            >
              <LogOut className="size-5" aria-hidden />
            </button>
          </div>
        </div>
      </header>

      <main className="mx-auto w-full max-w-screen-2xl flex-1 px-4 py-5">
        <Outlet />
      </main>
    </div>
  )
}

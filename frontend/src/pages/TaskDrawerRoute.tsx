import { TaskDrawer } from '@/components/tasks/TaskDrawer'

// O mesmo drawer em duas rotas; o "voltar" de cada uma é a página de trás.
export const BoardTaskDrawer = () => <TaskDrawer backTo="/board" />
export const ListTaskDrawer = () => <TaskDrawer backTo="/tasks" />

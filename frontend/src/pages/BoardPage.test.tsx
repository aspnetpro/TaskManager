import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { aTask } from '@/test/fixtures'
import { renderApp, signInAs } from '@/test/render'
import { db } from '@/test/server'

const column = (name: string) => screen.getByRole('region', { name })
const findColumn = (name: string) => screen.findByRole('region', { name })

describe('BoardPage', () => {
  it('puts each task in the column of its status', async () => {
    signInAs('Developer')
    db.tasks = [
      aTask({ title: 'Configurar CI', status: 'Pending' }),
      aTask({ title: 'Migrar Outbox', status: 'InProgress' }),
      aTask({ title: 'Login com JWT', status: 'Done' }),
    ]

    renderApp('/board')

    expect(await within(await findColumn('A fazer')).findByText('Configurar CI')).toBeInTheDocument()
    expect(within(column('Em andamento')).getByText('Migrar Outbox')).toBeInTheDocument()
    expect(within(column('Concluída')).getByText('Login com JWT')).toBeInTheDocument()
    expect(within(column('Cancelada')).getByText('Nenhuma tarefa aqui')).toBeInTheDocument()
  })

  it('hides write actions from a Viewer', async () => {
    signInAs('Viewer')
    db.tasks = [aTask({ title: 'Só leitura', status: 'Pending' })]

    renderApp('/board')

    expect(await screen.findByText('Só leitura')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /Nova tarefa/ })).not.toBeInTheDocument()
    expect(screen.queryByLabelText(/Mover “Só leitura” para/)).not.toBeInTheDocument()
  })

  it('offers only the transitions the domain allows and moves the card', async () => {
    signInAs('Developer')
    db.tasks = [aTask({ title: 'Revisar PR', status: 'Pending' })]
    renderApp('/board')

    const move = await screen.findByLabelText('Mover “Revisar PR” para')
    const options = within(move).getAllByRole('option').map((o) => o.textContent)
    expect(options).toEqual(['Mover para…', 'Em andamento', 'Cancelada'])

    await userEvent.selectOptions(move, 'InProgress')

    await waitFor(() => expect(within(column('Em andamento')).getByText('Revisar PR')).toBeInTheDocument())
    expect(db.tasks[0]?.status).toBe('InProgress')
  })

  it('keeps the filters in the URL and sends them to the Api', async () => {
    signInAs('Developer')
    db.tasks = [aTask({ title: 'Refatorar cache', status: 'Pending' }), aTask({ title: 'Ajustar CORS', status: 'Pending' })]
    const { router } = renderApp('/board')

    await userEvent.type(await screen.findByRole('searchbox'), 'cache')

    await waitFor(() => expect(router.state.location.search).toBe('?search=cache'))
    await waitFor(() => expect(screen.queryByText('Ajustar CORS')).not.toBeInTheDocument())
    expect(await screen.findByText('Refatorar cache')).toBeInTheDocument()
  })

  it('opens the task drawer from the card without losing the filters', async () => {
    signInAs('Developer')
    db.tasks = [aTask({ title: 'Detalhe com histórico', status: 'Pending' })]
    const { router } = renderApp('/board?priority=Medium')

    await userEvent.click(await screen.findByRole('link', { name: 'Detalhe com histórico' }))

    expect(await screen.findByRole('dialog', { name: 'Detalhe com histórico' })).toBeInTheDocument()
    expect(router.state.location.search).toBe('?priority=Medium')
  })
})

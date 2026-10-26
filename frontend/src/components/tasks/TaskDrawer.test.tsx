import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { http, HttpResponse } from 'msw/http'
import { describe, expect, it } from 'vitest'
import { aTask, userIds } from '@/test/fixtures'
import { renderApp, signInAs } from '@/test/render'
import { db, server } from '@/test/server'

describe('TaskDrawer', () => {
  it('sends only the changed fields with If-Match carrying the task version', async () => {
    signInAs('TechLead')
    const task = aTask({ title: 'Título antigo', reporterId: userIds.TechLead, version: 7 })
    db.tasks = [task]
    let ifMatch: string | null = null
    let body: unknown
    server.use(
      http.patch('*/api/v1/tasks/:id', async ({ request }) => {
        ifMatch = request.headers.get('If-Match')
        body = await request.json()
        return HttpResponse.json({ ...task, title: 'Título novo', version: 8 })
      }),
    )
    renderApp(`/board/tasks/${task.id}`)

    const title = await screen.findByLabelText('Título')
    await userEvent.clear(title)
    await userEvent.type(title, 'Título novo')
    await userEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    await waitFor(() => expect(ifMatch).toBe('"7"'))
    expect(body).toEqual({ title: 'Título novo' })
  })

  it('explains a 409 instead of overwriting someone else’s change', async () => {
    signInAs('TechLead')
    const task = aTask({ reporterId: userIds.TechLead })
    db.tasks = [task]
    server.use(
      http.patch('*/api/v1/tasks/:id', () =>
        HttpResponse.json({ status: 409, code: 'Concurrency.Conflict' }, { status: 409 })),
    )
    renderApp(`/board/tasks/${task.id}`)

    await userEvent.type(await screen.findByLabelText('Descrição'), 'mudança concorrente')
    await userEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    expect(await screen.findByText(/Alguém alterou esta tarefa/)).toBeInTheDocument()
  })

  it('validates the same rules as the backend before submitting', async () => {
    signInAs('TechLead')
    const task = aTask({ reporterId: userIds.TechLead })
    db.tasks = [task]
    renderApp(`/board/tasks/${task.id}`)

    const title = await screen.findByLabelText('Título')
    await userEvent.clear(title)
    await userEvent.type(screen.getByLabelText('Entrega'), '2020-01-01')
    await userEvent.click(screen.getByRole('button', { name: 'Salvar alterações' }))

    expect(await screen.findByText('Título é obrigatório.')).toBeInTheDocument()
    expect(screen.getByText('A data não pode estar no passado.')).toBeInTheDocument()
  })

  it('is read-only for someone who is neither reporter nor assignee', async () => {
    signInAs('Developer')
    const task = aTask({ description: 'Detalhes da tarefa', reporterId: userIds.TechLead })
    db.tasks = [task]
    renderApp(`/board/tasks/${task.id}`)

    expect(await screen.findByText('Detalhes da tarefa')).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Salvar alterações' })).not.toBeInTheDocument()
    // Developer ainda pode se auto-atribuir: o select só oferece ele mesmo.
    const assignee = within(screen.getByRole('dialog')).getByLabelText('Responsável')
    const enabled = within(assignee).getAllByRole('option').filter((o) => !(o as HTMLOptionElement).disabled)
    expect(enabled.map((o) => o.textContent)).toEqual(['Diana Dev'])
  })

  it('shows a not-found message for a task from another workspace', async () => {
    signInAs('Developer')
    renderApp('/board/tasks/0190b000-0000-7000-8000-ffffffffffff')

    expect(await screen.findByText(/Tarefa não encontrada/)).toBeInTheDocument()
  })
})

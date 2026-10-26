import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { bootstrapSession } from '@/api/http'
import { useAuthStore } from '@/stores/authStore'
import { sessionFor } from '@/test/fixtures'
import { renderApp, signInAs } from '@/test/render'
import { db } from '@/test/server'

describe('auth flow', () => {
  it('redirects an anonymous visitor to the login page', async () => {
    useAuthStore.getState().clear()

    const { router } = renderApp('/board')

    expect(await screen.findByRole('heading', { name: 'Entre na sua conta' })).toBeInTheDocument()
    expect(router.state.location.pathname).toBe('/login')
  })

  it('validates the form before calling the Api', async () => {
    useAuthStore.getState().clear()
    renderApp('/login')

    await userEvent.click(await screen.findByRole('button', { name: 'Entrar' }))

    expect(await screen.findByText('Informe um email válido.')).toBeInTheDocument()
    expect(screen.getByText('Informe a senha.')).toBeInTheDocument()
  })

  it('shows the translated message for invalid credentials', async () => {
    useAuthStore.getState().clear()
    renderApp('/login')

    await userEvent.type(await screen.findByLabelText('Email'), 'techlead@demo.local')
    await userEvent.type(screen.getByLabelText('Senha'), 'wrong-password')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Email ou senha incorretos.')
  })

  it('signs in and returns to the page the user tried to open', async () => {
    useAuthStore.getState().clear()
    const { router } = renderApp('/tasks?status=Done')

    await userEvent.type(await screen.findByLabelText('Email'), 'techlead@demo.local')
    await userEvent.type(screen.getByLabelText('Senha'), 'demo-password-123')
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('heading', { name: 'Tarefas' })).toBeInTheDocument()
    expect(router.state.location.pathname + router.state.location.search).toBe('/tasks?status=Done')
  })

  it('survives a page reload through the refresh cookie', async () => {
    db.refreshSession = sessionFor('Admin')
    await bootstrapSession()

    renderApp('/board')

    expect(await screen.findByRole('heading', { name: 'Board' })).toBeInTheDocument()
    expect(screen.getByText('Ana Admin')).toBeInTheDocument()
  })

  it('drops to the login page when the session expires mid-use', async () => {
    signInAs('Developer')
    renderApp('/board')
    expect(await screen.findByRole('heading', { name: 'Board' })).toBeInTheDocument()

    // Access token venceu e o cookie também: o próximo request faz refresh, leva 401 e limpa o store.
    db.validToken = 'rotated-elsewhere'
    db.refreshSession = null
    await userEvent.click(screen.getByRole('link', { name: 'Lista' }))

    await waitFor(() => expect(screen.getByRole('heading', { name: 'Entre na sua conta' })).toBeInTheDocument())
  })

  it('signs out from the header', async () => {
    signInAs('Developer')
    renderApp('/board')

    await userEvent.click(await screen.findByRole('button', { name: 'Sair' }))

    expect(await screen.findByRole('heading', { name: 'Entre na sua conta' })).toBeInTheDocument()
    expect(useAuthStore.getState().session).toBeNull()
  })
})

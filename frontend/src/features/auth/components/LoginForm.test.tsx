import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { useAuthStore } from '@/features/auth/store/authStore';

import { LoginForm } from './LoginForm';

function resetAuthState(): void {
  useAuthStore.setState({
    accessToken: null,
    refreshToken: null,
    user: null,
    isAuthenticated: false,
    isLoading: false,
  });
  window.localStorage.clear();
}

describe('LoginForm', () => {
  beforeEach(() => {
    resetAuthState();
  });

  it('shows inline validation messages', async () => {
    const user = userEvent.setup();

    render(<LoginForm />);

    await user.click(screen.getByRole('button', { name: /entrar/i }));

    expect(await screen.findByText('E-mail obrigatório')).toBeInTheDocument();
    expect(await screen.findByText('Senha obrigatória')).toBeInTheDocument();
  });

  it('submits and calls onSuccess for valid credentials', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();

    render(<LoginForm onSuccess={onSuccess} />);

    await user.type(screen.getByLabelText(/e-mail/i), 'carlos@gestorfinanceiro.com');
    await user.type(screen.getByLabelText(/senha/i), '123456');
    await user.click(screen.getByRole('button', { name: /entrar/i }));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalledTimes(1);
      expect(useAuthStore.getState().isAuthenticated).toBe(true);
    });
  });

  it('shows generic error when credentials are invalid', async () => {
    const user = userEvent.setup();

    render(<LoginForm />);

    await user.type(screen.getByLabelText(/e-mail/i), 'wrong@gestorfinanceiro.com');
    await user.type(screen.getByLabelText(/senha/i), '123456');
    await user.click(screen.getByRole('button', { name: /entrar/i }));

    expect(await screen.findByText('Credenciais inválidas')).toBeInTheDocument();
  });
});

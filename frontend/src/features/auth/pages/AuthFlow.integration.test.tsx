import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';

import { routes } from '@/app/router/routes';
import { useAuthStore } from '@/features/auth/store/authStore';

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

describe('Auth flow integration', () => {
  beforeEach(() => {
    resetAuthState();
  });

  it('goes from login to dashboard and back to login on logout', async () => {
    const user = userEvent.setup();
    const router = createMemoryRouter(routes, {
      initialEntries: ['/login'],
    });

    render(<RouterProvider router={router} />);

    await screen.findByRole('heading', { name: /bem-vindo de volta/i });

    await user.type(screen.getByLabelText(/e-mail/i), 'carlos@gestorfinanceiro.com');
    await user.type(screen.getByLabelText(/senha/i), '123456');
    await user.click(screen.getByRole('button', { name: /entrar/i }));

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/dashboard');
      expect(screen.getByRole('heading', { name: /visao geral/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /sair/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /sair/i }));

    await waitFor(() => {
      expect(router.state.location.pathname).toBe('/login');
      expect(screen.getByRole('heading', { name: /bem-vindo de volta/i })).toBeInTheDocument();
    });
  });
});

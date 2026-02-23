import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Navigate, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { useAuthStore } from '@/features/auth/store/authStore';
import { ProtectedRoute, AppShell, AdminRoute } from '@/shared/components/layout';

// Import pages directly (not lazy) for integration tests
import LoginPage from '@/features/auth/pages/LoginPage';
import DashboardPage from '@/features/dashboard/pages/DashboardPage';
import TransactionsPage from '@/features/transactions/pages/TransactionsPage';
import AccountsPage from '@/features/accounts/pages/AccountsPage';
import CategoriesPage from '@/features/categories/pages/CategoriesPage';
import AdminPage from '@/features/admin/pages/AdminPage';

function resetAuthState(): void {
  useAuthStore.setState({
    accessToken: null,
    refreshToken: null,
    user: null,
    isAuthenticated: false,
    isLoading: false,
  });
}

describe('Auth flow integration', () => {
  beforeEach(() => {
    resetAuthState();
  });

  it('goes from login to dashboard and back to login on logout', async () => {
    const user = userEvent.setup();
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });
    render(
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={['/login']}>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <AppShell />
                </ProtectedRoute>
              }
            >
              <Route index element={<Navigate to="/dashboard" replace />} />
              <Route path="dashboard" element={<DashboardPage />} />
              <Route path="transactions" element={<TransactionsPage />} />
              <Route path="accounts" element={<AccountsPage />} />
              <Route path="categories" element={<CategoriesPage />} />
              <Route path="admin" element={<AdminRoute />}>
                <Route index element={<AdminPage />} />
              </Route>
            </Route>
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>
    );

    // Page should render immediately (no lazy loading)
    expect(screen.getByRole('heading', { name: /bem-vindo de volta/i })).toBeInTheDocument();

    await user.type(screen.getByLabelText(/e-mail/i), 'carlos@gestorfinanceiro.com');
    await user.type(screen.getByLabelText(/senha/i), '123456');
    await user.click(screen.getByRole('button', { name: /entrar/i }));

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /visão geral/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /sair/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /sair/i }));

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /bem-vindo de volta/i })).toBeInTheDocument();
    });
  });
});

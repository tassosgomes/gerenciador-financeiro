import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

import { useAuthStore } from '@/features/auth/store/authStore';

import { Sidebar } from './Sidebar';

describe('Sidebar', () => {
  beforeEach(() => {
    useAuthStore.setState({
      user: {
        id: '4abcbabe-e8da-41cf-bbb4-8f2c0058d8f2',
        name: 'Carlos Silva',
        email: 'carlos@gestorfinanceiro.com',
        role: 'Admin',
        isActive: true,
        mustChangePassword: false,
        createdAt: '2026-01-01T00:00:00.000Z',
      },
      accessToken: 'test-token',
      refreshToken: 'test-refresh',
      isAuthenticated: true,
      isLoading: false,
    });
  });

  it('shows all expected navigation links for admin users', () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Sidebar />
      </MemoryRouter>,
    );

    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /transações/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /contas/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /categorias/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /admin/i })).toBeInTheDocument();
  });

  it('marks current route as active', () => {
    render(
      <MemoryRouter initialEntries={['/transactions']}>
        <Sidebar />
      </MemoryRouter>,
    );

    const activeLink = screen.getByRole('link', { name: /transações/i });

    expect(activeLink).toHaveClass('bg-primary/10');
    expect(activeLink).toHaveClass('text-primary');
  });

  it('hides admin navigation item for non-admin users', () => {
    useAuthStore.setState((state) => ({
      ...state,
      user: {
        id: '4abcbabe-e8da-41cf-bbb4-8f2c0058d8f2',
        name: 'Carlos Silva',
        email: 'carlos@gestorfinanceiro.com',
        role: 'Member',
        isActive: true,
        mustChangePassword: false,
        createdAt: '2026-01-01T00:00:00.000Z',
      },
    }));

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Sidebar />
      </MemoryRouter>,
    );

    expect(screen.queryByRole('link', { name: /admin/i })).not.toBeInTheDocument();
  });
});

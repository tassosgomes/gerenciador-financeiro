import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { describe, expect, it } from 'vitest';

import { AdminRoute } from '@/shared/components/layout/AdminRoute';
import { useAuthStore } from '@/features/auth/store/authStore';

describe('AdminRoute', () => {
  it('renders content for admin users', () => {
    useAuthStore.setState({
      user: {
        id: '1',
        name: 'Admin User',
        email: 'admin@example.com',
        role: 'Admin',
        isActive: true,
        mustChangePassword: false,
        createdAt: '2026-01-01T00:00:00Z',
      },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<AdminRoute />}>
            <Route index element={<div>Admin Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Admin Content')).toBeInTheDocument();
  });

  it('shows access denied for non-admin users', () => {
    useAuthStore.setState({
      user: {
        id: '2',
        name: 'Regular User',
        email: 'user@example.com',
        role: 'Member',
        isActive: true,
        mustChangePassword: false,
        createdAt: '2026-01-01T00:00:00Z',
      },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<AdminRoute />}>
            <Route index element={<div>Admin Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Acesso Restrito')).toBeInTheDocument();
    expect(screen.getByText(/Apenas administradores/i)).toBeInTheDocument();
    expect(screen.getByText('Voltar ao Dashboard')).toBeInTheDocument();
    expect(screen.queryByText('Admin Content')).not.toBeInTheDocument();
  });

  it('shows access denied when user is not authenticated', () => {
    useAuthStore.setState({
      user: null,
      isAuthenticated: false,
    });

    render(
      <MemoryRouter initialEntries={['/']}>
        <Routes>
          <Route element={<AdminRoute />}>
            <Route index element={<div>Admin Content</div>} />
          </Route>
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByText('Acesso Restrito')).toBeInTheDocument();
  });
});

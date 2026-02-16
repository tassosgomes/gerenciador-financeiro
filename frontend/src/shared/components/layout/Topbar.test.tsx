import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';

import { useAuthStore } from '@/features/auth/store/authStore';

import { Topbar } from './Topbar';

describe('Topbar', () => {
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

  it('renders page title and user information', () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Topbar />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: /visão geral/i })).toBeInTheDocument();
    expect(screen.getByText(/carlos silva/i)).toBeInTheDocument();
    expect(screen.getByText(/administrador/i)).toBeInTheDocument();
  });

  it('shows mobile menu button only on small screens', () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Topbar />
      </MemoryRouter>,
    );

    const menuButton = screen.getByRole('button', { name: /abrir menu de navegacao/i });

    expect(menuButton).toBeInTheDocument();
    expect(menuButton).toHaveClass('md:hidden');
  });

  it('opens mobile menu when hamburger button is clicked', async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Topbar />
      </MemoryRouter>,
    );

    const menuButton = screen.getByRole('button', { name: /abrir menu de navegacao/i });

    await user.click(menuButton);

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });
  });

  it('displays all navigation items in mobile menu for admin users', async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Topbar />
      </MemoryRouter>,
    );

    const menuButton = screen.getByRole('button', { name: /abrir menu de navegacao/i });

    await user.click(menuButton);

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    const mobileNav = screen.getByRole('navigation', { name: /menu mobile/i });

    expect(mobileNav).toBeInTheDocument();

    const links = screen.getAllByRole('link');

    expect(links.length).toBeGreaterThan(0);
    expect(screen.getByRole('link', { name: /dashboard/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /transações/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /contas/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /categorias/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /admin/i })).toBeInTheDocument();
  });

  it('hides admin menu item for non-admin users', async () => {
    const user = userEvent.setup();

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
        <Topbar />
      </MemoryRouter>,
    );

    const menuButton = screen.getByRole('button', { name: /abrir menu de navegacao/i });

    await user.click(menuButton);

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    expect(screen.queryByRole('link', { name: /admin/i })).not.toBeInTheDocument();
  });

  it('closes mobile menu when navigation item is clicked', async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Topbar />
      </MemoryRouter>,
    );

    const menuButton = screen.getByRole('button', { name: /abrir menu de navegacao/i });

    await user.click(menuButton);

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    const transactionsLink = screen.getByRole('link', { name: /transações/i });

    await user.click(transactionsLink);

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });

  it('displays GestorFinanceiro branding in mobile menu', async () => {
    const user = userEvent.setup();

    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Topbar />
      </MemoryRouter>,
    );

    const menuButton = screen.getByRole('button', { name: /abrir menu de navegacao/i });

    await user.click(menuButton);

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    expect(screen.getByText(/gestorfinanceiro/i)).toBeInTheDocument();
  });

  it('displays user role label correctly for admin', () => {
    render(
      <MemoryRouter initialEntries={['/dashboard']}>
        <Topbar />
      </MemoryRouter>,
    );

    expect(screen.getByText(/administrador/i)).toBeInTheDocument();
  });

  it('displays user role label correctly for member', () => {
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
        <Topbar />
      </MemoryRouter>,
    );

    expect(screen.getByText(/membro da familia/i)).toBeInTheDocument();
  });
});

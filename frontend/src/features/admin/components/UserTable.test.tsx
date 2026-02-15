import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';

import { UserTable } from '@/features/admin/components/UserTable';
import type { UserResponse } from '@/features/admin/types/admin';
import { RoleType } from '@/features/admin/types/admin';

const mockUsers: UserResponse[] = [
  {
    id: '1',
    name: 'Admin User',
    email: 'admin@example.com',
    role: RoleType.Admin,
    isActive: true,
    createdAt: '2026-01-01T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '2',
    name: 'João Silva',
    email: 'joao@example.com',
    role: RoleType.Member,
    isActive: true,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '3',
    name: 'Maria Santos',
    email: 'maria@example.com',
    role: RoleType.Member,
    isActive: false,
    createdAt: '2026-01-20T10:00:00Z',
    updatedAt: '2026-02-01T10:00:00Z',
  },
];

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('UserTable', () => {
  it('renders user list with all columns', () => {
    renderWithProviders(<UserTable users={mockUsers} />);

    expect(screen.getByText('Admin User')).toBeInTheDocument();
    expect(screen.getByText('admin@example.com')).toBeInTheDocument();
    expect(screen.getByText('João Silva')).toBeInTheDocument();
    expect(screen.getByText('joao@example.com')).toBeInTheDocument();
  });

  it('displays correct role badges', () => {
    renderWithProviders(<UserTable users={mockUsers} />);

    const adminBadges = screen.getAllByText('Admin');
    const memberBadges = screen.getAllByText('Membro');

    expect(adminBadges).toHaveLength(1);
    expect(memberBadges).toHaveLength(2);
  });

  it('displays correct status badges', () => {
    renderWithProviders(<UserTable users={mockUsers} />);

    const activeBadges = screen.getAllByText('Ativo');
    const inactiveBadges = screen.getAllByText('Inativo');

    expect(activeBadges).toHaveLength(2);
    expect(inactiveBadges).toHaveLength(1);
  });

  it('opens confirmation modal when toggle status is clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders(<UserTable users={mockUsers} />);

    const toggleButtons = screen.getAllByRole('button');
    await user.click(toggleButtons[0]);

    await waitFor(() => {
      expect(screen.getByText(/Tem certeza que deseja/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no users', () => {
    renderWithProviders(<UserTable users={[]} />);

    expect(screen.getByText('Nenhum usuário encontrado')).toBeInTheDocument();
  });
});

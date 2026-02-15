import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { AccountForm } from '@/features/accounts/components/AccountForm';
import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';

const mockAccount: AccountResponse = {
  id: '1',
  name: 'Banco Itaú',
  type: AccountType.Corrente,
  balance: 5230.45,
  allowNegativeBalance: false,
  isActive: true,
  createdAt: '2026-01-15T10:00:00Z',
  updatedAt: null,
};

function renderWithProviders(ui: React.ReactElement): void {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('AccountForm', () => {
  const mockOnOpenChange = vi.fn();

  beforeEach(() => {
    mockOnOpenChange.mockClear();
  });

  it('renders create form with all fields', () => {
    renderWithProviders(
      <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
    );

    expect(screen.getByText('Adicionar Nova Conta')).toBeInTheDocument();
    expect(screen.getByLabelText(/nome da conta/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/tipo de conta/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/saldo inicial/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/permitir saldo negativo/i)).toBeInTheDocument();
  });

  it('renders edit form without type and initial balance fields', () => {
    renderWithProviders(
      <AccountForm open={true} onOpenChange={mockOnOpenChange} account={mockAccount} />
    );

    expect(screen.getByText('Editar Conta')).toBeInTheDocument();
    expect(screen.getByLabelText(/nome da conta/i)).toBeInTheDocument();
    expect(screen.queryByLabelText(/tipo de conta/i)).not.toBeInTheDocument();
    expect(screen.queryByLabelText(/saldo inicial/i)).not.toBeInTheDocument();
    expect(screen.getByLabelText(/permitir saldo negativo/i)).toBeInTheDocument();
  });

  it('validates required fields', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
    );

    const submitButton = screen.getByRole('button', { name: /criar conta/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText(/nome deve ter no mínimo 2 caracteres/i)).toBeInTheDocument();
    });
  });

  it('populates form with account data in edit mode', () => {
    renderWithProviders(
      <AccountForm open={true} onOpenChange={mockOnOpenChange} account={mockAccount} />
    );

    const nameInput = screen.getByLabelText(/nome da conta/i) as HTMLInputElement;
    expect(nameInput.value).toBe('Banco Itaú');
  });

  it('calls onOpenChange when cancel button is clicked', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
    );

    const cancelButton = screen.getByRole('button', { name: /cancelar/i });
    await user.click(cancelButton);

    expect(mockOnOpenChange).toHaveBeenCalledWith(false);
  });

  it('submits form with valid data in create mode', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
    );

    await user.type(screen.getByLabelText(/nome da conta/i), 'Nova Conta Teste');
    await user.type(screen.getByLabelText(/saldo inicial/i), '1000');

    const submitButton = screen.getByRole('button', { name: /criar conta/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(mockOnOpenChange).toHaveBeenCalledWith(false);
    });
  });
});

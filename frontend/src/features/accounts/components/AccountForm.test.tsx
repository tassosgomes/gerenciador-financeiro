import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi } from 'vitest';

import { AccountForm } from '@/features/accounts/components/AccountForm';
import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';
import * as hooks from '@/features/accounts/hooks/useAccounts';

const mockAccount: AccountResponse = {
  id: '1',
  name: 'Banco Itaú',
  type: AccountType.Corrente,
  balance: 5230.45,
  allowNegativeBalance: false,
  isActive: true,
  createdAt: '2026-01-15T10:00:00Z',
  updatedAt: null,
  creditCard: null,
};

const mockDebitAccount: AccountResponse = {
  id: 'debit-account-123',
  name: 'Conta Corrente Principal',
  type: AccountType.Corrente,
  balance: 10000.0,
  allowNegativeBalance: false,
  isActive: true,
  createdAt: '2026-01-10T10:00:00Z',
  updatedAt: null,
  creditCard: null,
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
  const mockCreateAccount = vi.fn();
  const mockUpdateAccount = vi.fn();
  type UseAccountsResult = ReturnType<typeof hooks.useAccounts>;
  type UseCreateAccountResult = ReturnType<typeof hooks.useCreateAccount>;
  type UseUpdateAccountResult = ReturnType<typeof hooks.useUpdateAccount>;

  beforeEach(() => {
    mockOnOpenChange.mockClear();
    mockCreateAccount.mockClear();
    mockUpdateAccount.mockClear();

    // Mock the useAccounts hook to return debit accounts
    vi.spyOn(hooks, 'useAccounts').mockReturnValue({
      data: [mockDebitAccount],
      isLoading: false,
      isError: false,
      error: null,
      refetch: vi.fn(),
    } as unknown as UseAccountsResult);

    vi.spyOn(hooks, 'useCreateAccount').mockReturnValue({
      mutateAsync: mockCreateAccount,
      isPending: false,
    } as unknown as UseCreateAccountResult);

    vi.spyOn(hooks, 'useUpdateAccount').mockReturnValue({
      mutateAsync: mockUpdateAccount,
      isPending: false,
    } as unknown as UseUpdateAccountResult);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function changeSelectByLabel(label: RegExp, value: string): void {
    const trigger = screen.getByLabelText(label);
    const nativeSelect = trigger.parentElement?.querySelector('select') as HTMLSelectElement | null;

    if (!nativeSelect) {
      throw new Error(`Select nativo não encontrado para ${label.toString()}`);
    }

    fireEvent.change(nativeSelect, { target: { value } });
  }

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

  describe('Credit Card Fields', () => {
    it('shows credit card fields and hides regular fields when type is Cartao', async () => {
      renderWithProviders(
        <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
      );

      changeSelectByLabel(/tipo de conta/i, String(AccountType.Cartao));

      expect(screen.getByLabelText(/limite de crédito/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/dia de fechamento/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/dia de vencimento/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/conta de débito/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/limite rígido/i)).toBeInTheDocument();

      expect(screen.queryByLabelText(/saldo inicial/i)).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/permitir saldo negativo/i)).not.toBeInTheDocument();
    });

    it('shows regular account fields when type is not Cartao in create mode', async () => {
      renderWithProviders(
        <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
      );

      // By default, type is Corrente (0), so regular fields should be visible
      expect(screen.getByLabelText(/saldo inicial/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/permitir saldo negativo/i)).toBeInTheDocument();

      // Credit card fields should NOT appear
      expect(screen.queryByLabelText(/limite de crédito/i)).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/dia de fechamento/i)).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/dia de vencimento/i)).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/limite rígido/i)).not.toBeInTheDocument();
    });

    it('validates credit limit as positive when type is Cartao', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
      );

      await user.type(screen.getByLabelText(/nome da conta/i), 'Cartão Teste');
      changeSelectByLabel(/tipo de conta/i, String(AccountType.Cartao));
      await user.type(screen.getByLabelText(/dia de fechamento/i), '10');
      await user.type(screen.getByLabelText(/dia de vencimento/i), '20');
      changeSelectByLabel(/conta de débito/i, mockDebitAccount.id);

      await user.click(screen.getByRole('button', { name: /criar conta/i }));

      expect(mockCreateAccount).not.toHaveBeenCalled();
    });

    it('validates closing day between 1 and 28 when type is Cartao', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
      );

      await user.type(screen.getByLabelText(/nome da conta/i), 'Cartão Teste');
      changeSelectByLabel(/tipo de conta/i, String(AccountType.Cartao));
      await user.type(screen.getByLabelText(/limite de crédito/i), '5000');
      await user.clear(screen.getByLabelText(/dia de fechamento/i));
      await user.type(screen.getByLabelText(/dia de fechamento/i), '29');
      await user.type(screen.getByLabelText(/dia de vencimento/i), '20');
      changeSelectByLabel(/conta de débito/i, mockDebitAccount.id);

      await user.click(screen.getByRole('button', { name: /criar conta/i }));

      expect(mockCreateAccount).not.toHaveBeenCalled();
    });

    it('populates debit account select with active Corrente and Carteira accounts only', () => {
      const walletAccount: AccountResponse = {
        id: 'wallet-1',
        name: 'Carteira Casa',
        type: AccountType.Carteira,
        balance: 350,
        allowNegativeBalance: false,
        isActive: true,
        createdAt: '2026-01-10T10:00:00Z',
        updatedAt: null,
        creditCard: null,
      };

      const inactiveCurrentAccount: AccountResponse = {
        id: 'inactive-1',
        name: 'Conta Inativa',
        type: AccountType.Corrente,
        balance: 900,
        allowNegativeBalance: false,
        isActive: false,
        createdAt: '2026-01-10T10:00:00Z',
        updatedAt: null,
        creditCard: null,
      };

      const investmentAccount: AccountResponse = {
        id: 'invest-1',
        name: 'Conta Investimento',
        type: AccountType.Investimento,
        balance: 1500,
        allowNegativeBalance: false,
        isActive: true,
        createdAt: '2026-01-10T10:00:00Z',
        updatedAt: null,
        creditCard: null,
      };

      vi.spyOn(hooks, 'useAccounts').mockReturnValue({
        data: [mockDebitAccount, walletAccount, inactiveCurrentAccount, investmentAccount],
        isLoading: false,
        isError: false,
        error: null,
        refetch: vi.fn(),
      } as unknown as UseAccountsResult);

      renderWithProviders(
        <AccountForm open={true} onOpenChange={mockOnOpenChange} account={null} />
      );

      changeSelectByLabel(/tipo de conta/i, String(AccountType.Cartao));

      const debitTrigger = screen.getByLabelText(/conta de débito/i);
      const debitNativeSelect = debitTrigger.parentElement?.querySelector('select') as HTMLSelectElement;
      const optionLabels = Array.from(debitNativeSelect.options).map((option) => option.text);

      expect(optionLabels.some((text) => text.toLowerCase().includes('conta corrente principal'))).toBe(true);
      expect(optionLabels.some((text) => text.toLowerCase().includes('carteira casa'))).toBe(true);
      expect(optionLabels.some((text) => text.toLowerCase().includes('conta inativa'))).toBe(false);
      expect(optionLabels.some((text) => text.toLowerCase().includes('conta investimento'))).toBe(false);
    });

    it('populates credit card fields correctly in edit mode', () => {
      const creditCardAccount: AccountResponse = {
        id: '2',
        name: 'Nubank',
        type: AccountType.Cartao,
        balance: -1500.0,
        allowNegativeBalance: false,
        isActive: true,
        createdAt: '2026-01-20T10:00:00Z',
        updatedAt: null,
        creditCard: {
          creditLimit: 5000.0,
          closingDay: 15,
          dueDay: 25,
          debitAccountId: 'debit-account-123',
          enforceCreditLimit: true,
          availableLimit: 3500.0,
        },
      };

      renderWithProviders(
        <AccountForm open={true} onOpenChange={mockOnOpenChange} account={creditCardAccount} />
      );

      // Type should not be editable
      expect(screen.queryByLabelText(/tipo de conta/i)).not.toBeInTheDocument();

      // Credit card fields should be populated
      const nameInput = screen.getByLabelText(/nome da conta/i) as HTMLInputElement;
      expect(nameInput.value).toBe('Nubank');

      const limitInput = screen.getByLabelText(/limite de crédito/i) as HTMLInputElement;
      expect(limitInput.value).toBe('5000');

      const closingDayInput = screen.getByLabelText(/dia de fechamento/i) as HTMLInputElement;
      expect(closingDayInput.value).toBe('15');

      const dueDayInput = screen.getByLabelText(/dia de vencimento/i) as HTMLInputElement;
      expect(dueDayInput.value).toBe('25');

      // Verify the switch is present (it's a custom component, so we just check existence)
      expect(screen.getByLabelText(/limite rígido/i)).toBeInTheDocument();

      // Regular account fields should NOT appear
      expect(screen.queryByLabelText(/saldo inicial/i)).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/permitir saldo negativo/i)).not.toBeInTheDocument();
    });

    it('shows credit card specific fields when editing a credit card account', () => {
      const creditCardAccount: AccountResponse = {
        id: '2',
        name: 'Cartão Mastercard',
        type: AccountType.Cartao,
        balance: -2500.0,
        allowNegativeBalance: false,
        isActive: true,
        createdAt: '2026-01-20T10:00:00Z',
        updatedAt: null,
        creditCard: {
          creditLimit: 8000.0,
          closingDay: 10,
          dueDay: 20,
          debitAccountId: 'debit-account-123',
          enforceCreditLimit: false,
          availableLimit: 5500.0,
        },
      };

      renderWithProviders(
        <AccountForm open={true} onOpenChange={mockOnOpenChange} account={creditCardAccount} />
      );

      // All credit card fields should be present
      expect(screen.getByLabelText(/nome da conta/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/limite de crédito/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/dia de fechamento/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/dia de vencimento/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/conta de débito/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/limite rígido/i)).toBeInTheDocument();

      // Regular fields should NOT be present
      expect(screen.queryByLabelText(/saldo inicial/i)).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/permitir saldo negativo/i)).not.toBeInTheDocument();
      expect(screen.queryByLabelText(/tipo de conta/i)).not.toBeInTheDocument();
    });

    it('validates credit card fields when editing a credit card', async () => {
      const user = userEvent.setup();
      
      const creditCardAccount: AccountResponse = {
        id: '2',
        name: 'Nubank',
        type: AccountType.Cartao,
        balance: -1500.0,
        allowNegativeBalance: false,
        isActive: true,
        createdAt: '2026-01-20T10:00:00Z',
        updatedAt: null,
        creditCard: {
          creditLimit: 5000.0,
          closingDay: 15,
          dueDay: 25,
          debitAccountId: 'debit-account-123',
          enforceCreditLimit: true,
          availableLimit: 3500.0,
        },
      };

      renderWithProviders(
        <AccountForm open={true} onOpenChange={mockOnOpenChange} account={creditCardAccount} />
      );

      // Verify all credit card fields are present and editable
      expect(screen.getByLabelText(/limite de crédito/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/dia de fechamento/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/dia de vencimento/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/conta de débito/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/limite rígido/i)).toBeInTheDocument();

      // Update credit limit with valid value
      const limitInput = screen.getByLabelText(/limite de crédito/i);
      await user.clear(limitInput);
      await user.type(limitInput, '8000');

      // Verify the value was updated
      expect((limitInput as HTMLInputElement).value).toBe('8000');
    });
  });
});

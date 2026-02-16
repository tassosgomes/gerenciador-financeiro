import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { AccountCard } from '@/features/accounts/components/AccountCard';
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
  creditCard: null,
};

const mockCreditCardAccount: AccountResponse = {
  id: '2',
  name: 'Cartão Nubank',
  type: AccountType.Cartao,
  balance: -1500.0,
  allowNegativeBalance: true,
  isActive: true,
  createdAt: '2026-01-15T10:00:00Z',
  updatedAt: null,
  creditCard: {
    creditLimit: 5000.0,
    closingDay: 10,
    dueDay: 20,
    debitAccountId: '1',
    enforceCreditLimit: true,
    availableLimit: 3500.0,
  },
};

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return function Wrapper({ children }: { children: React.ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  };
}

describe('AccountCard', () => {
  const mockOnEdit = vi.fn();
  const mockOnToggleStatus = vi.fn();

  beforeEach(() => {
    mockOnEdit.mockClear();
    mockOnToggleStatus.mockClear();
  });

  it('renders account information correctly', () => {
    render(
      <AccountCard
        account={mockAccount}
        onEdit={mockOnEdit}
        onToggleStatus={mockOnToggleStatus}
      />
    );

    expect(screen.getByText('Banco Itaú')).toBeInTheDocument();
    expect(screen.getByText('Corrente')).toBeInTheDocument();
    expect(screen.getByText('R$ 5.230,45')).toBeInTheDocument();
    expect(screen.getByText('Ativa')).toBeInTheDocument();
  });

  it('displays negative balance in red', () => {
    const accountWithNegativeBalance: AccountResponse = {
      ...mockAccount,
      balance: -100.0,
    };

    render(
      <AccountCard
        account={accountWithNegativeBalance}
        onEdit={mockOnEdit}
        onToggleStatus={mockOnToggleStatus}
      />
    );

    const balanceElement = screen.getByText(/R\$\s*-?100,00/);
    expect(balanceElement).toHaveClass('text-danger');
  });

  it('calls onEdit when edit button is clicked', async () => {
    const user = userEvent.setup();

    render(
      <AccountCard
        account={mockAccount}
        onEdit={mockOnEdit}
        onToggleStatus={mockOnToggleStatus}
      />
    );

    const editButton = screen.getByTitle('Editar conta');
    await user.click(editButton);

    expect(mockOnEdit).toHaveBeenCalledWith(mockAccount);
  });

  it('calls onToggleStatus when switch is toggled', async () => {
    const user = userEvent.setup();

    render(
      <AccountCard
        account={mockAccount}
        onEdit={mockOnEdit}
        onToggleStatus={mockOnToggleStatus}
      />
    );

    const switchElement = screen.getByRole('switch');
    await user.click(switchElement);

    expect(mockOnToggleStatus).toHaveBeenCalledWith('1', false);
  });

  it('displays "Permite saldo negativo" badge when allowNegativeBalance is true', () => {
    const accountWithNegativeBalance: AccountResponse = {
      ...mockAccount,
      allowNegativeBalance: true,
    };

    render(
      <AccountCard
        account={accountWithNegativeBalance}
        onEdit={mockOnEdit}
        onToggleStatus={mockOnToggleStatus}
      />
    );

    expect(screen.getByText('Permite saldo negativo')).toBeInTheDocument();
  });

  it('displays correct icon and color for each account type', () => {
    const types = [
      { type: AccountType.Corrente, icon: 'account_balance' },
      { type: AccountType.Cartao, icon: 'credit_card' },
      { type: AccountType.Investimento, icon: 'trending_up' },
      { type: AccountType.Carteira, icon: 'wallet' },
    ];

    types.forEach(({ type, icon }) => {
      const { unmount } = render(
        <AccountCard
          account={{ ...mockAccount, type }}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />
      );

      expect(screen.getByText(icon)).toBeInTheDocument();
      unmount();
    });
  });

  describe('Credit Card Accounts', () => {
    it('should display "Fatura Atual" for credit card accounts', () => {
      render(
        <AccountCard
          account={mockCreditCardAccount}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />,
        { wrapper: createWrapper() }
      );

      expect(screen.getByText('Fatura Atual')).toBeInTheDocument();
      expect(screen.queryByText('Saldo Atual')).not.toBeInTheDocument();
      expect(screen.getByText('R$ 1.500,00')).toBeInTheDocument(); // Valor absoluto
    });

    it('should display "Saldo Atual" for regular accounts', () => {
      render(
        <AccountCard
          account={mockAccount}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />
      );

      expect(screen.getByText('Saldo Atual')).toBeInTheDocument();
      expect(screen.queryByText('Fatura Atual')).not.toBeInTheDocument();
    });

    it('should show limit and available limit for credit cards', () => {
      render(
        <AccountCard
          account={mockCreditCardAccount}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />,
        { wrapper: createWrapper() }
      );

      expect(screen.getByText(/Limite:/)).toBeInTheDocument();
      expect(screen.getByText(/R\$ 5\.000,00/)).toBeInTheDocument();
      expect(screen.getByText(/Disponível:/)).toBeInTheDocument();
      expect(screen.getByText(/R\$ 3\.500,00/)).toBeInTheDocument();
    });

    it('should show yellow alert when available limit < 20%', () => {
      const lowLimitAccount: AccountResponse = {
        ...mockCreditCardAccount,
        creditCard: {
          ...mockCreditCardAccount.creditCard!,
          availableLimit: 800.0, // 16% of 5000
        },
      };

      render(
        <AccountCard
          account={lowLimitAccount}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />,
        { wrapper: createWrapper() }
      );

      expect(screen.getByText(/Limite baixo/)).toBeInTheDocument();
      expect(screen.getByText(/16% disponível/)).toBeInTheDocument();
    });

    it('should show red alert when limit exhausted', () => {
      const exhaustedLimitAccount: AccountResponse = {
        ...mockCreditCardAccount,
        creditCard: {
          ...mockCreditCardAccount.creditCard!,
          availableLimit: 0,
        },
      };

      render(
        <AccountCard
          account={exhaustedLimitAccount}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />,
        { wrapper: createWrapper() }
      );

      expect(screen.getByText('Limite esgotado')).toBeInTheDocument();
    });

    it('should show green badge for positive balance (credit favor)', () => {
      const creditFavorAccount: AccountResponse = {
        ...mockCreditCardAccount,
        balance: 500.0, // Crédito a favor
      };

      render(
        <AccountCard
          account={creditFavorAccount}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />,
        { wrapper: createWrapper() }
      );

      expect(screen.getByText(/Crédito disponível:/)).toBeInTheDocument();
      // Verifica que há dois elementos com R$ 500,00 (fatura e badge)
      const amounts = screen.getAllByText(/R\$ 500,00/);
      expect(amounts).toHaveLength(2);
    });

    it('should show "Ver Fatura" button for credit cards', () => {
      render(
        <AccountCard
          account={mockCreditCardAccount}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />,
        { wrapper: createWrapper() }
      );

      expect(screen.getByRole('button', { name: /ver fatura/i })).toBeInTheDocument();
    });

    it('should handle credit card accounts without CreditCardDetails (legacy)', () => {
      const legacyCardAccount: AccountResponse = {
        ...mockCreditCardAccount,
        creditCard: null, // Cartão legacy sem detalhes
      };

      render(
        <AccountCard
          account={legacyCardAccount}
          onEdit={mockOnEdit}
          onToggleStatus={mockOnToggleStatus}
        />
      );

      // Deve exibir como conta normal
      expect(screen.getByText('Saldo Atual')).toBeInTheDocument();
      expect(screen.queryByText('Fatura Atual')).not.toBeInTheDocument();
      expect(screen.queryByRole('button', { name: /ver fatura/i })).not.toBeInTheDocument();
    });
  });
});

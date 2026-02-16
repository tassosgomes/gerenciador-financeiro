import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

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
});

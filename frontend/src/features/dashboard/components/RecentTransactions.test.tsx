import { render, screen } from '@testing-library/react';

import { RecentTransactions } from '@/features/dashboard/components/RecentTransactions';
import { useTransactions } from '@/features/transactions/hooks/useTransactions';
import { useAccounts } from '@/features/accounts/hooks/useAccounts';
import { useCategories } from '@/features/categories/hooks/useCategories';
import { TransactionStatus, TransactionType, type TransactionResponse } from '@/features/transactions/types/transaction';

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return {
    ...actual,
    useNavigate: () => vi.fn(),
  };
});

vi.mock('@/features/transactions/hooks/useTransactions', () => ({
  useTransactions: vi.fn(),
}));

vi.mock('@/features/accounts/hooks/useAccounts', () => ({
  useAccounts: vi.fn(),
}));

vi.mock('@/features/categories/hooks/useCategories', () => ({
  useCategories: vi.fn(),
}));

const mockedUseTransactions = vi.mocked(useTransactions);
const mockedUseAccounts = vi.mocked(useAccounts);
const mockedUseCategories = vi.mocked(useCategories);

function createTransaction(overrides: Partial<TransactionResponse> = {}): TransactionResponse {
  return {
    id: 'tx-1',
    accountId: 'acc-1',
    categoryId: 'cat-1',
    type: TransactionType.Debit,
    amount: 20,
    description: 'Compra mercado',
    competenceDate: '2026-02-17',
    dueDate: null,
    status: TransactionStatus.Paid,
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: null,
    installmentNumber: null,
    totalInstallments: null,
    isRecurrent: false,
    recurrenceTemplateId: null,
    transferGroupId: null,
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    createdAt: '2026-02-17T10:00:00Z',
    updatedAt: null,
    ...overrides,
    hasReceipt: overrides.hasReceipt ?? false,
  };
}

describe('RecentTransactions', () => {
  beforeEach(() => {
    mockedUseAccounts.mockReturnValue({
      data: [{ id: 'acc-1', name: 'Conta Corrente' }],
    } as ReturnType<typeof useAccounts>);

    mockedUseCategories.mockReturnValue({
      data: [{ id: 'cat-1', name: 'Alimentação' }],
    } as ReturnType<typeof useCategories>);
  });

  it('renderiza transações reais e não mostra placeholder de implementação', () => {
    mockedUseTransactions.mockReturnValue({
      data: {
        data: [createTransaction({ isRecurrent: true, recurrenceTemplateId: 'rec-1' })],
        pagination: { page: 1, size: 5, total: 1, totalPages: 1 },
      },
      isLoading: false,
    } as ReturnType<typeof useTransactions>);

    render(<RecentTransactions />);

    expect(screen.getByText('Transações Recentes')).toBeInTheDocument();
    expect(screen.getByText('Compra mercado')).toBeInTheDocument();
    expect(screen.getByText('Alimentação')).toBeInTheDocument();
    expect(screen.getByText('Conta Corrente')).toBeInTheDocument();
    expect(screen.getByText('Ver todas')).toBeInTheDocument();
    expect(screen.getByText('Recorrente')).toBeInTheDocument();

    expect(screen.queryByText('Em desenvolvimento')).not.toBeInTheDocument();
    expect(
      screen.queryByText('Esta seção será implementada na Task 8.0 (Transações)')
    ).not.toBeInTheDocument();
  });
});

import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';

import { TransactionTable } from '@/features/transactions/components/TransactionTable';
import type { TransactionResponse } from '@/features/transactions/types/transaction';
import { TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';

const mockTransactions: TransactionResponse[] = [
  {
    id: '1',
    description: 'Supermercado Pão de Açúcar',
    amount: -250.0,
    type: TransactionType.Debit,
    status: TransactionStatus.Paid,
    categoryId: 'cat-1',
    accountId: 'acc-1',
    dueDate: '2026-02-10',
    competenceDate: '2026-02-10',
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
    createdAt: '2026-02-10T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '2',
    description: 'Notebook Dell',
    amount: -3000.0,
    type: TransactionType.Debit,
    status: TransactionStatus.Pending,
    categoryId: 'cat-2',
    accountId: 'acc-2',
    dueDate: '2026-03-01',
    competenceDate: '2026-03-01',
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: 'group-1',
    installmentNumber: 1,
    totalInstallments: 12,
    isRecurrent: false,
    recurrenceTemplateId: null,
    transferGroupId: null,
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    createdAt: '2026-02-15T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '3',
    description: 'Aluguel',
    amount: -1500.0,
    type: TransactionType.Debit,
    status: TransactionStatus.Pending,
    categoryId: 'cat-3',
    accountId: 'acc-1',
    dueDate: '2026-03-01',
    competenceDate: '2026-03-01',
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: null,
    installmentNumber: null,
    totalInstallments: null,
    isRecurrent: true,
    recurrenceTemplateId: 'rec-1',
    transferGroupId: null,
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    createdAt: '2026-02-20T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '4',
    description: 'Transferência Poupança',
    amount: -500.0,
    type: TransactionType.Debit,
    status: TransactionStatus.Paid,
    categoryId: 'cat-transfer',
    accountId: 'acc-1',
    dueDate: '2026-02-12',
    competenceDate: '2026-02-12',
    isAdjustment: false,
    originalTransactionId: null,
    hasAdjustment: false,
    installmentGroupId: null,
    installmentNumber: null,
    totalInstallments: null,
    isRecurrent: false,
    recurrenceTemplateId: null,
    transferGroupId: 'transfer-1',
    cancellationReason: null,
    cancelledBy: null,
    cancelledAt: null,
    isOverdue: false,
    createdAt: '2026-02-12T10:00:00Z',
    updatedAt: null,
  },
];

function renderWithProviders(ui: React.ReactElement): void {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <BrowserRouter>
      <QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>
    </BrowserRouter>
  );
}

describe('TransactionTable', () => {
  it('renders table headers correctly', () => {
    renderWithProviders(
      <TransactionTable
        transactions={mockTransactions}
      />
    );

    expect(screen.getByText('Data')).toBeInTheDocument();
    expect(screen.getByText('Descrição')).toBeInTheDocument();
    expect(screen.getByText('Categoria')).toBeInTheDocument();
    expect(screen.getByText('Conta')).toBeInTheDocument();
    expect(screen.getByText('Valor')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
  });

  it('displays all transactions in the table', () => {
    renderWithProviders(
      <TransactionTable
        transactions={mockTransactions}
      />
    );

    expect(screen.getByText('Supermercado Pão de Açúcar')).toBeInTheDocument();
    expect(screen.getByText('Notebook Dell')).toBeInTheDocument();
    expect(screen.getByText('Aluguel')).toBeInTheDocument();
    expect(screen.getByText('Transferência Poupança')).toBeInTheDocument();
  });

  it('displays installment indicator correctly', () => {
    renderWithProviders(
      <TransactionTable
        transactions={mockTransactions}
      />
    );

    expect(screen.getByText('1/12')).toBeInTheDocument();
  });

  it('displays recurrence indicator correctly', () => {
    renderWithProviders(
      <TransactionTable
        transactions={mockTransactions}
      />
    );

    // Recurrence icon should be present
    const recurrenceRow = screen.getByText('Aluguel').closest('tr');
    expect(recurrenceRow).toBeInTheDocument();
  });

  it('displays transfer indicator correctly', () => {
    renderWithProviders(
      <TransactionTable
        transactions={mockTransactions}
      />
    );

    // Transfer icon should be present in the row with 'Transferência Poupança'
    const transferRow = screen.getByText('Transferência Poupança').closest('tr');
    expect(transferRow).toBeInTheDocument();
  });

  it('displays status badges with correct colors', () => {
    renderWithProviders(
      <TransactionTable
        transactions={mockTransactions}
      />
    );

    // Should have both Paid and Pending badges
    const paidBadges = screen.getAllByText('Pago');
    const pendingBadges = screen.getAllByText('Pendente');

    expect(paidBadges.length).toBeGreaterThan(0);
    expect(pendingBadges.length).toBeGreaterThan(0);
  });

  it('formats amount values with correct colors', () => {
    renderWithProviders(
      <TransactionTable
        transactions={mockTransactions}
      />
    );

    // All amounts are negative (expenses/outgoing), so they should be red
    expect(screen.getByText(/R\$ 250,00/i)).toBeInTheDocument();
    expect(screen.getByText(/R\$ 3\.000,00/i)).toBeInTheDocument();
    expect(screen.getByText(/R\$ 1\.500,00/i)).toBeInTheDocument();
    expect(screen.getByText(/R\$ 500,00/i)).toBeInTheDocument();
  });

  it('navigates to detail page when row is clicked', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <TransactionTable
        transactions={mockTransactions}
      />
    );

    const firstRow = screen.getByText('Supermercado Pão de Açúcar').closest('tr');
    expect(firstRow).toBeInTheDocument();

    if (firstRow) {
      await user.click(firstRow);

      await waitFor(() => {
        // Should navigate to /transactions/1
        expect(window.location.pathname).toContain('/transactions/1');
      });
    }
  });

  it('shows empty state when no transactions', () => {
    renderWithProviders(
      <TransactionTable
        transactions={[]}
      />
    );

    expect(screen.getByText(/nenhuma transação encontrada/i)).toBeInTheDocument();
  });
});

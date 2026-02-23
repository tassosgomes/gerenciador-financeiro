import { act, renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { useCreateTransaction } from '@/features/transactions/hooks/useTransactions';
import * as transactionsApi from '@/features/transactions/api/transactionsApi';
import { TransactionStatus, TransactionType } from '@/features/transactions/types/transaction';

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('Transaction hooks cache invalidation', () => {
  function createWrapper(queryClient: QueryClient): React.FC<React.PropsWithChildren> {
    return function Wrapper({ children }: React.PropsWithChildren) {
      return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
    };
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('useCreateTransaction should invalidate transactions, dashboard and budgets caches', async () => {
    const createTransactionSpy = vi
      .spyOn(transactionsApi, 'createTransaction')
      .mockResolvedValue({
        id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
        accountId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
        categoryId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
        type: TransactionType.Debit,
        amount: 100,
        description: 'Teste',
        competenceDate: '2026-02-01',
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
        createdAt: '2026-02-01T00:00:00Z',
        updatedAt: null,
      });

    const queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });
    const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(() => useCreateTransaction(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      await result.current.mutateAsync({
        accountId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
        categoryId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
        type: TransactionType.Debit,
        amount: 100,
        description: 'Teste',
        competenceDate: '2026-02-01',
        status: TransactionStatus.Paid,
      });
    });

    expect(createTransactionSpy).toHaveBeenCalledTimes(1);
    expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['transactions'] });
    expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['dashboard'] });
    expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['budgets'] });
  });
});

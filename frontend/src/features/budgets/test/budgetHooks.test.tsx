import { act, renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { useBudgets } from '@/features/budgets/hooks/useBudgets';
import { useCreateBudget } from '@/features/budgets/hooks/useCreateBudget';
import { useDeleteBudget } from '@/features/budgets/hooks/useDeleteBudget';
import * as budgetsApi from '@/features/budgets/api/budgetsApi';

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('Budget hooks', () => {
  function createWrapper(queryClient: QueryClient): React.FC<React.PropsWithChildren> {
    return function Wrapper({ children }: React.PropsWithChildren) {
      return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
    };
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('useBudgets should call API with correct parameters', async () => {
    const listBudgetsSpy = vi.spyOn(budgetsApi, 'listBudgets').mockResolvedValue([]);
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });

    renderHook(() => useBudgets(2, 2026), { wrapper: createWrapper(queryClient) });

    await waitFor(() => {
      expect(listBudgetsSpy).toHaveBeenCalledWith(2, 2026);
    });
  });

  it('useCreateBudget should invalidate queries after mutation', async () => {
    const createBudgetSpy = vi.spyOn(budgetsApi, 'createBudget').mockResolvedValue({
      id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
      name: 'Orçamento Teste',
      percentage: 10,
      referenceYear: 2026,
      referenceMonth: 2,
      isRecurrent: false,
      monthlyIncome: 10000,
      limitAmount: 1000,
      consumedAmount: 0,
      remainingAmount: 1000,
      consumedPercentage: 0,
      categories: [{ id: '11111111-1111-4111-8111-111111111111', name: 'Supermercado' }],
      createdAt: '2026-02-01T00:00:00Z',
      updatedAt: null,
    });

    const queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });
    const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(() => useCreateBudget(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      await result.current.mutateAsync({
        name: 'Orçamento Teste',
        percentage: 10,
        referenceYear: 2026,
        referenceMonth: 2,
        categoryIds: ['11111111-1111-4111-8111-111111111111'],
        isRecurrent: false,
      });
    });

    expect(createBudgetSpy).toHaveBeenCalledTimes(1);
    expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['budgets'] });
    expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['budgets', 'summary'] });
  });

  it('useDeleteBudget should invalidate queries after mutation', async () => {
    const deleteBudgetSpy = vi.spyOn(budgetsApi, 'deleteBudget').mockResolvedValue(undefined);
    const queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });
    const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(() => useDeleteBudget(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      await result.current.mutateAsync('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa');
    });

    expect(deleteBudgetSpy).toHaveBeenCalledWith('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa');
    expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['budgets'] });
    expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['budgets', 'summary'] });
  });
});

import { act, renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';

import { useReceiptImport } from '@/features/transactions/hooks/useReceiptImport';
import { useReceiptLookup } from '@/features/transactions/hooks/useReceiptLookup';
import { useTransactionReceipt } from '@/features/transactions/hooks/useTransactionReceipt';
import { server } from '@/shared/test/mocks/server';

const toastSuccessMock = vi.fn();
const toastErrorMock = vi.fn();

vi.mock('sonner', () => ({
  toast: {
    success: (...args: unknown[]) => toastSuccessMock(...args),
    error: (...args: unknown[]) => toastErrorMock(...args),
  },
}));

describe('Receipt hooks', () => {
  function createWrapper(queryClient: QueryClient): React.FC<React.PropsWithChildren> {
    return function Wrapper({ children }: React.PropsWithChildren) {
      return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
    };
  }

  beforeEach(() => {
    toastSuccessMock.mockClear();
    toastErrorMock.mockClear();
  });

  it('useReceiptLookup should return data on success', async () => {
    server.use(
      http.post('*/api/v1/receipts/lookup', () =>
        HttpResponse.json({
          accessKey: '12345678901234567890123456789012345678901234',
          establishmentName: 'SUPERMERCADO TESTE',
          establishmentCnpj: '12345678000190',
          issuedAt: '2026-02-20T14:30:00Z',
          totalAmount: 150,
          discountAmount: 5,
          paidAmount: 145,
          items: [
            {
              id: '00000000-0000-0000-0000-000000000000',
              description: 'ARROZ 5KG',
              productCode: '7891234567890',
              quantity: 1,
              unitOfMeasure: 'UN',
              unitPrice: 25.9,
              totalPrice: 25.9,
              itemOrder: 1,
            },
          ],
          alreadyImported: false,
        }),
      ),
    );

    const queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });

    const { result } = renderHook(() => useReceiptLookup(), {
      wrapper: createWrapper(queryClient),
    });

    let mutationResponse: Awaited<ReturnType<typeof result.current.mutateAsync>> | undefined;

    await act(async () => {
      mutationResponse = await result.current.mutateAsync({
        input: '12345678901234567890123456789012345678901234',
      });
    });

    expect(mutationResponse?.accessKey).toBe('12345678901234567890123456789012345678901234');
    expect(toastErrorMock).not.toHaveBeenCalled();
  });

  it('useReceiptLookup should show specific message for 404', async () => {
    server.use(
      http.post('*/api/v1/receipts/lookup', () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/rfc/rfc9110#section-15.5.5',
            title: 'Not Found',
            status: 404,
            detail: 'NFC-e não encontrada.',
          },
          { status: 404 },
        ),
      ),
    );

    const queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });

    const { result } = renderHook(() => useReceiptLookup(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      await expect(result.current.mutateAsync({ input: 'invalid' })).rejects.toBeTruthy();
    });

    expect(toastErrorMock).toHaveBeenCalledWith(
      'NFC-e não encontrada. Verifique se a nota está disponível na SEFAZ.',
    );
  });

  it('useReceiptLookup should show specific message for 502', async () => {
    server.use(
      http.post('*/api/v1/receipts/lookup', () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/rfc/rfc9110#section-15.6.3',
            title: 'Bad Gateway',
            status: 502,
            detail: 'SEFAZ indisponível.',
          },
          { status: 502 },
        ),
      ),
    );

    const queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });

    const { result } = renderHook(() => useReceiptLookup(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      await expect(result.current.mutateAsync({ input: '123' })).rejects.toBeTruthy();
    });

    expect(toastErrorMock).toHaveBeenCalledWith(
      'A SEFAZ está indisponível no momento. Tente novamente mais tarde.',
    );
  });

  it('useReceiptImport should invalidate transactions query on success', async () => {
    server.use(
      http.post('*/api/v1/receipts/import', () =>
        HttpResponse.json(
          {
            transaction: {
              id: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
              accountId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
              categoryId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
              type: 1,
              amount: 145,
              description: 'Supermercado Teste',
              competenceDate: '2026-02-20',
              dueDate: null,
              status: 1,
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
              hasReceipt: true,
              createdAt: '2026-02-20T14:30:00Z',
              updatedAt: null,
            },
            establishment: {
              id: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
              name: 'SUPERMERCADO TESTE',
              cnpj: '12345678000190',
              accessKey: '12345678901234567890123456789012345678901234',
            },
            items: [],
          },
          { status: 201 },
        ),
      ),
    );

    const queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });
    const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(() => useReceiptImport(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      await result.current.mutateAsync({
        accessKey: '12345678901234567890123456789012345678901234',
        accountId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
        categoryId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
        description: 'Supermercado Teste',
        competenceDate: '2026-02-20',
      });
    });

    expect(invalidateQueriesSpy).toHaveBeenCalledWith({ queryKey: ['transactions'] });
    expect(toastSuccessMock).toHaveBeenCalledWith('Cupom fiscal importado com sucesso!');
  });

  it('useReceiptImport should show specific message for 409', async () => {
    server.use(
      http.post('*/api/v1/receipts/import', () =>
        HttpResponse.json(
          {
            type: 'https://tools.ietf.org/rfc/rfc9110#section-15.5.10',
            title: 'Conflict',
            status: 409,
            detail: 'Cupom já importado.',
          },
          { status: 409 },
        ),
      ),
    );

    const queryClient = new QueryClient({
      defaultOptions: {
        mutations: { retry: false },
      },
    });

    const { result } = renderHook(() => useReceiptImport(), {
      wrapper: createWrapper(queryClient),
    });

    await act(async () => {
      await expect(
        result.current.mutateAsync({
          accessKey: '12345678901234567890123456789012345678901234',
          accountId: 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb',
          categoryId: 'cccccccc-cccc-4ccc-8ccc-cccccccccccc',
          description: 'Supermercado Teste',
          competenceDate: '2026-02-20',
        }),
      ).rejects.toBeTruthy();
    });

    expect(toastErrorMock).toHaveBeenCalledWith('Este cupom fiscal já foi importado anteriormente.');
  });

  it('useTransactionReceipt should fetch receipt when hasReceipt is true', async () => {
    server.use(
      http.get('*/api/v1/transactions/:transactionId/receipt', () =>
        HttpResponse.json({
          establishment: {
            id: 'dddddddd-dddd-4ddd-8ddd-dddddddddddd',
            name: 'SUPERMERCADO TESTE',
            cnpj: '12345678000190',
            accessKey: '12345678901234567890123456789012345678901234',
          },
          items: [
            {
              id: 'eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee',
              description: 'ARROZ 5KG',
              productCode: '7891234567890',
              quantity: 1,
              unitOfMeasure: 'UN',
              unitPrice: 25.9,
              totalPrice: 25.9,
              itemOrder: 1,
            },
          ],
        }),
      ),
    );

    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });

    const { result } = renderHook(
      () => useTransactionReceipt('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', true),
      {
        wrapper: createWrapper(queryClient),
      },
    );

    await waitFor(() => {
      expect(result.current.data?.establishment.name).toBe('SUPERMERCADO TESTE');
    });
  });

  it('useTransactionReceipt should stay idle when hasReceipt is false', async () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });

    const { result } = renderHook(
      () => useTransactionReceipt('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', false),
      {
        wrapper: createWrapper(queryClient),
      },
    );

    expect(result.current.fetchStatus).toBe('idle');
  });
});

import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { MemoryRouter, Route, Routes, useParams } from 'react-router-dom';
import { Toaster } from 'sonner';

import ImportReceiptPage from '@/features/transactions/pages/ImportReceiptPage';
import { server } from '@/shared/test/mocks/server';

class MockPointerEvent extends Event implements Partial<PointerEvent> {
  button: number;
  pointerId: number;
  width: number;
  height: number;
  pressure: number;
  tangentialPressure: number;
  tiltX: number;
  tiltY: number;
  twist: number;
  altitudeAngle: number;
  azimuthAngle: number;
  pointerType: string;
  isPrimary: boolean;

  constructor(type: string, init?: PointerEventInit) {
    super(type, init);
    this.button = init?.button ?? 0;
    this.pointerId = init?.pointerId ?? 0;
    this.width = init?.width ?? 1;
    this.height = init?.height ?? 1;
    this.pressure = init?.pressure ?? 0;
    this.tangentialPressure = init?.tangentialPressure ?? 0;
    this.tiltX = init?.tiltX ?? 0;
    this.tiltY = init?.tiltY ?? 0;
    this.twist = init?.twist ?? 0;
    this.altitudeAngle = init?.altitudeAngle ?? 0;
    this.azimuthAngle = init?.azimuthAngle ?? 0;
    this.pointerType = init?.pointerType ?? 'mouse';
    this.isPrimary = init?.isPrimary ?? false;
  }

  getCoalescedEvents(): PointerEvent[] { return []; }
  getPredictedEvents(): PointerEvent[] { return []; }
}

global.PointerEvent = MockPointerEvent as unknown as typeof PointerEvent;

Object.defineProperty(HTMLElement.prototype, 'hasPointerCapture', {
  value: () => false,
  writable: true,
});

function TransactionDetailStub(): JSX.Element {
  const params = useParams<{ id: string }>();
  return <p>Detalhe {params.id}</p>;
}

function renderWithProviders(): void {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <MemoryRouter initialEntries={['/transactions/import-receipt']}>
      <QueryClientProvider client={queryClient}>
        <Routes>
          <Route path="/transactions/import-receipt" element={<ImportReceiptPage />} />
          <Route path="/transactions/:id" element={<TransactionDetailStub />} />
        </Routes>
        <Toaster />
      </QueryClientProvider>
    </MemoryRouter>
  );
}

describe('ImportReceiptPage', () => {
  const accessKey = '12345678901234567890123456789012345678901234';

  it('renders step 1 initially', () => {
    renderWithProviders();

    expect(screen.getByRole('heading', { name: /importar cupom fiscal/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /consultar/i })).toBeInTheDocument();
  });

  it('advances to step 2 after successful lookup', async () => {
    const user = userEvent.setup();

    server.use(
      http.post('*/api/v1/receipts/lookup', () =>
        HttpResponse.json({
          accessKey,
          establishmentName: 'SUPERMERCADO TESTE',
          establishmentCnpj: '12345678000190',
          issuedAt: '2026-02-20T14:30:00Z',
          totalAmount: 150,
          discountAmount: 5,
          paidAmount: 145,
          items: [
            {
              id: 'item-1',
              description: 'ARROZ 5KG',
              productCode: null,
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

    renderWithProviders();

    await user.type(screen.getByLabelText(/chave de acesso/i), accessKey);
    await user.click(screen.getByRole('button', { name: /consultar/i }));

    await waitFor(() => {
      expect(screen.getByText('Preview do Cupom Fiscal')).toBeInTheDocument();
    });
  });

  it('shows duplicate warning when receipt is already imported', async () => {
    const user = userEvent.setup();

    server.use(
      http.post('*/api/v1/receipts/lookup', () =>
        HttpResponse.json({
          accessKey,
          establishmentName: 'SUPERMERCADO TESTE',
          establishmentCnpj: '12345678000190',
          issuedAt: '2026-02-20T14:30:00Z',
          totalAmount: 150,
          discountAmount: 0,
          paidAmount: 150,
          items: [],
          alreadyImported: true,
        }),
      ),
    );

    renderWithProviders();

    await user.type(screen.getByLabelText(/chave de acesso/i), accessKey);
    await user.click(screen.getByRole('button', { name: /consultar/i }));

    await waitFor(() => {
      expect(screen.getByText(/cupom já importado/i)).toBeInTheDocument();
    });
  });

  it('imports receipt and redirects to transaction detail', async () => {
    const user = userEvent.setup();
    const accountId = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';
    const categoryId = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';

    server.use(
      http.get('*/api/v1/accounts', () =>
        HttpResponse.json([
          {
            id: accountId,
            name: 'Banco Itaú',
            type: 1,
            balance: 1000,
            allowNegativeBalance: false,
            isActive: true,
            createdAt: '2026-02-01T00:00:00Z',
            updatedAt: null,
            creditCard: null,
          },
        ]),
      ),
      http.get('*/api/v1/categories', () =>
        HttpResponse.json([
          {
            id: categoryId,
            name: 'Alimentação',
            type: 2,
            isSystem: true,
            createdAt: '2026-02-01T00:00:00Z',
            updatedAt: null,
          },
        ]),
      ),
      http.post('*/api/v1/receipts/lookup', () =>
        HttpResponse.json({
          accessKey,
          establishmentName: 'SUPERMERCADO TESTE',
          establishmentCnpj: '12345678000190',
          issuedAt: '2026-02-20T14:30:00Z',
          totalAmount: 150,
          discountAmount: 5,
          paidAmount: 145,
          items: [
            {
              id: 'item-1',
              description: 'ARROZ 5KG',
              productCode: null,
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
      http.post('*/api/v1/receipts/import', () =>
        HttpResponse.json(
          {
            transaction: {
              id: 'tx-receipt',
              accountId,
              categoryId,
              type: 1,
              amount: 145,
              description: 'SUPERMERCADO TESTE',
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
              id: 'est-1',
              name: 'SUPERMERCADO TESTE',
              cnpj: '12345678000190',
              accessKey,
            },
            items: [],
          },
          { status: 201 },
        ),
      ),
    );

    renderWithProviders();

    await user.type(screen.getByLabelText(/chave de acesso/i), accessKey);
    await user.click(screen.getByRole('button', { name: /consultar/i }));

    await waitFor(() => {
      expect(screen.getByText('Preview do Cupom Fiscal')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('combobox', { name: /conta da transação/i }));
    await user.click(await screen.findByRole('option', { name: /banco itaú/i }));

    await user.click(screen.getByRole('combobox', { name: /categoria da transação/i }));
    await user.click(await screen.findByRole('option', { name: /alimentação/i }));

    await user.click(screen.getByRole('button', { name: /^importar$/i }));

    await waitFor(() => {
      expect(screen.getByText('Detalhe tx-receipt')).toBeInTheDocument();
    });
  });
});

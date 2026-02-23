import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse, delay } from 'msw';

import { ReceiptItemsSection } from '@/features/transactions/components/ReceiptItemsSection';
import { server } from '@/shared/test/mocks/server';

function renderWithProviders(ui: React.ReactElement): void {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}

describe('ReceiptItemsSection', () => {
  it('shows loading skeleton while data is fetched', async () => {
    server.use(
      http.get('*/api/v1/transactions/:id/receipt', async () => {
        await delay(300);

        return HttpResponse.json({
          establishment: {
            id: 'est-1',
            name: 'SUPERMERCADO TESTE',
            cnpj: '12345678000190',
            accessKey: '12345678901234567890123456789012345678901234',
          },
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
        });
      }),
    );

    renderWithProviders(<ReceiptItemsSection transactionId="1" hasReceipt />);

    expect(screen.getByLabelText('Carregando itens do cupom fiscal')).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByText('SUPERMERCADO TESTE')).toBeInTheDocument();
    });
  });

  it('renders establishment, access key and item table', async () => {
    server.use(
      http.get('*/api/v1/transactions/:id/receipt', () =>
        HttpResponse.json({
          establishment: {
            id: 'est-1',
            name: 'SUPERMERCADO TESTE',
            cnpj: '12345678000190',
            accessKey: '12345678901234567890123456789012345678901234',
          },
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
        }),
      ),
    );

    renderWithProviders(<ReceiptItemsSection transactionId="1" hasReceipt />);

    await waitFor(() => {
      expect(screen.getByText('ARROZ 5KG')).toBeInTheDocument();
    });

    expect(screen.getByText(/chave de acesso/i)).toBeInTheDocument();
    expect(screen.getByText('12.345.678/0001-90')).toBeInTheDocument();
  });
});

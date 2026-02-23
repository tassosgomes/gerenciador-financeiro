import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';

import { TransactionDetailPage } from '@/features/transactions/pages/TransactionDetailPage';

function renderWithProviders(initialPath: string): void {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(
    <MemoryRouter initialEntries={[initialPath]}>
      <QueryClientProvider client={queryClient}>
        <Routes>
          <Route path="/transactions/:id" element={<TransactionDetailPage />} />
        </Routes>
      </QueryClientProvider>
    </MemoryRouter>
  );
}

describe('TransactionDetailPage receipt section', () => {
  it('renders receipt section when transaction hasReceipt is true', async () => {
    renderWithProviders('/transactions/1');

    await waitFor(() => {
      expect(screen.getByText('Detalhes da Transação')).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText('Itens do Cupom Fiscal')).toBeInTheDocument();
    });
  });

  it('does not render receipt section when transaction hasReceipt is false', async () => {
    renderWithProviders('/transactions/2');

    await waitFor(() => {
      expect(screen.getByText('Detalhes da Transação')).toBeInTheDocument();
    });

    expect(screen.queryByText('Itens do Cupom Fiscal')).not.toBeInTheDocument();
  });
});

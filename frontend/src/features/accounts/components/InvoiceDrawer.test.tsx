import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { vi } from 'vitest';

import { InvoiceDrawer } from '@/features/accounts/components/InvoiceDrawer';
import * as useInvoiceHook from '@/features/accounts/hooks/useInvoice';
import type { InvoiceResponse } from '@/features/accounts/types/invoice';

const mockInvoice: InvoiceResponse = {
  accountId: '1',
  accountName: 'Cartão Nubank',
  month: 2,
  year: 2026,
  periodStart: '2026-01-11',
  periodEnd: '2026-02-10',
  dueDate: '2026-02-20',
  totalAmount: 1500.0,
  previousBalance: 0,
  amountDue: 1500.0,
  transactions: [
    {
      id: '1',
      description: 'Supermercado ABC',
      amount: 500.0,
      type: 1, // Debit
      competenceDate: '2026-01-15',
      installmentNumber: null,
      totalInstallments: null,
    },
    {
      id: '2',
      description: 'Netflix',
      amount: 49.9,
      type: 1,
      competenceDate: '2026-01-20',
      installmentNumber: 3,
      totalInstallments: 12,
    },
    {
      id: '3',
      description: 'Pagamento',
      amount: 200.0,
      type: 2, // Credit
      competenceDate: '2026-01-25',
      installmentNumber: null,
      totalInstallments: null,
    },
  ],
};

const mockEmptyInvoice: InvoiceResponse = {
  accountId: '1',
  accountName: 'Cartão Nubank',
  month: 1,
  year: 2026,
  periodStart: '2025-12-11',
  periodEnd: '2026-01-10',
  dueDate: '2026-01-20',
  totalAmount: 0,
  previousBalance: 0,
  amountDue: 0,
  transactions: [],
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

describe('InvoiceDrawer', () => {
  const mockOnClose = vi.fn();

  beforeEach(() => {
    mockOnClose.mockClear();
    vi.clearAllMocks();
  });

  it('should render invoice transactions', async () => {
    vi.spyOn(useInvoiceHook, 'useInvoice').mockReturnValue({
      data: mockInvoice,
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useInvoiceHook.useInvoice>);

    render(
      <InvoiceDrawer
        accountId="1"
        accountName="Cartão Nubank"
        isOpen={true}
        onClose={mockOnClose}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(screen.getByText('Supermercado ABC')).toBeInTheDocument();
      expect(screen.getByText(/Netflix/)).toBeInTheDocument();
      expect(screen.getByText('Pagamento')).toBeInTheDocument();
    });
  });

  it('should display installment info for parceled transactions', async () => {
    vi.spyOn(useInvoiceHook, 'useInvoice').mockReturnValue({
      data: mockInvoice,
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useInvoiceHook.useInvoice>);

    render(
      <InvoiceDrawer
        accountId="1"
        accountName="Cartão Nubank"
        isOpen={true}
        onClose={mockOnClose}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(screen.getByText(/Parcela 3\/12 — Netflix/)).toBeInTheDocument();
    });
  });

  it('should show empty state when no transactions', async () => {
    vi.spyOn(useInvoiceHook, 'useInvoice').mockReturnValue({
      data: mockEmptyInvoice,
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useInvoiceHook.useInvoice>);

    render(
      <InvoiceDrawer
        accountId="1"
        accountName="Cartão Nubank"
        isOpen={true}
        onClose={mockOnClose}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      expect(screen.getByText('Nenhuma transação neste período')).toBeInTheDocument();
    });
  });

  it('should navigate between months', async () => {
    const user = userEvent.setup();
    const mockUseInvoice = vi.spyOn(useInvoiceHook, 'useInvoice');
    
    mockUseInvoice.mockReturnValue({
      data: mockInvoice,
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useInvoiceHook.useInvoice>);

    render(
      <InvoiceDrawer
        accountId="1"
        accountName="Cartão Nubank"
        isOpen={true}
        onClose={mockOnClose}
      />,
      { wrapper: createWrapper() }
    );

    // Verifica mês inicial (Fevereiro 2026)
    expect(screen.getByText('Fevereiro 2026')).toBeInTheDocument();

    // Navega para o mês anterior
    const prevButton = screen.getByLabelText('Mês anterior');
    await user.click(prevButton);

    // Verifica se mudou para Janeiro
    await waitFor(() => {
      expect(screen.getByText('Janeiro 2026')).toBeInTheDocument();
    });

    // Navega para o próximo mês
    const nextButton = screen.getByLabelText('Próximo mês');
    await user.click(nextButton);

    // Verifica se voltou para Fevereiro
    await waitFor(() => {
      expect(screen.getByText('Fevereiro 2026')).toBeInTheDocument();
    });
  });

  it('should open payment dialog with amountDue prefilled', async () => {
    const user = userEvent.setup();
    
    vi.spyOn(useInvoiceHook, 'useInvoice').mockReturnValue({
      data: mockInvoice,
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useInvoiceHook.useInvoice>);

    vi.spyOn(useInvoiceHook, 'usePayInvoice').mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as unknown as ReturnType<typeof useInvoiceHook.usePayInvoice>);

    render(
      <InvoiceDrawer
        accountId="1"
        accountName="Cartão Nubank"
        isOpen={true}
        onClose={mockOnClose}
      />,
      { wrapper: createWrapper() }
    );

    const payButton = screen.getByRole('button', { name: /pagar fatura/i });
    await user.click(payButton);

    // Verifica se o dialog foi aberto com o título correto
    const pagarFaturaElements = await screen.findAllByText('Pagar Fatura');
    expect(pagarFaturaElements.length).toBeGreaterThanOrEqual(2); // button + dialog title
    expect(screen.getByText(/Valor total da fatura: R\$ 1\.500,00/)).toBeInTheDocument();
  });

  it('should disable pay button when nothing to pay', async () => {
    vi.spyOn(useInvoiceHook, 'useInvoice').mockReturnValue({
      data: mockEmptyInvoice,
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useInvoiceHook.useInvoice>);

    render(
      <InvoiceDrawer
        accountId="1"
        accountName="Cartão Nubank"
        isOpen={true}
        onClose={mockOnClose}
      />,
      { wrapper: createWrapper() }
    );

    await waitFor(() => {
      const payButton = screen.getByRole('button', { name: /pagar fatura/i });
      expect(payButton).toBeDisabled();
      expect(screen.getByText('Nada a pagar neste momento')).toBeInTheDocument();
    });
  });

  it('should display loading state', () => {
    vi.spyOn(useInvoiceHook, 'useInvoice').mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    } as unknown as ReturnType<typeof useInvoiceHook.useInvoice>);

    render(
      <InvoiceDrawer
        accountId="1"
        accountName="Cartão Nubank"
        isOpen={true}
        onClose={mockOnClose}
      />,
      { wrapper: createWrapper() }
    );

    expect(screen.getByText('Carregando fatura...')).toBeInTheDocument();
  });

  it('should display error state', () => {
    vi.spyOn(useInvoiceHook, 'useInvoice').mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('Failed to fetch'),
    } as unknown as ReturnType<typeof useInvoiceHook.useInvoice>);

    render(
      <InvoiceDrawer
        accountId="1"
        accountName="Cartão Nubank"
        isOpen={true}
        onClose={mockOnClose}
      />,
      { wrapper: createWrapper() }
    );

    expect(screen.getByText('Erro ao carregar fatura. Tente novamente.')).toBeInTheDocument();
  });
});

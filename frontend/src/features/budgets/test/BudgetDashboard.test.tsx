import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { delay, http, HttpResponse } from 'msw';

import { BudgetDashboard } from '@/features/budgets/components/BudgetDashboard';
import { server } from '@/shared/test/mocks/server';

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('BudgetDashboard', () => {
  it('should render summary header with consolidated data', async () => {
    renderWithProviders(<BudgetDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Renda Mensal')).toBeInTheDocument();
    });

    expect(screen.getByText('Total Orçado')).toBeInTheDocument();
    expect(screen.getByText('Total Gasto')).toBeInTheDocument();
    expect(screen.getByText('Saldo Restante')).toBeInTheDocument();
    expect(screen.getByText('Renda Não Orçada')).toBeInTheDocument();
    expect(screen.getByText(/R\$\s?10\.000,00/)).toBeInTheDocument();
  });

  it('should render budget cards for each budget', async () => {
    renderWithProviders(<BudgetDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Alimentação Essencial — 25%')).toBeInTheDocument();
    });

    expect(screen.getByText('Transporte e Mobilidade — 12%')).toBeInTheDocument();
    expect(screen.getByText('Moradia — 20%')).toBeInTheDocument();
  });

  it('should render empty state when no budgets', async () => {
    server.use(
      http.get('*/api/v1/budgets/summary', () => {
        return HttpResponse.json({
          referenceYear: 2026,
          referenceMonth: 2,
          monthlyIncome: 10000,
          totalBudgetedPercentage: 0,
          totalBudgetedAmount: 0,
          totalConsumedAmount: 0,
          totalRemainingAmount: 0,
          unbudgetedPercentage: 100,
          unbudgetedAmount: 10000,
          unbudgetedExpenses: 0,
          budgets: [],
        });
      })
    );

    renderWithProviders(<BudgetDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Nenhum orçamento criado para este mês')).toBeInTheDocument();
    });
  });

  it('should render loading skeleton while fetching', async () => {
    server.use(
      http.get('*/api/v1/budgets/summary', async () => {
        await delay(300);
        return HttpResponse.json({
          referenceYear: 2026,
          referenceMonth: 2,
          monthlyIncome: 10000,
          totalBudgetedPercentage: 10,
          totalBudgetedAmount: 1000,
          totalConsumedAmount: 200,
          totalRemainingAmount: 800,
          unbudgetedPercentage: 90,
          unbudgetedAmount: 9000,
          unbudgetedExpenses: 50,
          budgets: [],
        });
      })
    );

    const { container } = renderWithProviders(<BudgetDashboard />);

    expect(container.querySelectorAll('.animate-pulse').length).toBeGreaterThan(0);

    await waitFor(() => {
      expect(screen.getByText('Nenhum orçamento criado para este mês')).toBeInTheDocument();
    });
  });

  it('should change displayed data when month/year filter changes', async () => {
    const user = userEvent.setup();

    renderWithProviders(<BudgetDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Alimentação Essencial — 25%')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /próximo mês/i }));

    await waitFor(() => {
      expect(screen.getByText('Planejamento Março — 18%')).toBeInTheDocument();
    });

    expect(screen.queryByText('Alimentação Essencial — 25%')).not.toBeInTheDocument();
  });

  it('should hide action buttons for past months (read-only mode)', async () => {
    const user = userEvent.setup();

    renderWithProviders(<BudgetDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Alimentação Essencial — 25%')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /mês anterior/i }));

    await waitFor(() => {
      expect(screen.getByText('Saúde — 15%')).toBeInTheDocument();
    });

    expect(screen.queryByRole('button', { name: /novo orçamento/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /editar orçamento saúde/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /excluir orçamento saúde/i })).not.toBeInTheDocument();
  });

  it('should show "Novo Orçamento" button for current/future months', async () => {
    renderWithProviders(<BudgetDashboard />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /novo orçamento/i })).toBeInTheDocument();
    });
  });

  it('should show unbudgeted expenses in summary', async () => {
    renderWithProviders(<BudgetDashboard />);

    await waitFor(() => {
      expect(screen.getByText('Gastos Fora de Orçamento')).toBeInTheDocument();
    });

    expect(screen.getByText(/R\$\s?420,00/)).toBeInTheDocument();
    expect(screen.getByLabelText('Atenção: gastos fora de orçamento')).toBeInTheDocument();
  });
});

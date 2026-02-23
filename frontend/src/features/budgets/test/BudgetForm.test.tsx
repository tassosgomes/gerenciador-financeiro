import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';

import { BudgetForm } from '@/features/budgets/components/BudgetForm';
import { lowConsumptionBudget } from '@/features/budgets/test/handlers';
import { server } from '@/shared/test/mocks/server';

const formCategories = [
  { id: '11111111-1111-4111-8111-111111111111', name: 'Supermercado', type: 2, isSystem: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  { id: '22222222-2222-4222-8222-222222222222', name: 'Restaurante', type: 2, isSystem: false, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  { id: '33333333-3333-4333-8333-333333333333', name: 'Transporte', type: 2, isSystem: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  { id: '88888888-8888-4888-8888-888888888888', name: 'Viagem', type: 2, isSystem: false, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
  { id: '99999999-9999-4999-8999-999999999998', name: 'Salário', type: 1, isSystem: true, createdAt: '2026-01-01T00:00:00Z', updatedAt: null },
];

function renderWithProviders(ui: React.ReactElement): void {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('BudgetForm', () => {
  beforeEach(() => {
    server.use(
      http.get('*/api/v1/categories', ({ request }) => {
        const url = new URL(request.url);
        const type = Number(url.searchParams.get('type'));

        if (type === 2) {
          return HttpResponse.json(formCategories.filter((category) => category.type === 2));
        }

        return HttpResponse.json(formCategories);
      })
    );
  });

  it('should render all form fields', async () => {
    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={vi.fn()} onCancel={vi.fn()} />
    );

    await waitFor(() => {
      expect(screen.getByLabelText('Nome')).toBeInTheDocument();
    });

    expect(screen.getByLabelText('Percentual da Renda')).toBeInTheDocument();
    expect(screen.getByLabelText('Selecionar mês de referência')).toBeInTheDocument();
    expect(screen.getByLabelText('Selecionar ano de referência')).toBeInTheDocument();
    expect(screen.getByLabelText('Repetir mensalmente')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /criar orçamento/i })).toBeInTheDocument();
  });

  it('should show validation errors for empty name', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={vi.fn()} onCancel={vi.fn()} />
    );

    await user.click(screen.getByRole('button', { name: /criar orçamento/i }));

    await waitFor(() => {
      expect(screen.getByText('Nome deve ter ao menos 2 caracteres')).toBeInTheDocument();
    });
  });

  it('should show validation errors for percentage out of range', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={vi.fn()} onCancel={vi.fn()} />
    );

    await user.type(screen.getByLabelText('Nome'), 'Orçamento Teste');
    await user.clear(screen.getByLabelText('Percentual da Renda'));
    await user.type(screen.getByLabelText('Percentual da Renda'), '26');
    await user.click(await screen.findByRole('button', { name: 'Viagem' }));
    await user.click(screen.getByRole('button', { name: /criar orçamento/i }));

    await waitFor(() => {
      expect(screen.getByText('Percentual excede o disponível para o mês (25.00%)')).toBeInTheDocument();
    });
  });

  it('should show validation errors when no categories selected', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={vi.fn()} onCancel={vi.fn()} />
    );

    await user.type(screen.getByLabelText('Nome'), 'Orçamento Teste');
    await user.clear(screen.getByLabelText('Percentual da Renda'));
    await user.type(screen.getByLabelText('Percentual da Renda'), '5');
    await user.click(screen.getByRole('button', { name: /criar orçamento/i }));

    await waitFor(() => {
      expect(screen.getByText('Selecione ao menos uma categoria')).toBeInTheDocument();
    });
  });

  it('should show available percentage in real-time', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={vi.fn()} onCancel={vi.fn()} />
    );

    await waitFor(() => {
      expect(screen.getByText('Disponível: 25.00%')).toBeInTheDocument();
    });

    await user.clear(screen.getByLabelText('Percentual da Renda'));
    await user.type(screen.getByLabelText('Percentual da Renda'), '20');

    expect(screen.getByText(/20\.00%/)).toBeInTheDocument();
    expect(screen.getByText(/R\$\s?2\.000,00/)).toBeInTheDocument();
  });

  it('should disable categories already used in another budget', async () => {
    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={vi.fn()} onCancel={vi.fn()} />
    );

    const usedCategoryButton = await screen.findByRole('button', { name: 'Supermercado Em uso' });
    expect(usedCategoryButton).toBeDisabled();
  });

  it('should pre-fill fields when editing existing budget', async () => {
    renderWithProviders(
      <BudgetForm
        budget={lowConsumptionBudget}
        month={2}
        year={2026}
        onSuccess={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    await waitFor(() => {
      expect(screen.getByDisplayValue('Alimentação Essencial')).toBeInTheDocument();
    });

    const percentageInput = screen.getByLabelText('Percentual da Renda') as HTMLInputElement;
    expect(percentageInput.value).toBe('25');

    const selectedCategory = await screen.findByRole('button', { name: /supermercado/i });
    expect(selectedCategory).toHaveAttribute('aria-pressed', 'true');
  });

  it('should show reference month as read-only when editing', async () => {
    renderWithProviders(
      <BudgetForm
        budget={lowConsumptionBudget}
        month={2}
        year={2026}
        onSuccess={vi.fn()}
        onCancel={vi.fn()}
      />
    );

    await waitFor(() => {
      expect(screen.getByText(/2026/)).toBeInTheDocument();
    });

    expect(screen.queryByLabelText('Selecionar mês de referência')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Selecionar ano de referência')).not.toBeInTheDocument();
  });

  it('should call onSuccess after successful creation', async () => {
    const onSuccess = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={onSuccess} onCancel={vi.fn()} />
    );

    await user.type(screen.getByLabelText('Nome'), 'Orçamento Novo');
    await user.clear(screen.getByLabelText('Percentual da Renda'));
    await user.type(screen.getByLabelText('Percentual da Renda'), '5');
    await user.click(await screen.findByRole('button', { name: 'Viagem' }));
    await user.click(screen.getByRole('button', { name: /criar orçamento/i }));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalledTimes(1);
    });
  });

  it('should create budget when selecting system category Moradia with non-UUID id', async () => {
    const onSuccess = vi.fn();
    const user = userEvent.setup();

    server.use(
      http.get('*/api/v1/categories', ({ request }) => {
        const url = new URL(request.url);
        const type = Number(url.searchParams.get('type'));

        if (type === 2) {
          return HttpResponse.json([
            {
              id: '5',
              name: 'Moradia',
              type: 2,
              isSystem: true,
              createdAt: '2026-01-01T00:00:00Z',
              updatedAt: null,
            },
            {
              id: '8',
              name: 'Viagem',
              type: 2,
              isSystem: false,
              createdAt: '2026-01-01T00:00:00Z',
              updatedAt: null,
            },
          ]);
        }

        return HttpResponse.json([]);
      })
    );

    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={onSuccess} onCancel={vi.fn()} />
    );

    await user.type(screen.getByLabelText('Nome'), 'Orçamento Moradia');
    await user.clear(screen.getByLabelText('Percentual da Renda'));
    await user.type(screen.getByLabelText('Percentual da Renda'), '5');
    await user.click(await screen.findByRole('button', { name: 'Moradia' }));
    await user.click(screen.getByRole('button', { name: /criar orçamento/i }));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalledTimes(1);
    });
  });

  it('should call onCancel when cancel button is clicked', async () => {
    const onCancel = vi.fn();
    const user = userEvent.setup();

    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={vi.fn()} onCancel={onCancel} />
    );

    await user.click(screen.getByRole('button', { name: /cancelar/i }));

    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('should only show expense categories (not income categories)', async () => {
    renderWithProviders(
      <BudgetForm month={2} year={2026} onSuccess={vi.fn()} onCancel={vi.fn()} />
    );

    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Supermercado Em uso' })).toBeInTheDocument();
    });

    expect(screen.queryByRole('button', { name: /salário/i })).not.toBeInTheDocument();
  });
});

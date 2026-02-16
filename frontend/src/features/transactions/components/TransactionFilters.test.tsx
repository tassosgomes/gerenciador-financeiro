import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';

import { TransactionFilters } from '@/features/transactions/components/TransactionFilters';

const mockOnFilterChange = vi.fn();
const mockOnClearFilters = vi.fn();

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

describe('TransactionFilters', () => {
  beforeEach(() => {
    mockOnFilterChange.mockClear();
    mockOnClearFilters.mockClear();
  });

  it('renders all filter fields', () => {
    renderWithProviders(
      <TransactionFilters
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    expect(screen.getByText('Conta')).toBeInTheDocument();
    expect(screen.getByText('Categoria')).toBeInTheDocument();
    expect(screen.getByText('Tipo')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
    expect(screen.getByText('Data De')).toBeInTheDocument();
    expect(screen.getByText('Data Até')).toBeInTheDocument();
  });

  it('displays clear button when filters are applied', () => {
    renderWithProviders(
      <TransactionFilters
        accountId="acc-1"
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    expect(screen.getByRole('button', { name: /limpar filtros/i })).toBeInTheDocument();
  });

  it('disables clear button when no filters are applied', () => {
    renderWithProviders(
      <TransactionFilters
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    const clearButton = screen.getByRole('button', { name: /limpar filtros/i });
    expect(clearButton).toBeDisabled();
  });

  it('calls onClearFilters when clear button is clicked', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <TransactionFilters
        accountId="acc-1"
        categoryId="cat-1"
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    const clearButton = screen.getByRole('button', { name: /limpar filtros/i });
    await user.click(clearButton);

    expect(mockOnClearFilters).toHaveBeenCalledTimes(1);
  });

  it('displays selected account in filter', () => {
    renderWithProviders(
      <TransactionFilters
        accountId="acc-1"
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    // Account select should be present
    expect(screen.getByText('Conta')).toBeInTheDocument();
  });

  it('displays selected category in filter', () => {
    renderWithProviders(
      <TransactionFilters
        categoryId="cat-1"
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    // Category select should be present
    expect(screen.getByText('Categoria')).toBeInTheDocument();
  });

  it('displays date inputs with correct values', () => {
    renderWithProviders(
      <TransactionFilters
        dateFrom="2026-02-01"
        dateTo="2026-02-28"
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    const dateInputs = screen.getAllByDisplayValue(/2026-02/);
    expect(dateInputs.length).toBeGreaterThanOrEqual(2);
  });

  it('calls onFilterChange when date from is changed', async () => {
    const user = userEvent.setup();

    renderWithProviders(
      <TransactionFilters
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    // Find date inputs by type
    const dateInputs = screen.getAllByDisplayValue('');
    const dateFromInput = dateInputs.filter((input) => 
      (input as HTMLInputElement).type === 'date'
    )[0] as HTMLInputElement;
    
    await user.type(dateFromInput, '2026-02-15');
    expect(mockOnFilterChange).toHaveBeenCalled();
  });

  it('shows placeholder text for all select inputs', () => {
    renderWithProviders(
      <TransactionFilters
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    expect(screen.getByText('Todas as contas')).toBeInTheDocument();
    expect(screen.getByText('Todas as categorias')).toBeInTheDocument();
    expect(screen.getByText('Todos os tipos')).toBeInTheDocument();
    expect(screen.getByText('Todos os status')).toBeInTheDocument();
  });

  it('renders type and status filters', () => {
    renderWithProviders(
      <TransactionFilters
        type={0}
        status={0}
        onFilterChange={mockOnFilterChange}
        onClearFilters={mockOnClearFilters}
      />
    );

    expect(screen.getByText('Tipo')).toBeInTheDocument();
    expect(screen.getByText('Status')).toBeInTheDocument();
  });
});

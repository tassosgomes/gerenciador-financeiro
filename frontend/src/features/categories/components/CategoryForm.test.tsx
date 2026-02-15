import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { CategoryForm } from '@/features/categories/components/CategoryForm';
import type { CategoryResponse } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';

const mockCategory: CategoryResponse = {
  id: '1',
  name: 'Alimentação',
  type: CategoryType.Expense,
  isSystem: false,
  createdAt: '2026-01-15T10:00:00Z',
  updatedAt: null,
};

const mockSystemCategory: CategoryResponse = {
  id: '2',
  name: 'Salário',
  type: CategoryType.Income,
  isSystem: true,
  createdAt: '2026-01-16T10:00:00Z',
  updatedAt: null,
};

function renderWithClient(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('CategoryForm', () => {
  const mockOnOpenChange = vi.fn();

  beforeEach(() => {
    mockOnOpenChange.mockClear();
  });

  it('renders create mode with all fields', () => {
    renderWithClient(<CategoryForm open={true} onOpenChange={mockOnOpenChange} />);

    expect(screen.getByText('Nova Categoria')).toBeInTheDocument();
    expect(screen.getByLabelText(/Nome/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/Tipo/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Criar/i })).toBeInTheDocument();
  });

  it('renders edit mode with name field only', () => {
    renderWithClient(
      <CategoryForm open={true} onOpenChange={mockOnOpenChange} category={mockCategory} />
    );

    expect(screen.getByText('Editar Categoria')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Alimentação')).toBeInTheDocument();
    expect(screen.getByText('Despesa')).toBeInTheDocument();
    expect(screen.getByText('(não editável)')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Salvar/i })).toBeInTheDocument();
  });

  it('validates name field with minimum length', async () => {
    const user = userEvent.setup();

    renderWithClient(<CategoryForm open={true} onOpenChange={mockOnOpenChange} />);

    const nameInput = screen.getByLabelText(/Nome/i);
    await user.type(nameInput, 'A');

    const submitButton = screen.getByRole('button', { name: /Criar/i });
    await user.click(submitButton);

    await waitFor(() => {
      expect(screen.getByText('Nome deve ter no mínimo 2 caracteres')).toBeInTheDocument();
    });
  });

  it('displays type select in create mode', () => {
    renderWithClient(<CategoryForm open={true} onOpenChange={mockOnOpenChange} />);

    // Verificar se o select de tipo está presente
    const typeSelect = screen.getByRole('combobox');
    expect(typeSelect).toBeInTheDocument();
  });

  it('closes dialog when cancel button is clicked', async () => {
    const user = userEvent.setup();

    renderWithClient(<CategoryForm open={true} onOpenChange={mockOnOpenChange} />);

    const cancelButton = screen.getByRole('button', { name: /Cancelar/i });
    await user.click(cancelButton);

    expect(mockOnOpenChange).toHaveBeenCalledWith(false);
  });

  it('resets form when dialog is opened', () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
        mutations: { retry: false },
      },
    });

    const { rerender } = render(
      <QueryClientProvider client={queryClient}>
        <CategoryForm open={false} onOpenChange={mockOnOpenChange} />
      </QueryClientProvider>
    );

    rerender(
      <QueryClientProvider client={queryClient}>
        <CategoryForm open={true} onOpenChange={mockOnOpenChange} />
      </QueryClientProvider>
    );

    const nameInput = screen.getByLabelText(/Nome/i) as HTMLInputElement;
    expect(nameInput.value).toBe('');
  });

  it('displays warning message for system categories', () => {
    renderWithClient(
      <CategoryForm open={true} onOpenChange={mockOnOpenChange} category={mockSystemCategory} />
    );

    expect(screen.getByText(/Esta é uma categoria do sistema e não pode ser editada ou removida/i)).toBeInTheDocument();
  });

  it('disables name input for system categories', () => {
    renderWithClient(
      <CategoryForm open={true} onOpenChange={mockOnOpenChange} category={mockSystemCategory} />
    );

    const nameInput = screen.getByLabelText(/Nome/i);
    expect(nameInput).toBeDisabled();
  });

  it('hides save button for system categories', () => {
    renderWithClient(
      <CategoryForm open={true} onOpenChange={mockOnOpenChange} category={mockSystemCategory} />
    );

    expect(screen.queryByRole('button', { name: /Salvar/i })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Fechar/i })).toBeInTheDocument();
  });

  it('does not attempt to save when submitting system category', async () => {
    renderWithClient(
      <CategoryForm open={true} onOpenChange={mockOnOpenChange} category={mockSystemCategory} />
    );

    // Força o submit do formulário (caso alguém consiga acessar via programação)
    const form = screen.getByRole('button', { name: /Fechar/i }).closest('form');
    if (form) {
      form.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    }

    // Não deve chamar onOpenChange porque a categoria é do sistema
    // e o submit deve ser bloqueado
    expect(mockOnOpenChange).not.toHaveBeenCalledWith(false);
  });
});

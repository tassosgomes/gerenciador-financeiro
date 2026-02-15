import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { CategoryList } from '@/features/categories/components/CategoryList';
import type { CategoryResponse } from '@/features/categories/types/category';
import { CategoryType } from '@/features/categories/types/category';

const mockCategories: CategoryResponse[] = [
  {
    id: '1',
    name: 'Alimentação',
    type: CategoryType.Expense,
    isSystem: true,
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '2',
    name: 'Salário',
    type: CategoryType.Income,
    isSystem: false,
    createdAt: '2026-01-16T10:00:00Z',
    updatedAt: null,
  },
];

describe('CategoryList', () => {
  const mockOnEdit = vi.fn();

  beforeEach(() => {
    mockOnEdit.mockClear();
  });

  it('renders empty state when no categories', () => {
    render(<CategoryList categories={[]} onEdit={mockOnEdit} />);

    expect(screen.getByText('Nenhuma categoria encontrada')).toBeInTheDocument();
    expect(screen.getByText('Crie sua primeira categoria para organizar suas transações')).toBeInTheDocument();
  });

  it('renders categories with correct information', () => {
    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    expect(screen.getByText('Alimentação')).toBeInTheDocument();
    expect(screen.getByText('Salário')).toBeInTheDocument();
  });

  it('displays expense badge with red styling', () => {
    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    const expenseBadges = screen.getAllByText('Despesa');
    expect(expenseBadges[0]).toHaveClass('bg-red-100', 'text-red-800');
  });

  it('displays income badge with green styling', () => {
    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    const incomeBadges = screen.getAllByText('Receita');
    expect(incomeBadges[0]).toHaveClass('bg-green-100', 'text-green-800');
  });

  it('calls onEdit when edit button is clicked for non-system category', async () => {
    const user = userEvent.setup();

    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    // Buscar um botão de edição que não está desabilitado (categoria não-sistema)
    const editButtons = screen.getAllByRole('button');
    const enabledEditButton = editButtons.find(btn => 
      btn.getAttribute('aria-label')?.includes('Editar categoria Salário')
    );
    
    expect(enabledEditButton).toBeDefined();
    if (enabledEditButton) {
      await user.click(enabledEditButton);
      expect(mockOnEdit).toHaveBeenCalledWith(mockCategories[1]);
    }
  });

  it('renders table headers correctly', () => {
    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    expect(screen.getByText('Nome')).toBeInTheDocument();
    expect(screen.getByText('Tipo')).toBeInTheDocument();
    expect(screen.getByText('Ações')).toBeInTheDocument();
  });

  it('displays lock icon for system categories', () => {
    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    const lockIcon = screen.getByLabelText('Categoria do sistema');
    expect(lockIcon).toBeInTheDocument();
  });

  it('disables edit button for system categories', () => {
    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    const editButtons = screen.getAllByRole('button', { name: /Categoria.*sistema/i });
    expect(editButtons[0]).toBeDisabled();
    expect(editButtons[0]).toHaveClass('cursor-not-allowed', 'opacity-40');
  });

  it('enables edit button for non-system categories', async () => {
    const user = userEvent.setup();

    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    const editButtons = screen.getAllByRole('button');
    const nonSystemButton = editButtons.find(btn => !btn.hasAttribute('disabled'));
    
    expect(nonSystemButton).toBeDefined();
    if (nonSystemButton) {
      await user.click(nonSystemButton);
      expect(mockOnEdit).toHaveBeenCalled();
    }
  });
});

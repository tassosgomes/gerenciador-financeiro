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
    createdAt: '2026-01-15T10:00:00Z',
    updatedAt: null,
  },
  {
    id: '2',
    name: 'Salário',
    type: CategoryType.Income,
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
    expect(screen.getByText('Crie sua primeira categoria para começar')).toBeInTheDocument();
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

  it('calls onEdit when edit button is clicked', async () => {
    const user = userEvent.setup();

    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    const editButtons = screen.getAllByRole('button', { name: /Editar/i });
    await user.click(editButtons[0]);

    expect(mockOnEdit).toHaveBeenCalledWith(mockCategories[0]);
  });

  it('renders table headers correctly', () => {
    render(<CategoryList categories={mockCategories} onEdit={mockOnEdit} />);

    expect(screen.getByText('Nome')).toBeInTheDocument();
    expect(screen.getByText('Tipo')).toBeInTheDocument();
    expect(screen.getByText('Ações')).toBeInTheDocument();
  });
});

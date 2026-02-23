import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { BudgetCard } from '@/features/budgets/components/BudgetCard';
import {
  alertBudget,
  exceededBudget,
  highConsumptionBudget,
  lowConsumptionBudget,
  mediumHighConsumptionBudget,
} from '@/features/budgets/test/handlers';

describe('BudgetCard', () => {
  it('should render budget name and percentage', () => {
    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={false} />);

    expect(screen.getByText('Alimentação Essencial — 25%')).toBeInTheDocument();
    expect(screen.getByText('30% consumido')).toBeInTheDocument();
  });

  it('should render categories as badges/chips', () => {
    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={false} />);

    expect(screen.getByText('Supermercado')).toBeInTheDocument();
    expect(screen.getByText('Restaurante')).toBeInTheDocument();
  });

  it('should render formatted values (limit, consumed, remaining)', () => {
    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={false} />);

    expect(screen.getByText(/R\$\s*2\.500,00/)).toBeInTheDocument();
    expect(screen.getByText(/R\$\s*750,00/)).toBeInTheDocument();
    expect(screen.getByText(/R\$\s*1\.750,00/)).toBeInTheDocument();
  });

  it('should render green progress bar when consumed < 70%', () => {
    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={false} />);

    const progressBar = screen.getByRole('progressbar');
    const progressTrack = progressBar.firstElementChild as HTMLElement;
    expect(progressTrack).toHaveClass('bg-green-500');
  });

  it('should render yellow progress bar when consumed between 70-89%', () => {
    render(<BudgetCard budget={mediumHighConsumptionBudget} isReadOnly={false} />);

    const progressBar = screen.getByRole('progressbar');
    const progressTrack = progressBar.firstElementChild as HTMLElement;
    expect(progressTrack).toHaveClass('bg-yellow-500');
  });

  it('should render red progress bar when consumed >= 90%', () => {
    render(<BudgetCard budget={highConsumptionBudget} isReadOnly={false} />);

    const progressBar = screen.getByRole('progressbar');
    const progressTrack = progressBar.firstElementChild as HTMLElement;
    expect(progressTrack).toHaveClass('bg-red-500');
  });

  it('should render "Estourado" badge when consumed > 100%', () => {
    render(<BudgetCard budget={exceededBudget} isReadOnly={false} />);

    expect(screen.getByText('Estourado')).toBeInTheDocument();
  });

  it('should render alert icon when consumed >= 80% and <= 100%', () => {
    render(<BudgetCard budget={alertBudget} isReadOnly={false} />);

    expect(screen.getByLabelText('Orçamento em alerta')).toBeInTheDocument();
  });

  it('should render "Recorrente" badge when isRecurrent is true', () => {
    render(<BudgetCard budget={mediumHighConsumptionBudget} isReadOnly={false} />);

    expect(screen.getByText('Recorrente')).toBeInTheDocument();
  });

  it('should render edit and delete buttons when not read-only', () => {
    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={false} />);

    expect(screen.getByRole('button', { name: /editar orçamento alimentação essencial/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /excluir orçamento alimentação essencial/i })).toBeInTheDocument();
  });

  it('should not render edit and delete buttons when read-only', () => {
    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={true} />);

    expect(screen.queryByRole('button', { name: /editar orçamento alimentação essencial/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /excluir orçamento alimentação essencial/i })).not.toBeInTheDocument();
  });

  it('should call onEdit when edit button is clicked', async () => {
    const onEdit = vi.fn();
    const user = userEvent.setup();

    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={false} onEdit={onEdit} />);

    await user.click(screen.getByRole('button', { name: /editar orçamento alimentação essencial/i }));

    expect(onEdit).toHaveBeenCalledTimes(1);
  });

  it('should call onDelete when delete button is clicked', async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();

    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={false} onDelete={onDelete} />);

    await user.click(screen.getByRole('button', { name: /excluir orçamento alimentação essencial/i }));

    expect(onDelete).toHaveBeenCalledTimes(1);
  });

  it('should have accessible aria-label on progress bar', () => {
    render(<BudgetCard budget={lowConsumptionBudget} isReadOnly={false} />);

    const progressBar = screen.getByRole('progressbar');
    expect(progressBar).toHaveAttribute('aria-label', 'Consumo do orçamento Alimentação Essencial: 30%');
  });
});

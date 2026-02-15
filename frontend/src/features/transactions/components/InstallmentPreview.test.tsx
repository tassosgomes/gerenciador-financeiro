import { render, screen } from '@testing-library/react';

import { InstallmentPreview } from '@/features/transactions/components/InstallmentPreview';

describe('InstallmentPreview', () => {
  it('shows empty state when required fields are missing', () => {
    render(
      <InstallmentPreview
        totalAmount={0}
        installmentCount={0}
        firstCompetenceDate=""
        firstDueDate=""
      />
    );

    expect(
      screen.getByText('Preencha os campos acima para ver o preview das parcelas')
    ).toBeInTheDocument();
  });

  it('renders preview table with correct number of installments', () => {
    render(
      <InstallmentPreview
        totalAmount={1200}
        installmentCount={12}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    expect(screen.getByText('Preview das Parcelas')).toBeInTheDocument();

    // Should display installment rows (1/12, 2/12, etc.)
    expect(screen.getByText('1/12')).toBeInTheDocument();
    expect(screen.getByText('12/12')).toBeInTheDocument();
  });

  it('displays correct installment values', () => {
    render(
      <InstallmentPreview
        totalAmount={1200}
        installmentCount={12}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    // Each installment should be 100.00 (1200 / 12)
    const amountCells = screen.getAllByText('R$ 100,00');
    expect(amountCells.length).toBeGreaterThan(0);
  });

  it('displays correct installment labels', () => {
    render(
      <InstallmentPreview
        totalAmount={1200}
        installmentCount={12}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    expect(screen.getByText('1/12')).toBeInTheDocument();
    expect(screen.getByText('6/12')).toBeInTheDocument();
    expect(screen.getByText('12/12')).toBeInTheDocument();
  });

  it('calculates dates correctly with monthly intervals', () => {
    render(
      <InstallmentPreview
        totalAmount={1200}
        installmentCount={3}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    const dateCells = screen.getAllByText('01/03/2026');
    expect(dateCells.length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('01/04/2026').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('01/05/2026').length).toBeGreaterThanOrEqual(1);
  });

  it('handles uneven division correctly', () => {
    render(
      <InstallmentPreview
        totalAmount={100}
        installmentCount={3}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    // 100 / 3 = floor(33.33) = 33.33 per installment
    // First installment gets the remainder
    const cells33 = screen.getAllByText(/R\$ 33,3[34]/);
    expect(cells33.length).toBeGreaterThanOrEqual(2);
  });

  it('displays table headers', () => {
    render(
      <InstallmentPreview
        totalAmount={1200}
        installmentCount={12}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    expect(screen.getByText('Parcela')).toBeInTheDocument();
    expect(screen.getByText('Data Competência')).toBeInTheDocument();
    expect(screen.getByText('Data Vencimento')).toBeInTheDocument();
    expect(screen.getByText('Valor')).toBeInTheDocument();
  });

  it('displays total amount summary', () => {
    render(
      <InstallmentPreview
        totalAmount={1200}
        installmentCount={12}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    expect(screen.getByText(/total: R\$ 1\.200,00/i)).toBeInTheDocument();
  });

  it('handles single digit installment count', () => {
    render(
      <InstallmentPreview
        totalAmount={600}
        installmentCount={6}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    expect(screen.getByText('1/6')).toBeInTheDocument();
    expect(screen.getByText('6/6')).toBeInTheDocument();
  });

  it('handles maximum installment count', () => {
    render(
      <InstallmentPreview
        totalAmount={2400}
        installmentCount={24}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    expect(screen.getByText('1/24')).toBeInTheDocument();
    expect(screen.getByText('24/24')).toBeInTheDocument();
  });

  it('handles large amount values', () => {
    render(
      <InstallmentPreview
        totalAmount={120000}
        installmentCount={12}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    // Each installment should be 10,000.00
    const amountCells = screen.getAllByText('R$ 10.000,00');
    expect(amountCells.length).toBeGreaterThanOrEqual(1);
  });

  it('handles year transition in dates', () => {
    render(
      <InstallmentPreview
        totalAmount={1200}
        installmentCount={3}
        firstCompetenceDate="2026-11-15"
        firstDueDate="2026-11-15"
      />
    );

    // Should include months from November 2026 to January 2027
    const novDates = screen.getAllByText('15/11/2026');
    expect(novDates.length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('15/12/2026').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('15/01/2027').length).toBeGreaterThanOrEqual(1);
  });

  it('displays scrollable container for many installments', () => {
    const { container } = render(
      <InstallmentPreview
        totalAmount={2400}
        installmentCount={24}
        firstCompetenceDate="2026-03-01"
        firstDueDate="2026-03-01"
      />
    );

    // Preview container should have max height class
    const previewContainer = container.querySelector('.max-h-64');
    expect(previewContainer).toBeInTheDocument();
  });

  it('handles missing due date', () => {
    render(
      <InstallmentPreview
        totalAmount={1200}
        installmentCount={3}
        firstCompetenceDate="2026-03-01"
      />
    );

    // Should display -- for due dates when not provided
    const dashCells = screen.getAllByText('--');
    expect(dashCells.length).toBe(3);
  });
});

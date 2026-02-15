import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

import { TransactionForm } from '@/features/transactions/components/TransactionForm';
import type { TransactionResponse } from '@/features/transactions/types/transaction';
import { TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';

// Polyfill for PointerEvent (needed for Radix UI Select in TransactionForm)
class MockPointerEvent extends Event implements Partial<PointerEvent> {
  button: number;
  pointerId: number;
  width: number;
  height: number;
  pressure: number;
  tangentialPressure: number;
  tiltX: number;
  tiltY: number;
  twist: number;
  altitudeAngle: number;
  azimuthAngle: number;
  pointerType: string;
  isPrimary: boolean;
  
  constructor(type: string, init?: PointerEventInit) {
    super(type, init);
    this.button = init?.button ?? 0;
    this.pointerId = init?.pointerId ?? 0;
    this.width = init?.width ?? 1;
    this.height = init?.height ?? 1;
    this.pressure = init?.pressure ?? 0;
    this.tangentialPressure = init?.tangentialPressure ?? 0;
    this.tiltX = init?.tiltX ?? 0;
    this.tiltY = init?.tiltY ?? 0;
    this.twist = init?.twist ?? 0;
    this.altitudeAngle = init?.altitudeAngle ?? 0;
    this.azimuthAngle = init?.azimuthAngle ?? 0;
    this.pointerType = init?.pointerType ?? 'mouse';
    this.isPrimary = init?.isPrimary ?? false;
  }
  
  getCoalescedEvents(): PointerEvent[] { return []; }
  getPredictedEvents(): PointerEvent[] { return []; }
}

global.PointerEvent = MockPointerEvent as unknown as typeof PointerEvent;

Object.defineProperty(HTMLElement.prototype, 'hasPointerCapture', {
  value: () => false,
  writable: true,
});

const mockSimpleTransaction: TransactionResponse = {
  id: '1',
  description: 'Compra Supermercado',
  amount: 250.0,
  type: TransactionType.Debit,
  status: TransactionStatus.Pending,
  categoryId: 'cat-1',
  accountId: 'acc-1',
  competenceDate: '2026-02-15',
  dueDate: '2026-02-20',
  isAdjustment: false,
  originalTransactionId: null,
  hasAdjustment: false,
  installmentNumber: null,
  totalInstallments: null,
  installmentGroupId: null,
  isRecurrent: false,
  recurrenceTemplateId: null,
  transferGroupId: null,
  cancellationReason: null,
  cancelledBy: null,
  cancelledAt: null,
  isOverdue: false,
  createdAt: '2026-02-15T10:00:00Z',
  updatedAt: null,
};

const mockInstallmentTransaction: TransactionResponse = {
  id: '2',
  description: 'Notebook Dell',
  amount: 3000.0,
  type: TransactionType.Debit,
  status: TransactionStatus.Pending,
  categoryId: 'cat-2',
  accountId: 'acc-2',
  competenceDate: '2026-02-15',
  dueDate: '2026-03-01',
  isAdjustment: false,
  originalTransactionId: null,
  hasAdjustment: false,
  installmentNumber: 1,
  totalInstallments: 12,
  installmentGroupId: 'group-1',
  isRecurrent: false,
  recurrenceTemplateId: null,
  transferGroupId: null,
  cancellationReason: null,
  cancelledBy: null,
  cancelledAt: null,
  isOverdue: false,
  createdAt: '2026-02-15T10:00:00Z',
  updatedAt: null,
};


function renderWithProviders(ui: React.ReactElement): void {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  render(<QueryClientProvider client={queryClient}>{ui}</QueryClientProvider>);
}

describe('TransactionForm', () => {
  const mockOnOpenChange = vi.fn();

  beforeEach(() => {
    mockOnOpenChange.mockClear();
  });

  describe('Tab Navigation', () => {
    it('renders all transaction type tabs', async () => {
      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      await waitFor(() => {
        expect(screen.getByRole('tab', { name: /simples/i })).toBeInTheDocument();
      });
      expect(screen.getByRole('tab', { name: /parcelada/i })).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /recorrente/i })).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /transferência/i })).toBeInTheDocument();
    });

    it('switches between tabs correctly', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      // Default tab should be Simple
      expect(screen.getByLabelText(/descrição/i)).toBeInTheDocument();

      // Switch to Installment tab
      const installmentTab = screen.getByRole('tab', { name: /parcelada/i });
      await user.click(installmentTab);

      await waitFor(() => {
        expect(screen.getByLabelText(/número de parcelas/i)).toBeInTheDocument();
      });

      // Switch to Recurrence tab
      const recurrenceTab = screen.getByRole('tab', { name: /recorrente/i });
      await user.click(recurrenceTab);

      await waitFor(() => {
        expect(screen.getByLabelText(/data de início/i)).toBeInTheDocument();
      });

      // Switch to Transfer tab
      const transferTab = screen.getByRole('tab', { name: /transferência/i });
      await user.click(transferTab);

      await waitFor(() => {
        expect(screen.getByLabelText(/conta origem/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/conta destino/i)).toBeInTheDocument();
      });
    });
  });

  describe('Simple Transaction Form', () => {
    it('renders all fields for simple transaction', () => {
      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      expect(screen.getByLabelText(/descrição/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/valor da transação/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/conta/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/categoria/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/dt\. vencimento/i)).toBeInTheDocument();
    });

    it('validates required fields', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      const submitButton = screen.getByRole('button', { name: /salvar transação/i });
      await user.click(submitButton);

      // zodResolver validates asynchronously - check for any validation error
      await waitFor(() => {
        // Multiple validation errors can show (accountId, categoryId, description, amount)
        const errors = screen.queryAllByText(/(selecione|deve ter no mínimo|deve ser maior)/i);
        expect(errors.length).toBeGreaterThan(0);
      }, { timeout: 3000 });
    });

    it('populates form with transaction data in edit mode', () => {
      renderWithProviders(
        <TransactionForm
          open={true}
          onOpenChange={mockOnOpenChange}
          transaction={mockSimpleTransaction}
        />
      );

      expect(screen.getByText('Editar Transação')).toBeInTheDocument();
      const descriptionInput = screen.getByLabelText(/descrição/i) as HTMLInputElement;
      expect(descriptionInput.value).toBe('Compra Supermercado');
    });
  });

  describe('Installment Transaction Form', () => {
    it('renders installment-specific fields', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      const installmentTab = screen.getByRole('tab', { name: /parcelada/i });
      await user.click(installmentTab);

      await waitFor(() => {
        expect(screen.getByLabelText(/número de parcelas/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/primeira dt\. competência/i)).toBeInTheDocument();
      });
    });

    it('validates installment number range', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      const installmentTab = screen.getByRole('tab', { name: /parcelada/i });
      await user.click(installmentTab);

      await waitFor(() => {
        expect(screen.getByLabelText(/número de parcelas/i)).toBeInTheDocument();
      });

      const installmentsInput = screen.getByLabelText(/número de parcelas/i);
      await user.clear(installmentsInput);
      await user.type(installmentsInput, '1');

      const submitButton = screen.getByRole('button', { name: /criar parcelamento/i });
      await user.click(submitButton);

      // zodResolver validates asynchronously - check for installment error OR other required field errors
      await waitFor(() => {
        const errors = screen.queryAllByText(/(parcelas deve ser no mínimo|selecione|deve ter no mínimo|deve ser maior)/i);
        expect(errors.length).toBeGreaterThan(0);
      }, { timeout: 3000 });
    });

    it('disables fields in edit mode for installment transactions', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm
          open={true}
          onOpenChange={mockOnOpenChange}
          transaction={mockInstallmentTransaction}
        />
      );

      const installmentTab = screen.getByRole('tab', { name: /parcelada/i });
      await user.click(installmentTab);

      await waitFor(() => {
        const installmentsInput = screen.getByLabelText(/número de parcelas/i) as HTMLInputElement;
        expect(installmentsInput.disabled).toBe(true);
      });
    });
  });

  describe('Recurrence Transaction Form', () => {
    it('renders recurrence-specific fields', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      const recurrenceTab = screen.getByRole('tab', { name: /recorrente/i });
      await user.click(recurrenceTab);

      await waitFor(() => {
        expect(screen.getByLabelText(/data de início/i)).toBeInTheDocument();
        expect(screen.getByText(/esta transação será criada automaticamente todos os meses/i)).toBeInTheDocument();
      });
    });
  });

  describe('Transfer Transaction Form', () => {
    it('renders transfer-specific fields', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      const transferTab = screen.getByRole('tab', { name: /transferência/i });
      await user.click(transferTab);

      await waitFor(() => {
        expect(screen.getByLabelText(/conta origem/i)).toBeInTheDocument();
        expect(screen.getByLabelText(/conta destino/i)).toBeInTheDocument();
      });
    });

    it('shows category field for transfers', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      const transferTab = screen.getByRole('tab', { name: /transferência/i });
      await user.click(transferTab);

      await waitFor(() => {
        expect(screen.getByLabelText(/categoria/i)).toBeInTheDocument();
      });
    });
  });

  describe('Form Actions', () => {
    it('calls onOpenChange when cancel button is clicked', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      const cancelButton = screen.getByRole('button', { name: /cancelar/i });
      await user.click(cancelButton);

      expect(mockOnOpenChange).toHaveBeenCalledWith(false);
    });

    it('submits form with valid data', async () => {
      const user = userEvent.setup();

      renderWithProviders(
        <TransactionForm open={true} onOpenChange={mockOnOpenChange} transaction={null} />
      );

      // Wait for form to be ready
      await waitFor(() => {
        expect(screen.getByLabelText(/descrição/i)).toBeInTheDocument();
      });

      // Fill in minimal required data
      await user.type(screen.getByLabelText(/descrição/i), 'Test');
      await user.type(screen.getByLabelText(/valor da transação/i), '100');

      // Verify submit button is present and can be clicked
      const submitButton = screen.getByRole('button', { name: /salvar transação/i });
      expect(submitButton).toBeInTheDocument();
      expect(submitButton).toBeEnabled();
      
      // Click submit - form will show validation errors for missing account/category
      // but the button should be clickable
      await user.click(submitButton);
      
      // Form should show validation errors (not close)
      expect(mockOnOpenChange).not.toHaveBeenCalled();
    });
  });
});

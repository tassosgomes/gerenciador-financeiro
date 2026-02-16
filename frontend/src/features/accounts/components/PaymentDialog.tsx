import { useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
  Input,
} from '@/shared/components/ui';
import { usePayInvoice } from '@/features/accounts/hooks/useInvoice';
import { formatCurrency } from '@/shared/utils/formatters';

interface PaymentDialogProps {
  accountId: string;
  accountName: string;
  amountDue: number;
  isOpen: boolean;
  onClose: () => void;
}

export function PaymentDialog({
  accountId,
  accountName,
  amountDue,
  isOpen,
  onClose,
}: PaymentDialogProps): JSX.Element {
  const [amount, setAmount] = useState(amountDue.toFixed(2));
  const [competenceDate, setCompetenceDate] = useState(new Date().toISOString().split('T')[0]);
  const payInvoiceMutation = usePayInvoice();

  const handlePayTotal = (): void => {
    setAmount(amountDue.toFixed(2));
  };

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();
    const amountValue = parseFloat(amount);

    if (isNaN(amountValue) || amountValue <= 0) {
      return;
    }

    payInvoiceMutation.mutate(
      {
        accountId,
        request: {
          amount: amountValue,
          competenceDate,
        },
      },
      {
        onSuccess: () => {
          onClose();
          setAmount('');
          setCompetenceDate('');
        },
      }
    );
  };

  const handleClose = (): void => {
    if (!payInvoiceMutation.isPending) {
      onClose();
      setAmount('');
      setCompetenceDate('');
    }
  };

  const amountValue = parseFloat(amount);
  const isValidAmount = !isNaN(amountValue) && amountValue > 0;

  return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Pagar Fatura</DialogTitle>
          <DialogDescription>
            Cartão: {accountName}
            <br />
            Valor total da fatura: {formatCurrency(amountDue)}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit}>
          <div className="space-y-4">
            {/* Valor do Pagamento */}
            <div>
              <label htmlFor="amount" className="text-sm font-medium text-slate-700 block mb-2">
                Valor do Pagamento
              </label>
              <Input
                id="amount"
                type="number"
                step="0.01"
                min="0.01"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                placeholder="0,00"
                required
              />
              <div className="flex justify-between items-center mt-2">
                <span className="text-xs text-slate-600">
                  Permitido pagamento parcial ou acima do valor
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={handlePayTotal}
                  className="text-xs"
                >
                  Pagar Total
                </Button>
              </div>
            </div>

            {/* Data de Competência */}
            <div>
              <label htmlFor="competenceDate" className="text-sm font-medium text-slate-700 block mb-2">
                Data de Competência
              </label>
              <Input
                id="competenceDate"
                type="date"
                value={competenceDate}
                onChange={(e) => setCompetenceDate(e.target.value)}
                required
              />
              <p className="text-xs text-slate-500 mt-1">
                Data em que o pagamento será registrado
              </p>
            </div>
          </div>

          <DialogFooter className="mt-6">
            <Button
              type="button"
              variant="outline"
              onClick={handleClose}
              disabled={payInvoiceMutation.isPending}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              disabled={!isValidAmount || !competenceDate || payInvoiceMutation.isPending}
            >
              {payInvoiceMutation.isPending ? 'Processando...' : 'Confirmar Pagamento'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

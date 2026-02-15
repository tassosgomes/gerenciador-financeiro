import { useState } from 'react';
import { TrendingUp } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/shared/components/ui/dialog';
import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import { useAdjustTransaction } from '@/features/transactions/hooks/useTransactions';
import { formatCurrency } from '@/shared/utils/formatters';

interface AdjustModalProps {
  transactionId: string;
  currentAmount: number;
  open: boolean;
  onClose: () => void;
}

export function AdjustModal({ transactionId, currentAmount, open, onClose }: AdjustModalProps) {
  const [newAmount, setNewAmount] = useState(currentAmount);
  const [justification, setJustification] = useState('');
  const adjustTransaction = useAdjustTransaction();

  const handleConfirm = async () => {
    if (!justification.trim()) {
      return;
    }

    await adjustTransaction.mutateAsync({
      id: transactionId,
      data: {
        newAmount,
        justification,
      },
    });
    
    setNewAmount(currentAmount);
    setJustification('');
    onClose();
  };

  const handleClose = () => {
    setNewAmount(currentAmount);
    setJustification('');
    onClose();
  };

  const difference = newAmount - currentAmount;
  const differenceText =
    difference > 0 ? `+${formatCurrency(difference)}` : formatCurrency(difference);

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <TrendingUp className="h-5 w-5 text-blue-600" />
            Ajustar Transação
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Comparação de Valores */}
          <div className="rounded-lg border bg-muted/50 p-4">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-muted-foreground">Valor atual:</span>
              <span className="font-medium">{formatCurrency(currentAmount)}</span>
            </div>
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-muted-foreground">Novo valor:</span>
              <span className="font-bold text-lg">{formatCurrency(newAmount)}</span>
            </div>
            <div className="flex items-center justify-between pt-2 border-t">
              <span className="text-sm text-muted-foreground">Diferença:</span>
              <span
                className={`font-medium ${
                  difference > 0 ? 'text-green-600' : difference < 0 ? 'text-red-600' : ''
                }`}
              >
                {differenceText}
              </span>
            </div>
          </div>

          {/* Novo Valor */}
          <div>
            <label htmlFor="newAmount" className="block text-sm font-medium mb-2">
              Novo Valor *
            </label>
            <Input
              id="newAmount"
              type="number"
              step="0.01"
              value={newAmount}
              onChange={(e) => setNewAmount(Number(e.target.value))}
              min="0.01"
            />
          </div>

          {/* Justificativa */}
          <div>
            <label htmlFor="justification" className="block text-sm font-medium mb-2">
              Justificativa *
            </label>
            <Input
              id="justification"
              placeholder="Ex: Correção de valor incorreto"
              value={justification}
              onChange={(e) => setJustification(e.target.value)}
              maxLength={200}
            />
            {justification.trim() === '' && (
              <p className="text-sm text-muted-foreground mt-1">
                A justificativa é obrigatória para ajustes
              </p>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={handleClose}>
            Cancelar
          </Button>
          <Button
            type="button"
            onClick={handleConfirm}
            disabled={adjustTransaction.isPending || !justification.trim() || newAmount <= 0}
          >
            Confirmar Ajuste
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

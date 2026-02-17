import { useState } from 'react';
import { AlertTriangle } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from '@/shared/components/ui/dialog';
import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import { useCancelTransaction, useDeactivateRecurrence } from '@/features/transactions/hooks/useTransactions';

interface CancelModalProps {
  transactionId: string;
  recurrenceTemplateId?: string | null;
  open: boolean;
  onClose: () => void;
}

export function CancelModal({ transactionId, recurrenceTemplateId, open, onClose }: CancelModalProps) {
  const [reason, setReason] = useState('');
  const cancelTransaction = useCancelTransaction();
  const deactivateRecurrence = useDeactivateRecurrence();
  const isRecurrenceCancellation = !!recurrenceTemplateId;

  const handleConfirm = async () => {
    if (recurrenceTemplateId) {
      await deactivateRecurrence.mutateAsync({
        recurrenceTemplateId,
      });
    } else {
      await cancelTransaction.mutateAsync({
        id: transactionId,
        data: { reason: reason || undefined },
      });
    }

    setReason('');
    onClose();
  };

  const handleClose = () => {
    setReason('');
    onClose();
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-yellow-600" />
            {isRecurrenceCancellation ? 'Desativar Recorrência' : 'Cancelar Transação'}
          </DialogTitle>
          <DialogDescription>
            {isRecurrenceCancellation
              ? 'As ocorrências futuras não pagas serão removidas automaticamente. Ocorrências já pagas serão mantidas.'
              : 'Esta ação é irreversível. A transação será marcada como cancelada e não poderá ser mais editada.'}
          </DialogDescription>
        </DialogHeader>

        {!isRecurrenceCancellation && (
          <div className="space-y-4 py-4">
            <div>
              <label htmlFor="reason" className="block text-sm font-medium mb-2">
                Motivo do cancelamento (opcional)
              </label>
              <Input
                id="reason"
                placeholder="Ex: Transação duplicada"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                maxLength={200}
              />
            </div>
          </div>
        )}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={handleClose}>
            Voltar
          </Button>
          <Button
            type="button"
            variant="destructive"
            onClick={handleConfirm}
            disabled={cancelTransaction.isPending || deactivateRecurrence.isPending}
          >
            {isRecurrenceCancellation ? 'Confirmar Desativação' : 'Confirmar Cancelamento'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

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
import { useCancelTransaction } from '@/features/transactions/hooks/useTransactions';

interface CancelModalProps {
  transactionId: string;
  open: boolean;
  onClose: () => void;
}

export function CancelModal({ transactionId, open, onClose }: CancelModalProps) {
  const [reason, setReason] = useState('');
  const cancelTransaction = useCancelTransaction();

  const handleConfirm = async () => {
    await cancelTransaction.mutateAsync({
      id: transactionId,
      data: { reason: reason || undefined },
    });
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
            Cancelar Transação
          </DialogTitle>
          <DialogDescription>
            Esta ação é irreversível. A transação será marcada como cancelada e não poderá ser
            mais editada.
          </DialogDescription>
        </DialogHeader>

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

        <DialogFooter>
          <Button type="button" variant="outline" onClick={handleClose}>
            Voltar
          </Button>
          <Button
            type="button"
            variant="destructive"
            onClick={handleConfirm}
            disabled={cancelTransaction.isPending}
          >
            Confirmar Cancelamento
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

import { useParams } from 'react-router-dom';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useTransaction, useTransactionHistory } from '@/features/transactions/hooks/useTransactions';
import { TransactionDetail } from '@/features/transactions/components/TransactionDetail';
import { TransactionHistoryTimeline } from '@/features/transactions/components/TransactionHistoryTimeline';
import { ReceiptItemsSection } from '@/features/transactions/components/ReceiptItemsSection';

export function TransactionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: transaction, isLoading: isLoadingTransaction } = useTransaction(id!);
  const { data: history = [], isLoading: isLoadingHistory } = useTransactionHistory(id!);

  if (isLoadingTransaction || isLoadingHistory) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-10 w-64" />
        <Skeleton className="h-96" />
        <Skeleton className="h-64" />
      </div>
    );
  }

  if (!transaction) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-muted-foreground">Transação não encontrada</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <TransactionDetail transaction={transaction} />
      {transaction.hasReceipt && (
        <ReceiptItemsSection transactionId={transaction.id} hasReceipt={transaction.hasReceipt} />
      )}
      <TransactionHistoryTimeline history={history} />
    </div>
  );
}

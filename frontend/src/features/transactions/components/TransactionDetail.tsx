import { useState } from 'react';
import { ArrowLeft, Ban, TrendingUp, RepeatIcon, ArrowLeftRight, CheckCircle2, ReceiptText } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/shared/components/ui/button';
import { Badge } from '@/shared/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { formatCurrency, formatDate } from '@/shared/utils/formatters';
import {
  TransactionType,
  TransactionStatus,
  type TransactionResponse,
} from '@/features/transactions/types/transaction';
import { useAccounts } from '@/features/accounts/hooks/useAccounts';
import { useCategories } from '@/features/categories/hooks/useCategories';
import { useMarkTransactionAsPaid } from '@/features/transactions/hooks/useTransactions';
import { CancelModal } from './CancelModal';
import { AdjustModal } from './AdjustModal';

interface TransactionDetailProps {
  transaction: TransactionResponse;
}

export function TransactionDetail({ transaction }: TransactionDetailProps) {
  const navigate = useNavigate();
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();
  const markAsPaid = useMarkTransactionAsPaid();
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [showAdjustModal, setShowAdjustModal] = useState(false);

  const account = accounts?.find((acc) => acc.id === transaction.accountId);
  const category = categories?.find((cat) => cat.id === transaction.categoryId);

  const isCancelled = transaction.status === TransactionStatus.Cancelled;
  const isPending = transaction.status === TransactionStatus.Pending;
  const canCancel = !isCancelled;
  const canAdjust = !isCancelled && !transaction.hasAdjustment;

  const getStatusBadge = () => {
    if (isCancelled) {
      return (
        <Badge variant="outline" className="bg-gray-100 text-gray-800">
          Cancelado
        </Badge>
      );
    }
    if (transaction.status === TransactionStatus.Paid) {
      return (
        <Badge variant="outline" className="bg-green-100 text-green-800">
          Pago
        </Badge>
      );
    }
    return (
      <Badge variant="outline" className="bg-yellow-100 text-yellow-800">
        Pendente
      </Badge>
    );
  };

  const getTypeBadge = () => {
    return transaction.type === TransactionType.Credit ? (
      <Badge variant="outline" className="bg-green-100 text-green-800">
        Crédito
      </Badge>
    ) : (
      <Badge variant="outline" className="bg-red-100 text-red-800">
        Débito
      </Badge>
    );
  };

  const getAmountClassName = () => {
    if (isCancelled) return 'line-through text-muted-foreground';
    return transaction.type === TransactionType.Credit ? 'text-green-600' : 'text-red-600';
  };

  return (
    <div className="space-y-6">
      {/* Header com Botão Voltar */}
      <div className="flex items-center gap-4">
        <Button variant="outline" size="icon" onClick={() => navigate('/transactions')}>
          <ArrowLeft className="h-4 w-4" />
        </Button>
        <h1 className="text-2xl font-bold">Detalhes da Transação</h1>
      </div>

      {/* Card Principal */}
      <Card>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div>
              <CardTitle className={isCancelled ? 'line-through' : ''}>
                {transaction.description}
              </CardTitle>
              <div className="mt-2 flex flex-wrap items-center gap-2">
                {getStatusBadge()}
                {getTypeBadge()}
                {transaction.isAdjustment && (
                  <Badge variant="outline" className="bg-blue-100 text-blue-800">
                    Ajuste
                  </Badge>
                )}
                {transaction.installmentNumber && transaction.totalInstallments && (
                  <Badge variant="outline">
                    Parcela {transaction.installmentNumber}/{transaction.totalInstallments}
                  </Badge>
                )}
                {transaction.isRecurrent && (
                  <Badge variant="outline" className="flex items-center gap-1">
                    <RepeatIcon className="h-3 w-3" />
                    Recorrente
                  </Badge>
                )}
                {transaction.transferGroupId && (
                  <Badge variant="outline" className="flex items-center gap-1">
                    <ArrowLeftRight className="h-3 w-3" />
                    Transferência
                  </Badge>
                )}
                {transaction.hasReceipt && (
                  <Badge variant="outline" className="flex items-center gap-1">
                    <ReceiptText className="h-3 w-3" />
                    Cupom Fiscal
                  </Badge>
                )}
              </div>
            </div>
            <div className="text-right">
              <p className="text-sm text-muted-foreground">Valor</p>
              <p className={`text-3xl font-bold ${getAmountClassName()}`}>
                {formatCurrency(transaction.amount)}
              </p>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {/* Dados Principais */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className="text-sm text-muted-foreground">Conta</p>
              <p className="font-medium">{account?.name ?? 'N/A'}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Categoria</p>
              <p className="font-medium">{category?.name ?? 'N/A'}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Data de Competência</p>
              <p className="font-medium">{formatDate(transaction.competenceDate)}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Data de Vencimento</p>
              <p className="font-medium">
                {transaction.dueDate ? formatDate(transaction.dueDate) : '--'}
              </p>
            </div>
          </div>

          {/* Metadados Especiais */}
          {transaction.isAdjustment && transaction.originalTransactionId && (
            <div className="rounded-lg border border-blue-200 bg-blue-50 p-4">
              <p className="text-sm font-medium text-blue-800">Transação de Ajuste</p>
              <p className="text-sm text-blue-700">
                Esta é uma transação de ajuste referente à transação original.
              </p>
              <Button
                variant="link"
                size="sm"
                className="p-0 h-auto text-blue-700"
                onClick={() => navigate(`/transactions/${transaction.originalTransactionId}`)}
              >
                Ver transação original
              </Button>
            </div>
          )}

          {transaction.hasAdjustment && (
            <div className="rounded-lg border border-orange-200 bg-orange-50 p-4">
              <p className="text-sm font-medium text-orange-800">Transação Ajustada</p>
              <p className="text-sm text-orange-700">
                Esta transação foi ajustada. O valor exibido é o valor original.
              </p>
            </div>
          )}

          {isCancelled && transaction.cancellationReason && (
            <div className="rounded-lg border border-gray-200 bg-gray-50 p-4">
              <p className="text-sm font-medium text-gray-800">Motivo do Cancelamento</p>
              <p className="text-sm text-gray-700">{transaction.cancellationReason}</p>
              <p className="text-xs text-gray-600 mt-2">
                Cancelado em: {formatDate(transaction.cancelledAt!)}
              </p>
            </div>
          )}

          {transaction.isOverdue && !isCancelled && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-4">
              <p className="text-sm font-medium text-red-800">⚠️ Transação Atrasada</p>
              <p className="text-sm text-red-700">
                A data de vencimento já passou e a transação ainda está pendente.
              </p>
            </div>
          )}

          {/* Ações */}
          <div className="flex gap-2 pt-4 border-t">
            <Button
              variant="outline"
              onClick={() => markAsPaid.mutate({ id: transaction.id })}
              disabled={!isPending || markAsPaid.isPending}
            >
              <CheckCircle2 className="mr-2 h-4 w-4" />
              Marcar como Pago
            </Button>
            <Button
              variant="outline"
              onClick={() => setShowCancelModal(true)}
              disabled={!canCancel}
            >
              <Ban className="mr-2 h-4 w-4" />
              Cancelar
            </Button>
            <Button
              variant="outline"
              onClick={() => setShowAdjustModal(true)}
              disabled={!canAdjust}
            >
              <TrendingUp className="mr-2 h-4 w-4" />
              Ajustar
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Modais */}
      <CancelModal
        transactionId={transaction.id}
        recurrenceTemplateId={transaction.recurrenceTemplateId}
        open={showCancelModal}
        onClose={() => setShowCancelModal(false)}
      />
      <AdjustModal
        transactionId={transaction.id}
        currentAmount={transaction.amount}
        open={showAdjustModal}
        onClose={() => setShowAdjustModal(false)}
      />
    </div>
  );
}

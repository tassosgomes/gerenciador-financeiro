import { useNavigate } from 'react-router-dom';
import { RepeatIcon, ArrowLeftRight, Receipt } from 'lucide-react';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/shared/components/ui/table';
import { Badge } from '@/shared/components/ui/badge';
import { EmptyState } from '@/shared/components/ui';
import { formatCurrency, formatDate } from '@/shared/utils/formatters';
import { TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';
import type { TransactionResponse } from '@/features/transactions/types/transaction';
import { useAccounts } from '@/features/accounts/hooks/useAccounts';
import { useCategories } from '@/features/categories/hooks/useCategories';

interface TransactionTableProps {
  transactions: TransactionResponse[];
}

export function TransactionTable({ transactions }: TransactionTableProps) {
  const navigate = useNavigate();
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();

  const getAccountName = (accountId: string) => {
    return accounts?.find((acc) => acc.id === accountId)?.name ?? 'N/A';
  };

  const getCategoryName = (categoryId: string) => {
    return categories?.find((cat) => cat.id === categoryId)?.name ?? 'N/A';
  };

  const getStatusBadge = (status: TransactionStatus, isCancelled: boolean) => {
    if (isCancelled || status === TransactionStatus.Cancelled) {
      return (
        <Badge variant="outline" className="bg-gray-100 text-gray-800">
          Cancelado
        </Badge>
      );
    }

    if (status === TransactionStatus.Paid) {
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

  const getAmountClassName = (type: TransactionType) => {
    return type === TransactionType.Credit ? 'text-green-600' : 'text-red-600';
  };

  const formatAmount = (amount: number, type: TransactionType) => {
    const formattedAmount = formatCurrency(amount);
    return type === TransactionType.Debit ? `- ${formattedAmount}` : formattedAmount;
  };

  const getDescriptionWithIndicators = (transaction: TransactionResponse) => {
    const indicators = [];

    if (transaction.installmentNumber && transaction.totalInstallments) {
      indicators.push(
        <span key="installment" className="text-xs text-muted-foreground" aria-label={`Parcela ${transaction.installmentNumber} de ${transaction.totalInstallments}`}>
          {transaction.installmentNumber}/{transaction.totalInstallments}
        </span>
      );
    }

    if (transaction.isRecurrent) {
      indicators.push(<RepeatIcon key="recurrent" className="h-3 w-3 text-muted-foreground" aria-label="Transação recorrente" />);
    }

    if (transaction.transferGroupId) {
      indicators.push(
        <ArrowLeftRight key="transfer" className="h-3 w-3 text-muted-foreground" aria-label="Transferência" />
      );
    }

    const isCancelled = transaction.status === TransactionStatus.Cancelled;

    return (
      <div className="flex items-center gap-2">
        <span className={isCancelled ? 'line-through' : ''}>{transaction.description}</span>
        {indicators.length > 0 && (
          <span className="flex items-center gap-1">{indicators}</span>
        )}
      </div>
    );
  };

  if (transactions.length === 0) {
    return (
      <EmptyState
        icon={Receipt}
        title="Nenhuma transação encontrada"
        description="As transações que correspondem aos filtros aparecerão aqui"
      />
    );
  }

  return (
    <div className="w-full overflow-x-auto">
      <Table className="min-w-[640px]">
        <TableHeader>
          <TableRow>
            <TableHead className="min-w-[100px]">Data</TableHead>
            <TableHead className="min-w-[180px]">Descrição</TableHead>
            <TableHead className="min-w-[120px]">Categoria</TableHead>
            <TableHead className="min-w-[120px]">Conta</TableHead>
            <TableHead className="min-w-[120px] text-right">Valor</TableHead>
            <TableHead className="min-w-[100px]">Status</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {transactions.map((transaction) => {
            const isCancelled = transaction.status === TransactionStatus.Cancelled;

            return (
              <TableRow
                key={transaction.id}
                className="cursor-pointer"
                onClick={() => navigate(`/transactions/${transaction.id}`)}
              >
                <TableCell className={isCancelled ? 'line-through' : ''}>
                  {formatDate(transaction.competenceDate)}
                </TableCell>
                <TableCell>{getDescriptionWithIndicators(transaction)}</TableCell>
                <TableCell className={isCancelled ? 'line-through' : ''}>
                  {getCategoryName(transaction.categoryId)}
                </TableCell>
                <TableCell className={isCancelled ? 'line-through' : ''}>
                  {getAccountName(transaction.accountId)}
                </TableCell>
                <TableCell
                  className={`text-right font-medium ${
                    isCancelled ? 'line-through' : getAmountClassName(transaction.type)
                  }`}
                >
                  {formatAmount(transaction.amount, transaction.type)}
                </TableCell>
                <TableCell>{getStatusBadge(transaction.status, isCancelled)}</TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}

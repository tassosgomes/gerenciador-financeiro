import { useNavigate } from 'react-router-dom';
import { RepeatIcon } from 'lucide-react';

import { useTransactions } from '@/features/transactions/hooks/useTransactions';
import { useAccounts } from '@/features/accounts/hooks/useAccounts';
import { useCategories } from '@/features/categories/hooks/useCategories';
import { TransactionType } from '@/features/transactions/types/transaction';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { Badge } from '@/shared/components/ui/badge';
import { Button } from '@/shared/components/ui/button';
import { formatCurrency, formatDate } from '@/shared/utils/formatters';

export function RecentTransactions(): JSX.Element {
  const navigate = useNavigate();
  const { data, isLoading } = useTransactions({ page: 1, size: 5 });
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();

  const transactions = (data?.data ?? [])
    .slice()
    .sort(
      (left, right) =>
        new Date(right.competenceDate).getTime() - new Date(left.competenceDate).getTime()
    )
    .slice(0, 5);

  const getAccountName = (accountId: string) => {
    return accounts?.find((account) => account.id === accountId)?.name ?? 'Conta';
  };

  const getCategoryName = (categoryId: string) => {
    return categories?.find((category) => category.id === categoryId)?.name ?? 'Categoria';
  };

  const formatAmount = (amount: number, type: number) => {
    const formattedValue = formatCurrency(amount);
    return type === TransactionType.Debit ? `- ${formattedValue}` : formattedValue;
  };

  const amountClassName = (type: number) => {
    return type === TransactionType.Debit ? 'text-red-600' : 'text-green-600';
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Transações Recentes</CardTitle>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="space-y-3">
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
            <Skeleton className="h-16 w-full" />
          </div>
        ) : transactions.length === 0 ? (
          <div className="flex items-center justify-center py-6 text-sm text-muted-foreground">
            Nenhuma transação recente encontrada
          </div>
        ) : (
          <div className="space-y-3">
            {transactions.map((transaction) => (
              <button
                key={transaction.id}
                type="button"
                onClick={() => navigate(`/transactions/${transaction.id}`)}
                className="w-full rounded-md border p-3 text-left transition-colors hover:bg-accent"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0 space-y-1">
                    <p className="truncate text-sm font-medium">{transaction.description}</p>
                    <div className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
                      <span>{formatDate(transaction.competenceDate)}</span>
                      <span>•</span>
                      <span>{getCategoryName(transaction.categoryId)}</span>
                      <span>•</span>
                      <span>{getAccountName(transaction.accountId)}</span>
                    </div>
                  </div>
                  <div className="flex shrink-0 flex-col items-end gap-1">
                    <span className={`text-sm font-semibold ${amountClassName(transaction.type)}`}>
                      {formatAmount(transaction.amount, transaction.type)}
                    </span>
                    <div className="flex items-center gap-1">
                      <Badge variant="outline">
                        {transaction.type === TransactionType.Debit ? 'Despesa' : 'Receita'}
                      </Badge>
                      {transaction.isRecurrent && (
                        <Badge variant="outline" className="flex items-center gap-1">
                          <RepeatIcon className="h-3 w-3" />
                          Recorrente
                        </Badge>
                      )}
                    </div>
                  </div>
                </div>
              </button>
            ))}

            <div className="pt-1 text-right">
              <Button variant="ghost" size="sm" onClick={() => navigate('/transactions')}>
                Ver todas
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

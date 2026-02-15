import { useState } from 'react';
import { Plus } from 'lucide-react';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent } from '@/shared/components/ui/card';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useTransactions } from '@/features/transactions/hooks/useTransactions';
import { useTransactionFilters } from '@/features/transactions/hooks/useTransactionFilters';
import { TransactionFilters } from '@/features/transactions/components/TransactionFilters';
import { TransactionTable } from '@/features/transactions/components/TransactionTable';
import { Pagination } from '@/features/transactions/components/Pagination';
import { TransactionForm } from '@/features/transactions/components/TransactionForm';

export default function TransactionsPage(): JSX.Element {
  const [showForm, setShowForm] = useState(false);
  const { filters, setFilter, clearFilters } = useTransactionFilters();
  const { data, isLoading } = useTransactions(filters);

  const transactions = data?.data ?? [];
  const pagination = data?.pagination ?? { page: 1, size: 20, total: 0, totalPages: 1 };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Transações</h1>
          <p className="text-muted-foreground">
            Gerencie suas receitas, despesas e transferências
          </p>
        </div>
        <Button onClick={() => setShowForm(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Nova Transação
        </Button>
      </div>

      {/* Filtros */}
      <Card>
        <CardContent className="pt-6">
          <TransactionFilters
            accountId={filters.accountId}
            categoryId={filters.categoryId}
            type={filters.type}
            status={filters.status}
            dateFrom={filters.dateFrom}
            dateTo={filters.dateTo}
            onFilterChange={setFilter}
            onClearFilters={clearFilters}
          />
        </CardContent>
      </Card>

      {/* Tabela */}
      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="space-y-4 p-6">
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-12 w-full" />
            </div>
          ) : (
            <>
              <TransactionTable transactions={transactions} />
              {pagination.totalPages > 1 && (
                <div className="border-t">
                  <Pagination
                    currentPage={pagination.page}
                    totalPages={pagination.totalPages}
                    pageSize={pagination.size}
                    onPageChange={(page) => setFilter('page', page)}
                    onPageSizeChange={(size) => setFilter('size', size)}
                  />
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* Modal de Formulário */}
      <TransactionForm open={showForm} onOpenChange={setShowForm} />
    </div>
  );
}

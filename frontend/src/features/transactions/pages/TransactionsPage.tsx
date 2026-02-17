import { useEffect, useMemo, useState } from 'react';
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
import type { TransactionFilters as TransactionFiltersType } from '@/features/transactions/types/transaction';

export default function TransactionsPage(): JSX.Element {
  const [showForm, setShowForm] = useState(false);
  const { filters, setFilters, setFilter, clearFilters } = useTransactionFilters();
  const [draftFilters, setDraftFilters] = useState<TransactionFiltersType>(filters);
  const { data, isLoading } = useTransactions(filters);

  useEffect(() => {
    setDraftFilters(filters);
  }, [filters]);

  const handleFilterChange = (key: string, value: string | number | undefined) => {
    setDraftFilters((prev) => {
      const next = { ...prev, [key]: value } as TransactionFiltersType;

      if (key !== 'page' && key !== 'size') {
        next.page = 1;
      }

      return next;
    });
  };

  const handleSearch = () => {
    setFilters(draftFilters);
  };

  const handleClearFilters = () => {
    setDraftFilters({});
    clearFilters();
  };

  const hasPendingFilterChanges = useMemo(() => {
    const keys: Array<keyof TransactionFiltersType> = [
      'accountId',
      'categoryId',
      'type',
      'status',
      'dateFrom',
      'dateTo',
      'page',
      'size',
    ];

    return keys.some((key) => draftFilters[key] !== filters[key]);
  }, [draftFilters, filters]);

  const transactions = data?.data ?? [];
  const pagination = data?.pagination ?? { page: 1, size: 20, total: 0, totalPages: 1 };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold">Transações</h1>
          <p className="text-muted-foreground">
            Gerencie suas receitas, despesas e transferências
          </p>
        </div>
        <Button onClick={() => setShowForm(true)} className="shrink-0">
          <Plus className="mr-2 h-4 w-4" />
          Nova Transação
        </Button>
      </div>

      {/* Filtros */}
      <Card>
        <CardContent className="pt-6">
          <TransactionFilters
            accountId={draftFilters.accountId}
            categoryId={draftFilters.categoryId}
            type={draftFilters.type}
            status={draftFilters.status}
            dateFrom={draftFilters.dateFrom}
            dateTo={draftFilters.dateTo}
            onFilterChange={handleFilterChange}
            onClearFilters={handleClearFilters}
            onSearch={handleSearch}
            isSearchDisabled={!hasPendingFilterChanges}
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

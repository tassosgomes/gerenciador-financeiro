import { useEffect, useMemo, useState } from 'react';
import { Plus, ReceiptText } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent } from '@/shared/components/ui/card';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { Input } from '@/shared/components/ui/input';
import { useTransactions } from '@/features/transactions/hooks/useTransactions';
import { useTransactionFilters } from '@/features/transactions/hooks/useTransactionFilters';
import { TransactionFilters } from '@/features/transactions/components/TransactionFilters';
import { TransactionTable } from '@/features/transactions/components/TransactionTable';
import { Pagination } from '@/features/transactions/components/Pagination';
import { TransactionForm } from '@/features/transactions/components/TransactionForm';
import type { TransactionFilters as TransactionFiltersType } from '@/features/transactions/types/transaction';
import { MonthNavigator } from '@/features/dashboard/components/MonthNavigator';

function buildMonthRange(month: number, year: number): { dateFrom: string; dateTo: string } {
  const start = new Date(year, month - 1, 1);
  const end = new Date(year, month, 0);

  const toLocalIsoDate = (value: Date): string => {
    const yyyy = value.getFullYear();
    const mm = String(value.getMonth() + 1).padStart(2, '0');
    const dd = String(value.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  };

  return {
    dateFrom: toLocalIsoDate(start),
    dateTo: toLocalIsoDate(end),
  };
}

function getMonthAndYearFromDate(date?: string): { month: number; year: number } {
  if (!date) {
    const now = new Date();
    return { month: now.getMonth() + 1, year: now.getFullYear() };
  }

  const parsed = new Date(`${date}T00:00:00`);
  if (Number.isNaN(parsed.getTime())) {
    const now = new Date();
    return { month: now.getMonth() + 1, year: now.getFullYear() };
  }

  return { month: parsed.getMonth() + 1, year: parsed.getFullYear() };
}

export default function TransactionsPage(): JSX.Element {
  const navigate = useNavigate();
  const [showForm, setShowForm] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
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

  const { month: selectedMonth, year: selectedYear } = getMonthAndYearFromDate(filters.dateFrom);

  const handleMonthNavigate = (month: number, year: number) => {
    const monthRange = buildMonthRange(month, year);
    const nextFilters: TransactionFiltersType = {
      ...filters,
      dateFrom: monthRange.dateFrom,
      dateTo: monthRange.dateTo,
      page: 1,
    };

    setDraftFilters(nextFilters);
    setFilters(nextFilters);
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
        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            onClick={() => navigate('/transactions/import-receipt')}
            className="shrink-0"
          >
            <ReceiptText className="mr-2 h-4 w-4" />
            Importar Cupom
          </Button>
          <Button onClick={() => setShowForm(true)} className="shrink-0">
            <Plus className="mr-2 h-4 w-4" />
            Nova Transação
          </Button>
        </div>
      </div>

      {/* Filtros */}
      <Card>
        <CardContent className="pt-6">
          <div className="mb-4">
            <MonthNavigator month={selectedMonth} year={selectedYear} onNavigate={handleMonthNavigate} />
          </div>

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
              <div className="border-b p-4">
                <Input
                  value={searchTerm}
                  onChange={(event) => setSearchTerm(event.target.value)}
                  placeholder="Buscar por descrição, categoria ou conta"
                  aria-label="Busca livre"
                />
              </div>
              <TransactionTable transactions={transactions} searchTerm={searchTerm} />
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

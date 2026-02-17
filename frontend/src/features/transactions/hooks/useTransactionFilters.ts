import { useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { TransactionFilters, TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';

export function useTransactionFilters() {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters = useMemo<TransactionFilters>(() => {
    return {
      accountId: searchParams.get('accountId') ?? undefined,
      categoryId: searchParams.get('categoryId') ?? undefined,
      type: searchParams.get('type') ? (Number(searchParams.get('type')) as TransactionType) : undefined,
      status: searchParams.get('status') ? (Number(searchParams.get('status')) as TransactionStatus) : undefined,
      dateFrom: searchParams.get('dateFrom') ?? undefined,
      dateTo: searchParams.get('dateTo') ?? undefined,
      page: Number(searchParams.get('page') ?? 1),
      size: Number(searchParams.get('size') ?? 20),
    };
  }, [searchParams]);

  const setFilters = (nextFilters: TransactionFilters) => {
    setSearchParams(() => {
      const newParams = new URLSearchParams();

      Object.entries(nextFilters).forEach(([key, value]) => {
        if (value !== undefined && value !== '') {
          newParams.set(key, String(value));
        }
      });

      return newParams;
    });
  };

  const setFilter = (key: string, value: string | number | undefined) => {
    setSearchParams((prev) => {
      const newParams = new URLSearchParams(prev);
      
      if (value !== undefined && value !== '') {
        newParams.set(key, String(value));
      } else {
        newParams.delete(key);
      }
      
      // Reset page on filter change (except when changing page itself)
      if (key !== 'page' && key !== 'size') {
        newParams.set('page', '1');
      }
      
      return newParams;
    });
  };

  const clearFilters = () => {
    setSearchParams({});
  };

  return { filters, setFilters, setFilter, clearFilters };
}

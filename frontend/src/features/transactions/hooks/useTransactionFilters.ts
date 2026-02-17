import { useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import type { TransactionFilters, TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';

function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function getCurrentMonthRange(): { dateFrom: string; dateTo: string } {
  const now = new Date();
  const monthStart = new Date(now.getFullYear(), now.getMonth(), 1);
  const monthEnd = new Date(now.getFullYear(), now.getMonth() + 1, 0);

  return {
    dateFrom: toIsoDate(monthStart),
    dateTo: toIsoDate(monthEnd),
  };
}

export function useTransactionFilters() {
  const [searchParams, setSearchParams] = useSearchParams();
  const currentMonthRange = getCurrentMonthRange();

  const filters = useMemo<TransactionFilters>(() => {
    return {
      accountId: searchParams.get('accountId') ?? undefined,
      categoryId: searchParams.get('categoryId') ?? undefined,
      type: searchParams.get('type') ? (Number(searchParams.get('type')) as TransactionType) : undefined,
      status: searchParams.get('status') ? (Number(searchParams.get('status')) as TransactionStatus) : undefined,
      dateFrom: searchParams.get('dateFrom') ?? currentMonthRange.dateFrom,
      dateTo: searchParams.get('dateTo') ?? currentMonthRange.dateTo,
      page: Number(searchParams.get('page') ?? 1),
      size: Number(searchParams.get('size') ?? 20),
    };
  }, [searchParams, currentMonthRange.dateFrom, currentMonthRange.dateTo]);

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

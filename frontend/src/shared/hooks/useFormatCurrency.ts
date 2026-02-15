import { useMemo } from 'react';

import { formatCurrency } from '@/shared/utils';

export function useFormatCurrency(value: number): string {
  return useMemo(() => formatCurrency(value), [value]);
}

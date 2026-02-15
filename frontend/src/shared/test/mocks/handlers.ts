import { http, HttpResponse } from 'msw';

import { authHandlers } from '@/features/auth/test/handlers';
import { dashboardHandlers } from '@/features/dashboard/test/handlers';
import { accountsHandlers } from '@/features/accounts/test/handlers';
import { categoriesHandlers } from '@/features/categories/test/handlers';
import { transactionsHandlers } from '@/features/transactions/test/handlers';

export const handlers = [
  http.get('/api/health', () => {
    return HttpResponse.json({ status: 'ok' });
  }),
  ...authHandlers,
  ...dashboardHandlers,
  ...accountsHandlers,
  ...categoriesHandlers,
  ...transactionsHandlers,
];

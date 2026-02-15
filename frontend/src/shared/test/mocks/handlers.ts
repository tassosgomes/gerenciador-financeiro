import { http, HttpResponse } from 'msw';

import { authHandlers } from '@/features/auth/test/handlers';
import { dashboardHandlers } from '@/features/dashboard/test/handlers';
import { accountsHandlers } from '@/features/accounts/test/handlers';

export const handlers = [
  http.get('/api/health', () => {
    return HttpResponse.json({ status: 'ok' });
  }),
  ...authHandlers,
  ...dashboardHandlers,
  ...accountsHandlers,
];

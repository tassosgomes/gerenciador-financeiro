import { http, HttpResponse } from 'msw';

import { authHandlers } from '@/features/auth/test/handlers';

export const handlers = [
  http.get('/api/health', () => {
    return HttpResponse.json({ status: 'ok' });
  }),
  ...authHandlers,
];

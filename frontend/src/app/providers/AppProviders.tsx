import type { PropsWithChildren } from 'react';

import { Toaster } from '@/shared/components/ui';

import { QueryProvider } from './QueryProvider';

export function AppProviders({ children }: PropsWithChildren): JSX.Element {
  return (
    <QueryProvider>
      {children}
      <Toaster richColors position="top-right" />
    </QueryProvider>
  );
}

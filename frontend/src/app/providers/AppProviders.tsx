import type { PropsWithChildren } from 'react';

import { QueryProvider } from './QueryProvider';

export function AppProviders({ children }: PropsWithChildren): JSX.Element {
  return <QueryProvider>{children}</QueryProvider>;
}

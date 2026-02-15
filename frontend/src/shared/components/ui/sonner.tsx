import type { ComponentProps } from 'react';

import { Toaster as Sonner } from 'sonner';

type ToasterProps = ComponentProps<typeof Sonner>;

export function Toaster(props: ToasterProps): JSX.Element {
  return <Sonner {...props} />;
}

import type { LucideIcon } from 'lucide-react';

import { Button } from '@/shared/components/ui/button';

interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description: string;
  actionLabel?: string;
  onAction?: () => void;
}

export function EmptyState({
  icon: Icon,
  title,
  description,
  actionLabel,
  onAction,
}: EmptyStateProps): JSX.Element {
  return (
    <div className="flex flex-col items-center justify-center py-16 text-center" role="region" aria-label="Empty state">
      <Icon className="h-16 w-16 text-slate-300 mb-4" aria-hidden="true" />
      <h3 className="text-lg font-semibold text-slate-700 mb-2">{title}</h3>
      <p className="text-sm text-slate-500 mb-6 max-w-md">{description}</p>
      {actionLabel && onAction && (
        <Button onClick={onAction}>{actionLabel}</Button>
      )}
    </div>
  );
}

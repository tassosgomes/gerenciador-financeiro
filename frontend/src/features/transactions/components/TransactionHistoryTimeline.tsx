import { Clock } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { formatDate } from '@/shared/utils/formatters';
import type { TransactionHistoryEntry } from '@/features/transactions/types/transaction';

interface TransactionHistoryTimelineProps {
  history: TransactionHistoryEntry[];
}

export function TransactionHistoryTimeline({ history }: TransactionHistoryTimelineProps) {
  if (history.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Clock className="h-5 w-5" />
            Histórico de Auditoria
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground text-center py-4">
            Nenhuma ação registrada ainda.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Clock className="h-5 w-5" />
          Histórico de Auditoria
        </CardTitle>
      </CardHeader>
      <CardContent>
        <div className="relative space-y-6 before:absolute before:inset-0 before:ml-5 before:h-full before:w-0.5 before:bg-border">
          {history.map((entry, index) => (
            <div key={entry.id} className="relative flex gap-4">
              {/* Indicador */}
              <div
                className={`relative z-10 flex h-10 w-10 shrink-0 items-center justify-center rounded-full border-2 bg-background ${
                  index === 0 ? 'border-primary' : 'border-border'
                }`}
              >
                <Clock className={`h-4 w-4 ${index === 0 ? 'text-primary' : 'text-muted-foreground'}`} />
              </div>

              {/* Conteúdo */}
              <div className="flex-1 space-y-1">
                <div className="flex items-start justify-between">
                  <div>
                    <p className="font-medium">{getActionLabel(entry.action)}</p>
                    <p className="text-sm text-muted-foreground">Por {entry.performedBy}</p>
                  </div>
                  <time className="text-xs text-muted-foreground">
                    {formatDate(entry.performedAt)}
                  </time>
                </div>
                {entry.details && (
                  <p className="text-sm text-muted-foreground rounded-md bg-muted p-2 mt-2">
                    {entry.details}
                  </p>
                )}
              </div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}

function getActionLabel(action: string): string {
  const labels: Record<string, string> = {
    Created: 'Transação criada',
    Updated: 'Transação atualizada',
    Cancelled: 'Transação cancelada',
    Adjusted: 'Transação ajustada',
    StatusChanged: 'Status alterado',
  };

  return labels[action] ?? action;
}

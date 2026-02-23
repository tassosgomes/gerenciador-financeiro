import { ReceiptText } from 'lucide-react';

import { Badge } from '@/shared/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useTransactionReceipt } from '@/features/transactions/hooks/useTransactionReceipt';
import { ReceiptPreview } from '@/features/transactions/components/ReceiptPreview';
import { formatAccessKey } from '@/features/transactions/utils/receiptFormatters';

interface ReceiptItemsSectionProps {
  transactionId: string;
  hasReceipt: boolean;
}

export function ReceiptItemsSection({ transactionId, hasReceipt }: ReceiptItemsSectionProps): JSX.Element | null {
  const { data, isLoading } = useTransactionReceipt(transactionId, hasReceipt);

  if (!hasReceipt) {
    return null;
  }

  if (isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Itens do Cupom Fiscal</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3" aria-label="Carregando itens do cupom fiscal">
          <Skeleton className="h-5 w-1/3" />
          <Skeleton className="h-5 w-1/4" />
          <Skeleton className="h-40 w-full" />
        </CardContent>
      </Card>
    );
  }

  if (!data) {
    return null;
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <Badge variant="outline" className="flex items-center gap-1">
          <ReceiptText className="h-3.5 w-3.5" />
          Cupom Fiscal
        </Badge>
        <span className="text-sm text-muted-foreground">
          Chave de acesso: {formatAccessKey(data.establishment.accessKey)}
        </span>
      </div>

      <ReceiptPreview
        receipt={data}
        title="Itens do Cupom Fiscal"
      />
    </div>
  );
}

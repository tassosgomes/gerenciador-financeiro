import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Badge } from '@/shared/components/ui/badge';

export function RecentTransactions(): JSX.Element {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Transações Recentes</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="flex items-center justify-center py-8 text-sm text-muted-foreground">
          <Badge variant="outline">Em desenvolvimento</Badge>
        </div>
        <p className="text-center text-xs text-muted-foreground">
          Esta seção será implementada na Task 8.0 (Transações)
        </p>
      </CardContent>
    </Card>
  );
}

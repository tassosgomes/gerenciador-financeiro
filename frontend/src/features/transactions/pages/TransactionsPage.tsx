import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui';

export default function TransactionsPage(): JSX.Element {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Transacoes</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-slate-600">Listagem de transacoes sera implementada nas proximas tasks.</p>
      </CardContent>
    </Card>
  );
}

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui';

export default function AccountsPage(): JSX.Element {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Contas</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-slate-600">Gestao de contas sera implementada nas proximas tasks.</p>
      </CardContent>
    </Card>
  );
}

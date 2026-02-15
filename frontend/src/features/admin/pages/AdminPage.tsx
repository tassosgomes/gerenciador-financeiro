import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui';

export default function AdminPage(): JSX.Element {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Administracao</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-slate-600">Painel administrativo sera implementado nas proximas tasks.</p>
      </CardContent>
    </Card>
  );
}

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui';

export default function CategoriesPage(): JSX.Element {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Categorias</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-slate-600">Cadastro de categorias sera implementado nas proximas tasks.</p>
      </CardContent>
    </Card>
  );
}

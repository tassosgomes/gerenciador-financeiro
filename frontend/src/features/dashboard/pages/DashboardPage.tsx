import { BarChartWidget, DonutChartWidget } from '@/shared/components/charts';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui';

const revenueVsExpenseData = [
  { label: 'Mai', revenue: 4000, expense: 3000 },
  { label: 'Jun', revenue: 5500, expense: 4400 },
  { label: 'Jul', revenue: 3500, expense: 5000 },
  { label: 'Ago', revenue: 6200, expense: 3400 },
  { label: 'Set', revenue: 7000, expense: 3900 },
  { label: 'Out', revenue: 8200, expense: 4100 },
];

const expenseByCategoryData = [
  { name: 'Moradia', value: 1400 },
  { name: 'Alimentacao', value: 1000 },
  { name: 'Transporte', value: 820 },
  { name: 'Lazer', value: 880 },
];

export default function DashboardPage(): JSX.Element {
  return (
    <section className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold text-slate-900">Bom dia, Carlos!</h2>
        <p className="mt-1 text-sm text-slate-500">
          Aqui esta o resumo financeiro da sua familia.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Receita vs Despesa</CardTitle>
          </CardHeader>
          <CardContent>
            <BarChartWidget data={revenueVsExpenseData} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Despesas por Categoria</CardTitle>
          </CardHeader>
          <CardContent>
            <DonutChartWidget data={expenseByCategoryData} />
          </CardContent>
        </Card>
      </div>
    </section>
  );
}

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { BarChartWidget } from '@/shared/components/charts';
import type { MonthlyComparisonDto } from '@/features/dashboard/types/dashboard';

interface RevenueExpenseChartProps {
  data: MonthlyComparisonDto[] | undefined;
  isLoading: boolean;
}

const MONTH_LABELS: Record<string, string> = {
  '01': 'Jan',
  '02': 'Fev',
  '03': 'Mar',
  '04': 'Abr',
  '05': 'Mai',
  '06': 'Jun',
  '07': 'Jul',
  '08': 'Ago',
  '09': 'Set',
  '10': 'Out',
  '11': 'Nov',
  '12': 'Dez',
};

function formatMonthLabel(monthString: string): string {
  const [year, month] = monthString.split('-');
  return `${MONTH_LABELS[month]} ${year.slice(2)}`;
}

export function RevenueExpenseChart({
  data,
  isLoading,
}: RevenueExpenseChartProps): JSX.Element {
  if (isLoading || !data) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Receitas vs Despesas (6 meses)</CardTitle>
        </CardHeader>
        <CardContent>
          <Skeleton className="h-[280px] w-full" />
        </CardContent>
      </Card>
    );
  }

  const chartData = data.map((item) => ({
    label: formatMonthLabel(item.month),
    revenue: item.income,
    expense: item.expenses,
  }));

  return (
    <Card>
      <CardHeader>
        <CardTitle>Receitas vs Despesas (6 meses)</CardTitle>
      </CardHeader>
      <CardContent>
        {chartData.length === 0 ? (
          <div className="flex h-[280px] items-center justify-center text-sm text-muted-foreground">
            Nenhum dado disponível
          </div>
        ) : (
          <BarChartWidget data={chartData} />
        )}
      </CardContent>
    </Card>
  );
}

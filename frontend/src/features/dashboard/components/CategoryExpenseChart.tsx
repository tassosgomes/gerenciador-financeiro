import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { DonutChartWidget } from '@/shared/components/charts';
import type { CategoryExpenseDto } from '@/features/dashboard/types/dashboard';

interface CategoryExpenseChartProps {
  data: CategoryExpenseDto[] | undefined;
  isLoading: boolean;
}

const CATEGORY_COLORS = ['#137fec', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];

export function CategoryExpenseChart({
  data,
  isLoading,
}: CategoryExpenseChartProps): JSX.Element {
  if (isLoading || !data) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Despesas por Categoria</CardTitle>
        </CardHeader>
        <CardContent>
          <Skeleton className="h-[280px] w-full" />
        </CardContent>
      </Card>
    );
  }

  const chartData = data.map((item, index) => ({
    name: `${item.categoryName} (${item.percentage.toFixed(1)}%)`,
    value: item.total,
    color: CATEGORY_COLORS[index % CATEGORY_COLORS.length],
  }));

  return (
    <Card>
      <CardHeader>
        <CardTitle>Despesas por Categoria</CardTitle>
      </CardHeader>
      <CardContent>
        {chartData.length === 0 ? (
          <div className="flex h-[280px] items-center justify-center text-sm text-muted-foreground">
            Nenhum dado disponível
          </div>
        ) : (
          <DonutChartWidget data={chartData} />
        )}
      </CardContent>
    </Card>
  );
}

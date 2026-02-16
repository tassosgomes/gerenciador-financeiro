import { TrendingDown, TrendingUp, Wallet, CreditCard } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Progress } from '@/shared/components/ui/progress';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useFormatCurrency } from '@/shared/hooks';
import { cn } from '@/shared/utils';
import type { DashboardSummaryResponse } from '@/features/dashboard/types/dashboard';

interface SummaryCardsProps {
  data: DashboardSummaryResponse | undefined;
  isLoading: boolean;
  isError: boolean;
}

interface SummaryCardProps {
  title: string;
  value: number;
  icon: React.ReactNode;
  variant?: 'default' | 'success' | 'danger';
}

function SummaryCard({ title, value, icon, variant = 'default' }: SummaryCardProps): JSX.Element {
  const formattedValue = useFormatCurrency(value);
  const colorClass =
    variant === 'success'
      ? 'text-green-600'
      : variant === 'danger'
        ? 'text-red-600'
        : 'text-blue-600';

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <div className={colorClass}>{icon}</div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{formattedValue}</div>
      </CardContent>
    </Card>
  );
}

function SummaryCardSkeleton(): JSX.Element {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <Skeleton className="h-4 w-24" />
        <Skeleton className="h-5 w-5 rounded" />
      </CardHeader>
      <CardContent>
        <Skeleton className="h-8 w-32" />
      </CardContent>
    </Card>
  );
}

export function SummaryCards({ data, isLoading, isError }: SummaryCardsProps): JSX.Element {
  const navigate = useNavigate();

  if (isError) {
    return (
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card className="border-red-300 bg-red-50">
          <CardContent className="pt-6">
            <p className="text-sm text-red-600">Erro ao carregar dados do dashboard</p>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (isLoading || !data) {
    return (
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        <SummaryCardSkeleton />
        <SummaryCardSkeleton />
        <SummaryCardSkeleton />
        <SummaryCardSkeleton />
      </div>
    );
  }

  const getProgressIndicatorColor = (utilization: number | null): string => {
    if (utilization === null) return '[&>div]:bg-primary';
    if (utilization > 80) return '[&>div]:bg-red-500';
    if (utilization > 50) return '[&>div]:bg-yellow-500';
    return '[&>div]:bg-green-500';
  };

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
      <SummaryCard
        icon={<Wallet className="h-5 w-5" />}
        title="Saldo Total"
        value={data.totalBalance}
        variant="default"
      />
      <SummaryCard
        icon={<TrendingUp className="h-5 w-5" />}
        title="Receitas do Mês"
        value={data.monthlyIncome}
        variant="success"
      />
      <SummaryCard
        icon={<TrendingDown className="h-5 w-5" />}
        title="Despesas do Mês"
        value={data.monthlyExpenses}
        variant="danger"
      />
      <CardDebtSummary 
        creditCardDebt={data.creditCardDebt}
        totalCreditLimit={data.totalCreditLimit}
        creditUtilizationPercent={data.creditUtilizationPercent}
        navigate={navigate}
        getProgressIndicatorColor={getProgressIndicatorColor}
      />
    </div>
  );
}

interface CardDebtSummaryProps {
  creditCardDebt: number;
  totalCreditLimit: number | null;
  creditUtilizationPercent: number | null;
  navigate: ReturnType<typeof useNavigate>;
  getProgressIndicatorColor: (utilization: number | null) => string;
}

function CardDebtSummary({ 
  creditCardDebt, 
  totalCreditLimit, 
  creditUtilizationPercent, 
  navigate, 
  getProgressIndicatorColor 
}: CardDebtSummaryProps): JSX.Element {
  const formattedDebt = useFormatCurrency(creditCardDebt);
  const formattedLimit = totalCreditLimit !== null ? useFormatCurrency(totalCreditLimit) : null;

  return (
    <Card
      onClick={() => navigate('/contas?type=2')}
      className="cursor-pointer transition-colors hover:bg-accent/50"
    >
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">Dívida Cartões</CardTitle>
        <div className="text-red-600">
          <CreditCard className="h-5 w-5" />
        </div>
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{formattedDebt}</div>
        {totalCreditLimit !== null && (
          <div className="mt-2 space-y-1">
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>Limite total: {formattedLimit}</span>
              <span>{creditUtilizationPercent}% utilizado</span>
            </div>
            <Progress
              value={creditUtilizationPercent ?? 0}
              className={cn('h-2', getProgressIndicatorColor(creditUtilizationPercent))}
            />
          </div>
        )}
      </CardContent>
    </Card>
  );
}

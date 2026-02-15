import { useState } from 'react';

import { useAuthStore } from '@/features/auth/store/authStore';
import { useDashboardSummary, useDashboardCharts } from '@/features/dashboard/hooks/useDashboard';
import { MonthNavigator } from '@/features/dashboard/components/MonthNavigator';
import { SummaryCards } from '@/features/dashboard/components/SummaryCards';
import { RevenueExpenseChart } from '@/features/dashboard/components/RevenueExpenseChart';
import { CategoryExpenseChart } from '@/features/dashboard/components/CategoryExpenseChart';
import { RecentTransactions } from '@/features/dashboard/components/RecentTransactions';

function getGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Bom dia';
  if (hour < 18) return 'Boa tarde';
  return 'Boa noite';
}

export default function DashboardPage(): JSX.Element {
  const user = useAuthStore((state) => state.user);
  const userName = user?.name ?? 'Usuário';
  const greeting = getGreeting();

  const now = new Date();
  const [selectedMonth, setSelectedMonth] = useState(now.getMonth() + 1);
  const [selectedYear, setSelectedYear] = useState(now.getFullYear());

  const {
    data: summaryData,
    isLoading: isLoadingSummary,
    isError: isErrorSummary,
  } = useDashboardSummary(selectedMonth, selectedYear);

  const {
    data: chartsData,
    isLoading: isLoadingCharts,
  } = useDashboardCharts(selectedMonth, selectedYear);

  const handleNavigate = (month: number, year: number) => {
    setSelectedMonth(month);
    setSelectedYear(year);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          {greeting}, {userName}!
        </h1>
        <p className="text-muted-foreground">
          Aqui está o resumo financeiro do seu período selecionado
        </p>
      </div>

      {/* Month Navigator */}
      <MonthNavigator month={selectedMonth} year={selectedYear} onNavigate={handleNavigate} />

      {/* Summary Cards */}
      <SummaryCards data={summaryData} isError={isErrorSummary} isLoading={isLoadingSummary} />

      {/* Charts Grid */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <RevenueExpenseChart data={chartsData?.revenueVsExpense} isLoading={isLoadingCharts} />
        <CategoryExpenseChart data={chartsData?.expenseByCategory} isLoading={isLoadingCharts} />
      </div>

      {/* Recent Transactions */}
      <RecentTransactions />
    </div>
  );
}

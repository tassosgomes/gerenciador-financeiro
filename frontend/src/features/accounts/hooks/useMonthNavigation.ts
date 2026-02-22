import { useState } from 'react';

const MONTH_LABELS = [
  'Janeiro',
  'Fevereiro',
  'Março',
  'Abril',
  'Maio',
  'Junho',
  'Julho',
  'Agosto',
  'Setembro',
  'Outubro',
  'Novembro',
  'Dezembro',
] as const;

interface UseMonthNavigationResult {
  currentMonth: number;
  currentYear: number;
  monthLabel: string;
  handlePrevMonth: () => void;
  handleNextMonth: () => void;
}

export function useMonthNavigation(initialDate = new Date()): UseMonthNavigationResult {
  const [currentMonth, setCurrentMonth] = useState(initialDate.getMonth() + 1);
  const [currentYear, setCurrentYear] = useState(initialDate.getFullYear());

  const monthLabel = MONTH_LABELS[currentMonth - 1];

  const handlePrevMonth = (): void => {
    if (currentMonth === 1) {
      setCurrentMonth(12);
      setCurrentYear((previousYear) => previousYear - 1);
      return;
    }

    setCurrentMonth((previousMonth) => previousMonth - 1);
  };

  const handleNextMonth = (): void => {
    if (currentMonth === 12) {
      setCurrentMonth(1);
      setCurrentYear((previousYear) => previousYear + 1);
      return;
    }

    setCurrentMonth((previousMonth) => previousMonth + 1);
  };

  return {
    currentMonth,
    currentYear,
    monthLabel,
    handlePrevMonth,
    handleNextMonth,
  };
}

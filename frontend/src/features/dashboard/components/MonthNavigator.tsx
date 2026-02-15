import { ChevronLeft, ChevronRight } from 'lucide-react';

import { Button } from '@/shared/components/ui/button';

interface MonthNavigatorProps {
  month: number;
  year: number;
  onNavigate: (month: number, year: number) => void;
}

const MONTHS = [
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
];

export function MonthNavigator({ month, year, onNavigate }: MonthNavigatorProps): JSX.Element {
  const handlePrevious = () => {
    if (month === 1) {
      onNavigate(12, year - 1);
    } else {
      onNavigate(month - 1, year);
    }
  };

  const handleNext = () => {
    if (month === 12) {
      onNavigate(1, year + 1);
    } else {
      onNavigate(month + 1, year);
    }
  };

  return (
    <div className="flex items-center justify-between rounded-lg border bg-card p-4 shadow-sm">
      <Button onClick={handlePrevious} size="icon" variant="ghost">
        <ChevronLeft className="h-5 w-5" />
      </Button>
      <span className="text-lg font-semibold">
        {MONTHS[month - 1]} {year}
      </span>
      <Button onClick={handleNext} size="icon" variant="ghost">
        <ChevronRight className="h-5 w-5" />
      </Button>
    </div>
  );
}

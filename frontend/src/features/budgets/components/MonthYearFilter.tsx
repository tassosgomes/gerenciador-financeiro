import { ChevronLeft, ChevronRight } from 'lucide-react';

import {
  Button,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui';

interface MonthYearFilterProps {
  month: number;
  year: number;
  onChange: (month: number, year: number) => void;
}

const MONTHS = [
  { value: 1, label: 'Janeiro' },
  { value: 2, label: 'Fevereiro' },
  { value: 3, label: 'Março' },
  { value: 4, label: 'Abril' },
  { value: 5, label: 'Maio' },
  { value: 6, label: 'Junho' },
  { value: 7, label: 'Julho' },
  { value: 8, label: 'Agosto' },
  { value: 9, label: 'Setembro' },
  { value: 10, label: 'Outubro' },
  { value: 11, label: 'Novembro' },
  { value: 12, label: 'Dezembro' },
] as const;

export function MonthYearFilter({ month, year, onChange }: MonthYearFilterProps): JSX.Element {
  const currentYear = new Date().getFullYear();
  const years = Array.from({ length: currentYear + 1 - 2020 + 1 }, (_, index) => 2020 + index);

  const handlePreviousMonth = (): void => {
    if (month === 1) {
      onChange(12, year - 1);
      return;
    }

    onChange(month - 1, year);
  };

  const handleNextMonth = (): void => {
    if (month === 12) {
      onChange(1, year + 1);
      return;
    }

    onChange(month + 1, year);
  };

  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between" role="group" aria-label="Filtro de mês e ano">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 sm:min-w-[360px]">
        <Select value={month.toString()} onValueChange={(value) => onChange(Number(value), year)}>
          <SelectTrigger aria-label="Selecionar mês">
            <SelectValue placeholder="Mês" />
          </SelectTrigger>
          <SelectContent>
            {MONTHS.map((monthOption) => (
              <SelectItem key={monthOption.value} value={monthOption.value.toString()}>
                {monthOption.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select value={year.toString()} onValueChange={(value) => onChange(month, Number(value))}>
          <SelectTrigger aria-label="Selecionar ano">
            <SelectValue placeholder="Ano" />
          </SelectTrigger>
          <SelectContent>
            {years.map((yearOption) => (
              <SelectItem key={yearOption} value={yearOption.toString()}>
                {yearOption}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex items-center gap-2">
        <Button aria-label="Mês anterior" onClick={handlePreviousMonth} size="icon" type="button" variant="outline">
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <Button aria-label="Próximo mês" onClick={handleNextMonth} size="icon" type="button" variant="outline">
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}

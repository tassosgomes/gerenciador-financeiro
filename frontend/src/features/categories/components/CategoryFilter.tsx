import { Tabs, TabsList, TabsTrigger } from '@/shared/components/ui';

type FilterType = 'all' | 'income' | 'expense';

interface CategoryFilterProps {
  value: FilterType;
  onChange: (value: FilterType) => void;
}

export function CategoryFilter({ value, onChange }: CategoryFilterProps): JSX.Element {
  return (
    <Tabs value={value} onValueChange={(v) => onChange(v as FilterType)}>
      <TabsList>
        <TabsTrigger value="all">Todas</TabsTrigger>
        <TabsTrigger value="income">Receitas</TabsTrigger>
        <TabsTrigger value="expense">Despesas</TabsTrigger>
      </TabsList>
    </Tabs>
  );
}

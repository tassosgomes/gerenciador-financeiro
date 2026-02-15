import { X } from 'lucide-react';
import { Button } from '@/shared/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select';
import { Input } from '@/shared/components/ui/input';
import { useAccounts } from '@/features/accounts/hooks/useAccounts';
import { useCategories } from '@/features/categories/hooks/useCategories';
import { TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';

interface TransactionFiltersProps {
  accountId?: string;
  categoryId?: string;
  type?: number;
  status?: number;
  dateFrom?: string;
  dateTo?: string;
  onFilterChange: (key: string, value: string | number | undefined) => void;
  onClearFilters: () => void;
}

export function TransactionFilters({
  accountId,
  categoryId,
  type,
  status,
  dateFrom,
  dateTo,
  onFilterChange,
  onClearFilters,
}: TransactionFiltersProps) {
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();

  const activeAccounts = accounts?.filter((acc) => acc.isActive) ?? [];

  const hasActiveFilters =
    accountId || categoryId || type !== undefined || status !== undefined || dateFrom || dateTo;

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        {/* Conta */}
        <div>
          <label className="mb-2 block text-sm font-medium">Conta</label>
          <Select
            value={accountId ?? '__all__'}
            onValueChange={(value) => onFilterChange('accountId', value === '__all__' ? undefined : value)}
          >
            <SelectTrigger aria-label="Conta">
              <SelectValue placeholder="Todas as contas" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">Todas as contas</SelectItem>
              {activeAccounts.map((account) => (
                <SelectItem key={account.id} value={account.id}>
                  {account.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Categoria */}
        <div>
          <label className="mb-2 block text-sm font-medium">Categoria</label>
          <Select
            value={categoryId ?? '__all__'}
            onValueChange={(value) => onFilterChange('categoryId', value === '__all__' ? undefined : value)}
          >
            <SelectTrigger aria-label="Categoria">
              <SelectValue placeholder="Todas as categorias" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">Todas as categorias</SelectItem>
              {categories?.map((category) => (
                <SelectItem key={category.id} value={category.id}>
                  {category.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Tipo */}
        <div>
          <label className="mb-2 block text-sm font-medium">Tipo</label>
          <Select
            value={type !== undefined ? String(type) : '__all__'}
            onValueChange={(value) =>
              onFilterChange('type', value === '__all__' ? undefined : Number(value))
            }
          >
            <SelectTrigger aria-label="Tipo">
              <SelectValue placeholder="Todos os tipos" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">Todos os tipos</SelectItem>
              <SelectItem value={String(TransactionType.Debit)}>Débito</SelectItem>
              <SelectItem value={String(TransactionType.Credit)}>Crédito</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Status */}
        <div>
          <label className="mb-2 block text-sm font-medium">Status</label>
          <Select
            value={status !== undefined ? String(status) : '__all__'}
            onValueChange={(value) =>
              onFilterChange('status', value === '__all__' ? undefined : Number(value))
            }
          >
            <SelectTrigger aria-label="Status">
              <SelectValue placeholder="Todos os status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">Todos os status</SelectItem>
              <SelectItem value={String(TransactionStatus.Paid)}>Pago</SelectItem>
              <SelectItem value={String(TransactionStatus.Pending)}>Pendente</SelectItem>
              <SelectItem value={String(TransactionStatus.Cancelled)}>Cancelado</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Data De */}
        <div>
          <label className="mb-2 block text-sm font-medium">Data De</label>
          <Input
            type="date"
            value={dateFrom ?? ''}
            onChange={(e) => onFilterChange('dateFrom', e.target.value || undefined)}
          />
        </div>

        {/* Data Até */}
        <div>
          <label className="mb-2 block text-sm font-medium">Data Até</label>
          <Input
            type="date"
            value={dateTo ?? ''}
            onChange={(e) => onFilterChange('dateTo', e.target.value || undefined)}
          />
        </div>
      </div>

      {/* Botão Limpar Filtros */}
      {hasActiveFilters && (
        <div className="flex justify-end">
          <Button variant="outline" size="sm" onClick={onClearFilters}>
            <X className="mr-2 h-4 w-4" />
            Limpar filtros
          </Button>
        </div>
      )}
    </div>
  );
}

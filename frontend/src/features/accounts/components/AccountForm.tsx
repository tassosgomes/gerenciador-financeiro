import { useCallback, useMemo, useState } from 'react';

import type { AccountResponse } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';
import { createAccountSchema, updateAccountSchema } from '@/features/accounts/schemas/accountSchema';
import { useCreateAccount, useUpdateAccount } from '@/features/accounts/hooks/useAccounts';
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
} from '@/shared/components/ui';
import { ACCOUNT_TYPE_LABELS } from '@/shared/utils/constants';

interface AccountFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  account?: AccountResponse | null;
}

export function AccountForm({ open, onOpenChange, account }: AccountFormProps): JSX.Element {
  const isEditing = !!account;
  const createMutation = useCreateAccount();
  const updateMutation = useUpdateAccount();

  // Initialize state based on mode - this approach avoids effects
  const initialName = useMemo(() => (isEditing && account ? account.name : ''), [isEditing, account]);
  const initialAllowNegative = useMemo(() => (isEditing && account ? account.allowNegativeBalance : false), [isEditing, account]);

  const [name, setName] = useState(initialName);
  const [selectedType, setSelectedType] = useState<AccountType>(AccountType.Corrente);
  const [initialBalance, setInitialBalance] = useState(0);
  const [allowNegative, setAllowNegative] = useState(initialAllowNegative);
  const [errors, setErrors] = useState<{ name?: string; initialBalance?: string }>({});

  const resetForm = useCallback(() => {
    if (isEditing && account) {
      setName(account.name);
      setAllowNegative(account.allowNegativeBalance);
    } else {
      setName('');
      setInitialBalance(0);
      setSelectedType(AccountType.Corrente);
      setAllowNegative(false);
    }
    setErrors({});
  }, [isEditing, account]);

  function handleOpenChange(newOpen: boolean): void {
    if (newOpen) {
      // Reset form when opening
      resetForm();
    }
    onOpenChange(newOpen);
  }

  function validate(): boolean {
    const newErrors: { name?: string; initialBalance?: string } = {};
    const schema = isEditing ? updateAccountSchema : createAccountSchema;

    const nameResult = schema.shape.name.safeParse(name);
    if (!nameResult.success) {
      newErrors.name = nameResult.error.issues[0]?.message;
    }

    if (!isEditing) {
      const balanceResult = createAccountSchema.shape.initialBalance.safeParse(initialBalance);
      if (!balanceResult.success) {
        newErrors.initialBalance = balanceResult.error.issues[0]?.message;
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }

  async function handleSubmit(e: React.FormEvent): Promise<void> {
    e.preventDefault();

    if (!validate()) return;

    try {
      if (isEditing) {
        await updateMutation.mutateAsync({
          id: account.id,
          data: {
            name,
            allowNegativeBalance: allowNegative,
          },
        });
      } else {
        await createMutation.mutateAsync({
          name,
          type: selectedType,
          initialBalance,
          allowNegativeBalance: allowNegative,
        });
      }
      onOpenChange(false);
    } catch {
      // Error handling is done in the mutation hooks
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle>{isEditing ? 'Editar Conta' : 'Adicionar Nova Conta'}</DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Altere as informações da conta abaixo.'
              : 'Preencha os dados para criar uma nova conta.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Nome da conta */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-slate-700 mb-1.5">
              Nome da Conta
            </label>
            <Input
              id="name"
              placeholder="Ex: Banco Itaú, Nubank, Carteira"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className={errors.name ? 'border-danger' : ''}
            />
            {errors.name && <p className="mt-1 text-xs text-danger">{errors.name}</p>}
          </div>

          {/* Tipo de conta (apenas criação) */}
          {!isEditing && (
            <div>
              <label htmlFor="type" className="block text-sm font-medium text-slate-700 mb-1.5">
                Tipo de Conta
              </label>
              <Select
                value={selectedType.toString()}
                onValueChange={(value) => setSelectedType(Number(value) as AccountType)}
              >
                <SelectTrigger id="type">
                  <SelectValue placeholder="Selecione o tipo" />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(ACCOUNT_TYPE_LABELS).map(([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {/* Saldo inicial (apenas criação) */}
          {!isEditing && (
            <div>
              <label htmlFor="initialBalance" className="block text-sm font-medium text-slate-700 mb-1.5">
                Saldo Inicial
              </label>
              <Input
                id="initialBalance"
                type="number"
                step="0.01"
                placeholder="0.00"
                value={initialBalance}
                onChange={(e) => setInitialBalance(Number(e.target.value))}
                className={errors.initialBalance ? 'border-danger' : ''}
              />
              {errors.initialBalance && (
                <p className="mt-1 text-xs text-danger">{errors.initialBalance}</p>
              )}
            </div>
          )}

          {/* Permitir saldo negativo */}
          <div className="flex items-center justify-between rounded-lg border p-3">
            <div className="space-y-0.5">
              <label htmlFor="allowNegativeBalance" className="text-sm font-medium">
                Permitir Saldo Negativo
              </label>
              <p className="text-xs text-slate-600">Permite que a conta tenha saldo negativo</p>
            </div>
            <Switch
              id="allowNegativeBalance"
              checked={allowNegative}
              onCheckedChange={setAllowNegative}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => handleOpenChange(false)} disabled={isSubmitting}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Salvando...' : isEditing ? 'Salvar' : 'Criar Conta'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

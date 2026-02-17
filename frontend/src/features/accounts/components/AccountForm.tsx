import { useCallback, useEffect, useMemo, useState } from 'react';

import type { AccountResponse, CreateAccountRequest, UpdateAccountRequest } from '@/features/accounts/types/account';
import { AccountType } from '@/features/accounts/types/account';
import { useCreateAccount, useUpdateAccount, useAccounts } from '@/features/accounts/hooks/useAccounts';
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  CurrencyInput,
  Input,
  Select,
  AccountSelectOptionGroups,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
} from '@/shared/components/ui';
import { ACCOUNT_TYPE_LABELS } from '@/shared/utils/constants';
import { formatCurrency } from '@/shared/utils';

interface AccountFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  account?: AccountResponse | null;
}

export function AccountForm({ open, onOpenChange, account }: AccountFormProps): JSX.Element {
  const isEditing = !!account;
  const createMutation = useCreateAccount();
  const updateMutation = useUpdateAccount();
  const { data: allAccounts } = useAccounts();

  // Initialize state with account data or defaults
  const initialValues = useMemo(() => {
    if (isEditing && account) {
      return {
        name: account.name,
        type: account.type,
        allowNegative: account.allowNegativeBalance,
        creditLimit: account.creditCard?.creditLimit ?? 0,
        closingDay: account.creditCard?.closingDay ?? 1,
        dueDay: account.creditCard?.dueDay ?? 10,
        debitAccountId: account.creditCard?.debitAccountId ?? '',
        enforceCreditLimit: account.creditCard?.enforceCreditLimit ?? true,
      };
    }
    return {
      name: '',
      type: AccountType.Corrente,
      allowNegative: false,
      creditLimit: 0,
      closingDay: 1,
      dueDay: 10,
      debitAccountId: '',
      enforceCreditLimit: true,
    };
  }, [isEditing, account]);

  // State for common fields
  const [name, setName] = useState(initialValues.name);
  const [selectedType, setSelectedType] = useState<AccountType>(initialValues.type);
  
  // State for regular account fields
  const [initialBalance, setInitialBalance] = useState(0);
  const [allowNegative, setAllowNegative] = useState(initialValues.allowNegative);
  
  // State for credit card fields
  const [creditLimit, setCreditLimit] = useState(initialValues.creditLimit);
  const [closingDay, setClosingDay] = useState(initialValues.closingDay);
  const [dueDay, setDueDay] = useState(initialValues.dueDay);
  const [debitAccountId, setDebitAccountId] = useState(initialValues.debitAccountId);
  const [enforceCreditLimit, setEnforceCreditLimit] = useState(initialValues.enforceCreditLimit);
  
  const [errors, setErrors] = useState<Record<string, string>>({});

  const isCreditCard = selectedType === AccountType.Cartao;

  // Get available debit accounts (Corrente and Carteira, excluding current account if editing)
  const debitAccounts = useMemo(() => {
    if (!allAccounts) return [];
    
    return allAccounts.filter(
      (acc) =>
        acc.isActive &&
        (acc.type === AccountType.Corrente || acc.type === AccountType.Carteira) &&
        (!isEditing || acc.id !== account?.id) // Exclude self when editing
    );
  }, [allAccounts, isEditing, account?.id]);

  const debitAccountGroups = useMemo(() => {
    return debitAccounts;
  }, [debitAccounts]);

  const resetForm = useCallback(() => {
    setName(initialValues.name);
    setSelectedType(initialValues.type);
    setInitialBalance(0);
    setAllowNegative(initialValues.allowNegative);
    setCreditLimit(initialValues.creditLimit);
    setClosingDay(initialValues.closingDay);
    setDueDay(initialValues.dueDay);
    setDebitAccountId(initialValues.debitAccountId);
    setEnforceCreditLimit(initialValues.enforceCreditLimit);
    setErrors({});
  }, [initialValues]);

  useEffect(() => {
    if (open) {
      resetForm();
    }
  }, [open, resetForm]);

  function handleOpenChange(newOpen: boolean): void {
    if (newOpen) {
      resetForm();
    }
    onOpenChange(newOpen);
  }

  function validate(): boolean {
    const newErrors: Record<string, string> = {};

    // Common validations
    if (name.length < 2) {
      newErrors.name = 'Nome deve ter no mínimo 2 caracteres';
    } else if (name.length > 100) {
      newErrors.name = 'Nome muito longo';
    }

    if (!isEditing) {
      // Type-specific validations for creation
      if (isCreditCard) {
        if (creditLimit <= 0) {
          newErrors.creditLimit = 'Limite deve ser maior que zero';
        }
        if (closingDay < 1 || closingDay > 28) {
          newErrors.closingDay = 'Dia deve ser entre 1 e 28';
        }
        if (dueDay < 1 || dueDay > 28) {
          newErrors.dueDay = 'Dia deve ser entre 1 e 28';
        }
        if (!debitAccountId) {
          newErrors.debitAccountId = 'Conta de débito obrigatória';
        }
      }
    } else {
      // Validations for editing (if credit card fields are being updated)
      if (account?.type === AccountType.Cartao) {
        if (creditLimit <= 0) {
          newErrors.creditLimit = 'Limite deve ser maior que zero';
        }
        if (closingDay < 1 || closingDay > 28) {
          newErrors.closingDay = 'Dia deve ser entre 1 e 28';
        }
        if (dueDay < 1 || dueDay > 28) {
          newErrors.dueDay = 'Dia deve ser entre 1 e 28';
        }
        if (!debitAccountId) {
          newErrors.debitAccountId = 'Conta de débito obrigatória';
        }
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
        const updateData: Record<string, unknown> = {
          name,
        };

        if (account?.type === AccountType.Cartao) {
          // Update credit card fields
          updateData.creditLimit = creditLimit;
          updateData.closingDay = closingDay;
          updateData.dueDay = dueDay;
          updateData.debitAccountId = debitAccountId;
          updateData.enforceCreditLimit = enforceCreditLimit;
        } else {
          // Update regular account fields
          updateData.allowNegativeBalance = allowNegative;
        }

        await updateMutation.mutateAsync({
          id: account.id,
          data: updateData as unknown as UpdateAccountRequest,
        });
      } else {
        // Create new account
        const createData: Record<string, unknown> = {
          name,
          type: selectedType,
        };

        if (isCreditCard) {
          // Credit card fields
          createData.creditLimit = creditLimit;
          createData.closingDay = closingDay;
          createData.dueDay = dueDay;
          createData.debitAccountId = debitAccountId;
          createData.enforceCreditLimit = enforceCreditLimit;
        } else {
          // Regular account fields
          createData.initialBalance = initialBalance;
          createData.allowNegativeBalance = allowNegative;
        }

        await createMutation.mutateAsync(createData as unknown as CreateAccountRequest);
      }
      onOpenChange(false);
    } catch {
      // Error handling is done in the mutation hooks
    }
  }

  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[500px] max-h-[90vh] overflow-y-auto">
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

          {/* Campos para contas regulares (Corrente, Investimento, Carteira) */}
          {!isEditing && !isCreditCard && (
            <div className="space-y-4 transition-all duration-200 ease-in-out">
              <div>
                <label htmlFor="initialBalance" className="block text-sm font-medium text-slate-700 mb-1.5">
                  Saldo Inicial
                </label>
                <CurrencyInput
                  id="initialBalance"
                  aria-label="Saldo Inicial"
                  placeholder="R$ 0,00"
                  value={initialBalance}
                  onChange={setInitialBalance}
                  className={errors.initialBalance ? 'border-danger' : ''}
                />
                {errors.initialBalance && (
                  <p className="mt-1 text-xs text-danger">{errors.initialBalance}</p>
                )}
              </div>

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
            </div>
          )}

          {/* Campos para edição de contas regulares */}
          {isEditing && account?.type !== AccountType.Cartao && (
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
          )}

          {/* Campos para cartão de crédito (criação) */}
          {!isEditing && isCreditCard && (
            <div className="space-y-4 transition-all duration-200 ease-in-out">
              <div>
                <label htmlFor="creditLimit" className="block text-sm font-medium text-slate-700 mb-1.5">
                  Limite de Crédito
                </label>
                <CurrencyInput
                  id="creditLimit"
                  aria-label="Limite de Crédito"
                  placeholder="R$ 0,00"
                  value={creditLimit}
                  onChange={setCreditLimit}
                  className={errors.creditLimit ? 'border-danger' : ''}
                />
                {errors.creditLimit && (
                  <p className="mt-1 text-xs text-danger">{errors.creditLimit}</p>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="closingDay" className="block text-sm font-medium text-slate-700 mb-1.5">
                    Dia de Fechamento
                  </label>
                  <Input
                    id="closingDay"
                    type="number"
                    min={1}
                    max={28}
                    placeholder="1"
                    value={closingDay}
                    onChange={(e) => setClosingDay(Number(e.target.value))}
                    className={errors.closingDay ? 'border-danger' : ''}
                  />
                  {errors.closingDay && (
                    <p className="mt-1 text-xs text-danger">{errors.closingDay}</p>
                  )}
                </div>

                <div>
                  <label htmlFor="dueDay" className="block text-sm font-medium text-slate-700 mb-1.5">
                    Dia de Vencimento
                  </label>
                  <Input
                    id="dueDay"
                    type="number"
                    min={1}
                    max={28}
                    placeholder="10"
                    value={dueDay}
                    onChange={(e) => setDueDay(Number(e.target.value))}
                    className={errors.dueDay ? 'border-danger' : ''}
                  />
                  {errors.dueDay && (
                    <p className="mt-1 text-xs text-danger">{errors.dueDay}</p>
                  )}
                </div>
              </div>

              <div>
                <label htmlFor="debitAccountId" className="block text-sm font-medium text-slate-700 mb-1.5">
                  Conta de Débito
                </label>
                <Select
                  value={debitAccountId}
                  onValueChange={setDebitAccountId}
                >
                  <SelectTrigger id="debitAccountId" className={errors.debitAccountId ? 'border-danger' : ''}>
                    <SelectValue placeholder="Selecione a conta" />
                  </SelectTrigger>
                  <SelectContent>
                    <AccountSelectOptionGroups
                      items={debitAccountGroups}
                      typeLabels={ACCOUNT_TYPE_LABELS}
                      orderedTypes={[AccountType.Corrente, AccountType.Carteira]}
                      getItemLabel={(acc) => `${acc.name} - ${formatCurrency(acc.balance)}`}
                    />
                  </SelectContent>
                </Select>
                {errors.debitAccountId && (
                  <p className="mt-1 text-xs text-danger">{errors.debitAccountId}</p>
                )}
              </div>

              <div className="flex items-center justify-between rounded-lg border p-3">
                <div className="space-y-0.5">
                  <label htmlFor="enforceCreditLimit" className="text-sm font-medium">
                    Limite Rígido
                  </label>
                  <p className="text-xs text-slate-600">
                    Bloqueia compras que excedam o limite disponível
                  </p>
                </div>
                <Switch
                  id="enforceCreditLimit"
                  checked={enforceCreditLimit}
                  onCheckedChange={setEnforceCreditLimit}
                />
              </div>
            </div>
          )}

          {/* Campos para edição de cartão de crédito */}
          {isEditing && account?.type === AccountType.Cartao && (
            <div className="space-y-4 transition-all duration-200 ease-in-out">
              <div>
                <label htmlFor="creditLimit" className="block text-sm font-medium text-slate-700 mb-1.5">
                  Limite de Crédito
                </label>
                <CurrencyInput
                  id="creditLimit"
                  aria-label="Limite de Crédito"
                  placeholder="R$ 0,00"
                  value={creditLimit}
                  onChange={setCreditLimit}
                  className={errors.creditLimit ? 'border-danger' : ''}
                />
                {errors.creditLimit && (
                  <p className="mt-1 text-xs text-danger">{errors.creditLimit}</p>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label htmlFor="closingDay" className="block text-sm font-medium text-slate-700 mb-1.5">
                    Dia de Fechamento
                  </label>
                  <Input
                    id="closingDay"
                    type="number"
                    min={1}
                    max={28}
                    placeholder="1"
                    value={closingDay}
                    onChange={(e) => setClosingDay(Number(e.target.value))}
                    className={errors.closingDay ? 'border-danger' : ''}
                  />
                  {errors.closingDay && (
                    <p className="mt-1 text-xs text-danger">{errors.closingDay}</p>
                  )}
                </div>

                <div>
                  <label htmlFor="dueDay" className="block text-sm font-medium text-slate-700 mb-1.5">
                    Dia de Vencimento
                  </label>
                  <Input
                    id="dueDay"
                    type="number"
                    min={1}
                    max={28}
                    placeholder="10"
                    value={dueDay}
                    onChange={(e) => setDueDay(Number(e.target.value))}
                    className={errors.dueDay ? 'border-danger' : ''}
                  />
                  {errors.dueDay && (
                    <p className="mt-1 text-xs text-danger">{errors.dueDay}</p>
                  )}
                </div>
              </div>

              <div>
                <label htmlFor="debitAccountId" className="block text-sm font-medium text-slate-700 mb-1.5">
                  Conta de Débito
                </label>
                <Select
                  value={debitAccountId}
                  onValueChange={setDebitAccountId}
                >
                  <SelectTrigger id="debitAccountId" className={errors.debitAccountId ? 'border-danger' : ''}>
                    <SelectValue placeholder="Selecione a conta" />
                  </SelectTrigger>
                  <SelectContent>
                    <AccountSelectOptionGroups
                      items={debitAccountGroups}
                      typeLabels={ACCOUNT_TYPE_LABELS}
                      orderedTypes={[AccountType.Corrente, AccountType.Carteira]}
                      getItemLabel={(acc) => `${acc.name} - ${formatCurrency(acc.balance)}`}
                    />
                  </SelectContent>
                </Select>
                {errors.debitAccountId && (
                  <p className="mt-1 text-xs text-danger">{errors.debitAccountId}</p>
                )}
              </div>

              <div className="flex items-center justify-between rounded-lg border p-3">
                <div className="space-y-0.5">
                  <label htmlFor="enforceCreditLimit" className="text-sm font-medium">
                    Limite Rígido
                  </label>
                  <p className="text-xs text-slate-600">
                    Bloqueia compras que excedam o limite disponível
                  </p>
                </div>
                <Switch
                  id="enforceCreditLimit"
                  checked={enforceCreditLimit}
                  onCheckedChange={setEnforceCreditLimit}
                />
              </div>
            </div>
          )}

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

// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-nocheck - react-hook-form + Zod optional fields cause type inference issues in TypeScript
import { useState, useEffect } from 'react';
import { useForm, type UseFormReturn, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Save, RepeatIcon } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from '@/shared/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/shared/components/ui/tabs';
import { Button } from '@/shared/components/ui/button';
import { Input } from '@/shared/components/ui/input';
import { CurrencyInput } from '@/shared/components/ui/currency-input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select';
import { AccountSelectOptionGroups, CategorySelectOptionGroups } from '@/shared/components/ui';
import { Switch } from '@/shared/components/ui/switch';
import { useAccounts } from '@/features/accounts/hooks/useAccounts';
import { useCategories } from '@/features/categories/hooks/useCategories';
import { AccountType } from '@/features/accounts/types/account';
import { CategoryType } from '@/features/categories/types/category';
import { ACCOUNT_TYPE_LABELS } from '@/shared/utils/constants';
import {
  useCreateTransaction,
  useCreateInstallment,
  useCreateRecurrence,
  useCreateTransfer,
} from '@/features/transactions/hooks/useTransactions';
import type { TransactionResponse } from '@/features/transactions/types/transaction';
import { TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';
import { InstallmentPreview } from './InstallmentPreview';
import type {
  SimpleTransactionFormValues,
  InstallmentFormValues,
  RecurrenceFormValues,
  TransferFormValues,
} from '@/features/transactions/schemas/transactionSchema';
import {
  simpleTransactionSchema,
  installmentSchema,
  recurrenceSchema,
  transferSchema,
} from '@/features/transactions/schemas/transactionSchema';

interface TransactionFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  transaction?: TransactionResponse | null;
}

type TabValue = 'simple' | 'installment' | 'recurrence' | 'transfer';

export function TransactionForm({ open, onOpenChange, transaction }: TransactionFormProps) {
  const [activeTab, setActiveTab] = useState<TabValue>('simple');
  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories();

  const createTransaction = useCreateTransaction();
  const createInstallment = useCreateInstallment();
  const createRecurrence = useCreateRecurrence();
  const createTransfer = useCreateTransfer();

  const activeAccounts = accounts?.filter((acc) => acc.isActive) ?? [];
  const cardAccounts = activeAccounts.filter((acc) => acc.type === AccountType.Cartao);
  const transferAccounts = activeAccounts.filter((acc) => acc.type !== AccountType.Cartao);
  const expenseCategories = categories?.filter((cat) => cat.type === CategoryType.Expense) ?? [];
  const incomeCategories = categories?.filter((cat) => cat.type === CategoryType.Income) ?? [];

  // Form para transação simples
  // Note: TypeScript can't properly infer react-hook-form types with Zod optional fields
  const simpleForm = useForm<SimpleTransactionFormValues>({
    resolver: zodResolver(simpleTransactionSchema),
    defaultValues: {
      accountId: '',
      categoryId: '',
      type: TransactionType.Debit,
      amount: 0,
      description: '',
      competenceDate: new Date().toISOString().split('T')[0],
      dueDate: undefined,
      status: TransactionStatus.Pending,
    },
  }) as unknown as UseFormReturn<SimpleTransactionFormValues>;

  // Form para parcelamento
  // Note: TypeScript can't properly infer react-hook-form types with Zod optional fields
  const installmentForm = useForm<InstallmentFormValues>({
    resolver: zodResolver(installmentSchema),
    defaultValues: {
      accountId: '',
      categoryId: '',
      type: TransactionType.Debit,
      totalAmount: 0,
      installmentCount: 2,
      description: '',
      firstCompetenceDate: new Date().toISOString().split('T')[0],
      firstDueDate: undefined,
    },
  }) as unknown as UseFormReturn<InstallmentFormValues>;

  // Form para recorrência
  // Note: TypeScript can't properly infer react-hook-form types with Zod optional fields
  const recurrenceForm = useForm<RecurrenceFormValues>({
    resolver: zodResolver(recurrenceSchema),
    defaultValues: {
      accountId: '',
      categoryId: '',
      type: TransactionType.Debit,
      amount: 0,
      description: '',
      startDate: new Date().toISOString().split('T')[0],
      dueDate: undefined,
    },
  }) as unknown as UseFormReturn<RecurrenceFormValues>;

  // Form para transferência
  const transferForm = useForm<TransferFormValues>({
    resolver: zodResolver(transferSchema),
    defaultValues: {
      sourceAccountId: '',
      destinationAccountId: '',
      categoryId: '',
      amount: 0,
      description: '',
      competenceDate: new Date().toISOString().split('T')[0],
    },
  }) as unknown as UseFormReturn<TransferFormValues>;

  const simpleType = simpleForm.watch('type');
  const recurrenceType = recurrenceForm.watch('type');
  const simpleAccountId = simpleForm.watch('accountId') ?? '';
  const recurrenceAccountId = recurrenceForm.watch('accountId') ?? '';
  const simpleCategories = simpleType === TransactionType.Credit ? incomeCategories : expenseCategories;
  const recurrenceCategories = recurrenceType === TransactionType.Credit ? incomeCategories : expenseCategories;
  const simpleCategoriesToRender = simpleCategories.length > 0 ? simpleCategories : (categories ?? []);
  const recurrenceCategoriesToRender = recurrenceCategories.length > 0 ? recurrenceCategories : (categories ?? []);
  const installmentCategoriesToRender = expenseCategories.length > 0 ? expenseCategories : (categories ?? []);
  const isCreditCardAccount = (accountId: string) => activeAccounts.some(
    (account) => account.id === accountId && account.type === AccountType.Cartao
  );
  const isSimpleCreditCardAccount = isCreditCardAccount(simpleAccountId);
  const isRecurrenceCreditCardAccount = isCreditCardAccount(recurrenceAccountId);

  const resetAllForms = () => {
    simpleForm.reset();
    installmentForm.reset();
    recurrenceForm.reset();
    transferForm.reset();
    setActiveTab('simple');
  };

  useEffect(() => {
    if (open && !transaction) {
      resetAllForms();
    }
  }, [open, transaction]);

  // Populate forms when editing
  useEffect(() => {
    if (transaction) {
      if (transaction.installmentNumber && transaction.installmentNumber > 0) {
        setActiveTab('installment');
        installmentForm.reset({
          accountId: transaction.accountId,
          categoryId: transaction.categoryId,
          type: transaction.type,
          totalAmount: transaction.amount * (transaction.totalInstallments || 1), // Estimate total
          installmentCount: transaction.totalInstallments || 2,
          description: transaction.description,
          firstCompetenceDate: transaction.competenceDate,
          firstDueDate: transaction.dueDate || '',
        });
      } else if (transaction.isRecurrent) {
        setActiveTab('recurrence');
        recurrenceForm.reset({
          accountId: transaction.accountId,
          categoryId: transaction.categoryId,
          type: transaction.type,
          amount: transaction.amount,
          description: transaction.description,
          startDate: transaction.competenceDate,
          dueDate: transaction.dueDate || '',
        });
      } else if (transaction.transferGroupId) {
        setActiveTab('transfer');
        transferForm.reset({
          sourceAccountId: '', // TODO: Need proper transfer data
          destinationAccountId: '',
          categoryId: transaction.categoryId,
          amount: transaction.amount,
          description: transaction.description,
          competenceDate: transaction.competenceDate,
        });
      } else {
        setActiveTab('simple');
        simpleForm.reset({
          accountId: transaction.accountId,
          categoryId: transaction.categoryId,
          type: transaction.type,
          amount: transaction.amount,
          description: transaction.description,
          competenceDate: transaction.competenceDate,
          dueDate: transaction.dueDate || '',
          status: transaction.status,
        });
      }
    }
  }, [transaction, simpleForm, installmentForm, recurrenceForm, transferForm]);

  useEffect(() => {
    const selectedCategoryId = simpleForm.getValues('categoryId');
    if (selectedCategoryId && !simpleCategoriesToRender.some((cat) => cat.id === selectedCategoryId)) {
      simpleForm.setValue('categoryId', '');
    }
  }, [simpleCategoriesToRender, simpleForm, simpleType]);

  useEffect(() => {
    if (isSimpleCreditCardAccount && simpleForm.getValues('status') !== TransactionStatus.Paid) {
      simpleForm.setValue('status', TransactionStatus.Paid);
    }
  }, [isSimpleCreditCardAccount, simpleForm]);

  useEffect(() => {
    const selectedCategoryId = recurrenceForm.getValues('categoryId');
    if (selectedCategoryId && !recurrenceCategoriesToRender.some((cat) => cat.id === selectedCategoryId)) {
      recurrenceForm.setValue('categoryId', '');
    }
  }, [recurrenceCategoriesToRender, recurrenceForm, recurrenceType]);

  useEffect(() => {
    const selectedCategoryId = installmentForm.getValues('categoryId');
    if (selectedCategoryId && !installmentCategoriesToRender.some((cat) => cat.id === selectedCategoryId)) {
      installmentForm.setValue('categoryId', '');
    }
    if (installmentForm.getValues('type') !== TransactionType.Debit) {
      installmentForm.setValue('type', TransactionType.Debit);
    }
  }, [installmentCategoriesToRender, installmentForm]);

  const handleSimpleSubmit = async (data: SimpleTransactionFormValues) => {
    const payload: SimpleTransactionFormValues = {
      ...data,
      status: isCreditCardAccount(data.accountId) ? TransactionStatus.Paid : data.status,
    };

    await createTransaction.mutateAsync(payload);
    resetAllForms();
    onOpenChange(false);
  };

  const handleInstallmentSubmit = async (data: InstallmentFormValues) => {
    await createInstallment.mutateAsync(data);
    resetAllForms();
    onOpenChange(false);
  };

  const handleRecurrenceSubmit = async (data: RecurrenceFormValues) => {
    await createRecurrence.mutateAsync({
      ...data,
      defaultStatus: isCreditCardAccount(data.accountId)
        ? TransactionStatus.Paid
        : TransactionStatus.Pending,
    });
    resetAllForms();
    onOpenChange(false);
  };

  const handleTransferSubmit = async (data: TransferFormValues) => {
    await createTransfer.mutateAsync(data);
    resetAllForms();
    onOpenChange(false);
  };

  const handleClose = (open: boolean) => {
    if (!open) {
      resetAllForms();
    }
    onOpenChange(open);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{transaction ? 'Editar Transação' : 'Nova Transação'}</DialogTitle>
          <DialogDescription>
            Preencha os campos abaixo para criar ou editar uma transação
          </DialogDescription>
        </DialogHeader>

        <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as TabValue)}>
          <TabsList className="grid w-full grid-cols-2 sm:grid-cols-4">
            <TabsTrigger value="simple">Simples</TabsTrigger>
            <TabsTrigger value="installment">Parcelada</TabsTrigger>
            <TabsTrigger value="recurrence">Recorrente</TabsTrigger>
            <TabsTrigger value="transfer">Transferência</TabsTrigger>
          </TabsList>

          {/* Aba Simples */}
          <TabsContent value="simple" className="space-y-4">
            <form onSubmit={simpleForm.handleSubmit(handleSimpleSubmit)} className="space-y-4">
              {/* Valor */}
              <div>
                <label htmlFor="amount" className="block text-center text-sm font-medium mb-2">
                  Valor da transação
                </label>
                <Controller
                  name="amount"
                  control={simpleForm.control}
                  render={({ field }) => (
                    <CurrencyInput
                      id="amount"
                      placeholder="R$ 0,00"
                      value={field.value}
                      onChange={field.onChange}
                    />
                  )}
                />
                {simpleForm.formState.errors.amount && (
                  <p className="text-sm text-red-600 mt-1">
                    {simpleForm.formState.errors.amount.message}
                  </p>
                )}
              </div>

              {/* Descrição */}
              <div>
                <label htmlFor="description" className="block text-sm font-medium mb-2">Descrição</label>
                <Input
                  id="description"
                  placeholder="Ex: Compra no supermercado"
                  {...simpleForm.register('description')}
                />
                {simpleForm.formState.errors.description && (
                  <p className="text-sm text-red-600 mt-1">
                    {simpleForm.formState.errors.description.message}
                  </p>
                )}
              </div>

              {/* Categoria e Conta */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Categoria</label>
                   <Select
                     value={simpleForm.watch('categoryId') ?? ''}
                     onValueChange={(value) => simpleForm.setValue('categoryId', value)}
                   >
                     <SelectTrigger aria-label="Categoria">
                       <SelectValue placeholder="Selecione" />
                     </SelectTrigger>
                    <SelectContent>
                      <CategorySelectOptionGroups
                        items={simpleCategoriesToRender}
                        expenseType={CategoryType.Expense}
                        incomeType={CategoryType.Income}
                      />
                    </SelectContent>
                  </Select>
                  {simpleForm.formState.errors.categoryId && (
                    <p className="text-sm text-red-600 mt-1">
                      {simpleForm.formState.errors.categoryId.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Conta/Cartão</label>
                   <Select
                     value={simpleForm.watch('accountId') ?? ''}
                     onValueChange={(value) => simpleForm.setValue('accountId', value)}
                   >
                     <SelectTrigger aria-label="Conta/Cartão">
                       <SelectValue placeholder="Selecione" />
                     </SelectTrigger>
                    <SelectContent>
                      <AccountSelectOptionGroups items={activeAccounts} typeLabels={ACCOUNT_TYPE_LABELS} />
                    </SelectContent>
                  </Select>
                  {simpleForm.formState.errors.accountId && (
                    <p className="text-sm text-red-600 mt-1">
                      {simpleForm.formState.errors.accountId.message}
                    </p>
                  )}
                </div>
              </div>

              {/* Tipo */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Tipo</label>
                   <Select
                     value={String(simpleForm.watch('type'))}
                     onValueChange={(value) => simpleForm.setValue('type', Number(value) as TransactionType)}
                   >
                     <SelectTrigger aria-label="Tipo">
                       <SelectValue />
                     </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={String(TransactionType.Debit)}>Despesa</SelectItem>
                      <SelectItem value={String(TransactionType.Credit)}>Receita</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {/* Datas */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Dt. Competência</label>
                  <Input type="date" {...simpleForm.register('competenceDate')} />
                  {simpleForm.formState.errors.competenceDate && (
                    <p className="text-sm text-red-600 mt-1">
                      {simpleForm.formState.errors.competenceDate.message}
                    </p>
                  )}
                </div>

                <div>
                  <label htmlFor="dueDate" className="block text-sm font-medium mb-2">Dt. Vencimento</label>
                  <Input id="dueDate" type="date" {...simpleForm.register('dueDate')} />
                </div>
              </div>

              {/* Status do Pagamento */}
              <div className="rounded-lg border p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-medium">Status do Pagamento</p>
                    <p className="text-sm text-muted-foreground">
                      {isSimpleCreditCardAccount
                        ? 'Lançamentos em cartão de crédito são sempre registrados como pagos'
                        : 'Marque se a transação já foi realizada'}
                    </p>
                  </div>
                  <Switch
                    checked={isSimpleCreditCardAccount || simpleForm.watch('status') === TransactionStatus.Paid}
                    onCheckedChange={(checked) => {
                      if (isSimpleCreditCardAccount) {
                        simpleForm.setValue('status', TransactionStatus.Paid);
                        return;
                      }

                      simpleForm.setValue(
                        'status',
                        checked ? TransactionStatus.Paid : TransactionStatus.Pending
                      );
                    }}
                    disabled={isSimpleCreditCardAccount}
                  />
                </div>
              </div>

              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => handleClose(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={createTransaction.isPending}>
                  <Save className="mr-2 h-4 w-4" />
                  Salvar Transação
                </Button>
              </DialogFooter>
            </form>
          </TabsContent>

          {/* Aba Parcelada */}
          <TabsContent value="installment" className="space-y-4">
            <form
              onSubmit={installmentForm.handleSubmit(handleInstallmentSubmit)}
              className="space-y-4"
            >
              {/* Valor Total */}
              <div>
                <label htmlFor="totalAmount" className="block text-center text-sm font-medium mb-2">
                  Valor total do parcelamento
                </label>
                <Controller
                  name="totalAmount"
                  control={installmentForm.control}
                  render={({ field }) => (
                    <CurrencyInput
                      id="totalAmount"
                      placeholder="R$ 0,00"
                      value={field.value}
                      onChange={field.onChange}
                    />
                  )}
                />
                {installmentForm.formState.errors.totalAmount && (
                  <p className="text-sm text-red-600 mt-1">
                    {installmentForm.formState.errors.totalAmount.message}
                  </p>
                )}
              </div>

               {/* Número de Parcelas */}
               <div>
                 <label htmlFor="installmentCount" className="block text-sm font-medium mb-2">Número de Parcelas</label>
                   <Input
                     id="installmentCount"
                     type="number"
                     min="2"
                     max="60"
                     disabled={!!transaction}
                     {...installmentForm.register('installmentCount')}
                   />
                {installmentForm.formState.errors.installmentCount && (
                  <p className="text-sm text-red-600 mt-1">
                    {installmentForm.formState.errors.installmentCount.message}
                  </p>
                )}
              </div>

              {/* Descrição */}
              <div>
                <label htmlFor="installment-description" className="block text-sm font-medium mb-2">Descrição</label>
                <Input
                  id="installment-description"
                  placeholder="Ex: Compra parcelada no cartão"
                  {...installmentForm.register('description')}
                />
                {installmentForm.formState.errors.description && (
                  <p className="text-sm text-red-600 mt-1">
                    {installmentForm.formState.errors.description.message}
                  </p>
                )}
              </div>

              {/* Categoria e Conta */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Categoria</label>
                   <Select
                     value={installmentForm.watch('categoryId') ?? ''}
                     onValueChange={(value) => installmentForm.setValue('categoryId', value)}
                   >
                     <SelectTrigger aria-label="Categoria">
                       <SelectValue placeholder="Selecione" />
                     </SelectTrigger>
                    <SelectContent>
                      <CategorySelectOptionGroups
                        items={installmentCategoriesToRender}
                        expenseType={CategoryType.Expense}
                        incomeType={CategoryType.Income}
                      />
                    </SelectContent>
                  </Select>
                  {installmentForm.formState.errors.categoryId && (
                    <p className="text-sm text-red-600 mt-1">
                      {installmentForm.formState.errors.categoryId.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Conta</label>
                   <Select
                     value={installmentForm.watch('accountId') ?? ''}
                     onValueChange={(value) => installmentForm.setValue('accountId', value)}
                   >
                     <SelectTrigger aria-label="Conta">
                       <SelectValue placeholder="Selecione" />
                     </SelectTrigger>
                    <SelectContent>
                      <AccountSelectOptionGroups items={cardAccounts} typeLabels={ACCOUNT_TYPE_LABELS} />
                    </SelectContent>
                  </Select>
                  {installmentForm.formState.errors.accountId && (
                    <p className="text-sm text-red-600 mt-1">
                      {installmentForm.formState.errors.accountId.message}
                    </p>
                  )}
                </div>
              </div>

              {/* Tipo */}
              <div>
                <label className="block text-sm font-medium mb-2">Tipo</label>
                <Input value="Despesa" disabled aria-label="Tipo" />
              </div>

              {/* Datas */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="firstCompetenceDate" className="block text-sm font-medium mb-2">
                    Primeira Dt. Competência
                  </label>
                  <Input id="firstCompetenceDate" type="date" {...installmentForm.register('firstCompetenceDate')} />
                  {installmentForm.formState.errors.firstCompetenceDate && (
                    <p className="text-sm text-red-600 mt-1">
                      {installmentForm.formState.errors.firstCompetenceDate.message}
                    </p>
                  )}
                </div>

                <div>
                  <label htmlFor="firstDueDate" className="block text-sm font-medium mb-2">Primeiro Vencimento</label>
                  <Input id="firstDueDate" type="date" {...installmentForm.register('firstDueDate')} />
                </div>
              </div>

              {/* Preview */}
              <InstallmentPreview
                totalAmount={installmentForm.watch('totalAmount')}
                installmentCount={installmentForm.watch('installmentCount')}
                firstCompetenceDate={installmentForm.watch('firstCompetenceDate')}
                firstDueDate={installmentForm.watch('firstDueDate')}
              />

              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => handleClose(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={createInstallment.isPending}>
                  <Save className="mr-2 h-4 w-4" />
                  Criar Parcelamento
                </Button>
              </DialogFooter>
            </form>
          </TabsContent>

          {/* Aba Recorrente */}
          <TabsContent value="recurrence" className="space-y-4">
            <form
              onSubmit={recurrenceForm.handleSubmit(handleRecurrenceSubmit)}
              className="space-y-4"
            >
              {/* Valor */}
              <div>
                <label htmlFor="recurrenceAmount" className="block text-center text-sm font-medium mb-2">
                  Valor da transação recorrente
                </label>
                <Controller
                  name="amount"
                  control={recurrenceForm.control}
                  render={({ field }) => (
                    <CurrencyInput
                      id="recurrenceAmount"
                      placeholder="R$ 0,00"
                      value={field.value}
                      onChange={field.onChange}
                    />
                  )}
                />
                {recurrenceForm.formState.errors.amount && (
                  <p className="text-sm text-red-600 mt-1">
                    {recurrenceForm.formState.errors.amount.message}
                  </p>
                )}
              </div>

              {/* Descrição */}
              <div>
                <label htmlFor="recurrence-description" className="block text-sm font-medium mb-2">Descrição</label>
                <Input
                  id="recurrence-description"
                  placeholder="Ex: Assinatura Netflix"
                  {...recurrenceForm.register('description')}
                />
                {recurrenceForm.formState.errors.description && (
                  <p className="text-sm text-red-600 mt-1">
                    {recurrenceForm.formState.errors.description.message}
                  </p>
                )}
              </div>

              {/* Categoria e Conta */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Categoria</label>
                   <Select
                     value={recurrenceForm.watch('categoryId') ?? ''}
                     onValueChange={(value) => recurrenceForm.setValue('categoryId', value)}
                   >
                     <SelectTrigger aria-label="Categoria">
                       <SelectValue placeholder="Selecione" />
                     </SelectTrigger>
                    <SelectContent>
                      <CategorySelectOptionGroups
                        items={recurrenceCategoriesToRender}
                        expenseType={CategoryType.Expense}
                        incomeType={CategoryType.Income}
                      />
                    </SelectContent>
                  </Select>
                  {recurrenceForm.formState.errors.categoryId && (
                    <p className="text-sm text-red-600 mt-1">
                      {recurrenceForm.formState.errors.categoryId.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Conta/Cartão</label>
                   <Select
                     value={recurrenceForm.watch('accountId') ?? ''}
                     onValueChange={(value) => recurrenceForm.setValue('accountId', value)}
                   >
                     <SelectTrigger aria-label="Conta/Cartão">
                       <SelectValue placeholder="Selecione" />
                     </SelectTrigger>
                    <SelectContent>
                      <AccountSelectOptionGroups items={activeAccounts} typeLabels={ACCOUNT_TYPE_LABELS} />
                    </SelectContent>
                  </Select>
                  {recurrenceForm.formState.errors.accountId && (
                    <p className="text-sm text-red-600 mt-1">
                      {recurrenceForm.formState.errors.accountId.message}
                    </p>
                  )}
                </div>
              </div>

              {/* Tipo */}
              <div>
                <label className="block text-sm font-medium mb-2">Tipo</label>
                 <Select
                   value={String(recurrenceForm.watch('type'))}
                   onValueChange={(value) => recurrenceForm.setValue('type', Number(value) as TransactionType)}
                 >
                   <SelectTrigger aria-label="Tipo">
                     <SelectValue />
                   </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={String(TransactionType.Debit)}>Despesa</SelectItem>
                    <SelectItem value={String(TransactionType.Credit)}>Receita</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Datas */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="startDate" className="block text-sm font-medium mb-2">Data de Início</label>
                  <Input id="startDate" type="date" {...recurrenceForm.register('startDate')} />
                  {recurrenceForm.formState.errors.startDate && (
                    <p className="text-sm text-red-600 mt-1">
                      {recurrenceForm.formState.errors.startDate.message}
                    </p>
                  )}
                </div>

                <div>
                  <label htmlFor="recurrence-dueDate" className="block text-sm font-medium mb-2">Dt. Vencimento</label>
                  <Input id="recurrence-dueDate" type="date" {...recurrenceForm.register('dueDate')} />
                </div>
              </div>

              <div className="rounded-lg border border-blue-200 bg-blue-50 p-4">
                <p className="text-sm text-blue-800">
                  <RepeatIcon className="inline h-4 w-4 mr-2" />
                  Esta transação será criada automaticamente todos os meses a partir da data de
                  início.
                </p>
              </div>

              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => handleClose(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={createRecurrence.isPending}>
                  <Save className="mr-2 h-4 w-4" />
                  Criar Recorrência
                </Button>
              </DialogFooter>
            </form>
          </TabsContent>

          {/* Aba Transferência */}
          <TabsContent value="transfer" className="space-y-4">
            <form onSubmit={transferForm.handleSubmit(handleTransferSubmit)} className="space-y-4">
              {/* Valor */}
              <div>
                <label htmlFor="transferAmount" className="block text-center text-sm font-medium mb-2">
                  Valor da transferência
                </label>
                <Controller
                  name="amount"
                  control={transferForm.control}
                  render={({ field }) => (
                    <CurrencyInput
                      id="transferAmount"
                      placeholder="R$ 0,00"
                      value={field.value}
                      onChange={field.onChange}
                    />
                  )}
                />
                {transferForm.formState.errors.amount && (
                  <p className="text-sm text-red-600 mt-1">
                    {transferForm.formState.errors.amount.message}
                  </p>
                )}
              </div>

              {/* Descrição */}
              <div>
                <label htmlFor="transfer-description" className="block text-sm font-medium mb-2">Descrição</label>
                <Input
                  id="transfer-description"
                  placeholder="Ex: Transferência entre contas"
                  {...transferForm.register('description')}
                />
                {transferForm.formState.errors.description && (
                  <p className="text-sm text-red-600 mt-1">
                    {transferForm.formState.errors.description.message}
                  </p>
                )}
              </div>

              {/* Contas */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium mb-2">Conta Origem</label>
                   <Select
                     value={transferForm.watch('sourceAccountId') ?? ''}
                     onValueChange={(value) => transferForm.setValue('sourceAccountId', value)}
                   >
                     <SelectTrigger aria-label="Conta Origem">
                       <SelectValue placeholder="Selecione" />
                     </SelectTrigger>
                    <SelectContent>
                      <AccountSelectOptionGroups items={transferAccounts} typeLabels={ACCOUNT_TYPE_LABELS} />
                    </SelectContent>
                  </Select>
                  {transferForm.formState.errors.sourceAccountId && (
                    <p className="text-sm text-red-600 mt-1">
                      {transferForm.formState.errors.sourceAccountId.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className="block text-sm font-medium mb-2">Conta Destino</label>
                   <Select
                     value={transferForm.watch('destinationAccountId') ?? ''}
                     onValueChange={(value) => transferForm.setValue('destinationAccountId', value)}
                   >
                     <SelectTrigger aria-label="Conta Destino">
                       <SelectValue placeholder="Selecione" />
                     </SelectTrigger>
                    <SelectContent>
                      <AccountSelectOptionGroups items={transferAccounts} typeLabels={ACCOUNT_TYPE_LABELS} />
                    </SelectContent>
                  </Select>
                  {transferForm.formState.errors.destinationAccountId && (
                    <p className="text-sm text-red-600 mt-1">
                      {transferForm.formState.errors.destinationAccountId.message}
                    </p>
                  )}
                </div>
              </div>

              {/* Categoria */}
              <div>
                <label className="block text-sm font-medium mb-2">Categoria</label>
                 <Select
                   value={transferForm.watch('categoryId') ?? ''}
                   onValueChange={(value) => transferForm.setValue('categoryId', value)}
                 >
                   <SelectTrigger aria-label="Categoria">
                     <SelectValue placeholder="Selecione" />
                   </SelectTrigger>
                  <SelectContent>
                    <CategorySelectOptionGroups
                      items={categories ?? []}
                      expenseType={CategoryType.Expense}
                      incomeType={CategoryType.Income}
                    />
                  </SelectContent>
                </Select>
                {transferForm.formState.errors.categoryId && (
                  <p className="text-sm text-red-600 mt-1">
                    {transferForm.formState.errors.categoryId.message}
                  </p>
                )}
              </div>

              {/* Data */}
              <div>
                <label htmlFor="transfer-competenceDate" className="block text-sm font-medium mb-2">Data de Competência</label>
                <Input id="transfer-competenceDate" type="date" {...transferForm.register('competenceDate')} />
                {transferForm.formState.errors.competenceDate && (
                  <p className="text-sm text-red-600 mt-1">
                    {transferForm.formState.errors.competenceDate.message}
                  </p>
                )}
              </div>

              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => handleClose(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={createTransfer.isPending}>
                  <Save className="mr-2 h-4 w-4" />
                  Criar Transferência
                </Button>
              </DialogFooter>
            </form>
          </TabsContent>
        </Tabs>
      </DialogContent>
    </Dialog>
  );
}

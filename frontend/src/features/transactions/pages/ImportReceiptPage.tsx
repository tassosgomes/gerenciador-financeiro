import { useMemo, useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { Loader2, ReceiptText, Search, ArrowLeft, ArrowRight } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';

import { useAccounts } from '@/features/accounts/hooks/useAccounts';
import { AccountType } from '@/features/accounts/types/account';
import { useCategories } from '@/features/categories/hooks/useCategories';
import { CategoryType } from '@/features/categories/types/category';
import { ReceiptPreview } from '@/features/transactions/components/ReceiptPreview';
import { useReceiptImport } from '@/features/transactions/hooks/useReceiptImport';
import { useReceiptLookup } from '@/features/transactions/hooks/useReceiptLookup';
import {
  importReceiptSchema,
  type ImportReceiptFormData,
} from '@/features/transactions/schemas/importReceiptSchema';
import type { ReceiptLookupResponse } from '@/features/transactions/types/receipt';
import { ACCOUNT_TYPE_LABELS } from '@/shared/utils/constants';
import {
  AccountSelectOptionGroups,
  CategorySelectOptionGroups,
} from '@/shared/components/ui/grouped-select-options';
import { Button } from '@/shared/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/shared/components/ui/card';
import { Input } from '@/shared/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select';

type ImportStep = 1 | 2 | 3;
type ImportReceiptFieldErrorKey = keyof ImportReceiptFormData;
type ImportReceiptFieldErrors = Partial<Record<ImportReceiptFieldErrorKey, string>>;

function detectReceiptInputType(value: string): 'url' | 'key' {
  const normalized = value.trim().toLowerCase();
  if (normalized.includes('http') || normalized.includes('sefaz')) {
    return 'url';
  }

  return 'key';
}

function formatAccessKeyInput(value: string): string {
  const digits = value.replace(/\D/g, '').slice(0, 44);
  return digits.replace(/(\d{4})(?=\d)/g, '$1 ').trim();
}

function normalizeLookupInput(value: string): string {
  const type = detectReceiptInputType(value);

  if (type === 'url') {
    return value.trim();
  }

  return value.replace(/\D/g, '');
}

function toDateInputValue(value: string): string {
  const parsedDate = new Date(value);

  if (Number.isNaN(parsedDate.getTime())) {
    return new Date().toISOString().slice(0, 10);
  }

  return parsedDate.toISOString().slice(0, 10);
}

export default function ImportReceiptPage(): JSX.Element {
  const navigate = useNavigate();
  const [step, setStep] = useState<ImportStep>(1);
  const [receipt, setReceipt] = useState<ReceiptLookupResponse | null>(null);
  const [isDuplicateReceipt, setIsDuplicateReceipt] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<ImportReceiptFieldErrors>({});

  const { data: accounts } = useAccounts();
  const { data: categories } = useCategories(CategoryType.Expense);
  const lookupMutation = useReceiptLookup();
  const importMutation = useReceiptImport();

  const form = useForm<ImportReceiptFormData>({
    resolver: zodResolver(importReceiptSchema),
    defaultValues: {
      input: '',
      accountId: '',
      categoryId: '',
      description: '',
      competenceDate: new Date(),
    },
  });

  const activeAccounts = useMemo(
    () => (accounts ?? []).filter(
      (account) => account.isActive
        && [AccountType.Corrente, AccountType.Cartao, AccountType.Carteira, AccountType.Investimento].includes(account.type),
    ),
    [accounts],
  );

  const lookupInputValue = form.watch('input');
  const currentInputType = detectReceiptInputType(lookupInputValue ?? '');
  const watchedCompetenceDate = form.watch('competenceDate');
  const competenceDateValue = watchedCompetenceDate instanceof Date
    ? toDateInputValue(watchedCompetenceDate.toISOString())
    : toDateInputValue(new Date().toISOString());

  const handleLookupInputChange = (value: string) => {
    const detectedType = detectReceiptInputType(value);

    if (detectedType === 'url') {
      form.setValue('input', value);
      setFieldErrors((current) => ({ ...current, input: undefined }));
      return;
    }

    form.setValue('input', formatAccessKeyInput(value));
    setFieldErrors((current) => ({ ...current, input: undefined }));
  };

  const validateFormData = (values: ImportReceiptFormData, keys: ImportReceiptFieldErrorKey[]): boolean => {
    const parseResult = importReceiptSchema.safeParse(values);

    if (parseResult.success) {
      setFieldErrors((current) => {
        const next = { ...current };
        keys.forEach((key) => {
          next[key] = undefined;
        });
        return next;
      });
      return true;
    }

    const nextErrors: ImportReceiptFieldErrors = {};

    parseResult.error.issues.forEach((issue) => {
      const key = issue.path[0] as ImportReceiptFieldErrorKey;
      if (keys.includes(key) && !nextErrors[key]) {
        nextErrors[key] = issue.message;
      }
    });

    setFieldErrors((current) => ({
      ...current,
      ...nextErrors,
    }));

    return Object.keys(nextErrors).length === 0;
  };

  const handleLookup = async () => {
    const values: ImportReceiptFormData = {
      input: form.watch('input'),
      accountId: form.watch('accountId'),
      categoryId: form.watch('categoryId'),
      description: form.watch('description'),
      competenceDate: form.watch('competenceDate'),
    };

    const isValidInput = validateFormData(values, ['input']);
    if (!isValidInput) {
      return;
    }

    const rawInput = form.watch('input');
    const normalizedInput = normalizeLookupInput(rawInput);
    if (!normalizedInput) {
      return;
    }

    const lookupResult = await lookupMutation.mutateAsync({ input: normalizedInput });

    setReceipt(lookupResult);
    setIsDuplicateReceipt(lookupResult.alreadyImported);
    form.setValue('description', lookupResult.establishmentName);
    form.setValue('competenceDate', new Date(lookupResult.issuedAt));

    if (!lookupResult.alreadyImported) {
      setStep(2);
    }
  };

  const handleImport = async () => {
    if (!receipt) {
      return;
    }

    const values: ImportReceiptFormData = {
      input: form.watch('input'),
      accountId: form.watch('accountId'),
      categoryId: form.watch('categoryId'),
      description: form.watch('description'),
      competenceDate: form.watch('competenceDate'),
    };

    const isStep2Valid = validateFormData(values, ['accountId', 'categoryId', 'description', 'competenceDate']);
    if (!isStep2Valid) {
      return;
    }

    setStep(3);

    try {
      const response = await importMutation.mutateAsync({
        accessKey: receipt.accessKey,
        accountId: values.accountId,
        categoryId: values.categoryId,
        description: values.description.trim(),
        competenceDate: values.competenceDate.toISOString().slice(0, 10),
      });

      navigate(`/transactions/${response.transaction.id}`);
    } catch {
      setStep(2);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Importar Cupom Fiscal</h1>
        <p className="text-muted-foreground">
          Consulte a NFC-e e importe automaticamente a transação com os itens do cupom.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-base">
            <ReceiptText className="h-4 w-4" />
            Etapas: 1) Consultar 2) Revisar 3) Confirmar
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          {step === 1 && (
            <div className="space-y-4">
              <div>
                <label htmlFor="receipt-input" className="mb-2 block text-sm font-medium">
                  Chave de acesso (44 dígitos) ou URL da NFC-e
                </label>
                <Input
                  id="receipt-input"
                  value={lookupInputValue ?? ''}
                  onChange={(event) => handleLookupInputChange(event.target.value)}
                  placeholder="Cole a chave de acesso ou URL da SEFAZ"
                  aria-invalid={Boolean(form.formState.errors.input)}
                />
                <p className="mt-1 text-xs text-muted-foreground" aria-live="polite">
                  Tipo detectado: {currentInputType === 'url' ? 'URL da NFC-e' : 'Chave de acesso'}
                </p>
                {fieldErrors.input && (
                  <p className="mt-1 text-sm text-destructive" role="alert">
                    {fieldErrors.input}
                  </p>
                )}
              </div>

              {isDuplicateReceipt && receipt && (
                <div className="rounded-md border border-destructive/40 bg-destructive/10 p-4 text-destructive" role="alert">
                  <p className="font-medium">Cupom já importado</p>
                  <p className="text-sm">
                    Esta chave de acesso já foi importada anteriormente e não pode ser importada novamente.
                  </p>
                </div>
              )}

              <div className="flex justify-end">
                <Button
                  onClick={handleLookup}
                  disabled={lookupMutation.isPending}
                >
                  {lookupMutation.isPending ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Consultando SEFAZ...
                    </>
                  ) : (
                    <>
                      <Search className="mr-2 h-4 w-4" />
                      Consultar
                    </>
                  )}
                </Button>
              </div>
            </div>
          )}

          {step === 2 && receipt && (
            <div className="space-y-6">
              <ReceiptPreview receipt={receipt} issuedAt={receipt.issuedAt} />

              <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                <div>
                  <label className="mb-2 block text-sm font-medium">Conta</label>
                  <Select
                    value={form.watch('accountId') || '__empty__'}
                    onValueChange={(value) => {
                      form.setValue('accountId', value === '__empty__' ? '' : value);
                      setFieldErrors((current) => ({ ...current, accountId: undefined }));
                    }}
                  >
                    <SelectTrigger aria-label="Conta da transação">
                      <SelectValue placeholder="Selecione uma conta" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__empty__">Selecione uma conta</SelectItem>
                      <AccountSelectOptionGroups
                        items={activeAccounts}
                        typeLabels={ACCOUNT_TYPE_LABELS}
                        orderedTypes={[AccountType.Corrente, AccountType.Cartao, AccountType.Carteira, AccountType.Investimento]}
                        includeLeadingSeparator
                      />
                    </SelectContent>
                  </Select>
                  {fieldErrors.accountId && (
                    <p className="mt-1 text-sm text-destructive" role="alert">
                      {fieldErrors.accountId}
                    </p>
                  )}
                </div>

                <div>
                  <label className="mb-2 block text-sm font-medium">Categoria de despesa</label>
                  <Select
                    value={form.watch('categoryId') || '__empty__'}
                    onValueChange={(value) => {
                      form.setValue('categoryId', value === '__empty__' ? '' : value);
                      setFieldErrors((current) => ({ ...current, categoryId: undefined }));
                    }}
                  >
                    <SelectTrigger aria-label="Categoria da transação">
                      <SelectValue placeholder="Selecione uma categoria" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__empty__">Selecione uma categoria</SelectItem>
                      <CategorySelectOptionGroups
                        items={categories ?? []}
                        expenseType={CategoryType.Expense}
                        incomeType={CategoryType.Income}
                        includeLeadingSeparator
                      />
                    </SelectContent>
                  </Select>
                  {fieldErrors.categoryId && (
                    <p className="mt-1 text-sm text-destructive" role="alert">
                      {fieldErrors.categoryId}
                    </p>
                  )}
                </div>

                <div className="md:col-span-2">
                  <label htmlFor="receipt-description" className="mb-2 block text-sm font-medium">Descrição</label>
                  <Input
                    id="receipt-description"
                    value={form.watch('description')}
                    onChange={(event) => {
                      form.setValue('description', event.target.value);
                      setFieldErrors((current) => ({ ...current, description: undefined }));
                    }}
                  />
                  {fieldErrors.description && (
                    <p className="mt-1 text-sm text-destructive" role="alert">
                      {fieldErrors.description}
                    </p>
                  )}
                </div>

                <div>
                  <label htmlFor="receipt-date" className="mb-2 block text-sm font-medium">Data da transação</label>
                  <Input
                    id="receipt-date"
                    type="date"
                    value={competenceDateValue}
                    onChange={(event) => {
                      form.setValue('competenceDate', new Date(`${event.target.value}T00:00:00`));
                      setFieldErrors((current) => ({ ...current, competenceDate: undefined }));
                    }}
                  />
                  {fieldErrors.competenceDate && (
                    <p className="mt-1 text-sm text-destructive" role="alert">
                      {fieldErrors.competenceDate}
                    </p>
                  )}
                </div>
              </div>

              <div className="flex items-center justify-between">
                <Button variant="outline" onClick={() => setStep(1)}>
                  <ArrowLeft className="mr-2 h-4 w-4" />
                  Voltar
                </Button>
                <Button onClick={handleImport} disabled={importMutation.isPending}>
                  Importar
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Button>
              </div>
            </div>
          )}

          {step === 3 && (
            <div className="flex flex-col items-center justify-center gap-4 py-12 text-center">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
              <div>
                <p className="font-medium">Importando cupom fiscal...</p>
                <p className="text-sm text-muted-foreground">
                  Estamos criando a transação e vinculando os itens do cupom.
                </p>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

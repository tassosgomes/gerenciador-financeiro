import { z } from 'zod';
import { TransactionType, TransactionStatus } from '@/features/transactions/types/transaction';

// Schema para transação simples
export const simpleTransactionSchema = z.object({
  accountId: z.string().min(1, 'Selecione uma conta'),
  categoryId: z.string().min(1, 'Selecione uma categoria'),
  type: z.nativeEnum(TransactionType),
  amount: z.coerce.number().min(0.01, 'Valor deve ser maior que zero'),
  description: z.string().min(3, 'Descrição deve ter no mínimo 3 caracteres').max(200),
  competenceDate: z.string().min(1, 'Data de competência é obrigatória'),
  dueDate: z.string().optional(),
  status: z.nativeEnum(TransactionStatus),
});

// Schema para transação parcelada
export const installmentSchema = z.object({
  accountId: z.string().min(1, 'Selecione uma conta'),
  categoryId: z.string().min(1, 'Selecione uma categoria'),
  type: z.nativeEnum(TransactionType),
  totalAmount: z.coerce.number().min(0.01, 'Valor total deve ser maior que zero'),
  installmentCount: z.coerce.number().min(2, 'Número de parcelas deve ser no mínimo 2').max(60, 'Máximo de 60 parcelas'),
  description: z.string().min(3, 'Descrição deve ter no mínimo 3 caracteres').max(200),
  firstCompetenceDate: z.string().min(1, 'Data da primeira competência é obrigatória'),
  firstDueDate: z.string().optional(),
});

// Schema para transação recorrente
export const recurrenceSchema = z.object({
  accountId: z.string().min(1, 'Selecione uma conta'),
  categoryId: z.string().min(1, 'Selecione uma categoria'),
  type: z.nativeEnum(TransactionType),
  amount: z.coerce.number().min(0.01, 'Valor deve ser maior que zero'),
  description: z.string().min(3, 'Descrição deve ter no mínimo 3 caracteres').max(200),
  startDate: z.string().min(1, 'Data de início é obrigatória'),
  dueDate: z.string().optional(),
});

// Schema para transferência
export const transferSchema = z.object({
  sourceAccountId: z.string().min(1, 'Selecione a conta de origem'),
  destinationAccountId: z.string().min(1, 'Selecione a conta de destino'),
  categoryId: z.string().min(1, 'Selecione uma categoria'),
  amount: z.coerce.number().min(0.01, 'Valor deve ser maior que zero'),
  description: z.string().min(3, 'Descrição deve ter no mínimo 3 caracteres').max(200),
  competenceDate: z.string().min(1, 'Data de competência é obrigatória'),
}).refine((data) => data.sourceAccountId !== data.destinationAccountId, {
  message: 'Conta de origem e destino não podem ser iguais',
  path: ['destinationAccountId'],
});

export type SimpleTransactionFormValues = z.infer<typeof simpleTransactionSchema>;
export type InstallmentFormValues = z.infer<typeof installmentSchema>;
export type RecurrenceFormValues = z.infer<typeof recurrenceSchema>;
export type TransferFormValues = z.infer<typeof transferSchema>;

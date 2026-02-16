import { z } from 'zod';
import { AccountType } from '@/features/accounts/types/account';

// Base schema for common fields
const baseAccountSchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres').max(100, 'Nome muito longo'),
  type: z.number(),
});

// Schema for regular accounts (Corrente, Investimento, Carteira)
const regularAccountFields = z.object({
  initialBalance: z.number(),
  allowNegativeBalance: z.boolean(),
});

// Schema for credit card accounts
const creditCardFields = z.object({
  creditLimit: z.number().positive('Limite deve ser maior que zero'),
  closingDay: z.number().int().min(1, 'Dia deve ser entre 1 e 28').max(28, 'Dia deve ser entre 1 e 28'),
  dueDay: z.number().int().min(1, 'Dia deve ser entre 1 e 28').max(28, 'Dia deve ser entre 1 e 28'),
  debitAccountId: z.string().uuid('Conta de débito obrigatória'),
  enforceCreditLimit: z.boolean().default(true),
});

// Create account schema with conditional validation
export const createAccountSchema = baseAccountSchema.and(
  z.union([
    // Regular account
    regularAccountFields.extend({
      type: z.literal(AccountType.Corrente)
        .or(z.literal(AccountType.Investimento))
        .or(z.literal(AccountType.Carteira)),
    }),
    // Credit card account
    creditCardFields.extend({
      type: z.literal(AccountType.Cartao),
    }),
  ])
);

// Update account schema - simpler, allows updating both types
export const updateAccountSchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres').max(100, 'Nome muito longo'),
  allowNegativeBalance: z.boolean().optional(),
  creditLimit: z.number().positive('Limite deve ser maior que zero').optional(),
  closingDay: z.number().int().min(1, 'Dia deve ser entre 1 e 28').max(28, 'Dia deve ser entre 1 e 28').optional(),
  dueDay: z.number().int().min(1, 'Dia deve ser entre 1 e 28').max(28, 'Dia deve ser entre 1 e 28').optional(),
  debitAccountId: z.string().uuid('Conta de débito obrigatória').optional(),
  enforceCreditLimit: z.boolean().optional(),
});

export type CreateAccountFormValues = z.infer<typeof createAccountSchema>;
export type UpdateAccountFormValues = z.infer<typeof updateAccountSchema>;


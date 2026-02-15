import { z } from 'zod';

export const createAccountSchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres').max(100, 'Nome muito longo'),
  type: z.number(),
  initialBalance: z.number(),
  allowNegativeBalance: z.boolean(),
});

export const updateAccountSchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres').max(100, 'Nome muito longo'),
  allowNegativeBalance: z.boolean(),
});

export type CreateAccountFormValues = z.infer<typeof createAccountSchema>;
export type UpdateAccountFormValues = z.infer<typeof updateAccountSchema>;

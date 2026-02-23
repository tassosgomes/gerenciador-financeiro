import { z } from 'zod';

export const budgetSchema = z.object({
  name: z
    .string()
    .min(2, 'Nome deve ter ao menos 2 caracteres')
    .max(150, 'Nome deve ter no máximo 150 caracteres'),
  percentage: z
    .number({ error: 'Percentual é obrigatório' })
    .gt(0, 'Percentual deve ser maior que 0')
    .lte(100, 'Percentual deve ser no máximo 100%'),
  referenceYear: z.number().int().min(2020),
  referenceMonth: z.number().int().min(1).max(12),
  categoryIds: z
    .array(z.string().trim().min(1, 'Selecione ao menos uma categoria'))
    .min(1, 'Selecione ao menos uma categoria'),
  isRecurrent: z.boolean().default(false),
});

export type BudgetFormData = z.infer<typeof budgetSchema>;
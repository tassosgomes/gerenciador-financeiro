import { z } from 'zod';

const uuidMessage = 'Selecione uma opção válida';

export const importReceiptSchema = z.object({
  input: z.string().trim().min(1, 'Informe a chave de acesso ou URL do cupom fiscal'),
  accountId: z.string().uuid(uuidMessage),
  categoryId: z.string().uuid(uuidMessage),
  description: z.string().trim().min(1, 'Descrição é obrigatória'),
  competenceDate: z.coerce.date({
    error: 'Data de competência é obrigatória',
  }),
});

export type ImportReceiptFormData = z.infer<typeof importReceiptSchema>;

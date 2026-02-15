import { z } from 'zod';

import { RoleType } from '@/features/admin/types/admin';

export const createUserSchema = z.object({
  name: z.string().min(2, 'Nome deve ter no mínimo 2 caracteres'),
  email: z.string().email('E-mail inválido'),
  password: z
    .string()
    .min(8, 'Senha deve ter no mínimo 8 caracteres')
    .regex(/[A-Z]/, 'Senha deve conter pelo menos uma letra maiúscula')
    .regex(/[0-9]/, 'Senha deve conter pelo menos um número'),
  role: z.nativeEnum(RoleType),
});

export type CreateUserFormData = z.infer<typeof createUserSchema>;

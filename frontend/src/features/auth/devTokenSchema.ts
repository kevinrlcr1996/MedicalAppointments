import { z } from 'zod';

export const devTokenSchema = z.object({
  userName: z
    .string()
    .trim()
    .min(1, 'El nombre es obligatorio.')
    .max(100, 'El nombre no puede superar 100 caracteres.'),
});

export type DevTokenFormValues = z.infer<typeof devTokenSchema>;
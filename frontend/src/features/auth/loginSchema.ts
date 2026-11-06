import { z } from 'zod';

export const loginSchema = z.object({
  email: z.string().trim().email('Email inválido.').max(320),
  password: z.string().min(1, 'La contraseña es obligatoria.'),
});

export type LoginFormValues = z.infer<typeof loginSchema>;
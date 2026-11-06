import { z } from 'zod';

export const forgotPasswordSchema = z.object({
  email: z.string().trim().email('Email inválido.').max(320),
});

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;
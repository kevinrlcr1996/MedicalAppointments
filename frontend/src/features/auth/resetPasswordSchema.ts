import { z } from 'zod';

export const resetPasswordSchema = z
  .object({
    token: z.string().min(1, 'Token requerido.'),
    password: z
      .string()
      .min(10, 'La contraseña debe tener al menos 10 caracteres.')
      .regex(/[A-Za-z]/, 'Debe contener al menos una letra.')
      .regex(/[0-9]/, 'Debe contener al menos un número.')
      .regex(/[^A-Za-z0-9]/, 'Debe contener al menos un carácter especial.'),
    confirmPassword: z.string().min(1, 'Confirma la contraseña.'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Las contraseñas no coinciden.',
    path: ['confirmPassword'],
  });

export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;
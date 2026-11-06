import { z } from 'zod';

export const registerSchema = z
  .object({
    email: z.string().trim().email('Email inválido.').max(320),
    password: z
      .string()
      .min(10, 'La contraseña debe tener al menos 10 caracteres.')
      .regex(/[A-Za-z]/, 'Debe contener al menos una letra.')
      .regex(/[0-9]/, 'Debe contener al menos un número.')
      .regex(/[^A-Za-z0-9]/, 'Debe contener al menos un carácter especial.'),
    confirmPassword: z.string().min(1, 'Confirma la contraseña.'),
    firstName: z.string().trim().max(200).optional().or(z.literal('')),
    lastName: z.string().trim().max(200).optional().or(z.literal('')),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Las contraseñas no coinciden.',
    path: ['confirmPassword'],
  });

export type RegisterFormValues = z.infer<typeof registerSchema>;
import { z } from 'zod';

export const testEventSchema = z.object({
  message: z
    .string()
    .trim()
    .min(1, 'El mensaje es obligatorio.')
    .max(500, 'El mensaje no puede superar 500 caracteres.'),
});

export type TestEventFormValues = z.infer<typeof testEventSchema>;
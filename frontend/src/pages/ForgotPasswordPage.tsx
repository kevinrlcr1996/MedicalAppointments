import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { KeyRound } from 'lucide-react';
import { forgotPassword } from '../services/api/auth';
import { ApiError } from '../services/api/apiClient';
import { forgotPasswordSchema, type ForgotPasswordFormValues } from '../features/auth/forgotPasswordSchema';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { FormField } from '../components/ui/FormField';
import { Input } from '../components/ui/Input';

export function ForgotPasswordPage() {
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  const mutation = useMutation({
    mutationFn: forgotPassword,
    onSuccess: (response) => {
      setSuccessMessage(response.message);
    },
  });

  return (
    <Card>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-50">Recuperar contraseña</h2>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Te enviaremos un enlace si el email existe.
          </p>
        </div>

        {successMessage ? (
          <Alert variant="success" title="Solicitud procesada">
            {successMessage}
          </Alert>
        ) : null}

        <form className="space-y-4" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          <FormField label="Email" htmlFor="email" required error={errors.email?.message}>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              {...register('email')}
              invalid={Boolean(errors.email)}
            />
          </FormField>

          {mutation.isError ? (
            <Alert variant="error" title="Error">
              {mutation.error instanceof ApiError ? mutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          <Button type="submit" loading={mutation.isPending} leftIcon={<KeyRound className="h-4 w-4" />}>
            Enviar enlace
          </Button>
        </form>

        <div className="text-sm">
          <Link to="/login" className="text-sky-700 hover:underline dark:text-sky-300">
            Volver al login
          </Link>
        </div>
      </div>
    </Card>
  );
}
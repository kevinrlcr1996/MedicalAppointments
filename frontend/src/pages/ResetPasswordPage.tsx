import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { KeyRound } from 'lucide-react';
import { resetPassword } from '../services/api/auth';
import { ApiError } from '../services/api/apiClient';
import { resetPasswordSchema, type ResetPasswordFormValues } from '../features/auth/resetPasswordSchema';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { FormField } from '../components/ui/FormField';
import { Input } from '../components/ui/Input';

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: {
      token,
      password: '',
      confirmPassword: '',
    },
  });

  const mutation = useMutation({
    mutationFn: resetPassword,
  });

  return (
    <Card>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-50">Restablecer contraseña</h2>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Introduce tu nueva contraseña.
          </p>
        </div>

        {!token ? (
          <Alert variant="error" title="Token missing">
            El enlace no contiene un token válido.
          </Alert>
        ) : null}

        {mutation.isSuccess ? (
          <Alert variant="success" title="Contraseña actualizada">
            Puedes iniciar sesión con tu nueva contraseña.
          </Alert>
        ) : null}

        <form className="space-y-4" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          <Input type="hidden" {...register('token')} />

          <FormField label="Nueva contraseña" htmlFor="password" required error={errors.password?.message}>
            <Input
              id="password"
              type="password"
              autoComplete="new-password"
              {...register('password')}
              invalid={Boolean(errors.password)}
            />
          </FormField>

          <FormField
            label="Confirmar contraseña"
            htmlFor="confirmPassword"
            required
            error={errors.confirmPassword?.message}
          >
            <Input
              id="confirmPassword"
              type="password"
              autoComplete="new-password"
              {...register('confirmPassword')}
              invalid={Boolean(errors.confirmPassword)}
            />
          </FormField>

          {mutation.isError ? (
            <Alert variant="error" title="No se pudo restablecer">
              {mutation.error instanceof ApiError ? mutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          <Button
            type="submit"
            loading={mutation.isPending}
            disabled={!token}
            leftIcon={<KeyRound className="h-4 w-4" />}
          >
            Restablecer
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
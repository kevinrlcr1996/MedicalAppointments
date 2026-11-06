import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { Link, useNavigate } from 'react-router-dom';
import { LogIn } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { login } from '../services/api/auth';
import { ApiError } from '../services/api/apiClient';
import { loginSchema, type LoginFormValues } from '../features/auth/loginSchema';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { FormField } from '../components/ui/FormField';
import { Input } from '../components/ui/Input';

export function LoginPage() {
  const navigate = useNavigate();
  const { status, signIn } = useAuth();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      email: '',
      password: '',
    },
  });

  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (response) => {
      signIn(response);
      navigate('/', { replace: true });
    },
  });

  useEffect(() => {
    if (status === 'authenticated') {
      navigate('/', { replace: true });
    }
  }, [status, navigate]);

  return (
    <Card>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-50">Iniciar sesión</h2>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Accede con tu email y contraseña.
          </p>
        </div>

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

          <FormField label="Contraseña" htmlFor="password" required error={errors.password?.message}>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              {...register('password')}
              invalid={Boolean(errors.password)}
            />
          </FormField>

          {mutation.isError ? (
            <Alert variant="error" title="No se pudo iniciar sesión">
              {mutation.error instanceof ApiError ? mutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          <Button type="submit" loading={mutation.isPending} leftIcon={<LogIn className="h-4 w-4" />}>
            Entrar
          </Button>
        </form>

        <div className="flex flex-wrap gap-3 text-sm">
          <Link to="/register" className="text-sky-700 hover:underline dark:text-sky-300">
            Crear cuenta
          </Link>
          <Link to="/forgot-password" className="text-slate-600 hover:underline dark:text-slate-400">
            Olvidé mi contraseña
          </Link>
        </div>
      </div>
    </Card>
  );
}
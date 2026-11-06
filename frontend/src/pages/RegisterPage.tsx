import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { UserPlus } from 'lucide-react';
import { register } from '../services/api/auth';
import { ApiError } from '../services/api/apiClient';
import { registerSchema, type RegisterFormValues } from '../features/auth/registerSchema';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { FormField } from '../components/ui/FormField';
import { Input } from '../components/ui/Input';

export function RegisterPage() {
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const {
    register: registerField,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
      firstName: '',
      lastName: '',
    },
  });

  const mutation = useMutation({
    mutationFn: register,
    onSuccess: (response) => {
      setSuccessMessage(response.message);
    },
  });

  return (
    <Card>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-50">Crear cuenta</h2>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Regístrate como paciente. Recibirás un email de confirmación.
          </p>
        </div>

        {successMessage ? (
          <Alert variant="success" title="Registro recibido">
            {successMessage}
          </Alert>
        ) : null}

        <form className="space-y-4" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          <FormField label="Email" htmlFor="email" required error={errors.email?.message}>
            <Input
              id="email"
              type="email"
              autoComplete="email"
              {...registerField('email')}
              invalid={Boolean(errors.email)}
            />
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Nombre" htmlFor="firstName" error={errors.firstName?.message}>
              <Input id="firstName" autoComplete="given-name" {...registerField('firstName')} />
            </FormField>

            <FormField label="Apellido" htmlFor="lastName" error={errors.lastName?.message}>
              <Input id="lastName" autoComplete="family-name" {...registerField('lastName')} />
            </FormField>
          </div>

          <FormField label="Contraseña" htmlFor="password" required error={errors.password?.message}>
            <Input
              id="password"
              type="password"
              autoComplete="new-password"
              {...registerField('password')}
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
              {...registerField('confirmPassword')}
              invalid={Boolean(errors.confirmPassword)}
            />
          </FormField>

          {mutation.isError ? (
            <Alert variant="error" title="No se pudo registrar">
              {mutation.error instanceof ApiError ? mutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          <Button type="submit" loading={mutation.isPending} leftIcon={<UserPlus className="h-4 w-4" />}>
            Registrarse
          </Button>
        </form>

        <div className="text-sm">
          <Link to="/login" className="text-sky-700 hover:underline dark:text-sky-300">
            Ya tengo cuenta
          </Link>
        </div>
      </div>
    </Card>
  );
}
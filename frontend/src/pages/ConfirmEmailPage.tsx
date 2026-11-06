import { useEffect } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { MailCheck } from 'lucide-react';
import { confirmEmail } from '../services/api/auth';
import { ApiError } from '../services/api/apiClient';
import { Alert } from '../components/ui/Alert';
import { Card } from '../components/ui/Card';
import { Spinner } from '../components/ui/Spinner';

export function ConfirmEmailPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const mutation = useMutation({
    mutationFn: confirmEmail,
  });

  useEffect(() => {
    if (!token) return;
    mutation.mutate({ token });
  }, [token]);

  if (!token) {
    return (
      <Card>
        <Alert variant="error" title="Token missing">
          El enlace de confirmación no contiene un token válido.
        </Alert>
      </Card>
    );
  }

  return (
    <Card>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-50">Confirmar email</h2>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Estamos confirmando tu dirección de email.
          </p>
        </div>

        {mutation.isPending ? <Spinner label="Confirmando..." /> : null}

        {mutation.isSuccess ? (
          <Alert variant="success" title="Email confirmado">
            Tu email ha sido confirmado correctamente.
          </Alert>
        ) : null}

        {mutation.isError ? (
          <Alert variant="error" title="No se pudo confirmar">
            {mutation.error instanceof ApiError ? mutation.error.message : 'Error inesperado.'}
          </Alert>
        ) : null}

        <Link
          to="/login"
          className="inline-flex items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-900 transition hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:hover:bg-slate-800"
        >
          <MailCheck className="h-4 w-4" />
          Ir al login
        </Link>
      </div>
    </Card>
  );
}
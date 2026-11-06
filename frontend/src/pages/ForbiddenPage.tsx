import { Link } from 'react-router-dom';
import { ShieldAlert } from 'lucide-react';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';

export function ForbiddenPage() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 p-4 dark:bg-slate-950">
      <Card className="w-full max-w-lg text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-rose-100 text-rose-700 dark:bg-rose-950 dark:text-rose-200">
          <ShieldAlert className="h-6 w-6" />
        </div>

        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-50">Acceso denegado</h1>
        <p className="mt-2 text-sm text-slate-600 dark:text-slate-400">
          No tienes permisos suficientes para ver esta sección.
        </p>

        <Button className="mt-6" variant="secondary">
          <Link to="/dashboard">Volver al dashboard</Link>
        </Button>
      </Card>
    </div>
  );
}
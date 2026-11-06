import { Outlet } from 'react-router-dom';
import { Stethoscope } from 'lucide-react';

export function AuthLayout() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-50 p-4 dark:bg-slate-950">
      <div className="w-full max-w-md space-y-6">
        <div className="flex items-center justify-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-sky-600 text-white shadow-sm">
            <Stethoscope className="h-6 w-6" aria-hidden="true" />
          </div>
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-slate-900 dark:text-slate-50">
              Medical Appointments
            </h1>
            <p className="text-sm text-slate-600 dark:text-slate-400">Plataforma de gestión clínica</p>
          </div>
        </div>

        <Outlet />
      </div>
    </main>
  );
}
import type { ReactNode } from 'react';

interface EmptyStateProps {
  icon?: ReactNode;
  title: string;
  description?: string;
}

export function EmptyState({ icon, title, description }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-300 p-8 text-center dark:border-slate-700">
      {icon ? <div className="mb-3 text-slate-400 dark:text-slate-500">{icon}</div> : null}
      <p className="font-medium text-slate-900 dark:text-slate-100">{title}</p>
      {description ? <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">{description}</p> : null}
    </div>
  );
}
import type { ReactNode } from 'react';
import { CircleAlert, CircleCheck, Info, TriangleAlert } from 'lucide-react';
import { cn } from '../../utils/cn';

type Variant = 'info' | 'success' | 'warning' | 'error';

interface AlertProps {
  variant?: Variant;
  title?: string;
  children?: ReactNode;
  className?: string;
}

const styles: Record<Variant, { container: string; icon: ReactNode }> = {
  info: {
    container: 'border-sky-200 bg-sky-50 text-sky-900 dark:border-sky-900 dark:bg-sky-950 dark:text-sky-100',
    icon: <Info className="h-4 w-4" aria-hidden="true" />,
  },
  success: {
    container:
      'border-emerald-200 bg-emerald-50 text-emerald-900 dark:border-emerald-900 dark:bg-emerald-950 dark:text-emerald-100',
    icon: <CircleCheck className="h-4 w-4" aria-hidden="true" />,
  },
  warning: {
    container:
      'border-amber-200 bg-amber-50 text-amber-900 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-100',
    icon: <TriangleAlert className="h-4 w-4" aria-hidden="true" />,
  },
  error: {
    container: 'border-rose-200 bg-rose-50 text-rose-900 dark:border-rose-900 dark:bg-rose-950 dark:text-rose-100',
    icon: <CircleAlert className="h-4 w-4" aria-hidden="true" />,
  },
};

export function Alert({ variant = 'info', title, children, className }: AlertProps) {
  const style = styles[variant];

  return (
    <div
      role="alert"
      className={cn('flex gap-3 rounded-xl border p-4 text-sm', style.container, className)}
    >
      <div className="mt-0.5 shrink-0">{style.icon}</div>
      <div className="space-y-1">
        {title ? <p className="font-semibold">{title}</p> : null}
        {children ? <div>{children}</div> : null}
      </div>
    </div>
  );
}
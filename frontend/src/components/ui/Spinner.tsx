import { Loader2 } from 'lucide-react';
import { cn } from '../../utils/cn';

interface SpinnerProps {
  label?: string;
  className?: string;
}

export function Spinner({ label, className }: SpinnerProps) {
  return (
    <div className={cn('flex items-center gap-2 text-slate-600 dark:text-slate-300', className)}>
      <Loader2 className="h-5 w-5 animate-spin" aria-hidden="true" />
      {label ? <span className="text-sm">{label}</span> : null}
    </div>
  );
}
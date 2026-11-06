import type { InputHTMLAttributes } from 'react';
import { cn } from '../../utils/cn';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  invalid?: boolean;
}

export function Input({ className, invalid = false, ...props }: InputProps) {
  return (
    <input
      className={cn(
        'h-10 w-full rounded-lg border bg-white px-3 py-2 text-sm text-slate-900 shadow-sm transition',
        'placeholder:text-slate-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-600',
        'disabled:pointer-events-none disabled:opacity-60 dark:bg-slate-900 dark:text-slate-100',
        invalid
          ? 'border-rose-500 focus-visible:ring-rose-500'
          : 'border-slate-300 dark:border-slate-700',
        className,
      )}
      aria-invalid={invalid}
      {...props}
    />
  );
}
import type { SelectHTMLAttributes } from 'react';
import { cn } from '../../utils/cn';

export interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  options: SelectOption[];
  invalid?: boolean;
  placeholder?: string;
}

export function Select({
  options,
  invalid = false,
  placeholder,
  className,
  ...props
}: SelectProps) {
  return (
    <select
      className={cn(
        'h-10 w-full rounded-lg border bg-white px-3 py-2 text-sm text-slate-900 shadow-sm transition',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-600',
        'disabled:pointer-events-none disabled:opacity-60 dark:bg-slate-900 dark:text-slate-100',
        invalid
          ? 'border-rose-500 focus-visible:ring-rose-500'
          : 'border-slate-300 dark:border-slate-700',
        className,
      )}
      aria-invalid={invalid}
      {...props}
    >
      {placeholder ? <option value="">{placeholder}</option> : null}

      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  );
}
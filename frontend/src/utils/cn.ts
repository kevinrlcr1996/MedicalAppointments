export type ClassValue = string | number | null | undefined | false;

export function cn(...inputs: (string | false | null | undefined)[]): string {
  return inputs.filter(Boolean).join(' ');
}
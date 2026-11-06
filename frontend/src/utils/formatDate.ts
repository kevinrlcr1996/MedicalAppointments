const dateFormatter = new Intl.DateTimeFormat('es-CR', {
  dateStyle: 'medium',
  timeStyle: 'medium',
  hour12: true,
  timeZone: 'America/Costa_Rica',
});

export function formatDateTime(value: string | Date | null | undefined): string {
  if (!value) {
    return 'Fecha no disponible';
  }

  const date = value instanceof Date ? value : new Date(value);

  if (Number.isNaN(date.getTime())) {
    return 'Fecha inválida';
  }

  return dateFormatter.format(date);
}

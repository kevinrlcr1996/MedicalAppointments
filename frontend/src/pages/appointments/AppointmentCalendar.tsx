import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { CalendarDays, ChevronLeft, ChevronRight } from 'lucide-react';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { Modal } from '../../components/ui/Modal';
import { Spinner } from '../../components/ui/Spinner';
import { appointmentsApi, type Appointment } from '../../services/api/appointments';
import { ApiError } from '../../services/api/apiClient';
import { formatDateTime } from '../../utils/formatDate';

const dayFormatter = new Intl.DateTimeFormat('es-CR', { weekday: 'short' });
const monthFormatter = new Intl.DateTimeFormat('es-CR', { month: 'long', year: 'numeric' });
const timeFormatter = new Intl.DateTimeFormat('es-CR', {
  hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'America/Costa_Rica',
});

function addMonths(value: string, delta: number): string {
  const [year, month] = value.split('-').map(Number);
  const date = new Date(year, month - 1 + delta, 1);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
}

function localDateKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function monthAppointments(month: string, status?: string): Promise<Appointment[]> {
  const [year, monthNumber] = month.split('-').map(Number);
  // Add two UTC days on both sides because LocalDate belongs to each clinic's timezone.
  const fromUtc = new Date(Date.UTC(year, monthNumber - 1, 1) - 2 * 24 * 60 * 60 * 1000).toISOString();
  const toUtc = new Date(Date.UTC(year, monthNumber, 1) + 2 * 24 * 60 * 60 * 1000).toISOString();

  return appointmentsApi.list({ page: 1, pageSize: 100, fromUtc, toUtc, status }).then(async (firstPage) => {
    const rest = await Promise.all(Array.from({ length: Math.max(0, firstPage.totalPages - 1) }, (_, index) =>
      appointmentsApi.list({ page: index + 2, pageSize: 100, fromUtc, toUtc, status }),
    ));
    return [firstPage, ...rest].flatMap((result) => result.items)
      .filter((appointment) => appointment.localDate.startsWith(month));
  });
}

function statusVariant(status: Appointment['status']): 'neutral' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === 'Completed' || status === 'Confirmed') return 'success';
  if (status === 'Cancelled' || status === 'NoShow') return 'danger';
  if (status === 'CheckedIn' || status === 'InProgress') return 'info';
  if (status === 'Rescheduled') return 'neutral';
  return 'warning';
}

export function AppointmentCalendar({ status, enabled, refreshKey }: { status: string; enabled: boolean; refreshKey: number }) {
  const [month, setMonth] = useState(() => {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  });
  const [selected, setSelected] = useState<Appointment | null>(null);
  const query = useQuery({
    queryKey: ['appointments', 'calendar', month, status, refreshKey],
    queryFn: () => monthAppointments(month, status || undefined),
    enabled,
  });

  const [year, monthNumber] = month.split('-').map(Number);
  const firstDay = new Date(year, monthNumber - 1, 1);
  const mondayOffset = (firstDay.getDay() + 6) % 7;
  const gridStart = new Date(year, monthNumber - 1, 1 - mondayOffset);
  const days = Array.from({ length: 42 }, (_, index) => new Date(gridStart.getFullYear(), gridStart.getMonth(), gridStart.getDate() + index));
  const byLocalDate = new Map<string, Appointment[]>();
  for (const appointment of query.data ?? []) {
    const events = byLocalDate.get(appointment.localDate) ?? [];
    events.push(appointment);
    byLocalDate.set(appointment.localDate, events);
  }
  for (const events of byLocalDate.values()) events.sort((a, b) => a.startAtUtc.localeCompare(b.startAtUtc));

  const weekdayLabels = Array.from({ length: 7 }, (_, index) => {
    const date = new Date(2024, 0, 1 + index); // 2024-01-01 was a Monday.
    return dayFormatter.format(date);
  });

  return (
    <>
      <Card>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <CalendarDays className="h-5 w-5 text-sky-600" />
            <h2 className="font-semibold capitalize text-slate-900 dark:text-slate-50">{monthFormatter.format(firstDay)}</h2>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="secondary" size="sm" onClick={() => setMonth(addMonths(month, -1))} aria-label="Mes anterior" leftIcon={<ChevronLeft className="h-4 w-4" />}>Anterior</Button>
            <Button variant="secondary" size="sm" onClick={() => { const now = new Date(); setMonth(`${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`); }}>Hoy</Button>
            <Button variant="secondary" size="sm" onClick={() => setMonth(addMonths(month, 1))} aria-label="Mes siguiente">Siguiente <ChevronRight className="h-4 w-4" /></Button>
          </div>
        </div>

        {query.isError ? <Alert variant="error" title="No se pudo cargar el calendario">{query.error instanceof ApiError ? query.error.message : 'Error inesperado.'}</Alert> : null}
        {query.isLoading ? <Spinner label="Cargando citas del mes..." /> : null}
        {query.data ? (
          <div className="overflow-x-auto">
            <div className="min-w-[760px]">
              <div className="grid grid-cols-7 border-b border-slate-200 dark:border-slate-800">
                {weekdayLabels.map((label) => <div key={label} className="px-2 py-2 text-center text-xs font-semibold uppercase text-slate-500">{label}</div>)}
              </div>
              <div className="grid grid-cols-7">
                {days.map((date) => {
                  const localDate = localDateKey(date);
                  const appointments = byLocalDate.get(localDate) ?? [];
                  const inMonth = date.getMonth() === monthNumber - 1;
                  return (
                    <div key={localDate} className={`min-h-32 border-b border-r border-slate-200 p-1.5 dark:border-slate-800 ${inMonth ? 'bg-white dark:bg-slate-900' : 'bg-slate-50 text-slate-400 dark:bg-slate-950'}`}>
                      <div className={`mb-1 px-1 text-xs font-semibold ${localDate === localDateKey(new Date()) ? 'text-sky-700 dark:text-sky-300' : ''}`}>{date.getDate()}</div>
                      <div className="space-y-1">
                        {appointments.slice(0, 3).map((appointment) => (
                          <button key={appointment.id} type="button" onClick={() => setSelected(appointment)} className="block w-full rounded-md border border-slate-200 bg-slate-50 px-1.5 py-1 text-left text-xs hover:border-sky-400 hover:bg-sky-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-sky-600 dark:border-slate-700 dark:bg-slate-800 dark:hover:bg-slate-700">
                            <span className="block truncate font-semibold text-slate-800 dark:text-slate-100">{timeFormatter.format(new Date(appointment.startAtUtc))} · {appointment.patientFullName}</span>
                            <span className="block truncate text-slate-500 dark:text-slate-400">{appointment.doctorFullName}</span>
                          </button>
                        ))}
                        {appointments.length > 3 ? <p className="px-1 text-xs text-sky-700 dark:text-sky-300">+{appointments.length - 3} citas más</p> : null}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        ) : null}
        <p className="mt-3 text-xs text-slate-500 dark:text-slate-400">Las horas se muestran en la zona horaria de Costa Rica. Selecciona una cita para ver sus datos.</p>
      </Card>

      <Modal open={Boolean(selected)} title="Detalle de cita" onClose={() => setSelected(null)}>
        {selected ? (
          <div className="space-y-3 text-sm">
            <div><span className="text-slate-500">Paciente</span><p className="font-medium text-slate-900 dark:text-slate-100">{selected.patientFullName}</p></div>
            <div><span className="text-slate-500">Médico</span><p className="font-medium text-slate-900 dark:text-slate-100">{selected.doctorFullName}</p></div>
            <div><span className="text-slate-500">Clínica / consultorio</span><p className="font-medium text-slate-900 dark:text-slate-100">{selected.clinicName}{selected.roomName ? ` · ${selected.roomName}` : ''}</p></div>
            <div><span className="text-slate-500">Fecha y hora</span><p className="font-medium text-slate-900 dark:text-slate-100">{formatDateTime(selected.startAtUtc)} – {timeFormatter.format(new Date(selected.endAtUtc))}</p></div>
            <div><span className="text-slate-500">Estado</span><p className="mt-1"><Badge variant={statusVariant(selected.status)}>{selected.status}</Badge></p></div>
            <div className="flex justify-end"><Button variant="secondary" onClick={() => setSelected(null)}>Cerrar</Button></div>
          </div>
        ) : null}
      </Modal>
    </>
  );
}

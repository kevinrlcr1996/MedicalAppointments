import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Ban,
  CalendarCheck,
  CalendarDays,
  CalendarClock,
  CheckCircle2,
  ClipboardList,
  Clock,
  FileText,
  HeartPulse,
  LogIn,
  List,
  PlayCircle,
  RefreshCcw,
  Trash2,
  UserX,
} from 'lucide-react';
import { appointmentsApi, type Appointment } from '../../services/api/appointments';
import { patientsApi } from '../../services/api/patients';
import { ApiError } from '../../services/api/apiClient';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, hasAnyPermission, Permissions } from '../../lib/permissions';
import { useAppointmentRealtime } from '../../hooks/useAppointmentRealtime';
import { useVitalSignsRealtime } from '../../hooks/useVitalSignsRealtime';
import { useDiagnosisRealtime } from '../../hooks/useDiagnosisRealtime'; // 8d.2
import { formatDateTime } from '../../utils/formatDate';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pagination } from '../../components/ui/Pagination';
import { Spinner } from '../../components/ui/Spinner';
import { Textarea } from '../../components/ui/Textarea';
import { PatientOnboardingForm } from './PatientOnboardingForm';
import { ClinicalNotesDialog } from './ClinicalNotesDialog';
import { VitalSignsDialog } from './VitalSignsDialog';
import { DiagnosisDialog } from './DiagnosisDialog'; // 8d.2
import { AppointmentCalendar } from './AppointmentCalendar';

export function AppointmentsPage() {
  useAppointmentRealtime();
  useVitalSignsRealtime();
  useDiagnosisRealtime(); // 8d.2

  const queryClient = useQueryClient();
  const { user } = useAuth();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [status, setStatus] = useState('');
  const [viewMode, setViewMode] = useState<'list' | 'calendar'>('list');
  const [calendarRefreshKey, setCalendarRefreshKey] = useState(0);

  const [cancelTarget, setCancelTarget] = useState<Appointment | null>(null);
  const [rescheduleTarget, setRescheduleTarget] = useState<Appointment | null>(null);
  const [notesTarget, setNotesTarget] = useState<Appointment | null>(null);
  const [vitalsTarget, setVitalsTarget] = useState<Appointment | null>(null);
  const [diagnosisTarget, setDiagnosisTarget] = useState<Appointment | null>(null); // 8d.2
  const [cancelIdemKey, setCancelIdemKey] = useState('');
  const [cancelReason, setCancelReason] = useState('');
  const [rescheduleDate, setRescheduleDate] = useState('');
  const [rescheduleStart, setRescheduleStart] = useState('');
  const [rescheduleEnd, setRescheduleEnd] = useState('');
  const [rescheduleReason, setRescheduleReason] = useState('');
  const [rescheduleIdemKey, setRescheduleIdemKey] = useState('');

  const canRead = hasAnyPermission(user, [
    Permissions.AppointmentsReadAll,
    Permissions.AppointmentsReadAssigned,
    Permissions.AppointmentsReadOwn,
  ]);

  const canReschedule = hasAnyPermission(user, [
    Permissions.AppointmentsRescheduleAll,
    Permissions.AppointmentsRescheduleAssigned,
    Permissions.AppointmentsRescheduleOwn,
  ]);

  const canCancel = hasAnyPermission(user, [
    Permissions.AppointmentsCancelAll,
    Permissions.AppointmentsCancelAssigned,
    Permissions.AppointmentsCancelOwn,
  ]);

  const canReadNotes = hasPermission(user, Permissions.ClinicalRecordsRead);

  const isPatientOnly =
    hasPermission(user, Permissions.AppointmentsReadOwn) &&
    !hasPermission(user, Permissions.AppointmentsReadAll) &&
    !hasPermission(user, Permissions.AppointmentsReadAssigned);

  const patientQuery = useQuery({
    queryKey: ['patients-me'],
    queryFn: patientsApi.getMe,
    enabled: isPatientOnly,
    retry: false,
  });

  const rescheduleMutation = useMutation({
    mutationFn: () => appointmentsApi.reschedule(
      rescheduleTarget!.id,
      {
        newLocalDate: rescheduleDate,
        newStartAtUtc: new Date(`${rescheduleDate}T${rescheduleStart}`).toISOString(),
        newEndAtUtc: new Date(`${rescheduleDate}T${rescheduleEnd}`).toISOString(),
        reason: rescheduleReason.trim() || null,
      },
      rescheduleIdemKey,
    ),
    onSuccess: async () => {
      setRescheduleTarget(null);
      setRescheduleReason('');
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
      await queryClient.invalidateQueries({ queryKey: ['waiting-list'] });
    },
  });

  const needsOnboarding =
    isPatientOnly && patientQuery.isError && (patientQuery.error as ApiError)?.status === 404;

  const appointmentsQuery = useQuery({
    queryKey: ['appointments', { page, pageSize, status }],
    queryFn: () => appointmentsApi.list({ page, pageSize, status: status || undefined }),
    enabled: viewMode === 'list' && canRead && !needsOnboarding && (!isPatientOnly || Boolean(patientQuery.data)),
  });

  const actionMutation = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: string }) => {
      switch (action) {
        case 'confirm': return appointmentsApi.confirm(id);
        case 'check-in': return appointmentsApi.checkIn(id);
        case 'start': return appointmentsApi.start(id);
        case 'complete': return appointmentsApi.complete(id);
        case 'no-show': return appointmentsApi.markNoShow(id);
        default: throw new Error('Unknown action');
      }

    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });

  const cancelMutation = useMutation({
    mutationFn: () => appointmentsApi.cancel(cancelTarget!.id, cancelReason.trim(), cancelIdemKey),
    onSuccess: async () => {
      setCancelTarget(null);
      setCancelReason('');
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
      await queryClient.invalidateQueries({ queryKey: ['waiting-list'] });
    },
  });

  function openCancel(appointment: Appointment) {
    setCancelTarget(appointment);
    setCancelIdemKey(crypto.randomUUID());
    setCancelReason('');
    cancelMutation.reset();
  }

  function openReschedule(appointment: Appointment) {
    const start = new Date(appointment.startAtUtc);
    const end = new Date(appointment.endAtUtc);
    const localDate = appointment.localDate || start.toISOString().slice(0, 10);
    const localTime = (value: Date) => value.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });

    setRescheduleTarget(appointment);
    setRescheduleDate(localDate);
    setRescheduleStart(localTime(start));
    setRescheduleEnd(localTime(end));
    setRescheduleReason('');
    setRescheduleIdemKey(crypto.randomUUID());
    rescheduleMutation.reset();
  }

  if (isPatientOnly && patientQuery.isLoading) {
    return <Spinner label="Cargando tu perfil..." />;
  }

  if (needsOnboarding) {
    return (
      <div className="space-y-6">
        <PageHeader title="Citas" description="Primero completa tu ficha de paciente." />
        <PatientOnboardingForm onDone={() => void patientQuery.refetch()} />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Citas"
        description="Gestión de citas médicas."
        actions={
          <div className="flex flex-wrap gap-2">
            <Button variant={viewMode === 'list' ? 'primary' : 'secondary'} onClick={() => setViewMode('list')} leftIcon={<List className="h-4 w-4" />}>Lista</Button>
            <Button variant={viewMode === 'calendar' ? 'primary' : 'secondary'} onClick={() => setViewMode('calendar')} leftIcon={<CalendarDays className="h-4 w-4" />}>Calendario</Button>
            <Button variant="secondary" onClick={() => { if (viewMode === 'list') void appointmentsQuery.refetch(); else setCalendarRefreshKey((current) => current + 1); }} leftIcon={<RefreshCcw className="h-4 w-4" />}>Recargar</Button>
          </div>
        }
      />

      <Card>
        <div className="flex flex-wrap items-end gap-3">
          <label className="text-sm font-medium text-slate-800 dark:text-slate-200" htmlFor="status-filter">
            Estado
          </label>
          <select
            id="status-filter"
            value={status}
            onChange={(event) => { setStatus(event.target.value); setPage(1); }}
            className="h-10 rounded-lg border border-slate-300 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-900"
          >
            <option value="">Todos</option>
            <option value="Pending">Pending</option>
            <option value="Confirmed">Confirmed</option>
            <option value="CheckedIn">CheckedIn</option>
            <option value="InProgress">InProgress</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
            <option value="NoShow">NoShow</option>
            <option value="Rescheduled">Rescheduled</option>
          </select>
        </div>
      </Card>

      {viewMode === 'list' && appointmentsQuery.isError ? (
        <Alert variant="error" title="Error al cargar citas">
          {appointmentsQuery.error instanceof ApiError ? appointmentsQuery.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}

      {rescheduleMutation.isError ? (
        <Alert variant="error" title="Error al reprogramar">
          {rescheduleMutation.error instanceof ApiError ? rescheduleMutation.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}

      {actionMutation.isError ? (
        <Alert variant="error" title="Error en la acción">
          {actionMutation.error instanceof ApiError ? actionMutation.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}

      {viewMode === 'calendar' ? (
        <AppointmentCalendar
          status={status}
          enabled={canRead && !needsOnboarding && (!isPatientOnly || Boolean(patientQuery.data))}
          refreshKey={calendarRefreshKey}
        />
      ) : null}

      {viewMode === 'list' ? <Card>
        <DataTable
          loading={appointmentsQuery.isLoading}
          rows={appointmentsQuery.data?.items ?? []}
          columns={[
            { key: 'patient', header: 'Paciente', render: (row: Appointment) => row.patientFullName },
            { key: 'doctor', header: 'Médico', render: (row: Appointment) => row.doctorFullName },
            { key: 'clinicName', header: 'Clínica' },
            { key: 'time', header: 'Hora', render: (row: Appointment) => formatDateTime(row.startAtUtc) },
            { key: 'status', header: 'Estado', render: (row: Appointment) => <AppointmentStatusBadge status={row.status} /> },
            {
              key: 'actions',
              header: 'Acciones',
              render: (row: Appointment) => (
                <div className="flex flex-wrap gap-2">
                  {row.status === 'Pending' && hasPermission(user, Permissions.AppointmentsCreateAll) ? (
                    <Button size="sm" variant="secondary" onClick={() => actionMutation.mutate({ id: row.id, action: 'confirm' })} leftIcon={<CheckCircle2 className="h-4 w-4" />}>
                      Confirmar
                    </Button>
                  ) : null}

                  {row.status === 'Confirmed' && hasPermission(user, Permissions.AppointmentsCheckIn) ? (
                    <Button size="sm" variant="secondary" onClick={() => actionMutation.mutate({ id: row.id, action: 'check-in' })} leftIcon={<LogIn className="h-4 w-4" />}>
                      Check-in
                    </Button>
                  ) : null}

                  {row.status === 'CheckedIn' && hasPermission(user, Permissions.AppointmentsCheckIn) ? (
                    <Button size="sm" variant="secondary" onClick={() => actionMutation.mutate({ id: row.id, action: 'start' })} leftIcon={<PlayCircle className="h-4 w-4" />}>
                      Iniciar
                    </Button>
                  ) : null}

                  {row.status === 'InProgress' && hasPermission(user, Permissions.AppointmentsComplete) ? (
                    <Button size="sm" variant="secondary" onClick={() => actionMutation.mutate({ id: row.id, action: 'complete' })} leftIcon={<CalendarCheck className="h-4 w-4" />}>
                      Completar
                    </Button>
                  ) : null}

                  {(row.status === 'Confirmed' || row.status === 'CheckedIn') && hasPermission(user, Permissions.AppointmentsMarkNoShow) ? (
                    <Button size="sm" variant="danger" onClick={() => actionMutation.mutate({ id: row.id, action: 'no-show' })} leftIcon={<UserX className="h-4 w-4" />}>
                      No-show
                    </Button>
                  ) : null}

                  {canCancel && (row.status === 'Pending' || row.status === 'Confirmed') ? (
                    <Button size="sm" variant="danger" onClick={() => openCancel(row)} leftIcon={<Trash2 className="h-4 w-4" />}>
                      Cancelar
                    </Button>
                  ) : null}

                  {canReschedule && (row.status === 'Pending' || row.status === 'Confirmed') ? (
                    <Button size="sm" variant="secondary" onClick={() => openReschedule(row)} leftIcon={<CalendarClock className="h-4 w-4" />}>
                      Reprogramar
                    </Button>
                  ) : null}

                  {canReadNotes && (row.status === 'InProgress' || row.status === 'Completed') ? (
                    <Button size="sm" variant="secondary" onClick={() => setNotesTarget(row)} leftIcon={<FileText className="h-4 w-4" />}>
                      Notas clínicas
                    </Button>
                  ) : null}

                  {canReadNotes && (row.status === 'InProgress' || row.status === 'Completed') ? (
                    <Button size="sm" variant="secondary" onClick={() => setVitalsTarget(row)} leftIcon={<HeartPulse className="h-4 w-4" />}>
                      Signos vitales
                    </Button>
                  ) : null}

                  {canReadNotes && (row.status === 'InProgress' || row.status === 'Completed') ? (
                    <Button size="sm" variant="secondary" onClick={() => setDiagnosisTarget(row)} leftIcon={<ClipboardList className="h-4 w-4" />}>
                      Diagnósticos
                    </Button>
                  ) : null}
                </div>
              ),
            },
          ]}
        />

        {appointmentsQuery.data ? (
          <Pagination
            page={appointmentsQuery.data.page}
            pageSize={appointmentsQuery.data.pageSize}
            totalCount={appointmentsQuery.data.totalCount}
            onPageChange={setPage}
            onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
          />
        ) : null}
      </Card> : null}

      <Modal
        open={Boolean(cancelTarget)}
        title="Cancelar cita"
        onClose={() => setCancelTarget(null)}
      >
        <div className="space-y-4">
          {cancelTarget ? (
            <p className="text-sm text-slate-600 dark:text-slate-400">
              {cancelTarget.doctorFullName} · {formatDateTime(cancelTarget.startAtUtc)}
            </p>
          ) : null}

          <label className="block text-sm font-medium text-slate-800 dark:text-slate-200" htmlFor="cancel-reason">
            Motivo <span className="text-rose-600">*</span>
          </label>
          <Textarea
            id="cancel-reason"
            value={cancelReason}
            onChange={(event) => setCancelReason(event.target.value)}
            maxLength={500}
            placeholder="Motivo de la cancelación"
          />

          {cancelMutation.isError ? (
            <Alert variant="error" title="No se pudo cancelar">
              {cancelMutation.error instanceof ApiError ? cancelMutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setCancelTarget(null)}>Cerrar</Button>
            <Button
              variant="danger"
              loading={cancelMutation.isPending}
              disabled={cancelReason.trim().length === 0}
              onClick={() => cancelMutation.mutate()}
              leftIcon={<Ban className="h-4 w-4" />}
            >
              Confirmar cancelación
            </Button>
          </div>
        </div>
      </Modal>
      <Modal
        open={Boolean(rescheduleTarget)}
        title="Reprogramar cita"
        onClose={() => setRescheduleTarget(null)}
      >
        <div className="space-y-4">
          {rescheduleTarget ? (
            <p className="text-sm text-slate-600 dark:text-slate-400">
              {rescheduleTarget.patientFullName} · {rescheduleTarget.doctorFullName}
            </p>
          ) : null}

          <div className="grid gap-4 sm:grid-cols-3">
            <label className="text-sm font-medium text-slate-800 dark:text-slate-200">
              Fecha
              <Input type="date" value={rescheduleDate} onChange={(event) => setRescheduleDate(event.target.value)} />
            </label>
            <label className="text-sm font-medium text-slate-800 dark:text-slate-200">
              Inicio
              <Input type="time" value={rescheduleStart} onChange={(event) => setRescheduleStart(event.target.value)} />
            </label>
            <label className="text-sm font-medium text-slate-800 dark:text-slate-200">
              Fin
              <Input type="time" value={rescheduleEnd} onChange={(event) => setRescheduleEnd(event.target.value)} />
            </label>
          </div>

          <label className="block text-sm font-medium text-slate-800 dark:text-slate-200">
            Motivo
            <Textarea value={rescheduleReason} onChange={(event) => setRescheduleReason(event.target.value)} maxLength={500} placeholder="Motivo de la reprogramación (opcional)" />
          </label>

          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setRescheduleTarget(null)}>Cerrar</Button>
            <Button
              loading={rescheduleMutation.isPending}
              disabled={!rescheduleDate || !rescheduleStart || !rescheduleEnd || rescheduleStart >= rescheduleEnd}
              onClick={() => rescheduleMutation.mutate()}
              leftIcon={<CalendarClock className="h-4 w-4" />}
            >
              Confirmar reprogramación
            </Button>
          </div>
        </div>
      </Modal>
      <ClinicalNotesDialog
        open={Boolean(notesTarget)}
        appointmentId={notesTarget?.id ?? null}
        onClose={() => setNotesTarget(null)}
      />
      <VitalSignsDialog
        open={Boolean(vitalsTarget)}
        appointmentId={vitalsTarget?.id ?? null}
        onClose={() => setVitalsTarget(null)}
      />
      <DiagnosisDialog
        open={Boolean(diagnosisTarget)}
        appointmentId={diagnosisTarget?.id ?? null}
        onClose={() => setDiagnosisTarget(null)}
      />
    </div>
  );
}

function AppointmentStatusBadge({ status }: { status: Appointment['status'] }) {
  const variant =
    status === 'Completed' ? 'success'
      : status === 'Cancelled' || status === 'NoShow' ? 'danger'
        : status === 'InProgress' || status === 'CheckedIn' ? 'info'
          : status === 'Confirmed' ? 'success'
            : 'warning';

  const icon =
    status === 'Pending' ? <Clock className="h-3 w-3" />
      : status === 'Cancelled' ? <Ban className="h-3 w-3" />
        : null;

  return <Badge variant={variant}>{icon}{status}</Badge>;
}

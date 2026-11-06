import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Ban,
  CalendarCheck,
  CheckCircle2,
  Clock,
  LogIn,
  PlayCircle,
  RefreshCcw,
  UserX,
} from 'lucide-react';
import { appointmentsApi, type Appointment } from '../../services/api/appointments';
import { patientsApi } from '../../services/api/patients';
import { ApiError } from '../../services/api/apiClient';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, hasAnyPermission, Permissions } from '../../lib/permissions';
import { useAppointmentRealtime } from '../../hooks/useAppointmentRealtime';
import { formatDateTime } from '../../utils/formatDate';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pagination } from '../../components/ui/Pagination';
import { Spinner } from '../../components/ui/Spinner';
import { PatientOnboardingForm } from './PatientOnboardingForm';

export function AppointmentsPage() {
  useAppointmentRealtime();

  const queryClient = useQueryClient();
  const { user } = useAuth();

  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [status, setStatus] = useState('');

  const canRead = hasAnyPermission(user, [
    Permissions.AppointmentsReadAll,
    Permissions.AppointmentsReadAssigned,
    Permissions.AppointmentsReadOwn,
  ]);

  // Paciente puro = lee lo suyo pero no tiene alcance global/asignado.
  // Solo este perfil necesita ficha de paciente para ver algo.
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

  const needsOnboarding =
    isPatientOnly && patientQuery.isError && (patientQuery.error as ApiError)?.status === 404;

  const appointmentsQuery = useQuery({
    queryKey: ['appointments', { page, pageSize, status }],
    queryFn: () =>
      appointmentsApi.list({
        page,
        pageSize,
        status: status || undefined,
      }),
    // No llamar al backend si el paciente aún no tiene ficha (evita la 403/[] y muestra onboarding).
    enabled: canRead && !needsOnboarding && (!isPatientOnly || Boolean(patientQuery.data)),
  });

  const actionMutation = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: string }) => {
      switch (action) {
        case 'confirm':
          return appointmentsApi.confirm(id);
        case 'check-in':
          return appointmentsApi.checkIn(id);
        case 'start':
          return appointmentsApi.start(id);
        case 'complete':
          return appointmentsApi.complete(id);
        case 'no-show':
          return appointmentsApi.markNoShow(id);
        default:
          throw new Error('Unknown action');
      }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });

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
          <Button
            variant="secondary"
            onClick={() => void appointmentsQuery.refetch()}
            leftIcon={<RefreshCcw className="h-4 w-4" />}
          >
            Recargar
          </Button>
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
            onChange={(event) => {
              setStatus(event.target.value);
              setPage(1);
            }}
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

      {appointmentsQuery.isError ? (
        <Alert variant="error" title="Error al cargar citas">
          {appointmentsQuery.error instanceof ApiError ? appointmentsQuery.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}

      {actionMutation.isError ? (
        <Alert variant="error" title="Error en la acción">
          {actionMutation.error instanceof ApiError ? actionMutation.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}

      <Card>
        <DataTable
          loading={appointmentsQuery.isLoading}
          rows={appointmentsQuery.data?.items ?? []}
          columns={[
            { key: 'patient', header: 'Paciente', render: (row: Appointment) => row.patientFullName },
            { key: 'doctor', header: 'Médico', render: (row: Appointment) => row.doctorFullName },
            { key: 'clinicName', header: 'Clínica' },
            { key: 'time', header: 'Hora', render: (row: Appointment) => formatDateTime(row.startAtUtc) },
            {
              key: 'status',
              header: 'Estado',
              render: (row: Appointment) => <AppointmentStatusBadge status={row.status} />,
            },
            {
              key: 'actions',
              header: 'Acciones',
              render: (row: Appointment) => (
                <div className="flex flex-wrap gap-2">
                  {row.status === 'Pending' && hasPermission(user, Permissions.AppointmentsCreateAll) ? (
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => actionMutation.mutate({ id: row.id, action: 'confirm' })}
                      leftIcon={<CheckCircle2 className="h-4 w-4" />}
                    >
                      Confirmar
                    </Button>
                  ) : null}

                  {row.status === 'Confirmed' && hasPermission(user, Permissions.AppointmentsCheckIn) ? (
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => actionMutation.mutate({ id: row.id, action: 'check-in' })}
                      leftIcon={<LogIn className="h-4 w-4" />}
                    >
                      Check-in
                    </Button>
                  ) : null}

                  {row.status === 'CheckedIn' && hasPermission(user, Permissions.AppointmentsCheckIn) ? (
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => actionMutation.mutate({ id: row.id, action: 'start' })}
                      leftIcon={<PlayCircle className="h-4 w-4" />}
                    >
                      Iniciar
                    </Button>
                  ) : null}

                  {row.status === 'InProgress' && hasPermission(user, Permissions.AppointmentsComplete) ? (
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => actionMutation.mutate({ id: row.id, action: 'complete' })}
                      leftIcon={<CalendarCheck className="h-4 w-4" />}
                    >
                      Completar
                    </Button>
                  ) : null}

                  {(row.status === 'Confirmed' || row.status === 'CheckedIn') &&
                  hasPermission(user, Permissions.AppointmentsMarkNoShow) ? (
                    <Button
                      size="sm"
                      variant="danger"
                      onClick={() => actionMutation.mutate({ id: row.id, action: 'no-show' })}
                      leftIcon={<UserX className="h-4 w-4" />}
                    >
                      No-show
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
            onPageSizeChange={(size) => {
              setPageSize(size);
              setPage(1);
            }}
          />
        ) : null}
      </Card>
    </div>
  );
}

function AppointmentStatusBadge({ status }: { status: Appointment['status'] }) {
  const variant =
    status === 'Completed'
      ? 'success'
      : status === 'Cancelled' || status === 'NoShow'
        ? 'danger'
        : status === 'InProgress' || status === 'CheckedIn'
          ? 'info'
          : status === 'Confirmed'
            ? 'success'
            : 'warning';

  const icon =
    status === 'Pending' ? <Clock className="h-3 w-3" /> : status === 'Cancelled' ? <Ban className="h-3 w-3" /> : null;

  return (
    <Badge variant={variant}>
      {icon}
      {status}
    </Badge>
  );
}
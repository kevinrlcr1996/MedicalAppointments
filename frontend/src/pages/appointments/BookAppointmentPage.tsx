import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { CalendarPlus, Hourglass, RefreshCcw } from 'lucide-react';
import { crudApi } from '../../services/api/crudApi';
import { schedulingApi } from '../../services/api/scheduling';
import { appointmentsApi } from '../../services/api/appointments';
import { patientsApi } from '../../services/api/patients';
import { waitingListApi } from '../../services/api/waitingList';
import { apiClient, ApiError } from '../../services/api/apiClient';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { EmptyState } from '../../components/ui/EmptyState';
import { FormField } from '../../components/ui/FormField';
import { Input } from '../../components/ui/Input';
import { PageHeader } from '../../components/ui/PageHeader';
import { Select } from '../../components/ui/Select';
import { Spinner } from '../../components/ui/Spinner';
import { Textarea } from '../../components/ui/Textarea';
import { PatientOnboardingForm } from './PatientOnboardingForm';

interface DoctorClinicRef {
  clinicId: string;
  clinicName: string;
  isPrimary: boolean;
}

interface DoctorOption {
  id: string;
  firstName: string;
  lastName: string;
  clinics?: DoctorClinicRef[];
}

interface PatientOption {
  id: string;
  firstName: string;
  lastName: string;
  identificationNumber?: string | null;
  email?: string | null;
}

const bookingSchema = z.object({
  patientId: z.string().optional().or(z.literal('')),
  doctorId: z.string().min(1, 'Médico obligatorio.'),
  clinicId: z.string().min(1, 'Clínica obligatoria.'),
  date: z.string().min(1, 'Fecha obligatoria.'),
  startAtUtc: z.string().min(1, 'Selecciona un horario.'),
  reason: z.string().trim().max(500).optional().or(z.literal('')),
  notes: z.string().trim().max(2000).optional().or(z.literal('')),
});

type BookingFormValues = z.infer<typeof bookingSchema>;

export function BookAppointmentPage() {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const isPatient = hasPermission(user, Permissions.AppointmentsCreateOwn);
  const isStaff =
    hasPermission(user, Permissions.AppointmentsCreateAll) ||
    hasPermission(user, Permissions.AppointmentsCreateAssigned);

  // FIX gate: el onboarding de paciente SOLO aplica al rol Patient puro.
  // SuperAdmin/Admin/Receptionist tienen isPatient=true por bypass pero son staff:
  // deben ver el buscador de paciente, no el formulario de ficha.
  const isPatientOnly = isPatient && !isStaff;

  const [selectedSlotKey, setSelectedSlotKey] = useState<string>('');
  const [patientLabel, setPatientLabel] = useState('');

  const patientQuery = useQuery({
    queryKey: ['patients-me'],
    queryFn: patientsApi.getMe,
    enabled: isPatientOnly, // FIX: no disparar /patients/me para staff (evita el 404 que activaba el onboarding)
    retry: false,
  });

  // FIX 404: lista de médicos por endpoint paginado (existe desde Fase 3).
  const doctorsQuery = useQuery({
    queryKey: ['doctors-options'],
    queryFn: () => crudApi.list<DoctorOption>('/api/v1/doctors', { page: 1, pageSize: 100, status: 'Active' }),
  });

  const form = useForm<BookingFormValues>({
    resolver: zodResolver(bookingSchema),
    defaultValues: {
      patientId: '',
      doctorId: '',
      clinicId: '',
      date: new Date().toISOString().slice(0, 10),
      startAtUtc: '',
      reason: '',
      notes: '',
    },
  });

  const doctorId = form.watch('doctorId');
  const clinicId = form.watch('clinicId');
  const date = form.watch('date');
  const patientIdValue = form.watch('patientId');

  // FIX 404: clínicas del médico elegido vía GetById (incluye Clinics desde Fase 3).
  const selectedDoctorQuery = useQuery({
    queryKey: ['doctor-detail', doctorId],
    queryFn: () => apiClient.get<DoctorOption>(`/api/v1/doctors/${doctorId}`),
    enabled: Boolean(doctorId),
  });

  const selectedDoctor = selectedDoctorQuery.data ?? null;

  const clinicOptions = useMemo(
    () =>
      (selectedDoctor?.clinics ?? []).map((clinic) => ({
        value: clinic.clinicId,
        label: clinic.isPrimary ? `${clinic.clinicName} (principal)` : clinic.clinicName,
      })),
    [selectedDoctor],
  );

  const availabilityQuery = useQuery({
    queryKey: ['availability', doctorId, clinicId, date],
    queryFn: () => schedulingApi.getAvailability(doctorId, clinicId, date),
    enabled: Boolean(doctorId && clinicId && date),
  });

  useEffect(() => {
    if (patientQuery.data) {
      form.setValue('patientId', patientQuery.data.id);
      setPatientLabel(`${patientQuery.data.firstName} ${patientQuery.data.lastName}`);
    }
  }, [patientQuery.data, form]);

  useEffect(() => {
    form.setValue('clinicId', '');
    form.setValue('startAtUtc', '');
    setSelectedSlotKey('');
  }, [doctorId, form]);

  useEffect(() => {
    form.setValue('startAtUtc', '');
    setSelectedSlotKey('');
  }, [clinicId, date, form]);

  const selectedSlot = useMemo(() => {
    if (!availabilityQuery.data || !selectedSlotKey) {
      return null;
    }
    return availabilityQuery.data.slots.find((slot) => slot.startAtUtc === selectedSlotKey) ?? null;
  }, [availabilityQuery.data, selectedSlotKey]);

  const [idempotencyKey, setIdempotencyKey] = useState(() => crypto.randomUUID());

  const mutation = useMutation({
    mutationFn: async (values: BookingFormValues) => {
      if (isStaff && !values.patientId) {
        throw new Error('Selecciona un paciente.');
      }
      if (!selectedSlot) {
        throw new Error('Selecciona un horario disponible.');
      }
      return appointmentsApi.create(
        {
          patientId: values.patientId ?? '',
          doctorId: values.doctorId,
          clinicId: values.clinicId,
          roomId: null,
          localDate: values.date,
          startAtUtc: selectedSlot.startAtUtc,
          endAtUtc: selectedSlot.endAtUtc,
          reason: values.reason || null,
          notes: values.notes || null,
        },
        idempotencyKey,
      );
    },
    onSuccess: () => {
      setIdempotencyKey(crypto.randomUUID());
      form.reset({
        patientId: patientQuery.data?.id ?? '',
        doctorId: '',
        clinicId: '',
        date: new Date().toISOString().slice(0, 10),
        startAtUtc: '',
        reason: '',
        notes: '',
      });
      setSelectedSlotKey('');
      if (!isPatientOnly) {
        setPatientLabel('');
      }
    },
  });

  const joinListMutation = useMutation({
    mutationFn: () => {
      const from = new Date();
      const to = new Date(Date.now() + 14 * 24 * 60 * 60 * 1000);
      return waitingListApi.createEntry({
        patientId: patientQuery.data!.id,
        doctorId,
        clinicId,
        preferredFromUtc: from.toISOString(),
        preferredToUtc: to.toISOString(),
        priority: 'Normal',
        note: null,
      });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['waiting-list'] });
    },
  });

  // FIX gate: onboarding solo para Patient puro.
  const needsOnboarding =
    isPatientOnly && patientQuery.isError && (patientQuery.error as ApiError)?.status === 404;

  if (isPatientOnly && patientQuery.isLoading) {
    return <Spinner label="Cargando tu perfil..." />;
  }

  if (needsOnboarding) {
    return (
      <div className="space-y-6">
        <PageHeader title="Reservar cita" description="Primero completa tu ficha de paciente." />
        <PatientOnboardingForm onDone={() => void patientQuery.refetch()} />
      </div>
    );
  }

  if (isPatientOnly && patientQuery.isError) {
    return (
      <div className="space-y-6">
        <PageHeader title="Reservar cita" description="No se pudo cargar tu perfil." />
        <Alert variant="error" title="Error de perfil">
          {patientQuery.error instanceof ApiError ? patientQuery.error.message : 'Error inesperado.'}
        </Alert>
      </div>
    );
  }

  if (doctorsQuery.isLoading) {
    return <Spinner label="Cargando médicos..." />;
  }

  const doctorOptions = (doctorsQuery.data?.items ?? []).map((doctor) => ({
    value: doctor.id,
    label: `${doctor.firstName} ${doctor.lastName}`,
  }));

  return (
    <div className="space-y-6">
      <PageHeader title="Reservar cita" description="La disponibilidad siempre la calcula el backend." />

      <Card>
        <form className="space-y-6" onSubmit={form.handleSubmit((values) => mutation.mutate(values))}>
          {isStaff ? (
            <FormField
              label="Paciente"
              htmlFor="patient-search"
              required
              error={isStaff && !patientIdValue ? 'Selecciona un paciente.' : undefined}
            >
              <PatientPicker
                selectedLabel={patientLabel}
                onPick={(id, label) => {
                  form.setValue('patientId', id, { shouldValidate: true });
                  setPatientLabel(label);
                }}
                onClear={() => {
                  form.setValue('patientId', '', { shouldValidate: true });
                  setPatientLabel('');
                }}
              />
            </FormField>
          ) : (
            <Input type="hidden" {...form.register('patientId')} />
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Médico" htmlFor="doctorId" required error={form.formState.errors.doctorId?.message}>
              <Select id="doctorId" options={doctorOptions} {...form.register('doctorId')} />
            </FormField>

            <FormField label="Clínica" htmlFor="clinicId" required error={form.formState.errors.clinicId?.message}>
              <Select
                id="clinicId"
                disabled={!selectedDoctor}
                options={[
                  { value: '', label: selectedDoctor ? 'Seleccionar...' : 'Elige un médico primero' },
                  ...clinicOptions,
                ]}
                {...form.register('clinicId')}
              />
            </FormField>

            <FormField label="Fecha" htmlFor="date" required>
              <Input id="date" type="date" {...form.register('date')} />
            </FormField>
          </div>

          <div>
            <div className="mb-3 flex items-center justify-between">
              <p className="text-sm font-semibold text-slate-900 dark:text-slate-50">Horarios disponibles</p>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => void availabilityQuery.refetch()}
                disabled={!doctorId || !clinicId || !date}
                leftIcon={<RefreshCcw className="h-4 w-4" />}
              >
                Recargar
              </Button>
            </div>

            {!doctorId || !clinicId ? (
              <EmptyState title="Selecciona médico y clínica" description="Los horarios dependen de ambos." />
            ) : availabilityQuery.isFetching ? (
              <Spinner label="Calculando disponibilidad..." />
            ) : !availabilityQuery.data || availabilityQuery.data.slots.length === 0 ? (
              <div className="space-y-3">
                <EmptyState title="Sin horarios" description="No hay disponibilidad para esa fecha." />
                {isPatientOnly && patientQuery.data ? (
                  <Button
                    type="button"
                    variant="secondary"
                    loading={joinListMutation.isPending}
                    disabled={joinListMutation.isPending || joinListMutation.isSuccess}
                    onClick={() => joinListMutation.mutate()}
                    leftIcon={<Hourglass className="h-4 w-4" />}
                  >
                    {joinListMutation.isSuccess ? 'Apuntado a la lista' : 'Apuntarme a la lista de espera'}
                  </Button>
                ) : null}

                {joinListMutation.isSuccess ? (
                  <Alert variant="success" title="Estás en la lista de espera">
                    Te avisaremos cuando se libere un hueco compatible. Revísalo en <strong>Lista de espera</strong>.
                  </Alert>
                ) : null}

                {joinListMutation.isError ? (
                  <Alert variant="error" title="No pudimos apuntarte">
                    {joinListMutation.error instanceof ApiError
                      ? `${joinListMutation.error.status}: ${joinListMutation.error.message}`
                      : joinListMutation.error instanceof Error
                        ? joinListMutation.error.message
                        : 'Error inesperado.'}
                  </Alert>
                ) : null}
              </div>
            ) : (
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
                {availabilityQuery.data.slots.map((slot) => (
                  <button
                    key={slot.startAtUtc}
                    type="button"
                    onClick={() => {
                      setSelectedSlotKey(slot.startAtUtc);
                      form.setValue('startAtUtc', slot.startAtUtc);
                    }}
                    className={
                      selectedSlotKey === slot.startAtUtc
                        ? 'rounded-xl border-2 border-sky-600 bg-sky-50 p-4 text-left dark:bg-sky-950'
                        : 'rounded-xl border border-slate-200 bg-white p-4 text-left hover:border-sky-300 dark:border-slate-800 dark:bg-slate-900'
                    }
                  >
                    <p className="font-semibold text-slate-900 dark:text-slate-50">
                      {slot.startLocal} – {slot.endLocal}
                    </p>
                    <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{slot.durationMinutes} min</p>
                  </button>
                ))}
              </div>
            )}
          </div>

          <FormField label="Motivo" htmlFor="reason">
            <Textarea id="reason" {...form.register('reason')} maxLength={500} />
          </FormField>

          <FormField label="Notas" htmlFor="notes">
            <Textarea id="notes" {...form.register('notes')} maxLength={2000} />
          </FormField>

          {mutation.isError ? (
            <Alert variant="error" title="No se pudo crear la cita">
              {mutation.error instanceof ApiError ? mutation.error.message : 'Error inesperado.'}
            </Alert>
          ) : null}

          {mutation.isSuccess ? (
            <Alert variant="success" title="Cita creada">
              Cita creada correctamente.
            </Alert>
          ) : null}

          <Button
            type="submit"
            loading={mutation.isPending}
            disabled={
              !selectedSlot ||
              (isPatientOnly && !patientQuery.data) ||
              (isStaff && !patientIdValue)
            }
            leftIcon={<CalendarPlus className="h-4 w-4" />}
          >
            Reservar cita
          </Button>
        </form>
      </Card>
    </div>
  );
}

function PatientPicker({
  selectedLabel,
  onPick,
  onClear,
}: {
  selectedLabel: string;
  onPick: (id: string, label: string) => void;
  onClear: () => void;
}) {
  const [search, setSearch] = useState('');

  const query = useQuery({
    queryKey: ['patients-search', search],
    queryFn: () => crudApi.list<PatientOption>('/api/v1/patients', { page: 1, pageSize: 20, search }),
    enabled: search.trim().length >= 2,
  });

  const results = query.data?.items ?? [];

  return (
    <div className="space-y-2">
      {selectedLabel ? (
        <div className="flex items-center justify-between rounded-lg bg-sky-50 px-3 py-2 text-sm dark:bg-sky-950">
          <span className="font-medium text-sky-800 dark:text-sky-200">{selectedLabel}</span>
          <button type="button" onClick={onClear} className="text-xs text-sky-700 underline dark:text-sky-300">
            cambiar
          </button>
        </div>
      ) : (
        <input
          id="patient-search"
          type="text"
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Buscar paciente por nombre, identificación o teléfono…"
          className="h-10 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm dark:border-slate-700 dark:bg-slate-900"
        />
      )}

      {!selectedLabel && search.trim().length >= 2 ? (
        <div className="max-h-60 overflow-auto rounded-xl border border-slate-200 dark:border-slate-800">
          {query.isFetching ? (
            <p className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">Buscando…</p>
          ) : results.length === 0 ? (
            <p className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">Sin resultados.</p>
          ) : (
            results.map((patient) => (
              <button
                key={patient.id}
                type="button"
                onClick={() => {
                  onPick(patient.id, `${patient.firstName} ${patient.lastName}`);
                  setSearch('');
                }}
                className="block w-full px-3 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-800"
              >
                <span className="font-medium text-slate-900 dark:text-slate-50">
                  {patient.firstName} {patient.lastName}
                </span>
                {patient.identificationNumber ? (
                  <span className="ml-2 text-xs text-slate-500 dark:text-slate-400">
                    · {patient.identificationNumber}
                  </span>
                ) : null}
                {patient.email ? (
                  <span className="ml-2 text-xs text-slate-400 dark:text-slate-500">· {patient.email}</span>
                ) : null}
              </button>
            ))
          )}
        </div>
      ) : null}
    </div>
  );
}
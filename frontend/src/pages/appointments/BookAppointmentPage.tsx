import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { CalendarPlus, RefreshCcw } from 'lucide-react';
import { crudApi } from '../../services/api/crudApi';
import { schedulingApi } from '../../services/api/scheduling';
import { appointmentsApi } from '../../services/api/appointments';
import { patientsApi } from '../../services/api/patients';
import { ApiError } from '../../services/api/apiClient';
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
  clinics: DoctorClinicRef[];
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

  const isPatient = hasPermission(user, Permissions.AppointmentsCreateOwn);
  const isStaff =
    hasPermission(user, Permissions.AppointmentsCreateAll) ||
    hasPermission(user, Permissions.AppointmentsCreateAssigned);

  const [selectedSlotKey, setSelectedSlotKey] = useState<string>('');

  const patientQuery = useQuery({
    queryKey: ['patients-me'],
    queryFn: patientsApi.getMe,
    enabled: isPatient,
    retry: false,
  });

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

  const selectedDoctor = useMemo(
    () => doctorsQuery.data?.items.find((doctor) => doctor.id === doctorId) ?? null,
    [doctorsQuery.data, doctorId],
  );

  // Clínicas DERIVADAS del médico. Sin /clinics global, sin clinics.read.
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
    }
  }, [patientQuery.data, form]);

  // Al cambiar médico, la clínica y el slot anteriores dejan de valer.
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
    },
  });

  const needsOnboarding =
    isPatient && patientQuery.isError && (patientQuery.error as ApiError)?.status === 404;

  if (isPatient && patientQuery.isLoading) {
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

  if (isPatient && patientQuery.isError) {
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
            <FormField label="Paciente (PatientId)" htmlFor="patientId" required>
              <Input id="patientId" {...form.register('patientId')} placeholder="Guid del paciente" />
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
              <EmptyState title="Sin horarios" description="No hay disponibilidad para esa fecha." />
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
            disabled={!selectedSlot || (isPatient && !patientQuery.data)}
            leftIcon={<CalendarPlus className="h-4 w-4" />}
          >
            Reservar cita
          </Button>
        </form>
      </Card>
    </div>
  );
}
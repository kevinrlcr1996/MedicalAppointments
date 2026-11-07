import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { CalendarClock, RefreshCcw } from 'lucide-react';
import { crudApi } from '../../services/api/crudApi';
import { schedulingApi } from '../../services/api/scheduling';
import { apiClient, ApiError } from '../../services/api/apiClient';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { useSchedulingRealtime } from '../../hooks/useSchedulingRealtime';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { EmptyState } from '../../components/ui/EmptyState';
import { FormField } from '../../components/ui/FormField';
import { Input } from '../../components/ui/Input';
import { PageHeader } from '../../components/ui/PageHeader';
import { Select } from '../../components/ui/Select';
import { Spinner } from '../../components/ui/Spinner';

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

export function AvailabilityPage() {
    useSchedulingRealtime();

    const { user } = useAuth();

    const [doctorId, setDoctorId] = useState('');
    const [clinicId, setClinicId] = useState('');
    const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));

    // FIX 404: lista de médicos por endpoint paginado.
    const doctorsQuery = useQuery({
        queryKey: ['doctors-options'],
        queryFn: () => crudApi.list<DoctorOption>('/api/v1/doctors', { page: 1, pageSize: 100, status: 'Active' }),
        enabled: hasPermission(user, Permissions.DoctorsRead),
    });

    // FIX 404: clínicas del médico elegido vía GetById.
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
        enabled: hasPermission(user, Permissions.AvailabilityRead) && Boolean(doctorId && clinicId && date),
    });

    function handleDoctorChange(value: string) {
        setDoctorId(value);
        setClinicId('');
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
            <PageHeader
                title="Disponibilidad"
                description="Consulta calculada por el backend. Nunca confiar en cálculo frontend."
            />

            <Card>
                <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                    <FormField label="Médico" htmlFor="availability-doctor" required>
                        <Select
                            id="availability-doctor"
                            value={doctorId}
                            onChange={(event) => handleDoctorChange(event.target.value)}
                            options={[{ value: '', label: 'Seleccionar...' }, ...doctorOptions]}
                        />
                    </FormField>

                    <FormField label="Clínica" htmlFor="availability-clinic" required>
                        <Select
                            id="availability-clinic"
                            value={clinicId}
                            onChange={(event) => setClinicId(event.target.value)}
                            disabled={!selectedDoctor}
                            options={[
                                { value: '', label: selectedDoctor ? 'Seleccionar...' : 'Elige un médico primero' },
                                ...clinicOptions,
                            ]}
                        />
                    </FormField>

                    <FormField label="Fecha" htmlFor="availability-date" required>
                        <Input id="availability-date" type="date" value={date} onChange={(event) => setDate(event.target.value)} />
                    </FormField>

                    <div className="flex items-end">
                        <Button
                            variant="secondary"
                            onClick={() => void availabilityQuery.refetch()}
                            disabled={!doctorId || !clinicId || !date}
                            leftIcon={<RefreshCcw className="h-4 w-4" />}
                        >
                            Recargar
                        </Button>
                    </div>
                </div>
            </Card>

            {availabilityQuery.isError ? (
                <Alert variant="error" title="Error al calcular disponibilidad">
                    {availabilityQuery.error instanceof ApiError ? availabilityQuery.error.message : 'Error inesperado.'}
                </Alert>
            ) : null}

            <Card>
                {!doctorId || !clinicId ? (
                    <EmptyState
                        icon={<CalendarClock className="h-8 w-8" />}
                        title="Selecciona médico y clínica"
                        description="Las clínicas disponibles dependen del médico elegido."
                    />
                ) : availabilityQuery.isFetching ? (
                    <Spinner label="Calculando disponibilidad..." />
                ) : !availabilityQuery.data || availabilityQuery.data.slots.length === 0 ? (
                    <EmptyState
                        icon={<CalendarClock className="h-8 w-8" />}
                        title="Sin disponibilidad"
                        description="No hay slots disponibles para esa fecha, médico y clínica."
                    />
                ) : (
                    <div className="space-y-4">
                        <div className="flex flex-wrap items-center gap-2">
                            <Badge variant="info">{availabilityQuery.data.timeZoneId}</Badge>
                            <Badge variant="neutral">{availabilityQuery.data.slots.length} slots</Badge>
                        </div>

                        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
                            {availabilityQuery.data.slots.map((slot) => (
                                <div
                                    key={`${slot.startAtUtc}-${slot.endAtUtc}`}
                                    className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900"
                                >
                                    <p className="text-sm font-semibold text-slate-900 dark:text-slate-50">
                                        {slot.startLocal} – {slot.endLocal}
                                    </p>
                                    <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{slot.durationMinutes} min</p>
                                    <p className="mt-2 break-all text-xs text-slate-500 dark:text-slate-400">
                                        UTC: {new Date(slot.startAtUtc).toISOString()}
                                    </p>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </Card>
        </div>
    );
}
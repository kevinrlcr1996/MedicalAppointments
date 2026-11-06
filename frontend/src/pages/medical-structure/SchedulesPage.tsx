import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { Pencil, Plus, RefreshCcw, Save } from 'lucide-react';
import { ApiError } from '../../services/api/apiClient';
import { crudApi } from '../../services/api/crudApi';
import { schedulingApi, type Schedule } from '../../services/api/scheduling';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { useSchedulingRealtime } from '../../hooks/useSchedulingRealtime';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { FormField } from '../../components/ui/FormField';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pagination } from '../../components/ui/Pagination';
import { Select } from '../../components/ui/Select';
import { Spinner } from '../../components/ui/Spinner';
import { Switch } from '../../components/ui/Switch';

interface DoctorOption {
    id: string;
    firstName: string;
    lastName: string;
}

interface ClinicOption {
    id: string;
    name: string;
}

interface DayEditor {
    dayOfWeek: number;
    label: string;
    enabled: boolean;
    startTime: string;
    endTime: string;
}

const dayLabels = [
    'Sunday',
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
];

function getDayIndex(dayOfWeek: number | string): number | undefined {
    if (typeof dayOfWeek === 'number') {
        return dayOfWeek >= 0 && dayOfWeek < dayLabels.length ? dayOfWeek : undefined;
    }

    const index = dayLabels.findIndex((label) => label.toLowerCase() === dayOfWeek.toLowerCase());

    return index >= 0 ? index : undefined;
}

const scheduleSchema = z.object({
    doctorId: z.string().min(1, 'Médico obligatorio.'),
    clinicId: z.string().min(1, 'Clínica obligatoria.'),
    name: z.string().trim().min(1, 'Nombre obligatorio.').max(200),
    effectiveFrom: z.string().min(1, 'Fecha inicio obligatoria.'),
    effectiveTo: z.union([z.literal(''), z.string()]).optional(),
    slotDurationMinutes: z.coerce.number().int().min(5).max(240),
    bufferMinutes: z.coerce.number().int().min(0).max(120),
    isDefault: z.boolean(),
    isActive: z.boolean(),
});

type ScheduleFormInput = z.input<typeof scheduleSchema>;
type ScheduleFormValues = z.output<typeof scheduleSchema>;

function createDefaultDays(): DayEditor[] {
    return dayLabels.map((label, index) => ({
        dayOfWeek: index,
        label,
        enabled: index >= 1 && index <= 5,
        startTime: '09:00',
        endTime: '17:00',
    }));
}

export function SchedulesPage() {
    useSchedulingRealtime();

    const { user } = useAuth();
    const queryClient = useQueryClient();

    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [doctorId, setDoctorId] = useState('');
    const [clinicId, setClinicId] = useState('');
    const [isActive, setIsActive] = useState('');

    const [modalOpen, setModalOpen] = useState(false);
    const [editingSchedule, setEditingSchedule] = useState<Schedule | null>(null);
    const [days, setDays] = useState<DayEditor[]>(createDefaultDays);

    const canManage = hasPermission(user, Permissions.SchedulesManage);

    const queryParams = useMemo(
        () => ({
            page,
            pageSize,
            doctorId: doctorId || undefined,
            clinicId: clinicId || undefined,
            isActive: isActive || undefined,
        }),
        [page, pageSize, doctorId, clinicId, isActive],
    );

    const schedulesQuery = useQuery({
        queryKey: ['doctor-schedules', queryParams],
        queryFn: () => schedulingApi.listSchedules(queryParams),
        enabled: hasPermission(user, Permissions.SchedulesRead),
    });

    const doctorsQuery = useQuery({
        queryKey: ['doctors-options'],
        queryFn: () => crudApi.list<DoctorOption>('/api/v1/doctors', { page: 1, pageSize: 100, status: 'Active' }),
    });

    const clinicsQuery = useQuery({
        queryKey: ['clinics-options'],
        queryFn: () => crudApi.list<ClinicOption>('/api/v1/clinics', { page: 1, pageSize: 100, isActive: true }),
    });

    const form = useForm<ScheduleFormInput, unknown, ScheduleFormValues>({
        resolver: zodResolver(scheduleSchema),
        defaultValues: {
            doctorId: '',
            clinicId: '',
            name: '',
            effectiveFrom: new Date().toISOString().slice(0, 10),
            effectiveTo: '',
            slotDurationMinutes: 30,
            bufferMinutes: 0,
            isDefault: false,
            isActive: true,
        },
    });

    useEffect(() => {
        if (!modalOpen) {
            setDays(createDefaultDays());
            form.reset();
        }
    }, [modalOpen, form]);

    const openCreate = () => {
        setEditingSchedule(null);
        setDays(createDefaultDays());
        form.reset();
        setModalOpen(true);
    };

    const openEdit = (schedule: Schedule) => {
        setEditingSchedule(schedule);
        form.reset({
            doctorId: schedule.doctorId,
            clinicId: schedule.clinicId,
            name: schedule.name,
            effectiveFrom: schedule.effectiveFrom,
            effectiveTo: schedule.effectiveTo ?? '',
            slotDurationMinutes: schedule.slotDurationMinutes,
            bufferMinutes: schedule.bufferMinutes,
            isDefault: schedule.isDefault,
            isActive: schedule.isActive,
        });

        setDays(
            dayLabels.map((label, index) => {
                const existing = schedule.days.find((day) => getDayIndex(day.dayOfWeek) === index);

                return {
                    dayOfWeek: index,
                    label,
                    enabled: Boolean(existing),
                    startTime: existing?.startTime ?? '09:00',
                    endTime: existing?.endTime ?? '17:00',
                };
            }),
        );

        setModalOpen(true);
    };

    const saveMutation = useMutation({
        mutationFn: async (values: ScheduleFormValues) => {
            const payload = {
                ...values,
                effectiveTo: values.effectiveTo === '' ? null : values.effectiveTo,
                days: days
                    .filter((day) => day.enabled)
                    .map((day) => ({
                        dayOfWeek: day.dayOfWeek,
                        startTime: day.startTime,
                        endTime: day.endTime,
                    })),
            };

            if (editingSchedule) {
                return schedulingApi.updateSchedule(editingSchedule.id, payload);
            }

            return schedulingApi.createSchedule(payload);
        },
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ['doctor-schedules'] });
            await queryClient.invalidateQueries({ queryKey: ['availability'] });
            setModalOpen(false);
        },
    });

    if (doctorsQuery.isLoading || clinicsQuery.isLoading) {
        return <Spinner label="Cargando catálogos..." />;
    }

    const doctorOptions = (doctorsQuery.data?.items ?? []).map((doctor) => ({
        value: doctor.id,
        label: `${doctor.firstName} ${doctor.lastName}`,
    }));

    const clinicOptions = (clinicsQuery.data?.items ?? []).map((clinic) => ({
        value: clinic.id,
        label: clinic.name,
    }));

    const doctorMap = new Map(doctorOptions.map((x) => [x.value, x.label]));
    const clinicMap = new Map(clinicOptions.map((x) => [x.value, x.label]));

    return (
        <div className="space-y-6">
            <PageHeader
                title="Agendas médicas"
                description="Horarios semanales por médico y clínica."
                actions={
                    canManage ? (
                        <Button onClick={openCreate} leftIcon={<Plus className="h-4 w-4" />}>
                            Nueva agenda
                        </Button>
                    ) : null
                }
            />

            <Card>
                <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                    <FormField label="Médico" htmlFor="filter-doctor">
                        <Select
                            id="filter-doctor"
                            value={doctorId}
                            onChange={(event) => {
                                setDoctorId(event.target.value);
                                setPage(1);
                            }}
                            options={[{ value: '', label: 'Todos' }, ...doctorOptions]}
                        />
                    </FormField>

                    <FormField label="Clínica" htmlFor="filter-clinic">
                        <Select
                            id="filter-clinic"
                            value={clinicId}
                            onChange={(event) => {
                                setClinicId(event.target.value);
                                setPage(1);
                            }}
                            options={[{ value: '', label: 'Todas' }, ...clinicOptions]}
                        />
                    </FormField>

                    <FormField label="Estado" htmlFor="filter-active">
                        <Select
                            id="filter-active"
                            value={isActive}
                            onChange={(event) => {
                                setIsActive(event.target.value);
                                setPage(1);
                            }}
                            options={[
                                { value: '', label: 'Todos' },
                                { value: 'true', label: 'Activas' },
                                { value: 'false', label: 'Inactivas' },
                            ]}
                        />
                    </FormField>

                    <div className="flex items-end">
                        <Button
                            variant="ghost"
                            onClick={() => {
                                setDoctorId('');
                                setClinicId('');
                                setIsActive('');
                                setPage(1);
                            }}
                            leftIcon={<RefreshCcw className="h-4 w-4" />}
                        >
                            Limpiar
                        </Button>
                    </div>
                </div>
            </Card>

            {schedulesQuery.isError ? (
                <Alert variant="error" title="Error al cargar agendas">
                    {schedulesQuery.error instanceof ApiError ? schedulesQuery.error.message : 'Error inesperado.'}
                </Alert>
            ) : null}

            <Card>
                <DataTable
                    loading={schedulesQuery.isLoading}
                    rows={schedulesQuery.data?.items ?? []}
                    columns={[
                        { key: 'name', header: 'Nombre' },
                        {
                            key: 'doctor',
                            header: 'Médico',
                            render: (row: Schedule) => doctorMap.get(row.doctorId) ?? row.doctorId,
                        },
                        {
                            key: 'clinic',
                            header: 'Clínica',
                            render: (row: Schedule) => clinicMap.get(row.clinicId) ?? row.clinicId,
                        },
                        {
                            key: 'slot',
                            header: 'Slot',
                            render: (row: Schedule) => `${row.slotDurationMinutes} min`,
                        },
                        {
                            key: 'days',
                            header: 'Días',
                            render: (row: Schedule) => (
                                <div className="flex flex-wrap gap-1">
                                    {row.days.map((day) => {
                                        const dayIndex = getDayIndex(day.dayOfWeek);

                                        return (
                                            <Badge key={`${day.dayOfWeek}-${day.startTime}`} variant="neutral">
                                                {dayIndex === undefined ? '—' : dayLabels[dayIndex].slice(0, 3)}
                                            </Badge>
                                        );
                                    })}
                                </div>
                            ),
                        },
                        {
                            key: 'isActive',
                            header: 'Estado',
                            render: (row: Schedule) =>
                                row.isActive ? <Badge variant="success">Activa</Badge> : <Badge variant="danger">Inactiva</Badge>,
                        },
                        {
                            key: 'actions',
                            header: 'Acciones',
                            render: (row: Schedule) =>
                                canManage ? (
                                    <Button variant="secondary" size="sm" onClick={() => openEdit(row)} leftIcon={<Pencil className="h-4 w-4" />}>
                                        Editar
                                    </Button>
                                ) : null,
                        },
                    ]}
                />

                {schedulesQuery.data ? (
                    <Pagination
                        page={schedulesQuery.data.page}
                        pageSize={schedulesQuery.data.pageSize}
                        totalCount={schedulesQuery.data.totalCount}
                        onPageChange={setPage}
                        onPageSizeChange={(size) => {
                            setPageSize(size);
                            setPage(1);
                        }}
                    />
                ) : null}
            </Card>

            <Modal
                open={modalOpen}
                title={editingSchedule ? 'Editar agenda' : 'Nueva agenda'}
                onClose={() => setModalOpen(false)}
                size="xl"
            >
                <form className="space-y-6" onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}>
                    <div className="grid gap-4 sm:grid-cols-2">
                        <FormField label="Médico" htmlFor="doctorId" required error={form.formState.errors.doctorId?.message}>
                            <Select id="doctorId" options={doctorOptions} {...form.register('doctorId')} />
                        </FormField>

                        <FormField label="Clínica" htmlFor="clinicId" required error={form.formState.errors.clinicId?.message}>
                            <Select id="clinicId" options={clinicOptions} {...form.register('clinicId')} />
                        </FormField>

                        <FormField label="Nombre" htmlFor="name" required error={form.formState.errors.name?.message}>
                            <Input id="name" {...form.register('name')} />
                        </FormField>

                        <FormField label="Vigente desde" htmlFor="effectiveFrom" required>
                            <Input id="effectiveFrom" type="date" {...form.register('effectiveFrom')} />
                        </FormField>

                        <FormField label="Vigente hasta" htmlFor="effectiveTo">
                            <Input id="effectiveTo" type="date" {...form.register('effectiveTo')} />
                        </FormField>

                        <FormField label="Duración slot (min)" htmlFor="slotDurationMinutes" required>
                            <Input id="slotDurationMinutes" type="number" {...form.register('slotDurationMinutes')} />
                        </FormField>

                        <FormField label="Buffer (min)" htmlFor="bufferMinutes">
                            <Input id="bufferMinutes" type="number" {...form.register('bufferMinutes')} />
                        </FormField>
                    </div>

                    <div className="grid gap-4 sm:grid-cols-2">
                        <div className="flex items-center justify-between rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                            <span className="text-sm font-medium text-slate-800 dark:text-slate-200">Agenda predeterminada</span>
                            <Switch
                                checked={Boolean(form.watch('isDefault'))}
                                onChange={(checked) => form.setValue('isDefault', checked)}
                                label="Agenda predeterminada"
                            />
                        </div>

                        <div className="flex items-center justify-between rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                            <span className="text-sm font-medium text-slate-800 dark:text-slate-200">Activa</span>
                            <Switch
                                checked={Boolean(form.watch('isActive'))}
                                onChange={(checked) => form.setValue('isActive', checked)}
                                label="Activa"
                            />
                        </div>
                    </div>

                    <div>
                        <p className="mb-3 text-sm font-semibold text-slate-900 dark:text-slate-50">Días semanales</p>

                        <div className="space-y-3">
                            {days.map((day, index) => (
                                <div
                                    key={day.dayOfWeek}
                                    className="grid gap-3 rounded-xl border border-slate-200 p-3 sm:grid-cols-[auto_1fr_1fr] dark:border-slate-800"
                                >
                                    <div className="flex items-center gap-3">
                                        <Switch
                                            checked={day.enabled}
                                            onChange={(checked) => {
                                                const next = [...days];
                                                next[index] = { ...day, enabled: checked };
                                                setDays(next);
                                            }}
                                            label={day.label}
                                        />
                                        <span className="text-sm font-medium text-slate-800 dark:text-slate-200">{day.label}</span>
                                    </div>

                                    <Input
                                        type="time"
                                        value={day.startTime}
                                        disabled={!day.enabled}
                                        onChange={(event) => {
                                            const next = [...days];
                                            next[index] = { ...day, startTime: event.target.value };
                                            setDays(next);
                                        }}
                                    />

                                    <Input
                                        type="time"
                                        value={day.endTime}
                                        disabled={!day.enabled}
                                        onChange={(event) => {
                                            const next = [...days];
                                            next[index] = { ...day, endTime: event.target.value };
                                            setDays(next);
                                        }}
                                    />
                                </div>
                            ))}
                        </div>
                    </div>

                    {saveMutation.isError ? (
                        <Alert variant="error" title="No se pudo guardar la agenda">
                            {saveMutation.error instanceof ApiError ? saveMutation.error.message : 'Error inesperado.'}
                        </Alert>
                    ) : null}

                    <div className="flex justify-end gap-3">
                        <Button variant="secondary" onClick={() => setModalOpen(false)}>
                            Cancelar
                        </Button>
                        <Button type="submit" loading={saveMutation.isPending} leftIcon={<Save className="h-4 w-4" />}>
                            Guardar
                        </Button>
                    </div>
                </form>
            </Modal>
        </div>
    );
}
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
    CheckCheck,
    DoorOpen,
    PhoneForwarded,
    RefreshCcw,
    SkipForward,
    Stethoscope,
    UserCheck,
} from 'lucide-react';
import { useMemo, useState } from 'react';
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
import { useAuth } from '../../contexts/AuthContext';
import { useWaitingRoomRealtime } from '../../hooks/useWaitingRoomRealtime';
import { hasAnyPermission, Permissions } from '../../lib/permissions';
import { apiClient, ApiError } from '../../services/api/apiClient';
import { appointmentsApi, type Appointment } from '../../services/api/appointments';
import { crudApi } from '../../services/api/crudApi';
import { waitingRoomApi, type WaitingRoomCard } from '../../services/api/waitingRoom';
import { formatDateTime } from '../../utils/formatDate';

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

interface RoomOption {
    id: string;
    name: string;
}

export function WaitingRoomBoardPage() {
    useWaitingRoomRealtime();
    const queryClient = useQueryClient();
    const { user } = useAuth();

    const [doctorId, setDoctorId] = useState('');
    const [clinicId, setClinicId] = useState('');
    const [roomId, setRoomId] = useState('');
    const [localDate, setLocalDate] = useState(() => new Date().toISOString().slice(0, 10));

    const canOperate = hasAnyPermission(user, [
        Permissions.WaitingRoomManage,
        Permissions.AppointmentsCheckIn,
        Permissions.AppointmentsComplete,
        Permissions.AppointmentsMarkNoShow,
    ]);

    // FIX 404: lista de médicos por endpoint paginado.
    const doctorsQuery = useQuery({
        queryKey: ['doctors-options'],
        queryFn: () => crudApi.list<DoctorOption>('/api/v1/doctors', { page: 1, pageSize: 100, status: 'Active' }),
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
            (selectedDoctor?.clinics ?? []).map((c) => ({
                value: c.clinicId,
                label: c.isPrimary ? `${c.clinicName} (principal)` : c.clinicName,
            })),
        [selectedDoctor],
    );

    const roomsQuery = useQuery({
        queryKey: ['rooms-options', clinicId],
        queryFn: () => crudApi.list<RoomOption>('/api/v1/rooms', { page: 1, pageSize: 100, clinicId, isActive: true }),
        enabled: Boolean(clinicId),
    });

    const roomOptions = useMemo(
        () => (roomsQuery.data?.items ?? []).map((r) => ({ value: r.id, label: r.name })),
        [roomsQuery.data],
    );

    const boardQuery = useQuery({
        queryKey: ['waiting-room', 'board', doctorId, clinicId, localDate],
        queryFn: () => waitingRoomApi.getBoard(doctorId, clinicId, localDate),
        enabled: Boolean(doctorId && clinicId && localDate),
    });

    const checkedInQuery = useQuery({
        queryKey: ['appointments', 'checkedin', doctorId, clinicId, localDate],
        queryFn: () => appointmentsApi.list({ page: 1, pageSize: 100, status: 'CheckedIn', doctorId, clinicId }),
        enabled: Boolean(doctorId && clinicId && localDate),
    });

    const inRoomAppointmentIds = useMemo(() => {
        const board = boardQuery.data;
        if (!board) {
            return new Set<string>();
        }
        const ids = [
            ...board.queued,
            ...board.called,
            ...board.inConsultation,
            ...board.skipped,
            ...board.completed,
        ].map((c) => c.appointmentId);
        return new Set(ids);
    }, [boardQuery.data]);

    // Filtro en cliente por médico/clínica/fecha para no asumir que /appointments filtra por doctorId/clinicId.
    const pendingCheckIn = useMemo(() => {
        const items = checkedInQuery.data?.items ?? [];
        return items.filter(
            (a: Appointment) =>
                a.localDate === localDate &&
                a.doctorId === doctorId &&
                a.clinicId === clinicId &&
                !inRoomAppointmentIds.has(a.id),
        );
    }, [checkedInQuery.data, localDate, doctorId, clinicId, inRoomAppointmentIds]);

    const invalidate = async () => {
        await queryClient.invalidateQueries({ queryKey: ['waiting-room'] });
        await queryClient.invalidateQueries({ queryKey: ['appointments'] });
    };

    const checkInMutation = useMutation({
        mutationFn: (appointmentId: string) => waitingRoomApi.checkInToRoom(appointmentId, null),
        onSuccess: invalidate,
    });
    const callNextMutation = useMutation({
        mutationFn: () => waitingRoomApi.callNext(doctorId, clinicId, localDate, roomId || null),
        onSuccess: invalidate,
    });
    const answerMutation = useMutation({ mutationFn: (id: string) => waitingRoomApi.answer(id), onSuccess: invalidate });
    const skipMutation = useMutation({
        mutationFn: (id: string) => waitingRoomApi.skip(id, 'skipped from board'),
        onSuccess: invalidate,
    });
    const recallMutation = useMutation({ mutationFn: (id: string) => waitingRoomApi.recall(id), onSuccess: invalidate });
    const completeMutation = useMutation({ mutationFn: (id: string) => waitingRoomApi.complete(id), onSuccess: invalidate });

    const error = firstError(
        boardQuery.error,
        checkedInQuery.error,
        checkInMutation.error,
        callNextMutation.error,
        answerMutation.error,
        skipMutation.error,
        recallMutation.error,
        completeMutation.error,
    );

    if (doctorsQuery.isLoading) {
        return <Spinner label="Cargando médicos..." />;
    }

    const doctorOptions = (doctorsQuery.data?.items ?? []).map((d) => ({
        value: d.id,
        label: `${d.firstName} ${d.lastName}`,
    }));

    return (
        <div className="space-y-6">
            <PageHeader
                title="Sala de espera"
                description="Tablero operativo en vivo. El estado de la cita lo mueve el workflow de Fase 5."
            />

            <Card>
                <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
                    <FormField label="Médico" htmlFor="wr-doctor" required>
                        <Select
                            id="wr-doctor"
                            value={doctorId}
                            onChange={(e) => {
                                setDoctorId(e.target.value);
                                setClinicId('');
                            }}
                            options={[{ value: '', label: 'Seleccionar...' }, ...doctorOptions]}
                        />
                    </FormField>
                    <FormField label="Clínica" htmlFor="wr-clinic" required>
                        <Select
                            id="wr-clinic"
                            value={clinicId}
                            onChange={(e) => setClinicId(e.target.value)}
                            disabled={!selectedDoctor}
                            options={[{ value: '', label: selectedDoctor ? 'Seleccionar...' : 'Elige médico' }, ...clinicOptions]}
                        />
                    </FormField>
                    <FormField label="Consultorio (al llamar)" htmlFor="wr-room">
                        <Select
                            id="wr-room"
                            value={roomId}
                            onChange={(e) => setRoomId(e.target.value)}
                            options={[{ value: '', label: 'Sin asignar' }, ...roomOptions]}
                        />
                    </FormField>
                    <FormField label="Fecha" htmlFor="wr-date" required>
                        <Input id="wr-date" type="date" value={localDate} onChange={(e) => setLocalDate(e.target.value)} />
                    </FormField>
                    <div className="flex items-end gap-2">
                        <Button
                            variant="primary"
                            disabled={!doctorId || !clinicId || !canOperate || callNextMutation.isPending}
                            onClick={() => callNextMutation.mutate()}
                            leftIcon={<PhoneForwarded className="h-4 w-4" />}
                        >
                            Llamar siguiente
                        </Button>
                        <Button variant="ghost" onClick={() => void boardQuery.refetch()} leftIcon={<RefreshCcw className="h-4 w-4" />}>
                            Recargar
                        </Button>
                    </div>
                </div>
            </Card>

            {error ? <Alert variant="error" title="Error">{error}</Alert> : null}

            {!doctorId || !clinicId ? (
                <Card>
                    <EmptyState
                        icon={<Stethoscope className="h-8 w-8" />}
                        title="Selecciona médico y clínica"
                        description="El tablero depende de ambos."
                    />
                </Card>
            ) : boardQuery.isFetching ? (
                <Spinner label="Cargando tablero..." />
            ) : boardQuery.data ? (
                <div className="grid gap-4 lg:grid-cols-3 xl:grid-cols-6">
                    <CheckInColumn
                        cards={pendingCheckIn}
                        canOperate={canOperate}
                        busy={checkInMutation.isPending}
                        onEnter={(appointmentId) => checkInMutation.mutate(appointmentId)}
                    />
                    <Column title="En espera" tone="warning" cards={boardQuery.data.queued} canOperate={false} />
                    <Column
                        title="Llamado"
                        tone="info"
                        cards={boardQuery.data.called}
                        canOperate={canOperate}
                        busy={answerMutation.isPending || skipMutation.isPending || recallMutation.isPending}
                        onAnswer={(id) => answerMutation.mutate(id)}
                        onSkip={(id) => skipMutation.mutate(id)}
                        onRecall={(id) => recallMutation.mutate(id)}
                    />
                    <Column
                        title="En consulta"
                        tone="success"
                        cards={boardQuery.data.inConsultation}
                        canOperate={canOperate}
                        busy={completeMutation.isPending}
                        onComplete={(id) => completeMutation.mutate(id)}
                    />
                    <Column
                        title="Saltados"
                        tone="danger"
                        cards={boardQuery.data.skipped}
                        canOperate={canOperate}
                        busy={recallMutation.isPending}
                        onRecall={(id) => recallMutation.mutate(id)}
                    />
                    <Column title="Completados" tone="neutral" cards={boardQuery.data.completed} canOperate={false} />
                </div>
            ) : null}
        </div>
    );
}

function CheckInColumn({
    cards,
    canOperate,
    busy,
    onEnter,
}: {
    cards: Appointment[];
    canOperate: boolean;
    busy: boolean;
    onEnter: (appointmentId: string) => void;
}) {
    return (
        <Card className="min-h-[12rem]">
            <div className="mb-3 flex items-center justify-between">
                <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-50">Check-in pendiente</h3>
                <Badge variant="neutral">{cards.length}</Badge>
            </div>
            {cards.length === 0 ? (
                <p className="py-6 text-center text-xs text-slate-500 dark:text-slate-400">
                    Sin citas checked-in fuera de sala
                </p>
            ) : (
                <div className="space-y-2">
                    {cards.map((a) => (
                        <div key={a.id} className="rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                            <p className="text-sm font-semibold text-slate-900 dark:text-slate-50">{a.patientFullName}</p>
                            <p className="text-xs text-slate-500 dark:text-slate-400">{formatDateTime(a.startAtUtc)}</p>
                            {canOperate ? (
                                <Button
                                    size="sm"
                                    variant="secondary"
                                    className="mt-2"
                                    disabled={busy}
                                    onClick={() => onEnter(a.id)}
                                    leftIcon={<DoorOpen className="h-3 w-3" />}
                                >
                                    Entrar en sala
                                </Button>
                            ) : null}
                        </div>
                    ))}
                </div>
            )}
        </Card>
    );
}

function Column({
    title,
    tone,
    cards,
    canOperate,
    busy,
    onAnswer,
    onSkip,
    onRecall,
    onComplete,
}: {
    title: string;
    tone: 'warning' | 'info' | 'success' | 'danger' | 'neutral';
    cards: WaitingRoomCard[];
    canOperate: boolean;
    busy?: boolean;
    onAnswer?: (id: string) => void;
    onSkip?: (id: string) => void;
    onRecall?: (id: string) => void;
    onComplete?: (id: string) => void;
}) {
    return (
        <Card className="min-h-[12rem]">
            <div className="mb-3 flex items-center justify-between">
                <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-50">{title}</h3>
                <Badge variant={tone}>{cards.length}</Badge>
            </div>
            {cards.length === 0 ? (
                <p className="py-6 text-center text-xs text-slate-500 dark:text-slate-400">Vacío</p>
            ) : (
                <div className="space-y-2">
                    {cards.map((card) => (
                        <div key={card.callId} className="rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                            <div className="flex items-center justify-between">
                                <span className="text-xs font-bold text-slate-500 dark:text-slate-400">#{card.sequenceNumber}</span>
                                {card.recallCount > 0 ? <Badge variant="neutral">recibido ×{card.recallCount}</Badge> : null}
                            </div>
                            <p className="mt-1 text-sm font-semibold text-slate-900 dark:text-slate-50">{card.patientFullName}</p>
                            <p className="text-xs text-slate-500 dark:text-slate-400">
                                {card.roomName ? <>{card.roomName} · </> : null}
                                {formatDateTime(card.appointmentStartAtUtc)}
                            </p>
                            {canOperate ? (
                                <div className="mt-2 flex flex-wrap gap-1">
                                    {onAnswer ? (
                                        <Button
                                            size="sm"
                                            variant="primary"
                                            disabled={busy}
                                            onClick={() => onAnswer(card.callId)}
                                            leftIcon={<UserCheck className="h-3 w-3" />}
                                        >
                                            Atender
                                        </Button>
                                    ) : null}
                                    {onSkip ? (
                                        <Button
                                            size="sm"
                                            variant="secondary"
                                            disabled={busy}
                                            onClick={() => onSkip(card.callId)}
                                            leftIcon={<SkipForward className="h-3 w-3" />}
                                        >
                                            Saltar
                                        </Button>
                                    ) : null}
                                    {onRecall ? (
                                        <Button
                                            size="sm"
                                            variant="secondary"
                                            disabled={busy}
                                            onClick={() => onRecall(card.callId)}
                                            leftIcon={<PhoneForwarded className="h-3 w-3" />}
                                        >
                                            Recordar
                                        </Button>
                                    ) : null}
                                    {onComplete ? (
                                        <Button
                                            size="sm"
                                            variant="primary"
                                            disabled={busy}
                                            onClick={() => onComplete(card.callId)}
                                            leftIcon={<CheckCheck className="h-3 w-3" />}
                                        >
                                            Completar
                                        </Button>
                                    ) : null}
                                </div>
                            ) : null}
                        </div>
                    ))}
                </div>
            )}
        </Card>
    );
}

function firstError(...errors: unknown[]): string | null {
    for (const e of errors) {
        if (e instanceof ApiError) return `${e.status}: ${e.message}`;
        if (e instanceof Error) return e.message;
    }
    return null;
}
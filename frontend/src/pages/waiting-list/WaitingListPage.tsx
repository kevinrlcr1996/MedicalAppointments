import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { CalendarClock, Check, Clock, Hourglass, RefreshCcw, Trash2, X } from 'lucide-react';
import { waitingListApi, type WaitingListEntry, type WaitingListOffer } from '../../services/api/waitingList';
import { ApiError } from '../../services/api/apiClient';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, hasAnyPermission, Permissions } from '../../lib/permissions';
import { useWaitingListRealtime } from '../../hooks/useWaitingListRealtime';
import { formatDateTime } from '../../utils/formatDate';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { EmptyState } from '../../components/ui/EmptyState';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pagination } from '../../components/ui/Pagination';
import { Spinner } from '../../components/ui/Spinner';

export function WaitingListPage() {
    useWaitingListRealtime();

    const queryClient = useQueryClient();
    const { user } = useAuth();

    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);

    const isPatientOnly =
        hasPermission(user, Permissions.WaitingListReadOwn) &&
        !hasPermission(user, Permissions.WaitingListReadAll) &&
        !hasPermission(user, Permissions.WaitingListReadAssigned);

    const canRead = hasAnyPermission(user, [
        Permissions.WaitingListReadAll,
        Permissions.WaitingListReadAssigned,
        Permissions.WaitingListReadOwn,
    ]);

    const entriesQuery = useQuery({
        queryKey: ['waiting-list', 'entries', { page, pageSize }],
        queryFn: () => waitingListApi.listEntries({ page, pageSize }),
        enabled: canRead,
    });

    const offersQuery = useQuery({
        queryKey: ['waiting-list', 'offers', { page, pageSize }],
        queryFn: () => waitingListApi.listOffers({ page, pageSize, status: 'Pending' }),
        enabled: canRead,
    });

    const cancelMutation = useMutation({
        mutationFn: (id: string) => waitingListApi.cancelEntry(id),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ['waiting-list'] });
        },
    });

    const respondMutation = useMutation({
        mutationFn: ({ id, accept }: { id: string; accept: boolean }) => waitingListApi.respondOffer(id, accept),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ['waiting-list'] });
            await queryClient.invalidateQueries({ queryKey: ['appointments'] });
        },
    });

    if (!canRead) {
        return <EmptyState title="Sin acceso" description="No tienes permisos para la lista de espera." />;
    }

    return (
        <div className="space-y-6">
            <PageHeader
                title={isPatientOnly ? 'Mi lista de espera' : 'Lista de espera'}
                description="Cuando se libera un hueco compatible, recibes una oferta con tiempo límite."
                actions={
                    <Button
                        variant="secondary"
                        onClick={() => {
                            void entriesQuery.refetch();
                            void offersQuery.refetch();
                        }}
                        leftIcon={<RefreshCcw className="h-4 w-4" />}
                    >
                        Recargar
                    </Button>
                }
            />

            {(entriesQuery.isError || offersQuery.isError || cancelMutation.isError || respondMutation.isError) ? (
                <Alert variant="error" title="Error">
                    {firstErrorMessage(entriesQuery.error, offersQuery.error, cancelMutation.error, respondMutation.error)}
                </Alert>
            ) : null}

            <Card>
                <h2 className="mb-3 flex items-center gap-2 text-base font-semibold text-slate-900 dark:text-slate-50">
                    <Hourglass className="h-4 w-4" /> Ofertas pendientes
                </h2>
                {offersQuery.isLoading ? (
                    <Spinner label="Cargando ofertas..." />
                ) : (offersQuery.data?.items.length ?? 0) === 0 ? (
                    <EmptyState icon={<CalendarClock className="h-8 w-8" />} title="Sin ofertas" description="No tienes ofertas activas." />
                ) : (
                    <div className="space-y-3">
                        {offersQuery.data!.items.map((offer) => (
                            <OfferCard
                                key={offer.id}
                                offer={offer}
                                onRespond={(accept) => respondMutation.mutate({ id: offer.id, accept })}
                                busy={respondMutation.isPending}
                            />
                        ))}
                    </div>
                )}
            </Card>

            <Card>
                <h2 className="mb-3 flex items-center gap-2 text-base font-semibold text-slate-900 dark:text-slate-50">
                    <Clock className="h-4 w-4" /> Entradas en espera
                </h2>
                <DataTable
                    loading={entriesQuery.isLoading}
                    rows={entriesQuery.data?.items ?? []}
                    columns={[
                        { key: 'doctorFullName', header: 'Médico' },
                        { key: 'clinicName', header: 'Clínica', render: (r: WaitingListEntry) => r.clinicName ?? 'Cualquiera' },
                        {
                            key: 'window',
                            header: 'Ventana',
                            render: (r: WaitingListEntry) => `${formatDateTime(r.preferredFromUtc)} → ${formatDateTime(r.preferredToUtc)}`,
                        },
                        { key: 'priority', header: 'Prioridad', render: (r: WaitingListEntry) => <Badge variant="info">{r.priority}</Badge> },
                        {
                            key: 'status',
                            header: 'Estado',
                            render: (r: WaitingListEntry) => <EntryStatusBadge status={r.status} />,
                        },
                        {
                            key: 'actions',
                            header: 'Acciones',
                            render: (r: WaitingListEntry) =>
                                r.status === 'Waiting' ? (
                                    <Button
                                        size="sm"
                                        variant="danger"
                                        onClick={() => cancelMutation.mutate(r.id)}
                                        leftIcon={<Trash2 className="h-4 w-4" />}
                                    >
                                        Cancelar
                                    </Button>
                                ) : null,
                        },
                    ]}
                />
                {entriesQuery.data ? (
                    <Pagination
                        page={entriesQuery.data.page}
                        pageSize={entriesQuery.data.pageSize}
                        totalCount={entriesQuery.data.totalCount}
                        onPageChange={setPage}
                        onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
                    />
                ) : null}
            </Card>
        </div>
    );
}

function OfferCard({
    offer,
    onRespond,
    busy,
}: {
    offer: WaitingListOffer;
    onRespond: (accept: boolean) => void;
    busy: boolean;
}) {
    const [remaining, setRemaining] = useState(() => msUntil(offer.expiresAt));

    // Countdown simple; el backend es la autoridad de expiración (worker).
    useState(() => {
        const timer = setInterval(() => setRemaining(msUntil(offer.expiresAt)), 1000);
        return () => clearInterval(timer);
    });

    const expired = remaining <= 0;

    return (
        <div className="flex flex-col gap-3 rounded-xl border border-slate-200 p-4 sm:flex-row sm:items-center sm:justify-between dark:border-slate-800">
            <div>
                <p className="font-semibold text-slate-900 dark:text-slate-50">
                    {offer.doctorFullName} · {offer.clinicName ?? 'Clínica'}
                </p>
                <p className="text-sm text-slate-600 dark:text-slate-400">{formatDateTime(offer.slotStartUtc)}</p>
                <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                    {expired ? 'Oferta expirada' : `Expira en ${formatCountdown(remaining)}`}
                </p>
            </div>
            {!expired ? (
                <div className="flex gap-2">
                    <Button size="sm" variant="primary" disabled={busy} onClick={() => onRespond(true)} leftIcon={<Check className="h-4 w-4" />}>
                        Aceptar
                    </Button>
                    <Button size="sm" variant="secondary" disabled={busy} onClick={() => onRespond(false)} leftIcon={<X className="h-4 w-4" />}>
                        Rechazar
                    </Button>
                </div>
            ) : (
                <Badge variant="neutral">Expirada</Badge>
            )}
        </div>
    );
}

function EntryStatusBadge({ status }: { status: WaitingListEntry['status'] }) {
    const variant = status === 'Fulfilled' ? 'success' : status === 'Cancelled' ? 'danger' : 'warning';
    return <Badge variant={variant}>{status}</Badge>;
}

function msUntil(iso: string): number {
    return new Date(iso).getTime() - Date.now();
}

function formatCountdown(ms: number): string {
    const totalSeconds = Math.max(0, Math.floor(ms / 1000));
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
}

function firstErrorMessage(...errors: unknown[]): string {
    for (const error of errors) {
        if (error instanceof ApiError) return error.message;
        if (error instanceof Error) return error.message;
    }
    return 'Error inesperado.';
}
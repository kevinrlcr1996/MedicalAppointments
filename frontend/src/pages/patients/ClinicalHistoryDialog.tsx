import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../../services/api/apiClient';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { formatDateTime } from '../../utils/formatDate';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Modal } from '../../components/ui/Modal';
import { Spinner } from '../../components/ui/Spinner';

interface HistoryItem {
    noteId: string;
    appointmentId: string;
    appointmentLocalDate: string;
    appointmentStartAtUtc: string;
    doctorFullName: string;
    clinicName: string;
    category: string;
    title: string;
    content: string;
    isFinal: boolean;
    authorDisplayName: string;
    occurredAtUtc: string;
    finalizedAt?: string | null;
    correctsNoteId?: string | null;
    attachmentCount: number;
}

export function ClinicalHistoryDialog({
    open,
    patientId,
    patientLabel,
    onClose,
}: {
    open: boolean;
    patientId: string | null;
    patientLabel: string;
    onClose: () => void;
}) {
    const { user } = useAuth();
    const canRead = hasPermission(user, Permissions.ClinicalRecordsRead);

    const query = useQuery({
        queryKey: ['clinical-history', 'patient', patientId],
        queryFn: () => apiClient.get<HistoryItem[]>(`/api/v1/clinical-records/patients/${patientId}/history`),
        enabled: open && Boolean(patientId) && canRead,
    });

    return (
        <Modal open={open} title={`Historial clínico — ${patientLabel}`} onClose={onClose}>
            {!canRead ? (
                <Alert variant="error" title="Sin acceso">No tienes permiso para ver historiales clínicos.</Alert>
            ) : query.isLoading ? (
                <Spinner label="Cargando historial..." />
            ) : query.isError ? (
                <Alert variant="error" title="Error al cargar historial">
                    {query.error instanceof Error ? query.error.message : 'Error inesperado.'}
                </Alert>
            ) : (query.data?.length ?? 0) === 0 ? (
                <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">Sin notas clínicas registradas.</p>
            ) : (
                <ol className="space-y-3">
                    {query.data!.map((item) => (
                        <li key={item.noteId} className="rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                            <div className="flex flex-wrap items-center gap-2 text-xs text-slate-500 dark:text-slate-400">
                                <span className="font-semibold text-slate-700 dark:text-slate-300">
                                    {formatDateTime(item.appointmentStartAtUtc)}
                                </span>
                                <span>· {item.doctorFullName}</span>
                                <span>· {item.clinicName}</span>
                                <Badge variant="info">{item.category}</Badge>
                                {item.isFinal ? <Badge variant="success">Firmada</Badge> : <Badge variant="warning">Borrador</Badge>}
                                {item.correctsNoteId ? <Badge variant="danger">Corrección</Badge> : null}
                            </div>
                            <p className="mt-1 text-sm font-semibold text-slate-900 dark:text-slate-50">{item.title}</p>
                            <p className="mt-1 whitespace-pre-wrap text-sm text-slate-700 dark:text-slate-300">{item.content}</p>
                            <p className="mt-1 text-xs text-slate-400">Autor: {item.authorDisplayName}</p>
                        </li>
                    ))}
                </ol>
            )}
        </Modal>
    );
}
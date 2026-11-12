import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
    Check,
    ClipboardList,
    PenLine,
    Plus,
    RefreshCw,
    Send,
    ShieldAlert,
    X,
} from 'lucide-react';
import {
    prescriptionsApi,
    type Prescription,
    type PrescriptionPayload,
    type PrescriptionStatus,
} from '../../services/api/prescriptions';
import { ApiError } from '../../services/api/apiClient';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { formatDateTime } from '../../utils/formatDate';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Modal } from '../../components/ui/Modal';
import { Spinner } from '../../components/ui/Spinner';
import { Textarea } from '../../components/ui/Textarea';
import { Input } from '../../components/ui/Input';
import { PrescriptionDocumentsPanel } from './PrescriptionDocumentsPanel';

const statusLabel: Record<PrescriptionStatus, string> = {
    Draft: 'Borrador',
    Issued: 'Emitida',
    Dispensed: 'Dispensada',
    Expired: 'Expirada',
    Cancelled: 'Cancelada',
};

const statusVariant: Record<
    PrescriptionStatus,
    'warning' | 'info' | 'success' | 'neutral' | 'danger'
> = {
    Draft: 'warning',
    Issued: 'info',
    Dispensed: 'success',
    Expired: 'neutral',
    Cancelled: 'danger',
};

function toNumberOrNull(value: string): number | null {
    const trimmed = value.trim();
    if (!trimmed) return null;

    const parsed = Number(trimmed);
    return Number.isFinite(parsed) ? parsed : null;
}

export function PrescriptionsDialog({
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
    const queryClient = useQueryClient();
    const { user } = useAuth();

    const canCreate = hasPermission(user, Permissions.PrescriptionsCreate);
    const canIssue = hasPermission(user, Permissions.PrescriptionsIssue);
    const canCorrect = hasPermission(user, Permissions.PrescriptionsCorrect);
    const canRenew = hasPermission(user, Permissions.PrescriptionsRenew);
    const canCancel = hasPermission(user, Permissions.PrescriptionsCancel);
    const canDispense = hasPermission(user, Permissions.PrescriptionsDispense);
    const canExpire = hasPermission(user, Permissions.PrescriptionsExpire);
    const canReadDocuments = hasPermission(user, Permissions.DocumentsRead);
    const canManageDocuments = hasPermission(user, Permissions.DocumentsManage);

    const [creating, setCreating] = useState(false);
    const [editing, setEditing] = useState<Prescription | null>(null);
    const [correcting, setCorrecting] = useState<Prescription | null>(null);

    const [name, setName] = useState('');
    const [strength, setStrength] = useState('');
    const [dosageForm, setDosageForm] = useState('');
    const [frequency, setFrequency] = useState('');
    const [route, setRoute] = useState('');
    const [quantity, setQuantity] = useState('');
    const [refillsAllowed, setRefillsAllowed] = useState('');
    const [daysSupply, setDaysSupply] = useState('');
    const [instructions, setInstructions] = useState('');
    const [note, setNote] = useState('');

    const query = useQuery({
        queryKey: ['prescriptions', 'patient', patientId],
        queryFn: () => prescriptionsApi.listByPatient(patientId!),
        enabled: open && Boolean(patientId),
    });

    const invalidate = async () => {
        await queryClient.invalidateQueries({ queryKey: ['prescriptions'] });
    };

    const resetForm = () => {
        setCreating(false);
        setEditing(null);
        setCorrecting(null);
        setName('');
        setStrength('');
        setDosageForm('');
        setFrequency('');
        setRoute('');
        setQuantity('');
        setRefillsAllowed('');
        setDaysSupply('');
        setInstructions('');
        setNote('');
    };

    const startCreate = () => {
        resetForm();
        setCreating(true);
    };

    const startEdit = (p: Prescription) => {
        setCreating(false);
        setCorrecting(null);
        setEditing(p);
        setName(p.medicationName);
        setStrength(p.strength ?? '');
        setDosageForm(p.dosageForm ?? '');
        setFrequency(p.frequency ?? '');
        setRoute(p.route ?? '');
        setQuantity(p.quantity?.toString() ?? '');
        setRefillsAllowed(p.refillsAllowed?.toString() ?? '');
        setDaysSupply(p.daysSupply?.toString() ?? '');
        setInstructions(p.instructions ?? '');
        setNote(p.note ?? '');
    };

    const startCorrect = (p: Prescription) => {
        setCreating(false);
        setEditing(null);
        setCorrecting(p);
        setName(p.medicationName);
        setStrength(p.strength ?? '');
        setDosageForm(p.dosageForm ?? '');
        setFrequency(p.frequency ?? '');
        setRoute(p.route ?? '');
        setQuantity(p.quantity?.toString() ?? '');
        setRefillsAllowed(p.refillsAllowed?.toString() ?? '');
        setDaysSupply(p.daysSupply?.toString() ?? '');
        setInstructions(p.instructions ?? '');
        setNote(p.note ?? '');
    };

    const saveMutation = useMutation({
        mutationFn: async () => {
            if (!patientId) throw new Error('Sin paciente.');

            const payload: PrescriptionPayload = {
                medicationName: name.trim(),
                strength: strength.trim() || null,
                dosageForm: dosageForm.trim() || null,
                frequency: frequency.trim() || null,
                route: route.trim() || null,
                quantity: toNumberOrNull(quantity),
                refillsAllowed: toNumberOrNull(refillsAllowed),
                daysSupply: toNumberOrNull(daysSupply),
                instructions: instructions.trim() || null,
                note: note.trim() || null,
            };

            if (correcting) {
                return prescriptionsApi.correct(correcting.id, {
                    ...payload,
                    patientId,
                });
            }

            if (editing) {
                return prescriptionsApi.update(editing.id, payload);
            }

            return prescriptionsApi.create(patientId, payload);
        },
        onSuccess: async () => {
            resetForm();
            await invalidate();
        },
    });

    const issueMutation = useMutation({
        mutationFn: (id: string) => prescriptionsApi.issue(id),
        onSuccess: invalidate,
    });

    const renewMutation = useMutation({
        mutationFn: (id: string) => prescriptionsApi.renew(id),
        onSuccess: invalidate,
    });

    const cancelMutation = useMutation({
        mutationFn: (id: string) => prescriptionsApi.cancel(id),
        onSuccess: invalidate,
    });

    const dispenseMutation = useMutation({ mutationFn: (id: string) => prescriptionsApi.dispense(id), onSuccess: invalidate });
    const expireMutation = useMutation({ mutationFn: (id: string) => prescriptionsApi.expire(id), onSuccess: invalidate });

    const formOpen = creating || Boolean(editing) || Boolean(correcting);
    const isCorrection = Boolean(correcting);

    const actionError =
        issueMutation.error ??
        renewMutation.error ??
        cancelMutation.error ?? dispenseMutation.error ?? expireMutation.error;

    return (
        <Modal
            open={open}
            title={`Recetas — ${patientLabel}`}
            size="lg"
            onClose={() => {
                resetForm();
                onClose();
            }}
        >
            <div className="space-y-4">
                {query.isError ? (
                    <Alert variant="error" title="Error al cargar recetas">
                        {query.error instanceof ApiError
                            ? query.error.message
                            : 'Error inesperado.'}
                    </Alert>
                ) : null}

                {saveMutation.isError ? (
                    <Alert variant="error" title="No se pudo guardar la receta">
                        {saveMutation.error instanceof ApiError
                            ? `${saveMutation.error.status}: ${saveMutation.error.message}`
                            : 'Error inesperado.'}
                    </Alert>
                ) : null}

                {actionError ? (
                    <Alert variant="error" title="Error en la acción">
                        {actionError instanceof ApiError
                            ? actionError.message
                            : 'Error inesperado.'}
                    </Alert>
                ) : null}

                {query.isLoading ? (
                    <Spinner label="Cargando recetas..." />
                ) : (query.data?.length ?? 0) === 0 ? (
                    <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">
                        Sin recetas registradas.
                    </p>
                ) : (
                    <div className="space-y-3">
                        {query.data!.map((p) => (
                            <div
                                key={p.id}
                                className="min-w-0 rounded-xl border border-slate-200 p-4 dark:border-slate-800"
                            >
                                <div className="min-w-0 space-y-3">
                                    <div className="min-w-0">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <Badge variant="info">
                                                {p.medicationName}
                                                {p.strength ? ` ${p.strength}` : ''}
                                            </Badge>

                                            <Badge variant={statusVariant[p.status]}>
                                                {statusLabel[p.status]}
                                            </Badge>

                                            {p.correctsPrescriptionId ? (
                                                <Badge variant="danger">
                                                    Corrige: {p.correctsPrescriptionName ?? 'receta'}
                                                </Badge>
                                            ) : null}

                                            {p.renewedFromPrescriptionId ? (
                                                <Badge variant="neutral">Renovada</Badge>
                                            ) : null}
                                        </div>

                                        <p className="mt-2 break-words text-xs text-slate-500 dark:text-slate-400">
                                            {p.authorDisplayName} · registrada{' '}
                                            {formatDateTime(p.recordedAtUtc)}
                                            {p.issuedAt ? ` · emitida ${formatDateTime(p.issuedAt)}` : ''}
                                        </p>

                                        <p className="mt-1 break-words text-sm text-slate-700 dark:text-slate-300">
                                            {[p.frequency, p.route, p.dosageForm]
                                                .filter(Boolean)
                                                .join(' · ')}
                                        </p>

                                        {(p.quantity ?? p.refillsAllowed ?? p.daysSupply) ? (
                                            <p className="mt-1 break-words text-xs text-slate-500 dark:text-slate-400">
                                                {[
                                                    p.quantity != null ? `Cantidad: ${p.quantity}` : null,
                                                    p.refillsAllowed != null
                                                        ? `Refills: ${p.refillsAllowed}`
                                                        : null,
                                                    p.daysSupply != null
                                                        ? `Días: ${p.daysSupply}`
                                                        : null,
                                                ]
                                                    .filter(Boolean)
                                                    .join(' · ')}
                                            </p>
                                        ) : null}

                                        {p.instructions ? (
                                            <p className="mt-1 break-words text-xs text-slate-500 dark:text-slate-400">
                                                Instrucciones: {p.instructions}
                                            </p>
                                        ) : null}

                                        {p.note ? (
                                            <p className="mt-1 break-words text-xs text-slate-500 dark:text-slate-400">
                                                Nota: {p.note}
                                            </p>
                                        ) : null}
                                    </div>

                                </div>
                                <div className="mt-3 flex min-w-0 flex-wrap items-center gap-2 border-t border-slate-100 pt-3 dark:border-slate-800">
                                    {p.status === 'Draft' && canCreate ? (
                                            <Button
                                                size="sm"
                                                variant="secondary"
                                                onClick={() => startEdit(p)}
                                                leftIcon={<PenLine className="h-3 w-3" />}
                                            >
                                                Editar
                                            </Button>
                                        ) : null}

                                    {p.status === 'Draft' && canIssue ? (
                                            <Button
                                                size="sm"
                                                variant="primary"
                                                onClick={() => issueMutation.mutate(p.id)}
                                                leftIcon={<Send className="h-3 w-3" />}
                                            >
                                                Emitir
                                            </Button>
                                        ) : null}

                                    {p.status === 'Issued' && canDispense ? (
                                            <Button size="sm" variant="primary" onClick={() => dispenseMutation.mutate(p.id)} leftIcon={<Check className="h-3 w-3" />}>
                                                Dispensar
                                            </Button>
                                        ) : null}

                                    {(p.status === 'Issued' || p.status === 'Dispensed') && canExpire ? (
                                            <Button size="sm" variant="secondary" onClick={() => expireMutation.mutate(p.id)}>
                                                Marcar expirada
                                            </Button>
                                        ) : null}

                                    {(p.status === 'Issued' || p.status === 'Dispensed') &&
                                        canRenew ? (
                                            <Button
                                                size="sm"
                                                variant="secondary"
                                                onClick={() => renewMutation.mutate(p.id)}
                                                leftIcon={<RefreshCw className="h-3 w-3" />}
                                            >
                                                Renovar
                                            </Button>
                                        ) : null}

                                    {(p.status === 'Issued' || p.status === 'Dispensed') &&
                                        canCorrect ? (
                                            <Button
                                                size="sm"
                                                variant="secondary"
                                                onClick={() => startCorrect(p)}
                                                leftIcon={<ShieldAlert className="h-3 w-3" />}
                                            >
                                                Corregir
                                            </Button>
                                        ) : null}

                                    {(p.status === 'Draft' ||
                                        p.status === 'Issued' ||
                                        p.status === 'Dispensed') &&
                                        canCancel ? (
                                            <Button
                                                size="sm"
                                                variant="danger"
                                                onClick={() => cancelMutation.mutate(p.id)}
                                                leftIcon={<X className="h-3 w-3" />}
                                            >
                                                Cancelar
                                            </Button>
                                        ) : null}
                                </div>
                                {canReadDocuments && (p.status === 'Issued' || p.status === 'Dispensed') ? (
                                    <PrescriptionDocumentsPanel
                                        prescriptionId={p.id}
                                        canGenerate={canManageDocuments}
                                    />
                                ) : null}
                            </div>
                        ))}
                    </div>
                )}

                {open && canCreate && !formOpen ? (
                    <Button
                        variant="secondary"
                        onClick={startCreate}
                        leftIcon={<Plus className="h-4 w-4" />}
                    >
                        Nueva receta
                    </Button>
                ) : null}

                {formOpen ? (
                    <div className="space-y-3 rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                        <p className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-50">
                            <ClipboardList className="h-4 w-4" />
                            {isCorrection
                                ? 'Corrección de receta'
                                : editing
                                    ? 'Editar borrador'
                                    : 'Nueva receta'}
                        </p>

                        <div className="grid gap-3 sm:grid-cols-2">
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Medicamento
                                <Input
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                    maxLength={200}
                                    placeholder="p. ej. Amoxicilina"
                                />
                            </label>

                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Concentración
                                <Input
                                    value={strength}
                                    onChange={(e) => setStrength(e.target.value)}
                                    maxLength={100}
                                    placeholder="p. ej. 500 mg"
                                />
                            </label>

                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Forma
                                <Input
                                    value={dosageForm}
                                    onChange={(e) => setDosageForm(e.target.value)}
                                    maxLength={100}
                                    placeholder="Cápsula, jarabe…"
                                />
                            </label>

                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Frecuencia
                                <Input
                                    value={frequency}
                                    onChange={(e) => setFrequency(e.target.value)}
                                    maxLength={100}
                                    placeholder="1-0-1, cada 8 horas…"
                                />
                            </label>

                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Vía
                                <Input
                                    value={route}
                                    onChange={(e) => setRoute(e.target.value)}
                                    maxLength={100}
                                    placeholder="oral"
                                />
                            </label>

                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Cantidad
                                <Input
                                    type="number"
                                    min={0}
                                    value={quantity}
                                    onChange={(e) => setQuantity(e.target.value)}
                                    placeholder="21"
                                />
                            </label>

                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Renovaciones permitidas
                                <Input
                                    type="number"
                                    min={0}
                                    value={refillsAllowed}
                                    onChange={(e) => setRefillsAllowed(e.target.value)}
                                    placeholder="0"
                                />
                            </label>

                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Días de suministro
                                <Input
                                    type="number"
                                    min={0}
                                    value={daysSupply}
                                    onChange={(e) => setDaysSupply(e.target.value)}
                                    placeholder="7"
                                />
                            </label>
                        </div>

                        <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                            Instrucciones
                            <Textarea
                                value={instructions}
                                onChange={(e) => setInstructions(e.target.value)}
                                maxLength={2000}
                                rows={2}
                                placeholder="Tomar con alimentos, evitar alcohol…"
                            />
                        </label>

                        <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                            Nota interna
                            <Textarea
                                value={note}
                                onChange={(e) => setNote(e.target.value)}
                                maxLength={2000}
                                rows={2}
                                placeholder="Observación clínica opcional…"
                            />
                        </label>

                        <div className="flex justify-end gap-2">
                            <Button
                                variant="secondary"
                                onClick={resetForm}
                                leftIcon={<X className="h-4 w-4" />}
                            >
                                Cancelar
                            </Button>

                            <Button
                                variant="primary"
                                loading={saveMutation.isPending}
                                disabled={!name.trim()}
                                onClick={() => saveMutation.mutate()}
                                leftIcon={<Check className="h-4 w-4" />}
                            >
                                {isCorrection
                                    ? 'Guardar corrección'
                                    : editing
                                        ? 'Guardar cambios'
                                        : 'Guardar borrador'}
                            </Button>
                        </div>
                    </div>
                ) : null}
            </div>
        </Modal>
    );
}

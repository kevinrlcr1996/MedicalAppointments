import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, Check, PenLine, Plus, RotateCcw, Send, ShieldAlert, X } from 'lucide-react';
import { patientAllergiesApi, type PatientAllergy, type AllergyPayload, type AllergenClass, type AllergySeverity } from '../../services/api/patientAllergies';
import { ApiError } from '../../services/api/apiClient';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { formatDateTime } from '../../utils/formatDate';
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Modal } from '../../components/ui/Modal';
import { Select } from '../../components/ui/Select';
import { Spinner } from '../../components/ui/Spinner';
import { Textarea } from '../../components/ui/Textarea';
import { Input } from '../../components/ui/Input';

const ALLERGEN_CLASSES: { value: AllergenClass; label: string }[] = [
    { value: 'Drug', label: 'Medicamento' }, { value: 'Food', label: 'Alimento' },
    { value: 'Latex', label: 'Látex' }, { value: 'Environmental', label: 'Ambiental' },
    { value: 'InsectVenom', label: 'Veneno de insecto' }, { value: 'Other', label: 'Otro' },
    { value: 'Unknown', label: 'Desconocido' },
];

const SEVERITIES: { value: AllergySeverity; label: string }[] = [
    { value: 'Mild', label: 'Leve' }, { value: 'Moderate', label: 'Moderada' },
    { value: 'Severe', label: 'Severa' }, { value: 'Unknown', label: 'Desconocida' },
];

export function PatientAllergiesDialog({ open, patientId, patientLabel, onClose }: { open: boolean; patientId: string | null; patientLabel: string; onClose: () => void }) {
    const queryClient = useQueryClient();
    const { user } = useAuth();
    const canCreate = hasPermission(user, Permissions.ClinicalRecordsCreate);
    const canUpdate = hasPermission(user, Permissions.ClinicalRecordsUpdate);
    const canFinalize = hasPermission(user, Permissions.ClinicalRecordsFinalize);
    const canCorrect = hasPermission(user, Permissions.ClinicalRecordsCorrect);

    const [editing, setEditing] = useState<PatientAllergy | null>(null);
    const [correcting, setCorrecting] = useState<PatientAllergy | null>(null);
    const [allergen, setAllergen] = useState('');
    const [allergenClass, setAllergenClass] = useState<AllergenClass>('Drug');
    const [severity, setSeverity] = useState<AllergySeverity>('Unknown');
    const [reaction, setReaction] = useState('');
    const [onsetDate, setOnsetDate] = useState('');

    const query = useQuery({
        queryKey: ['patient-allergies', 'patient', patientId],
        queryFn: () => patientAllergiesApi.listByPatient(patientId!),
        enabled: open && Boolean(patientId),
    });

    const invalidate = async () => { await queryClient.invalidateQueries({ queryKey: ['patient-allergies'] }); };
    const resetForm = () => { setEditing(null); setCorrecting(null); setAllergen(''); setAllergenClass('Drug'); setSeverity('Unknown'); setReaction(''); setOnsetDate(''); };
    const startEdit = (a: PatientAllergy) => { setEditing(a); setCorrecting(null); setAllergen(a.allergen); setAllergenClass(a.allergenClass); setSeverity(a.severity); setReaction(a.reaction ?? ''); setOnsetDate(a.onsetDate ?? ''); };
    const startCorrect = (a: PatientAllergy) => { setCorrecting(a); setEditing(null); setAllergen(a.allergen); setAllergenClass(a.allergenClass); setSeverity(a.severity); setReaction(''); setOnsetDate(''); };

    const saveMutation = useMutation({
        mutationFn: async () => {
            if (!patientId) throw new Error('Sin paciente.');
            const payload: AllergyPayload = { allergen: allergen.trim(), allergenClass, severity, reaction: reaction.trim() || null, onsetDate: onsetDate || null };
            if (correcting) return patientAllergiesApi.correct(correcting.id, { ...payload, patientId });
            if (editing) return patientAllergiesApi.update(editing.id, payload);
            return patientAllergiesApi.create(patientId, payload);
        },
        onSuccess: async () => { resetForm(); await invalidate(); },
    });

    const finalizeMutation = useMutation({ mutationFn: (id: string) => patientAllergiesApi.finalize(id), onSuccess: invalidate });
    const resolveMutation = useMutation({ mutationFn: (id: string) => patientAllergiesApi.resolve(id), onSuccess: invalidate });
    const unresolveMutation = useMutation({ mutationFn: (id: string) => patientAllergiesApi.unresolve(id), onSuccess: invalidate });

    const formOpen = Boolean(editing || correcting || (!editing && !correcting && canCreate && open));
    const isCorrection = Boolean(correcting);

    return (
        <Modal open={open} title={`Alergias — ${patientLabel}`} onClose={() => { resetForm(); onClose(); }}>
            <div className="space-y-4">
                {query.isError ? <Alert variant="error" title="Error al cargar alergias">{query.error instanceof ApiError ? query.error.message : 'Error inesperado.'}</Alert> : null}
                {saveMutation.isError ? <Alert variant="error" title="No se pudo guardar">{saveMutation.error instanceof ApiError ? `${saveMutation.error.status}: ${saveMutation.error.message}` : 'Error inesperado.'}</Alert> : null}
                {resolveMutation.isError || unresolveMutation.isError || finalizeMutation.isError ? <Alert variant="error" title="Error en la acción">{(resolveMutation.error ?? unresolveMutation.error ?? finalizeMutation.error) instanceof ApiError ? (resolveMutation.error ?? unresolveMutation.error ?? finalizeMutation.error) as ApiError instanceof ApiError ? ((resolveMutation.error ?? unresolveMutation.error ?? finalizeMutation.error) as ApiError).message : 'Error' : 'Error inesperado.'}</Alert> : null}

                {query.isLoading ? <Spinner label="Cargando alergias..." /> : (query.data?.length ?? 0) === 0 ? (
                    <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">Sin alergias registradas.</p>
                ) : (
                    <div className="space-y-3">
                        {query.data!.map((a) => (
                            <div key={a.id} className="rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                                <div className="flex items-start justify-between gap-2">
                                    <div className="min-w-0">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <Badge variant={a.severity === 'Severe' ? 'danger' : a.severity === 'Moderate' ? 'warning' : 'neutral'}>{a.allergen}</Badge>
                                            <Badge variant="info">{a.allergenClass}</Badge>
                                            {a.isResolved ? <Badge variant="neutral">Resuelta</Badge> : <Badge variant="danger">Activa</Badge>}
                                            {a.isFinal ? <Badge variant="success"><Check className="mr-1 h-3 w-3" />Firmada</Badge> : <Badge variant="warning">Borrador</Badge>}
                                            {a.correctsAllergyId ? <Badge variant="danger">Corrige: {a.correctsAllergyAllergen ?? 'alergia'}</Badge> : null}
                                        </div>
                                        <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{a.authorDisplayName} · registrada {formatDateTime(a.recordedAtUtc)}{a.onsetDate ? ` · inicio ${a.onsetDate}` : ''}{a.resolvedAt ? ` · resuelta ${formatDateTime(a.resolvedAt)}` : ''}</p>
                                        {a.reaction ? <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700 dark:text-slate-300">Reacción: {a.reaction}</p> : null}
                                    </div>
                                    <div className="flex shrink-0 flex-wrap gap-1">
                                        {!a.isFinal && canUpdate ? <Button size="sm" variant="secondary" onClick={() => startEdit(a)} leftIcon={<PenLine className="h-3 w-3" />}>Editar</Button> : null}
                                        {!a.isFinal && canFinalize ? <Button size="sm" variant="primary" onClick={() => finalizeMutation.mutate(a.id)} leftIcon={<Send className="h-3 w-3" />}>Firmar</Button> : null}
                                        {a.isFinal && !a.isResolved && canUpdate ? <Button size="sm" variant="secondary" onClick={() => resolveMutation.mutate(a.id)} leftIcon={<AlertTriangle className="h-3 w-3" />}>Resolver</Button> : null}
                                        {a.isFinal && a.isResolved && canUpdate ? <Button size="sm" variant="secondary" onClick={() => unresolveMutation.mutate(a.id)} leftIcon={<RotateCcw className="h-3 w-3" />}>Reactivar</Button> : null}
                                        {a.isFinal && canCorrect ? <Button size="sm" variant="secondary" onClick={() => startCorrect(a)} leftIcon={<ShieldAlert className="h-3 w-3" />}>Corregir</Button> : null}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}

                {open && canCreate && !editing && !correcting ? <Button variant="secondary" onClick={resetForm} leftIcon={<Plus className="h-4 w-4" />}>Nueva alergia</Button> : null}

                {formOpen ? (
                    <div className="space-y-3 rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                        <p className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-50"><AlertTriangle className="h-4 w-4" />{isCorrection ? 'Corrección de alergia' : editing ? 'Editar borrador' : 'Nueva alergia'}</p>
                        <div className="grid gap-3 sm:grid-cols-2">
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Alérgeno<Input value={allergen} onChange={(e) => setAllergen(e.target.value)} maxLength={200} placeholder="p. ej. Penicilina" /></label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Clase<Select value={allergenClass} onChange={(e) => setAllergenClass(e.target.value as AllergenClass)} options={ALLERGEN_CLASSES} /></label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Severidad<Select value={severity} onChange={(e) => setSeverity(e.target.value as AllergySeverity)} options={SEVERITIES} /></label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Fecha de inicio<Input type="date" value={onsetDate} onChange={(e) => setOnsetDate(e.target.value)} /></label>
                        </div>
                        <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Reacción<Textarea value={reaction} onChange={(e) => setReaction(e.target.value)} maxLength={1000} rows={2} placeholder="Qué le provoca (opcional)..." /></label>
                        <div className="flex justify-end gap-2">
                            <Button variant="secondary" onClick={resetForm} leftIcon={<X className="h-4 w-4" />}>Cancelar</Button>
                            <Button variant="primary" loading={saveMutation.isPending} disabled={!allergen.trim()} onClick={() => saveMutation.mutate()} leftIcon={<Check className="h-4 w-4" />}>{isCorrection ? 'Guardar corrección' : editing ? 'Guardar cambios' : 'Guardar borrador'}</Button>
                        </div>
                    </div>
                ) : null}
            </div>
        </Modal>
    );
}
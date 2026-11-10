import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, PenLine, Plus, Send, ShieldAlert, Stethoscope, X } from 'lucide-react';
import { diagnosesApi, type Diagnosis, type DiagnosisPayload, type DiagnosisCategory } from '../../services/api/diagnoses';
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

const CATEGORIES: { value: DiagnosisCategory; label: string }[] = [
    { value: 'Primary', label: 'Principal' },
    { value: 'Secondary', label: 'Secundario' },
    { value: 'Differential', label: 'Diferencial' },
    { value: 'Other', label: 'Otro' },
];

export function DiagnosisDialog({ open, appointmentId, onClose }: { open: boolean; appointmentId: string | null; onClose: () => void }) {
    const queryClient = useQueryClient();
    const { user } = useAuth();

    const canCreate = hasPermission(user, Permissions.ClinicalRecordsCreate);
    const canUpdate = hasPermission(user, Permissions.ClinicalRecordsUpdate);
    const canFinalize = hasPermission(user, Permissions.ClinicalRecordsFinalize);
    const canCorrect = hasPermission(user, Permissions.ClinicalRecordsCorrect);

    const [editing, setEditing] = useState<Diagnosis | null>(null);
    const [correcting, setCorrecting] = useState<Diagnosis | null>(null);
    const [code, setCode] = useState('');
    const [displayTerm, setDisplayTerm] = useState('');
    const [category, setCategory] = useState<DiagnosisCategory>('Primary');
    const [onsetDate, setOnsetDate] = useState('');
    const [note, setNote] = useState('');

    const query = useQuery({
        queryKey: ['diagnoses', 'appointment', appointmentId],
        queryFn: () => diagnosesApi.listByAppointment(appointmentId!),
        enabled: open && Boolean(appointmentId),
    });

    const invalidate = async () => { await queryClient.invalidateQueries({ queryKey: ['diagnoses'] }); };
    const resetForm = () => { setEditing(null); setCorrecting(null); setCode(''); setDisplayTerm(''); setCategory('Primary'); setOnsetDate(''); setNote(''); };
    const startEdit = (d: Diagnosis) => { setEditing(d); setCorrecting(null); setCode(d.code); setDisplayTerm(d.displayTerm); setCategory(d.category); setOnsetDate(d.onsetDate ?? ''); setNote(d.note ?? ''); };
    const startCorrect = (d: Diagnosis) => { setCorrecting(d); setEditing(null); setCode(d.code); setDisplayTerm(`Corrección: ${d.displayTerm}`); setCategory(d.category); setOnsetDate(''); setNote(''); };

    const saveMutation = useMutation({
        mutationFn: async () => {
            if (!appointmentId) throw new Error('Sin cita.');
            const payload: DiagnosisPayload = {
                code: code.trim(), displayTerm: displayTerm.trim(), category,
                onsetDate: onsetDate || null, note: note.trim() || null,
            };
            if (correcting) return diagnosesApi.correct(correcting.id, payload);
            if (editing) return diagnosesApi.update(editing.id, payload);
            return diagnosesApi.create(appointmentId, payload);
        },
        onSuccess: async () => { resetForm(); await invalidate(); },
    });

    const finalizeMutation = useMutation({ mutationFn: (id: string) => diagnosesApi.finalize(id), onSuccess: invalidate });

    const formOpen = Boolean(editing || correcting || (!editing && !correcting && canCreate && open));
    const isCorrection = Boolean(correcting);

    return (
        <Modal open={open} title="Diagnósticos" onClose={() => { resetForm(); onClose(); }}>
            <div className="space-y-4">
                {query.isError ? <Alert variant="error" title="Error al cargar diagnósticos">{query.error instanceof ApiError ? query.error.message : 'Error inesperado.'}</Alert> : null}
                {saveMutation.isError ? <Alert variant="error" title="No se pudo guardar">{saveMutation.error instanceof ApiError ? `${saveMutation.error.status}: ${saveMutation.error.message}` : 'Error inesperado.'}</Alert> : null}
                {finalizeMutation.isError ? <Alert variant="error" title="No se pudo finalizar">{finalizeMutation.error instanceof ApiError ? finalizeMutation.error.message : 'Error inesperado.'}</Alert> : null}

                {query.isLoading ? <Spinner label="Cargando diagnósticos..." /> : (query.data?.length ?? 0) === 0 ? (
                    <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">Sin diagnósticos para esta cita.</p>
                ) : (
                    <div className="space-y-3">
                        {query.data!.map((d) => (
                            <div key={d.id} className="rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                                <div className="flex items-start justify-between gap-2">
                                    <div className="min-w-0">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <Badge variant="info">{d.code}</Badge>
                                            <Badge variant="neutral">{d.category}</Badge>
                                            {d.isFinal ? <Badge variant="success"><Check className="mr-1 h-3 w-3" />Firmado</Badge> : <Badge variant="warning">Borrador</Badge>}
                                            {d.correctsDiagnosisId ? <Badge variant="danger">Corrige: {d.correctsDiagnosisTerm ?? 'diagnóstico'}</Badge> : null}
                                        </div>
                                        <p className="mt-1 truncate text-sm font-semibold text-slate-900 dark:text-slate-50">{d.displayTerm}</p>
                                        <p className="text-xs text-slate-500 dark:text-slate-400">{d.authorDisplayName} · registrado {formatDateTime(d.recordedAtUtc)}{d.onsetDate ? ` · inicio ${d.onsetDate}` : ''}{d.finalizedAt ? ` · firmado ${formatDateTime(d.finalizedAt)}` : ''}</p>
                                        {d.note ? <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700 dark:text-slate-300">{d.note}</p> : null}
                                    </div>
                                    <div className="flex shrink-0 flex-wrap gap-1">
                                        {!d.isFinal && canUpdate ? <Button size="sm" variant="secondary" onClick={() => startEdit(d)} leftIcon={<PenLine className="h-3 w-3" />}>Editar</Button> : null}
                                        {!d.isFinal && canFinalize ? <Button size="sm" variant="primary" onClick={() => finalizeMutation.mutate(d.id)} leftIcon={<Send className="h-3 w-3" />}>Firmar</Button> : null}
                                        {d.isFinal && canCorrect ? <Button size="sm" variant="secondary" onClick={() => startCorrect(d)} leftIcon={<ShieldAlert className="h-3 w-3" />}>Corregir</Button> : null}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}

                {open && canCreate && !editing && !correcting ? <Button variant="secondary" onClick={() => { setCode(''); setDisplayTerm(''); setCategory('Primary'); setOnsetDate(''); setNote(''); }} leftIcon={<Plus className="h-4 w-4" />}>Nuevo diagnóstico</Button> : null}

                {formOpen ? (
                    <div className="space-y-3 rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                        <p className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-50"><Stethoscope className="h-4 w-4" />{isCorrection ? 'Corrección de diagnóstico' : editing ? 'Editar borrador' : 'Nuevo diagnóstico'}</p>
                        <div className="grid gap-3 sm:grid-cols-2">
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Código ICD-10
                                <Input value={code} onChange={(e) => setCode(e.target.value.toUpperCase())} maxLength={10} placeholder="p. ej. I10, E11.9" />
                            </label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Término
                                <Input value={displayTerm} onChange={(e) => setDisplayTerm(e.target.value)} maxLength={300} placeholder="p. ej. Hipertensión esencial" />
                            </label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Categoría
                                <Select value={category} onChange={(e) => setCategory(e.target.value as DiagnosisCategory)} options={CATEGORIES} />
                            </label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Fecha de inicio
                                <Input type="date" value={onsetDate} onChange={(e) => setOnsetDate(e.target.value)} />
                            </label>
                        </div>
                        <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                            Nota
                            <Textarea value={note} onChange={(e) => setNote(e.target.value)} maxLength={2000} rows={3} placeholder="Observaciones (opcional)..." />
                        </label>
                        <div className="flex justify-end gap-2">
                            <Button variant="secondary" onClick={resetForm} leftIcon={<X className="h-4 w-4" />}>Cancelar</Button>
                            <Button variant="primary" loading={saveMutation.isPending} disabled={!code.trim() || !displayTerm.trim()} onClick={() => saveMutation.mutate()} leftIcon={<Check className="h-4 w-4" />}>{isCorrection ? 'Guardar corrección' : editing ? 'Guardar cambios' : 'Guardar borrador'}</Button>
                        </div>
                    </div>
                ) : null}
            </div>
        </Modal>
    );
}
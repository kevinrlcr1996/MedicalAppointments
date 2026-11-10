import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, PauseCircle, PenLine, Pill, PlayCircle, Plus, Send, ShieldAlert, X } from 'lucide-react';
import { patientMedicationsApi, type PatientMedication, type MedicationPayload } from '../../services/api/patientMedications';
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

export function PatientMedicationsDialog({ open, patientId, patientLabel, onClose }: { open: boolean; patientId: string | null; patientLabel: string; onClose: () => void }) {
    const queryClient = useQueryClient();
    const { user } = useAuth();
    const canCreate = hasPermission(user, Permissions.ClinicalRecordsCreate);
    const canUpdate = hasPermission(user, Permissions.ClinicalRecordsUpdate);
    const canFinalize = hasPermission(user, Permissions.ClinicalRecordsFinalize);
    const canCorrect = hasPermission(user, Permissions.ClinicalRecordsCorrect);

    const [editing, setEditing] = useState<PatientMedication | null>(null);
    const [correcting, setCorrecting] = useState<PatientMedication | null>(null);
    const [name, setName] = useState('');
    const [strength, setStrength] = useState('');
    const [dosageForm, setDosageForm] = useState('');
    const [frequency, setFrequency] = useState('');
    const [route, setRoute] = useState('');
    const [startDate, setStartDate] = useState('');
    const [indication, setIndication] = useState('');

    const query = useQuery({
        queryKey: ['patient-medications', 'patient', patientId],
        queryFn: () => patientMedicationsApi.listByPatient(patientId!),
        enabled: open && Boolean(patientId),
    });

    const invalidate = async () => { await queryClient.invalidateQueries({ queryKey: ['patient-medications'] }); };
    const resetForm = () => { setEditing(null); setCorrecting(null); setName(''); setStrength(''); setDosageForm(''); setFrequency(''); setRoute(''); setStartDate(''); setIndication(''); };
    const startEdit = (m: PatientMedication) => { setEditing(m); setCorrecting(null); setName(m.medicationName); setStrength(m.strength ?? ''); setDosageForm(m.dosageForm ?? ''); setFrequency(m.frequency ?? ''); setRoute(m.route ?? ''); setStartDate(m.startDate ?? ''); setIndication(m.indication ?? ''); };
    const startCorrect = (m: PatientMedication) => { setCorrecting(m); setEditing(null); setName(m.medicationName); setStrength(m.strength ?? ''); setDosageForm(m.dosageForm ?? ''); setFrequency(m.frequency ?? ''); setRoute(m.route ?? ''); setStartDate(''); setIndication(''); };

    const saveMutation = useMutation({
        mutationFn: async () => {
            if (!patientId) throw new Error('Sin paciente.');
            const payload: MedicationPayload = { medicationName: name.trim(), strength: strength.trim() || null, dosageForm: dosageForm.trim() || null, frequency: frequency.trim() || null, route: route.trim() || null, startDate: startDate || null, indication: indication.trim() || null };
            if (correcting) return patientMedicationsApi.correct(correcting.id, { ...payload, patientId });
            if (editing) return patientMedicationsApi.update(editing.id, payload);
            return patientMedicationsApi.create(patientId, payload);
        },
        onSuccess: async () => { resetForm(); await invalidate(); },
    });

    const finalizeMutation = useMutation({ mutationFn: (id: string) => patientMedicationsApi.finalize(id), onSuccess: invalidate });
    const suspendMutation = useMutation({ mutationFn: (id: string) => patientMedicationsApi.suspend(id), onSuccess: invalidate });
    const resumeMutation = useMutation({ mutationFn: (id: string) => patientMedicationsApi.resume(id), onSuccess: invalidate });
    const discontinueMutation = useMutation({ mutationFn: (id: string) => patientMedicationsApi.discontinue(id), onSuccess: invalidate });

    const formOpen = Boolean(editing || correcting || (!editing && !correcting && canCreate && open));
    const isCorrection = Boolean(correcting);
    const actionError = suspendMutation.error ?? resumeMutation.error ?? discontinueMutation.error ?? finalizeMutation.error;

    return (
        <Modal open={open} title={`Medicación — ${patientLabel}`} onClose={() => { resetForm(); onClose(); }}>
            <div className="space-y-4">
                {query.isError ? <Alert variant="error" title="Error al cargar medicación">{query.error instanceof ApiError ? query.error.message : 'Error inesperado.'}</Alert> : null}
                {saveMutation.isError ? <Alert variant="error" title="No se pudo guardar">{saveMutation.error instanceof ApiError ? `${saveMutation.error.status}: ${saveMutation.error.message}` : 'Error inesperado.'}</Alert> : null}
                {actionError ? <Alert variant="error" title="Error en la acción">{actionError instanceof ApiError ? actionError.message : 'Error inesperado.'}</Alert> : null}

                {query.isLoading ? <Spinner label="Cargando medicación..." /> : (query.data?.length ?? 0) === 0 ? (
                    <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">Sin medicación registrada.</p>
                ) : (
                    <div className="space-y-3">
                        {query.data!.map((m) => (
                            <div key={m.id} className="rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                                <div className="flex items-start justify-between gap-2">
                                    <div className="min-w-0">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <Badge variant="info">{m.medicationName}{m.strength ? ` ${m.strength}` : ''}</Badge>
                                            <Badge variant={m.status === 'Active' ? 'success' : m.status === 'Suspended' ? 'warning' : 'neutral'}>{m.status}</Badge>
                                            {m.isFinal ? <Badge variant="success"><Check className="mr-1 h-3 w-3" />Firmada</Badge> : <Badge variant="warning">Borrador</Badge>}
                                            {m.correctsMedicationId ? <Badge variant="danger">Corrige: {m.correctsMedicationName ?? 'medicación'}</Badge> : null}
                                        </div>
                                        <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{m.authorDisplayName} · registrada {formatDateTime(m.recordedAtUtc)}{m.startDate ? ` · inicio ${m.startDate}` : ''}</p>
                                        <p className="mt-1 text-sm text-slate-700 dark:text-slate-300">
                                            {[m.frequency, m.route, m.dosageForm].filter(Boolean).join(' · ')}
                                        </p>
                                        {m.indication ? <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">Indicación: {m.indication}</p> : null}
                                    </div>
                                    <div className="flex shrink-0 flex-wrap gap-1">
                                        {!m.isFinal && canUpdate ? <Button size="sm" variant="secondary" onClick={() => startEdit(m)} leftIcon={<PenLine className="h-3 w-3" />}>Editar</Button> : null}
                                        {!m.isFinal && canFinalize ? <Button size="sm" variant="primary" onClick={() => finalizeMutation.mutate(m.id)} leftIcon={<Send className="h-3 w-3" />}>Firmar</Button> : null}
                                        {m.isFinal && m.status === 'Active' && canUpdate ? <Button size="sm" variant="secondary" onClick={() => suspendMutation.mutate(m.id)} leftIcon={<PauseCircle className="h-3 w-3" />}>Suspender</Button> : null}
                                        {m.isFinal && m.status === 'Suspended' && canUpdate ? <Button size="sm" variant="secondary" onClick={() => resumeMutation.mutate(m.id)} leftIcon={<PlayCircle className="h-3 w-3" />}>Reanudar</Button> : null}
                                        {m.isFinal && m.status !== 'Discontinued' && canUpdate ? <Button size="sm" variant="danger" onClick={() => discontinueMutation.mutate(m.id)} leftIcon={<X className="h-3 w-3" />}>Discontinuar</Button> : null}
                                        {m.isFinal && canCorrect ? <Button size="sm" variant="secondary" onClick={() => startCorrect(m)} leftIcon={<ShieldAlert className="h-3 w-3" />}>Corregir</Button> : null}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}

                {open && canCreate && !editing && !correcting ? <Button variant="secondary" onClick={resetForm} leftIcon={<Plus className="h-4 w-4" />}>Nueva medicación</Button> : null}

                {formOpen ? (
                    <div className="space-y-3 rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                        <p className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-50"><Pill className="h-4 w-4" />{isCorrection ? 'Corrección de medicación' : editing ? 'Editar borrador' : 'Nueva medicación'}</p>
                        <div className="grid gap-3 sm:grid-cols-2">
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Medicamento<Input value={name} onChange={(e) => setName(e.target.value)} maxLength={200} placeholder="p. ej. Metformina" /></label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Concentración<Input value={strength} onChange={(e) => setStrength(e.target.value)} maxLength={100} placeholder="p. ej. 500 mg" /></label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Forma<Input value={dosageForm} onChange={(e) => setDosageForm(e.target.value)} maxLength={100} placeholder="Tableta, jarabe…" /></label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Frecuencia<Input value={frequency} onChange={(e) => setFrequency(e.target.value)} maxLength={100} placeholder="cada 12 horas" /></label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Vía<Input value={route} onChange={(e) => setRoute(e.target.value)} maxLength={100} placeholder="oral" /></label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Fecha de inicio<Input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></label>
                        </div>
                        <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">Indicación<Textarea value={indication} onChange={(e) => setIndication(e.target.value)} maxLength={1000} rows={2} placeholder="Para qué se prescribe (opcional)..." /></label>
                        <div className="flex justify-end gap-2">
                            <Button variant="secondary" onClick={resetForm} leftIcon={<X className="h-4 w-4" />}>Cancelar</Button>
                            <Button variant="primary" loading={saveMutation.isPending} disabled={!name.trim()} onClick={() => saveMutation.mutate()} leftIcon={<Check className="h-4 w-4" />}>{isCorrection ? 'Guardar corrección' : editing ? 'Guardar cambios' : 'Guardar borrador'}</Button>
                        </div>
                    </div>
                ) : null}
            </div>
        </Modal>
    );
}
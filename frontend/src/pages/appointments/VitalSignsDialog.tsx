import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, HeartPulse, PenLine, Plus, Send, ShieldAlert, X } from 'lucide-react';
import { vitalSignsApi, type VitalSigns, type VitalSignsPayload, type TemperatureMethod, type Position } from '../../services/api/vitalSigns';
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

const TEMPERATURE_METHODS: { value: TemperatureMethod; label: string }[] = [
    { value: 'Oral', label: 'Oral' },
    { value: 'Axillary', label: 'Axilar' },
    { value: 'Tympanic', label: 'Timpánica' },
    { value: 'Forehead', label: 'Frente' },
    { value: 'Rectal', label: 'Rectal' },
    { value: 'Other', label: 'Otro' },
];

const POSITIONS: { value: Position; label: string }[] = [
    { value: 'Sitting', label: 'Sentado' },
    { value: 'Standing', label: 'De pie' },
    { value: 'LyingDown', label: 'Acostado' },
    { value: 'Other', label: 'Otro' },
];

const NUM_FIELDS: { key: keyof VitalSignsPayload; label: string; step?: string }[] = [
    { key: 'systolicmmHg', label: 'Sistólica (mmHg)' },
    { key: 'diastolicmmHg', label: 'Diastólica (mmHg)' },
    { key: 'heartRateBpm', label: 'FC (lpm)' },
    { key: 'temperatureC', label: 'Temp (°C)', step: '0.1' },
    { key: 'respiratoryRatePerMin', label: 'FR (/min)' },
    { key: 'spo2Percent', label: 'SpO2 (%)' },
    { key: 'weightKg', label: 'Peso (kg)', step: '0.1' },
    { key: 'heightCm', label: 'Talla (cm)', step: '0.1' },
    { key: 'painScore0to10', label: 'Dolor (0-10)' },
];

export function VitalSignsDialog({ open, appointmentId, onClose }: { open: boolean; appointmentId: string | null; onClose: () => void }) {
    const queryClient = useQueryClient();
    const { user } = useAuth();

    const canCreate = hasPermission(user, Permissions.ClinicalRecordsCreate);
    const canUpdate = hasPermission(user, Permissions.ClinicalRecordsUpdate);
    const canFinalize = hasPermission(user, Permissions.ClinicalRecordsFinalize);
    const canCorrect = hasPermission(user, Permissions.ClinicalRecordsCorrect);

    const [editing, setEditing] = useState<VitalSigns | null>(null);
    const [correcting, setCorrecting] = useState<VitalSigns | null>(null);
    const [values, setValues] = useState<VitalSignsPayload>({});
    const [note, setNote] = useState('');

    const query = useQuery({
        queryKey: ['vital-signs', 'appointment', appointmentId],
        queryFn: () => vitalSignsApi.listByAppointment(appointmentId!),
        enabled: open && Boolean(appointmentId),
    });

    const invalidate = async () => { await queryClient.invalidateQueries({ queryKey: ['vital-signs'] }); };
    const resetForm = () => { setEditing(null); setCorrecting(null); setValues({}); setNote(''); };
    const startEdit = (v: VitalSigns) => { setEditing(v); setCorrecting(null); setValues(toPayload(v)); setNote(v.note ?? ''); };
    const startCorrect = (v: VitalSigns) => { setCorrecting(v); setEditing(null); setValues({}); setNote(''); };

    const saveMutation = useMutation({
        mutationFn: async () => {
            if (!appointmentId) throw new Error('Sin cita.');
            const payload: VitalSignsPayload = { ...values, note: note.trim() || null };
            if (correcting) return vitalSignsApi.correct(correcting.id, payload);
            if (editing) return vitalSignsApi.update(editing.id, payload);
            return vitalSignsApi.create(appointmentId, payload);
        },
        onSuccess: async () => { resetForm(); await invalidate(); },
    });

    const finalizeMutation = useMutation({ mutationFn: (id: string) => vitalSignsApi.finalize(id), onSuccess: invalidate });

    const formOpen = Boolean(editing || correcting || (!editing && !correcting && canCreate && open));
    const isCorrection = Boolean(correcting);

    return (
        <Modal open={open} title="Signos vitales" onClose={() => { resetForm(); onClose(); }}>
            <div className="space-y-4">
                {query.isError ? <Alert variant="error" title="Error al cargar vitales">{query.error instanceof ApiError ? query.error.message : 'Error inesperado.'}</Alert> : null}
                {saveMutation.isError ? <Alert variant="error" title="No se pudo guardar">{saveMutation.error instanceof ApiError ? `${saveMutation.error.status}: ${saveMutation.error.message}` : 'Error inesperado.'}</Alert> : null}
                {finalizeMutation.isError ? <Alert variant="error" title="No se pudo finalizar">{finalizeMutation.error instanceof ApiError ? finalizeMutation.error.message : 'Error inesperado.'}</Alert> : null}

                {query.isLoading ? <Spinner label="Cargando vitales..." /> : (query.data?.length ?? 0) === 0 ? (
                    <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">Sin signos vitales para esta cita.</p>
                ) : (
                    <div className="space-y-3">
                        {query.data!.map((v) => (
                            <div key={v.id} className="rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                                <div className="flex items-start justify-between gap-2">
                                    <div className="min-w-0">
                                        <div className="flex flex-wrap items-center gap-2">
                                            {v.isFinal ? <Badge variant="success"><Check className="mr-1 h-3 w-3" />Firmado</Badge> : <Badge variant="warning">Borrador</Badge>}
                                            {v.correctsVitalSignsId ? <Badge variant="danger">Corrige: {v.correctsVitalSignsTitle ?? 'registro'}</Badge> : null}
                                        </div>
                                        <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">{v.authorDisplayName} · registrado {formatDateTime(v.recordedAtUtc)}{v.finalizedAt ? ` · firmado ${formatDateTime(v.finalizedAt)}` : ''}</p>
                                        <div className="mt-2 grid grid-cols-2 gap-x-4 gap-y-1 text-sm text-slate-700 dark:text-slate-300 sm:grid-cols-3">
                                            <Vital label="PA" value={v.systolicmmHg != null && v.diastolicmmHg != null ? `${v.systolicmmHg}/${v.diastolicmmHg}` : null} unit="mmHg" />
                                            <Vital label="FC" value={v.heartRateBpm} unit="lpm" />
                                            <Vital label="Temp" value={v.temperatureC} unit={`°C${v.temperatureMethod ? ` (${v.temperatureMethod})` : ''}`} />
                                            <Vital label="FR" value={v.respiratoryRatePerMin} unit="/min" />
                                            <Vital label="SpO2" value={v.spo2Percent} unit="%" />
                                            <Vital label="Dolor" value={v.painScore0to10} unit="/10" />
                                            <Vital label="Peso" value={v.weightKg} unit="kg" />
                                            <Vital label="Talla" value={v.heightCm} unit="cm" />
                                            <Vital label="Posición" value={v.position} unit="" />
                                        </div>
                                        {v.note ? <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700 dark:text-slate-300">{v.note}</p> : null}
                                    </div>
                                    <div className="flex shrink-0 flex-wrap gap-1">
                                        {!v.isFinal && canUpdate ? <Button size="sm" variant="secondary" onClick={() => startEdit(v)} leftIcon={<PenLine className="h-3 w-3" />}>Editar</Button> : null}
                                        {!v.isFinal && canFinalize ? <Button size="sm" variant="primary" onClick={() => finalizeMutation.mutate(v.id)} leftIcon={<Send className="h-3 w-3" />}>Firmar</Button> : null}
                                        {v.isFinal && canCorrect ? <Button size="sm" variant="secondary" onClick={() => startCorrect(v)} leftIcon={<ShieldAlert className="h-3 w-3" />}>Corregir</Button> : null}
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}

                {open && canCreate && !editing && !correcting ? <Button variant="secondary" onClick={() => { setValues({}); setNote(''); }} leftIcon={<Plus className="h-4 w-4" />}>Nuevo registro</Button> : null}

                {formOpen ? (
                    <div className="space-y-3 rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                        <p className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-50"><HeartPulse className="h-4 w-4" />{isCorrection ? 'Corrección de vitales' : editing ? 'Editar borrador' : 'Nuevo registro'}</p>
                        <div className="grid gap-3 sm:grid-cols-3">
                            {NUM_FIELDS.map((f) => (
                                <label key={f.key} className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                    {f.label}
                                    <Input
                                        type="number"
                                        step={f.step}
                                        value={(values[f.key] as number | null | undefined) ?? ''}
                                        onChange={(e) => setValues((prev) => ({ ...prev, [f.key]: e.target.value === '' ? null : Number(e.target.value) }))}
                                    />
                                </label>
                            ))}
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Método temp
                                <Select value={values.temperatureMethod ?? ''} onChange={(e) => setValues((prev) => ({ ...prev, temperatureMethod: (e.target.value || null) as TemperatureMethod | null }))} options={TEMPERATURE_METHODS} placeholder="—" />
                            </label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Posición
                                <Select value={values.position ?? ''} onChange={(e) => setValues((prev) => ({ ...prev, position: (e.target.value || null) as Position | null }))} options={POSITIONS} placeholder="—" />
                            </label>
                        </div>
                        <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                            Nota
                            <Textarea value={note} onChange={(e) => setNote(e.target.value)} maxLength={2000} rows={3} placeholder="Observaciones (opcional)..." />
                        </label>
                        <div className="flex justify-end gap-2">
                            <Button variant="secondary" onClick={resetForm} leftIcon={<X className="h-4 w-4" />}>Cancelar</Button>
                            <Button variant="primary" loading={saveMutation.isPending} onClick={() => saveMutation.mutate()} leftIcon={<Check className="h-4 w-4" />}>{isCorrection ? 'Guardar corrección' : editing ? 'Guardar cambios' : 'Guardar borrador'}</Button>
                        </div>
                    </div>
                ) : null}
            </div>
        </Modal>
    );
}

function Vital({ label, value, unit }: { label: string; value: number | string | null | undefined; unit: string }) {
    if (value == null || value === '') return null;
    return <span><span className="font-semibold text-slate-900 dark:text-slate-50">{label}</span> {value}{unit ? ` ${unit}` : ''}</span>;
}

function toPayload(v: VitalSigns): VitalSignsPayload {
    return {
        systolicmmHg: v.systolicmmHg, diastolicmmHg: v.diastolicmmHg, heartRateBpm: v.heartRateBpm,
        temperatureC: v.temperatureC, temperatureMethod: v.temperatureMethod, position: v.position,
        respiratoryRatePerMin: v.respiratoryRatePerMin, spo2Percent: v.spo2Percent, weightKg: v.weightKg,
        heightCm: v.heightCm, painScore0to10: v.painScore0to10,
    };
}
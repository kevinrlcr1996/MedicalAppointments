import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, FileText, PenLine, Plus, Send, ShieldAlert, X } from 'lucide-react';
import {
    clinicalRecordsApi,
    type ClinicalNote,
    type ClinicalNoteCategory,
} from '../../services/api/clinicalRecords';
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

const CATEGORIES: { value: ClinicalNoteCategory; label: string }[] = [
    { value: 'Subjective', label: 'Subjetivo (S)' },
    { value: 'Objective', label: 'Objetivo (O)' },
    { value: 'Assessment', label: 'Evaluación (A)' },
    { value: 'Plan', label: 'Plan (P)' },
    { value: 'Other', label: 'Otro' },
];

export function ClinicalNotesDialog({
    open,
    appointmentId,
    onClose,
}: {
    open: boolean;
    appointmentId: string | null;
    onClose: () => void;
}) {
    const queryClient = useQueryClient();
    const { user } = useAuth();

    const canCreate = hasPermission(user, Permissions.ClinicalRecordsCreate);
    const canUpdate = hasPermission(user, Permissions.ClinicalRecordsUpdate);
    const canFinalize = hasPermission(user, Permissions.ClinicalRecordsFinalize);
    const canCorrect = hasPermission(user, Permissions.ClinicalRecordsCorrect);

    const [editing, setEditing] = useState<ClinicalNote | null>(null);
    const [correcting, setCorrecting] = useState<ClinicalNote | null>(null);
    const [category, setCategory] = useState<ClinicalNoteCategory>('Subjective');
    const [title, setTitle] = useState('');
    const [content, setContent] = useState('');

    const notesQuery = useQuery({
        queryKey: ['clinical-records', 'appointment', appointmentId],
        queryFn: () => clinicalRecordsApi.listByAppointment(appointmentId!),
        enabled: open && Boolean(appointmentId),
    });

    const invalidate = async () => {
        await queryClient.invalidateQueries({ queryKey: ['clinical-records'] });
    };

    const resetForm = () => {
        setEditing(null);
        setCorrecting(null);
        setCategory('Subjective');
        setTitle('');
        setContent('');
    };

    const startEdit = (note: ClinicalNote) => {
        setEditing(note);
        setCorrecting(null);
        setCategory(note.category);
        setTitle(note.title);
        setContent(note.content);
    };

    const startCorrect = (note: ClinicalNote) => {
        setCorrecting(note);
        setEditing(null);
        setCategory(note.category);
        setTitle(`Corrección: ${note.title}`);
        setContent('');
    };

    const saveMutation = useMutation({
        mutationFn: async () => {
            if (!appointmentId) throw new Error('Sin cita.');
            if (correcting) {
                return clinicalRecordsApi.correct(correcting.id, { category, title, content });
            }
            if (editing) {
                return clinicalRecordsApi.update(editing.id, { category, title, content });
            }
            return clinicalRecordsApi.create(appointmentId, { category, title, content });
        },
        onSuccess: async () => {
            resetForm();
            await invalidate();
        },
    });

    const finalizeMutation = useMutation({
        mutationFn: (id: string) => clinicalRecordsApi.finalize(id),
        onSuccess: invalidate,
    });

    const formOpen = Boolean(editing || correcting || (!editing && !correcting && canCreate && open));
    const isCorrection = Boolean(correcting);

    return (
        <Modal open={open} title="Notas clínicas" onClose={() => { resetForm(); onClose(); }}>
            <div className="space-y-4">
                {notesQuery.isError ? (
                    <Alert variant="error" title="Error al cargar notas">
                        {notesQuery.error instanceof ApiError ? notesQuery.error.message : 'Error inesperado.'}
                    </Alert>
                ) : null}

                {saveMutation.isError ? (
                    <Alert variant="error" title="No se pudo guardar la nota">
                        {saveMutation.error instanceof ApiError
                            ? `${saveMutation.error.status}: ${saveMutation.error.message}`
                            : saveMutation.error instanceof Error
                                ? saveMutation.error.message
                                : 'Error inesperado.'}
                    </Alert>
                ) : null}

                {finalizeMutation.isError ? (
                    <Alert variant="error" title="No se pudo finalizar">
                        {finalizeMutation.error instanceof ApiError ? finalizeMutation.error.message : 'Error inesperado.'}
                    </Alert>
                ) : null}

                {notesQuery.isLoading ? (
                    <Spinner label="Cargando notas..." />
                ) : (notesQuery.data?.length ?? 0) === 0 ? (
                    <p className="py-4 text-center text-sm text-slate-500 dark:text-slate-400">
                        Sin notas clínicas para esta cita.
                    </p>
                ) : (
                    <div className="space-y-3">
                        {notesQuery.data!.map((note) => (
                            <div
                                key={note.id}
                                className="rounded-xl border border-slate-200 p-3 dark:border-slate-800"
                            >
                                <div className="flex items-start justify-between gap-2">
                                    <div className="min-w-0">
                                        <div className="flex flex-wrap items-center gap-2">
                                            <Badge variant="info">{note.category}</Badge>
                                            {note.isFinal ? (
                                                <Badge variant="success"><Check className="mr-1 h-3 w-3" />Firmada</Badge>
                                            ) : (
                                                <Badge variant="warning">Borrador</Badge>
                                            )}
                                            {note.correctsNoteId ? (
                                                <Badge variant="danger">Corrige: {note.correctsNoteTitle ?? 'nota'}</Badge>
                                            ) : null}
                                        </div>
                                        <p className="mt-1 truncate text-sm font-semibold text-slate-900 dark:text-slate-50">
                                            {note.title}
                                        </p>
                                        <p className="text-xs text-slate-500 dark:text-slate-400">
                                            {note.authorDisplayName} · {formatDateTime(note.occurredAtUtc)}
                                            {note.finalizedAt ? ` · firmada ${formatDateTime(note.finalizedAt)}` : ''}
                                        </p>
                                    </div>
                                    <div className="flex shrink-0 flex-wrap gap-1">
                                        {!note.isFinal && canUpdate ? (
                                            <Button size="sm" variant="secondary" onClick={() => startEdit(note)} leftIcon={<PenLine className="h-3 w-3" />}>
                                                Editar
                                            </Button>
                                        ) : null}
                                        {!note.isFinal && canFinalize ? (
                                            <Button size="sm" variant="primary" onClick={() => finalizeMutation.mutate(note.id)} leftIcon={<Send className="h-3 w-3" />}>
                                                Firmar
                                            </Button>
                                        ) : null}
                                        {note.isFinal && canCorrect ? (
                                            <Button size="sm" variant="secondary" onClick={() => startCorrect(note)} leftIcon={<ShieldAlert className="h-3 w-3" />}>
                                                Corregir
                                            </Button>
                                        ) : null}
                                    </div>
                                </div>
                                <p className="mt-2 whitespace-pre-wrap text-sm text-slate-700 dark:text-slate-300">
                                    {note.content}
                                </p>
                            </div>
                        ))}
                    </div>
                )}

                {open && canCreate && !editing && !correcting ? (
                    <Button variant="secondary" onClick={() => { setCategory('Subjective'); setTitle(''); setContent(''); }} leftIcon={<Plus className="h-4 w-4" />}>
                        Nueva nota
                    </Button>
                ) : null}

                {formOpen ? (
                    <div className="space-y-3 rounded-xl border border-slate-200 p-3 dark:border-slate-800">
                        <p className="flex items-center gap-2 text-sm font-semibold text-slate-900 dark:text-slate-50">
                            <FileText className="h-4 w-4" />
                            {isCorrection ? 'Nota de corrección' : editing ? 'Editar borrador' : 'Nueva nota'}
                        </p>

                        <div className="grid gap-3 sm:grid-cols-2">
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Categoría
                                <Select value={category} onChange={(e) => setCategory(e.target.value as ClinicalNoteCategory)} options={CATEGORIES} />
                            </label>
                            <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                                Título
                                <Input value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} placeholder="p. ej. Motivo de consulta" />
                            </label>
                        </div>

                        <label className="flex flex-col gap-1 text-xs font-medium text-slate-700 dark:text-slate-300">
                            Contenido
                            <Textarea value={content} onChange={(e) => setContent(e.target.value)} maxLength={20000} rows={5} placeholder="Texto clínico libre..." />
                        </label>

                        <div className="flex justify-end gap-2">
                            <Button variant="secondary" onClick={resetForm} leftIcon={<X className="h-4 w-4" />}>
                                Cancelar
                            </Button>
                            <Button
                                variant="primary"
                                loading={saveMutation.isPending}
                                disabled={!title.trim() || !content.trim()}
                                onClick={() => saveMutation.mutate()}
                                leftIcon={<Check className="h-4 w-4" />}
                            >
                                {isCorrection ? 'Guardar corrección' : editing ? 'Guardar cambios' : 'Guardar borrador'}
                            </Button>
                        </div>
                    </div>
                ) : null}
            </div>
        </Modal>
    );
}
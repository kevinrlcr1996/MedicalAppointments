import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, Download, FileText, Paperclip, PenLine, Plus, Send, ShieldAlert, ShieldCheck, ShieldQuestion, Trash, X } from 'lucide-react';
import { useState } from 'react';
import {
    clinicalRecordsApi,
    type ClinicalNote,
    type ClinicalNoteCategory,
} from '../../services/api/clinicalRecords';
// 8b: adjuntos. Este módulo lo entregué en la fase 8b; su upload usa apiClient.postForm
// y su downloadUrl devuelve una ruta relativa. AMBOS dependen de detalles de tu apiClient
// que NO confirmé. Si apiClient no tiene postForm, el error TS vive en clinicalAttachments.ts
// (UNA línea), NO en este dialog. Pégame apiClient.ts y alineo upload+download allí.
import { Alert } from '../../components/ui/Alert';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { Select } from '../../components/ui/Select';
import { Spinner } from '../../components/ui/Spinner';
import { Textarea } from '../../components/ui/Textarea';
import { useAuth } from '../../contexts/AuthContext';
import { hasPermission, Permissions } from '../../lib/permissions';
import { ApiError } from '../../services/api/apiClient';
import { clinicalAttachmentsApi, type ClinicalAttachment } from '../../services/api/clinicalAttachments';
import { formatDateTime } from '../../utils/formatDate';

// NOTA de diagnóstico (React DevTools, según react.dev/link/react-devtools): si la lista de
// adjuntos de una nota saliera vacía, la EXTENSIÓN del navegador (Chrome/Edge/Firefox) añade
// los paneles Components y Profiler al visitar el sitio React; para Safari y otros navegadores
// instala el paquete npm (npm install -g react-devtools), ábrelo desde terminal (react-devtools)
// y conecta el sitio añadiendo <script src="http://localhost:8097"></script> al INICIO del <head>,
// luego recarga. (Para apps React Native existe React Native DevTools, el depurador integrado;
// NO aplica a este stack web React/Vite.) DevTools confirma QUE el server devolvió vacío (el
// useQuery muestra data: [] / undefined), NO el POR QUÉ (¿scope? ¿403? ¿nota sin adjuntos?).
// El por-qué se decide con Network (status+response) y SQL, no con DevTools.

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

                                {/* 8b: adjuntos de esta nota. canUpdate && !isFinal => solo borradores suben;
                                    listar/descargar siempre (quien ve la nota ve sus adjuntos). */}
                                <AttachmentsSection note={note} canUpdate={canUpdate && !note.isFinal} />
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

function AttachmentsSection({ note, canUpdate }: { note: ClinicalNote; canUpdate: boolean }) {
    const queryClient = useQueryClient();
    const [uploading, setUploading] = useState(false);
    const [downloadingId, setDownloadingId] = useState<string | null>(null);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [downloadError, setDownloadError] = useState<string | null>(null);

    const attachmentsQuery = useQuery({
        queryKey: ['clinical-attachments', 'note', note.id],
        queryFn: () => clinicalAttachmentsApi.listByNote(note.id),
    });

    const invalidateAttachments = async () => {
        await queryClient.invalidateQueries({ queryKey: ['clinical-attachments', 'note', note.id] });
    };

    const uploadMutation = useMutation({
        mutationFn: (file: File) => clinicalAttachmentsApi.upload(note.id, file),
        onSuccess: async () => { setUploading(false); await invalidateAttachments(); },
        onError: () => setUploading(false),
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => clinicalAttachmentsApi.remove(id),
        onSuccess: async () => { setDeletingId(null); await invalidateAttachments(); },
        onError: () => setDeletingId(null),
    });

    const handleDownload = async (a: ClinicalAttachment) => {
        setDownloadError(null);
        setDownloadingId(a.id);
        try {
            const blob = await clinicalAttachmentsApi.downloadBlob(a.id);
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = a.fileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            URL.revokeObjectURL(url);
        } catch (err) {
            setDownloadError(err instanceof ApiError ? `${err.status}: ${err.message}` : 'No se pudo descargar el adjunto.');
        } finally {
            setDownloadingId(null);
        }
    };

    const handleDelete = (a: ClinicalAttachment) => {
        setDeletingId(a.id);
        deleteMutation.mutate(a.id);
    };

    const items = attachmentsQuery.data ?? [];

    return (
        <div className="mt-3 border-t border-slate-200 pt-2 dark:border-slate-800">
            <p className="mb-1 flex items-center gap-1 text-xs font-semibold text-slate-600 dark:text-slate-400">
                <Paperclip className="h-3 w-3" /> Adjuntos ({items.length})
            </p>

            {items.map((a) => (
                <div key={a.id} className="flex items-center justify-between gap-2 py-1 text-xs">
                    <span className="truncate text-slate-700 dark:text-slate-300">{a.fileName}</span>
                    <span className="flex shrink-0 items-center gap-2">
                        <ScanBadge status={a.scanStatus} />
                        <Button variant="secondary" size="sm" loading={downloadingId === a.id}
                            onClick={() => handleDownload(a)} leftIcon={<Download className="h-3 w-3" />}>
                            descargar
                        </Button>
                        {canUpdate ? (
                            <Button variant="secondary" size="sm" loading={deletingId === a.id}
                                onClick={() => handleDelete(a)} leftIcon={<Trash className="h-3 w-3" />}>
                                quitar
                            </Button>
                        ) : null}
                    </span>
                </div>
            ))}

            {items.length === 0 && !attachmentsQuery.isLoading ? (
                <p className="text-xs text-slate-400">Sin adjuntos.</p>
            ) : null}

            {canUpdate ? (
                <label className="mt-2 inline-flex cursor-pointer items-center gap-1 text-xs font-medium text-sky-700 dark:text-sky-300">
                    <Paperclip className="h-3 w-3" />
                    {uploading || uploadMutation.isPending ? 'Subiendo…' : 'Adjuntar archivo'}
                    <input type="file" className="hidden" disabled={uploading || uploadMutation.isPending}
                        onChange={(e) => {
                            const file = e.target.files?.[0];
                            if (file) { setUploading(true); uploadMutation.mutate(file); }
                            e.target.value = '';
                        }} />
                </label>
            ) : null}

            {uploadMutation.isError ? (
                <p className="mt-1 text-xs text-rose-600 dark:text-rose-400">
                    {uploadMutation.error instanceof ApiError ? `${uploadMutation.error.status}: ${uploadMutation.error.message}` : 'No se pudo subir el adjunto.'}
                </p>
            ) : null}
            {deleteMutation.isError ? (
                <p className="mt-1 text-xs text-rose-600 dark:text-rose-400">
                    {deleteMutation.error instanceof ApiError ? `${deleteMutation.error.status}: ${deleteMutation.error.message}` : 'No se pudo quitar el adjunto.'}
                </p>
            ) : null}
            {downloadError ? (
                <p className="mt-1 text-xs text-rose-600 dark:text-rose-400">{downloadError}</p>
            ) : null}
        </div>
    );
}

function ScanBadge({ status }: { status: ClinicalAttachment['scanStatus'] }) {
    if (status === 'Clean') {
        return <span className="inline-flex items-center gap-1 text-emerald-600 dark:text-emerald-400"><ShieldCheck className="h-3 w-3" />limpio</span>;
    }
    if (status === 'Skipped') {
        return <span className="inline-flex items-center gap-1 text-amber-600 dark:text-amber-400"><ShieldQuestion className="h-3 w-3" />sin analizar</span>;
    }
    if (status === 'Infected') {
        return <span className="inline-flex items-center gap-1 text-rose-600 dark:text-rose-400"><ShieldAlert className="h-3 w-3" />cuarentena</span>;
    }
    return <span className="text-slate-400">pendiente</span>;
}
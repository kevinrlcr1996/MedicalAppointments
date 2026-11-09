import { apiClient } from './apiClient';

export type ScanStatus = 'Pending' | 'Clean' | 'Skipped' | 'Infected';

export interface ClinicalAttachment {
    id: string;
    noteId: string;
    fileName: string;
    contentType: string;
    sizeBytes: number;
    scanStatus: ScanStatus;
    scannedAt?: string | null;
    uploadedByUserId: string;
    createdAt: string;
}

export const clinicalAttachmentsApi = {
    listByNote(noteId: string): Promise<ClinicalAttachment[]> {
        return apiClient.get<ClinicalAttachment[]>(`/api/v1/clinical-records/notes/${noteId}/attachments`);
    },

    // FIX runtime: `post` hacia JSON.stringify(FormData) => "{}" (confirmado en el cuerpo
    // de apiClient). postMultipart manda el FormData crudo y deja que el navegador arme el
    // boundary multipart. Coincide con el [FromForm] IFormFile file del controller de 8b.
    upload(noteId: string, file: File): Promise<ClinicalAttachment> {
        const form = new FormData();
        form.append('file', file);
        return apiClient.postMultipart<ClinicalAttachment>(
            `/api/v1/clinical-records/notes/${noteId}/attachments`,
            form,
        );
    },

    // FIX runtime: el <a href="/api/v1/..."> caia en el puerto del frontend (404) y sin
    // header Authorization (401). downloadBlob reutiliza token+apiBaseUrl internos y
    // devuelve el binario integro (request() lo corromperia con text()+JSON.parse).
    downloadBlob(attachmentId: string): Promise<Blob> {
        return apiClient.downloadBlob(`/api/v1/clinical-records/attachments/${attachmentId}/download`);
    },
};
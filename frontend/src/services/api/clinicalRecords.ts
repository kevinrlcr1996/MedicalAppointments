import { apiClient } from './apiClient';

export type ClinicalNoteCategory = 'Subjective' | 'Objective' | 'Assessment' | 'Plan' | 'Other';

export interface ClinicalNote {
    id: string;
    appointmentId: string;
    authorUserId: string;
    authorDoctorId: string;
    authorDisplayName: string;
    category: ClinicalNoteCategory;
    title: string;
    content: string;
    isFinal: boolean;
    occurredAtUtc: string;
    finalizedAt?: string | null;
    finalizedByUserId?: string | null;
    correctsNoteId?: string | null;
    correctsNoteTitle?: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface CreateNotePayload {
    category: ClinicalNoteCategory;
    title: string;
    content: string;
    occurredAtUtc?: string | null;
}

export interface UpdateNotePayload {
    category: ClinicalNoteCategory;
    title: string;
    content: string;
}

export const clinicalRecordsApi = {
    listByAppointment(appointmentId: string): Promise<ClinicalNote[]> {
        return apiClient.get<ClinicalNote[]>(`/api/v1/clinical-records/appointments/${appointmentId}/notes`);
    },
    create(appointmentId: string, payload: CreateNotePayload): Promise<ClinicalNote> {
        return apiClient.post<ClinicalNote>(`/api/v1/clinical-records/appointments/${appointmentId}/notes`, payload);
    },
    update(noteId: string, payload: UpdateNotePayload): Promise<ClinicalNote> {
        return apiClient.put<ClinicalNote>(`/api/v1/clinical-records/notes/${noteId}`, payload);
    },
    finalize(noteId: string): Promise<ClinicalNote> {
        return apiClient.post<ClinicalNote>(`/api/v1/clinical-records/notes/${noteId}/finalize`, {});
    },
    correct(noteId: string, payload: CreateNotePayload): Promise<ClinicalNote> {
        return apiClient.post<ClinicalNote>(`/api/v1/clinical-records/notes/${noteId}/corrections`, payload);
    },
};
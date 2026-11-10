import { apiClient } from './apiClient';

export type DiagnosisCategory = 'Primary' | 'Secondary' | 'Differential' | 'Other';

export interface Diagnosis {
    id: string;
    appointmentId: string;
    authorUserId: string;
    authorDoctorId: string;
    authorDisplayName: string;
    code: string;
    displayTerm: string;
    category: DiagnosisCategory;
    onsetDate?: string | null;
    note?: string | null;
    isFinal: boolean;
    recordedAtUtc: string;
    finalizedAt?: string | null;
    finalizedByUserId?: string | null;
    correctsDiagnosisId?: string | null;
    correctsDiagnosisTerm?: string | null;
    createdAt: string;
}

export interface DiagnosisPayload {
    code: string;
    displayTerm: string;
    category: DiagnosisCategory;
    onsetDate?: string | null;
    note?: string | null;
    recordedAtUtc?: string | null;
}

export const diagnosesApi = {
    listByAppointment(appointmentId: string): Promise<Diagnosis[]> {
        return apiClient.get<Diagnosis[]>(`/api/v1/clinical-records/appointments/${appointmentId}/diagnoses`);
    },
    create(appointmentId: string, payload: DiagnosisPayload): Promise<Diagnosis> {
        return apiClient.post<Diagnosis>(`/api/v1/clinical-records/appointments/${appointmentId}/diagnoses`, payload);
    },
    update(id: string, payload: DiagnosisPayload): Promise<Diagnosis> {
        return apiClient.put<Diagnosis>(`/api/v1/clinical-records/diagnoses/${id}`, payload);
    },
    finalize(id: string): Promise<Diagnosis> {
        return apiClient.post<Diagnosis>(`/api/v1/clinical-records/diagnoses/${id}/finalize`, {});
    },
    correct(id: string, payload: DiagnosisPayload): Promise<Diagnosis> {
        return apiClient.post<Diagnosis>(`/api/v1/clinical-records/diagnoses/${id}/corrections`, payload);
    },
};
import { apiClient } from './apiClient';

export type MedicationStatus = 'Active' | 'Suspended' | 'Discontinued';

export interface PatientMedication {
    id: string; patientId: string; authorUserId: string; authorDoctorId: string; authorDisplayName: string;
    medicationName: string; strength?: string | null; dosageForm?: string | null; frequency?: string | null;
    route?: string | null; startDate?: string | null; indication?: string | null;
    status: MedicationStatus; suspendedAt?: string | null; discontinuedAt?: string | null; statusChangedByUserId?: string | null;
    isFinal: boolean; recordedAtUtc: string; finalizedAt?: string | null; finalizedByUserId?: string | null;
    correctsMedicationId?: string | null; correctsMedicationName?: string | null; createdAt: string;
}

export interface MedicationPayload {
    medicationName: string; strength?: string | null; dosageForm?: string | null; frequency?: string | null;
    route?: string | null; startDate?: string | null; indication?: string | null; recordedAtUtc?: string | null;
}

export const patientMedicationsApi = {
    listByPatient(patientId: string): Promise<PatientMedication[]> {
        return apiClient.get<PatientMedication[]>(`/api/v1/clinical-records/patients/${patientId}/medications`);
    },
    create(patientId: string, payload: MedicationPayload): Promise<PatientMedication> {
        return apiClient.post<PatientMedication>(`/api/v1/clinical-records/patients/${patientId}/medications`, payload);
    },
    update(id: string, payload: MedicationPayload): Promise<PatientMedication> {
        return apiClient.put<PatientMedication>(`/api/v1/clinical-records/medications/${id}`, payload);
    },
    finalize(id: string): Promise<PatientMedication> {
        return apiClient.post<PatientMedication>(`/api/v1/clinical-records/medications/${id}/finalize`, {});
    },
    suspend(id: string): Promise<PatientMedication> {
        return apiClient.post<PatientMedication>(`/api/v1/clinical-records/medications/${id}/suspend`, {});
    },
    resume(id: string): Promise<PatientMedication> {
        return apiClient.post<PatientMedication>(`/api/v1/clinical-records/medications/${id}/resume`, {});
    },
    discontinue(id: string): Promise<PatientMedication> {
        return apiClient.post<PatientMedication>(`/api/v1/clinical-records/medications/${id}/discontinue`, {});
    },
    correct(id: string, payload: MedicationPayload & { patientId: string }): Promise<PatientMedication> {
        return apiClient.post<PatientMedication>(`/api/v1/clinical-records/medications/${id}/corrections`, payload);
    },
};
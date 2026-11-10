import { apiClient } from './apiClient';

export type AllergenClass = 'Unknown' | 'Drug' | 'Food' | 'Latex' | 'Environmental' | 'InsectVenom' | 'Other';
export type AllergySeverity = 'Unknown' | 'Mild' | 'Moderate' | 'Severe';

export interface PatientAllergy {
    id: string; patientId: string; authorUserId: string; authorDoctorId: string; authorDisplayName: string;
    allergen: string; allergenClass: AllergenClass; severity: AllergySeverity; reaction?: string | null;
    onsetDate?: string | null; isResolved: boolean; resolvedAt?: string | null; resolvedByUserId?: string | null;
    isFinal: boolean; recordedAtUtc: string; finalizedAt?: string | null; finalizedByUserId?: string | null;
    correctsAllergyId?: string | null; correctsAllergyAllergen?: string | null; createdAt: string;
}

export interface AllergyPayload {
    allergen: string; allergenClass: AllergenClass; severity: AllergySeverity;
    reaction?: string | null; onsetDate?: string | null; recordedAtUtc?: string | null;
}

export const patientAllergiesApi = {
    listByPatient(patientId: string): Promise<PatientAllergy[]> {
        return apiClient.get<PatientAllergy[]>(`/api/v1/clinical-records/patients/${patientId}/allergies`);
    },
    create(patientId: string, payload: AllergyPayload): Promise<PatientAllergy> {
        return apiClient.post<PatientAllergy>(`/api/v1/clinical-records/patients/${patientId}/allergies`, payload);
    },
    update(id: string, payload: AllergyPayload): Promise<PatientAllergy> {
        return apiClient.put<PatientAllergy>(`/api/v1/clinical-records/allergies/${id}`, payload);
    },
    finalize(id: string): Promise<PatientAllergy> {
        return apiClient.post<PatientAllergy>(`/api/v1/clinical-records/allergies/${id}/finalize`, {});
    },
    resolve(id: string): Promise<PatientAllergy> {
        return apiClient.post<PatientAllergy>(`/api/v1/clinical-records/allergies/${id}/resolve`, {});
    },
    unresolve(id: string): Promise<PatientAllergy> {
        return apiClient.post<PatientAllergy>(`/api/v1/clinical-records/allergies/${id}/unresolve`, {});
    },
    correct(id: string, payload: AllergyPayload & { patientId: string }): Promise<PatientAllergy> {
        return apiClient.post<PatientAllergy>(`/api/v1/clinical-records/allergies/${id}/corrections`, payload);
    },
};
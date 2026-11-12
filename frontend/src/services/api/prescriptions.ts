import { apiClient } from './apiClient';

export type PrescriptionStatus =
    | 'Draft'
    | 'Issued'
    | 'Dispensed'
    | 'Expired'
    | 'Cancelled';

export interface Prescription {
    id: string;
    patientId: string;
    authorUserId: string;
    authorDoctorId: string;
    authorDisplayName: string;

    medicationName: string;
    strength?: string | null;
    dosageForm?: string | null;
    frequency?: string | null;
    route?: string | null;
    quantity?: number | null;
    refillsAllowed?: number | null;
    daysSupply?: number | null;
    instructions?: string | null;
    note?: string | null;

    status: PrescriptionStatus;
    isFinal: boolean;

    recordedAtUtc: string;
    issuedAt?: string | null;
    issuedByUserId?: string | null;
    cancelledAt?: string | null;
    cancelledByUserId?: string | null;
    dispensedAt?: string | null;
    dispensedByUserId?: string | null;
    expiredAt?: string | null;

    correctsPrescriptionId?: string | null;
    correctsPrescriptionName?: string | null;
    renewedFromPrescriptionId?: string | null;

    createdAt: string;
}

export interface PrescriptionPayload {
    medicationName: string;
    strength?: string | null;
    dosageForm?: string | null;
    frequency?: string | null;
    route?: string | null;
    quantity?: number | null;
    refillsAllowed?: number | null;
    daysSupply?: number | null;
    instructions?: string | null;
    note?: string | null;
    recordedAtUtc?: string | null;
}

export const prescriptionsApi = {
    listByPatient(patientId: string): Promise<Prescription[]> {
        return apiClient.get<Prescription[]>(
            `/api/v1/clinical-records/patients/${patientId}/prescriptions`,
        );
    },

    create(patientId: string, payload: PrescriptionPayload): Promise<Prescription> {
        return apiClient.post<Prescription>(
            `/api/v1/clinical-records/patients/${patientId}/prescriptions`,
            payload,
        );
    },

    update(id: string, payload: PrescriptionPayload): Promise<Prescription> {
        return apiClient.put<Prescription>(
            `/api/v1/clinical-records/prescriptions/${id}`,
            payload,
        );
    },

    issue(id: string): Promise<Prescription> {
        return apiClient.post<Prescription>(
            `/api/v1/clinical-records/prescriptions/${id}/issue`,
            {},
        );
    },

    renew(id: string): Promise<Prescription> {
        return apiClient.post<Prescription>(
            `/api/v1/clinical-records/prescriptions/${id}/renew`,
            {},
        );
    },

    cancel(id: string): Promise<Prescription> {
        return apiClient.post<Prescription>(
            `/api/v1/clinical-records/prescriptions/${id}/cancel`,
            {},
        );
    },

    dispense(id: string): Promise<Prescription> {
        return apiClient.post<Prescription>(`/api/v1/clinical-records/prescriptions/${id}/dispense`, {});
    },

    expire(id: string): Promise<Prescription> {
        return apiClient.post<Prescription>(`/api/v1/clinical-records/prescriptions/${id}/expire`, {});
    },

    correct(
        id: string,
        payload: PrescriptionPayload & { patientId: string },
    ): Promise<Prescription> {
        return apiClient.post<Prescription>(
            `/api/v1/clinical-records/prescriptions/${id}/corrections`,
            payload,
        );
    },
};

import { apiClient } from './apiClient';

export type TemperatureMethod = 'Other' | 'Oral' | 'Axillary' | 'Tympanic' | 'Forehead' | 'Rectal';
export type Position = 'Other' | 'Sitting' | 'Standing' | 'LyingDown';

export interface VitalSigns {
    id: string;
    appointmentId: string;
    authorUserId: string;
    authorDoctorId: string;
    authorDisplayName: string;
    systolicmmHg?: number | null;
    diastolicmmHg?: number | null;
    heartRateBpm?: number | null;
    temperatureC?: number | null;
    temperatureMethod?: TemperatureMethod | null;
    position?: Position | null;
    respiratoryRatePerMin?: number | null;
    spo2Percent?: number | null;
    weightKg?: number | null;
    heightCm?: number | null;
    painScore0to10?: number | null;
    note?: string | null;
    isFinal: boolean;
    recordedAtUtc: string;
    finalizedAt?: string | null;
    finalizedByUserId?: string | null;
    correctsVitalSignsId?: string | null;
    correctsVitalSignsTitle?: string | null;
    createdAt: string;
}

export interface VitalSignsPayload {
    systolicmmHg?: number | null;
    diastolicmmHg?: number | null;
    heartRateBpm?: number | null;
    temperatureC?: number | null;
    temperatureMethod?: TemperatureMethod | null;
    position?: Position | null;
    respiratoryRatePerMin?: number | null;
    spo2Percent?: number | null;
    weightKg?: number | null;
    heightCm?: number | null;
    painScore0to10?: number | null;
    note?: string | null;
    recordedAtUtc?: string | null;
}

export const vitalSignsApi = {
    listByAppointment(appointmentId: string): Promise<VitalSigns[]> {
        return apiClient.get<VitalSigns[]>(`/api/v1/clinical-records/appointments/${appointmentId}/vital-signs`);
    },
    create(appointmentId: string, payload: VitalSignsPayload): Promise<VitalSigns> {
        return apiClient.post<VitalSigns>(`/api/v1/clinical-records/appointments/${appointmentId}/vital-signs`, payload);
    },
    update(id: string, payload: VitalSignsPayload): Promise<VitalSigns> {
        return apiClient.put<VitalSigns>(`/api/v1/clinical-records/vital-signs/${id}`, payload);
    },
    finalize(id: string): Promise<VitalSigns> {
        return apiClient.post<VitalSigns>(`/api/v1/clinical-records/vital-signs/${id}/finalize`, {});
    },
    correct(id: string, payload: VitalSignsPayload): Promise<VitalSigns> {
        return apiClient.post<VitalSigns>(`/api/v1/clinical-records/vital-signs/${id}/corrections`, payload);
    },
};
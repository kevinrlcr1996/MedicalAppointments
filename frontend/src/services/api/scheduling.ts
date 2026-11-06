import { apiClient } from './apiClient';
import { crudApi, type PaginatedResponse, type QueryParams } from './crudApi';

export interface ScheduleDay {
    dayOfWeek: number | string;
    startTime: string;
    endTime: string;
}

export interface Schedule {
    id: string;
    doctorId: string;
    clinicId: string;
    name: string;
    isDefault: boolean;
    isActive: boolean;
    effectiveFrom: string;
    effectiveTo?: string | null;
    slotDurationMinutes: number;
    bufferMinutes: number;
    createdAt: string;
    updatedAt: string;
    days: ScheduleDay[];
}

export interface AvailabilitySlot {
    doctorId: string;
    clinicId: string;
    date: string;
    startAtUtc: string;
    endAtUtc: string;
    startLocal: string;
    endLocal: string;
    durationMinutes: number;
    timeZoneId: string;
}

export interface AvailabilityDay {
    doctorId: string;
    clinicId: string;
    date: string;
    timeZoneId: string;
    slots: AvailabilitySlot[];
}

export const schedulingApi = {
    listSchedules(params: QueryParams): Promise<PaginatedResponse<Schedule>> {
        return crudApi.list<Schedule>('/api/v1/doctor-schedules', params);
    },

    getSchedule(id: string): Promise<Schedule> {
        return crudApi.get<Schedule>('/api/v1/doctor-schedules', id);
    },

    createSchedule(body: unknown): Promise<Schedule> {
        return crudApi.create<Schedule>('/api/v1/doctor-schedules', body);
    },

    updateSchedule(id: string, body: unknown): Promise<Schedule> {
        return crudApi.update<Schedule>('/api/v1/doctor-schedules', id, body);
    },

    setScheduleStatus(id: string, isActive: boolean): Promise<Schedule> {
        return crudApi.setStatus<Schedule>('/api/v1/doctor-schedules', id, isActive);
    },

    getAvailability(doctorId: string, clinicId: string, date: string): Promise<AvailabilityDay> {
        return apiClient.get<AvailabilityDay>(
            `/api/v1/availability/doctors/${doctorId}/clinics/${clinicId}?date=${encodeURIComponent(date)}`,
        );
    },
};
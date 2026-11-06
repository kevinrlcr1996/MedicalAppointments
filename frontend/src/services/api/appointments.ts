import { apiClient } from './apiClient';
import type { PaginatedResponse, QueryParams } from './crudApi';

export type AppointmentStatus =
  | 'Pending'
  | 'Confirmed'
  | 'CheckedIn'
  | 'InProgress'
  | 'Completed'
  | 'Cancelled'
  | 'NoShow'
  | 'Rescheduled';

export interface Appointment {
  id: string;
  patientId: string;
  patientFullName: string;
  doctorId: string;
  doctorFullName: string;
  clinicId: string;
  clinicName: string;
  roomId?: string | null;
  roomName?: string | null;
  localDate: string;
  startAtUtc: string;
  endAtUtc: string;
  status: AppointmentStatus;
  reason?: string | null;
  notes?: string | null;
  originalAppointmentId?: string | null;
  cancelledAt?: string | null;
  cancelledByUserId?: string | null;
  cancellationReason?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAppointmentPayload {
  patientId: string;
  doctorId: string;
  clinicId: string;
  roomId?: string | null;
  localDate: string;
  startAtUtc: string;
  endAtUtc: string;
  reason?: string | null;
  notes?: string | null;
}

export interface RescheduleAppointmentPayload {
  newLocalDate: string;
  newStartAtUtc: string;
  newEndAtUtc: string;
  reason?: string | null;
}

export interface RescheduleAppointmentResponse {
  oldAppointment: Appointment;
  newAppointment: Appointment;
}

function idempotencyHeaders(key: string): HeadersInit {
  return {
    'Idempotency-Key': key,
  };
}

export const appointmentsApi = {
  list(params: QueryParams): Promise<PaginatedResponse<Appointment>> {
    return apiClient.get<PaginatedResponse<Appointment>>(
      `/api/v1/appointments${buildQuery(params)}`,
    );
  },

  get(id: string): Promise<Appointment> {
    return apiClient.get<Appointment>(`/api/v1/appointments/${id}`);
  },

  create(payload: CreateAppointmentPayload, idempotencyKey: string): Promise<Appointment> {
    return apiClient.post<Appointment>('/api/v1/appointments', payload, {
      headers: idempotencyHeaders(idempotencyKey),
    });
  },

  cancel(id: string, reason: string, idempotencyKey: string): Promise<Appointment> {
    return apiClient.post<Appointment>(`/api/v1/appointments/${id}/cancel`, { reason }, {
      headers: idempotencyHeaders(idempotencyKey),
    });
  },

  reschedule(
    id: string,
    payload: RescheduleAppointmentPayload,
    idempotencyKey: string,
  ): Promise<RescheduleAppointmentResponse> {
    return apiClient.post<RescheduleAppointmentResponse>(`/api/v1/appointments/${id}/reschedule`, payload, {
      headers: idempotencyHeaders(idempotencyKey),
    });
  },

  confirm(id: string): Promise<Appointment> {
    return apiClient.post<Appointment>(`/api/v1/appointments/${id}/confirm`, {});
  },

  checkIn(id: string): Promise<Appointment> {
    return apiClient.post<Appointment>(`/api/v1/appointments/${id}/check-in`, {});
  },

  start(id: string): Promise<Appointment> {
    return apiClient.post<Appointment>(`/api/v1/appointments/${id}/start`, {});
  },

  complete(id: string): Promise<Appointment> {
    return apiClient.post<Appointment>(`/api/v1/appointments/${id}/complete`, {});
  },

  markNoShow(id: string): Promise<Appointment> {
    return apiClient.post<Appointment>(`/api/v1/appointments/${id}/no-show`, {});
  },
};

function buildQuery(params: QueryParams): string {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value === null || value === undefined || value === '') {
      continue;
    }

    searchParams.append(key, String(value));
  }

  const query = searchParams.toString();

  return query ? `?${query}` : '';
}
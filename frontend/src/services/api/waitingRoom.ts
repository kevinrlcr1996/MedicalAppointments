import { apiClient } from './apiClient';

export type WaitingRoomCallStatus = 'Queued' | 'Called' | 'InConsultation' | 'Skipped' | 'Completed';

export interface WaitingRoomCard {
    callId: string;
    appointmentId: string;
    sequenceNumber: number;
    patientId: string;
    patientFullName: string;
    doctorId: string;
    doctorFullName: string;
    clinicId: string;
    clinicName: string;
    roomId?: string | null;
    roomName?: string | null;
    localDate: string;
    status: WaitingRoomCallStatus;
    recallCount: number;
    calledAt?: string | null;
    answeredAt?: string | null;
    skippedAt?: string | null;
    completedAt?: string | null;
    appointmentStartAtUtc: string;
    queuedAt: string;
    note?: string | null;
}

export interface WaitingRoomBoard {
    doctorId: string;
    doctorFullName: string;
    clinicId: string;
    clinicName: string;
    localDate: string;
    queued: WaitingRoomCard[];
    called: WaitingRoomCard[];
    inConsultation: WaitingRoomCard[];
    skipped: WaitingRoomCard[];
    completed: WaitingRoomCard[];
}

export interface PatientCalledPayload {
    callId: string;
    appointmentId: string;
    patientId: string;
    patientUserId?: string | null;
    patientFullName: string;
    sequenceNumber: number;
    doctorId: string;
    doctorFullName: string;
    clinicId: string;
    roomId?: string | null;
    roomName?: string | null;
    occurredAt: string;
    correlationId?: string | null;
}

export const waitingRoomApi = {
    getBoard(doctorId: string, clinicId: string, localDate: string): Promise<WaitingRoomBoard> {
        const params = new URLSearchParams({ doctorId, clinicId, localDate });
        return apiClient.get<WaitingRoomBoard>(`/api/v1/waiting-room/board?${params.toString()}`);
    },
    checkInToRoom(appointmentId: string, note?: string | null): Promise<WaitingRoomCard> {
        const params = new URLSearchParams({ appointmentId });
        return apiClient.post<WaitingRoomCard>(`/api/v1/waiting-room/check-in?${params.toString()}`, { note: note ?? null });
    },
    callNext(doctorId: string, clinicId: string, localDate: string, roomId?: string | null): Promise<WaitingRoomCard> {
        return apiClient.post<WaitingRoomCard>('/api/v1/waiting-room/call-next', { doctorId, clinicId, localDate, roomId: roomId ?? null });
    },
    answer(callId: string): Promise<WaitingRoomCard> {
        return apiClient.post<WaitingRoomCard>(`/api/v1/waiting-room/${callId}/answer`, {});
    },
    skip(callId: string, reason?: string | null): Promise<WaitingRoomCard> {
        return apiClient.post<WaitingRoomCard>(`/api/v1/waiting-room/${callId}/skip`, { reason: reason ?? null });
    },
    recall(callId: string): Promise<WaitingRoomCard> {
        return apiClient.post<WaitingRoomCard>(`/api/v1/waiting-room/${callId}/recall`, {});
    },
    complete(callId: string): Promise<WaitingRoomCard> {
        return apiClient.post<WaitingRoomCard>(`/api/v1/waiting-room/${callId}/complete`, {});
    },
};
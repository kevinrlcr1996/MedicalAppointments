import { apiClient } from './apiClient';
import type { PaginatedResponse, QueryParams } from './crudApi';

export type WaitingListEntryStatus = 'Waiting' | 'Fulfilled' | 'Cancelled';
export type WaitingListOfferStatus = 'Pending' | 'Accepted' | 'Declined' | 'Expired';
export type WaitingListPriority = 'Low' | 'Normal' | 'High' | 'Urgent';

export interface WaitingListEntry {
    id: string;
    patientId: string;
    patientFullName: string;
    doctorId: string;
    doctorFullName: string;
    clinicId?: string | null;
    clinicName?: string | null;
    preferredFromUtc: string;
    preferredToUtc: string;
    priority: WaitingListPriority;
    status: WaitingListEntryStatus;
    note?: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface WaitingListOffer {
    id: string;
    entryId: string;
    patientId: string;
    doctorId: string;
    doctorFullName: string;
    clinicId?: string | null;
    clinicName?: string | null;
    roomId?: string | null;
    localDate: string;
    slotStartUtc: string;
    slotEndUtc: string;
    status: WaitingListOfferStatus;
    expiresAt: string;
    respondedAt?: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface CreateEntryPayload {
    patientId: string;
    doctorId: string;
    clinicId?: string | null;
    preferredFromUtc: string;
    preferredToUtc: string;
    priority: WaitingListPriority;
    note?: string | null;
}

function buildQuery(params: QueryParams): string {
    const searchParams = new URLSearchParams();
    for (const [key, value] of Object.entries(params)) {
        if (value === null || value === undefined || value === '') continue;
        searchParams.append(key, String(value));
    }
    const query = searchParams.toString();
    return query ? `?${query}` : '';
}

export const waitingListApi = {
    listEntries(params: QueryParams): Promise<PaginatedResponse<WaitingListEntry>> {
        return apiClient.get<PaginatedResponse<WaitingListEntry>>(`/api/v1/waiting-list/entries${buildQuery(params)}`);
    },
    createEntry(payload: CreateEntryPayload): Promise<WaitingListEntry> {
        return apiClient.post<WaitingListEntry>('/api/v1/waiting-list/entries', payload);
    },
    cancelEntry(id: string): Promise<WaitingListEntry> {
        return apiClient.delete<WaitingListEntry>(`/api/v1/waiting-list/entries/${id}`);
    },
    listOffers(params: QueryParams): Promise<PaginatedResponse<WaitingListOffer>> {
        return apiClient.get<PaginatedResponse<WaitingListOffer>>(`/api/v1/waiting-list/offers${buildQuery(params)}`);
    },
    respondOffer(id: string, accept: boolean): Promise<WaitingListOffer> {
        return apiClient.post<WaitingListOffer>(`/api/v1/waiting-list/offers/${id}/respond`, { accept });
    },
};
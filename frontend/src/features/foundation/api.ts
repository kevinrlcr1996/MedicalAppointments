import { apiClient } from '../../services/api/apiClient';

export interface PublishTestEventResponse {
  outboxMessageId: string;
  status: string;
}

export async function publishTestEvent(message: string): Promise<PublishTestEventResponse> {
  return apiClient.post<PublishTestEventResponse>('/api/v1/foundation/realtime/test', {
    message,
  });
}
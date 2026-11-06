export type ConnectionState =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'reconnected'
  | 'error';

export interface FoundationTestEventPayload {
  id: string;
  message: string;
  occurredAt: string;
  correlationId?: string | null;
}

export interface ScheduleChangedPayload {
  scheduleId: string;
  doctorId: string;
  clinicId: string;
  changeType: string;
  affectedFrom: string;
  affectedTo: string;
  occurredAt: string;
  correlationId?: string | null;
}

export interface AvailabilityChangedPayload {
  doctorId: string;
  clinicId: string;
  affectedFrom: string;
  affectedTo: string;
  occurredAt: string;
  correlationId?: string | null;
}
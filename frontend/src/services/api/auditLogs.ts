import { apiClient } from './apiClient';

export interface AuditLogItem {
  id: string;
  userId: string | null;
  action: string;
  entityName: string;
  entityId: string;
  timestamp: string;
  correlationId: string | null;
}

export interface AuditLogPage {
  items: AuditLogItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuditLogFilters {
  fromDate: string;
  toDate: string;
  page: number;
  pageSize: number;
  action?: string;
  entityName?: string;
  userId?: string;
  entityId?: string;
}

export const auditLogsApi = {
  list(filters: AuditLogFilters): Promise<AuditLogPage> {
    const query = new URLSearchParams();
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== '') query.set(key, String(value));
    });
    return apiClient.get(`/api/v1/audit-logs?${query.toString()}`);
  },
};

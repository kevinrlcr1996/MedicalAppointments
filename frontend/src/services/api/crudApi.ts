import { apiClient } from './apiClient';

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type QueryParams = Record<string, string | number | boolean | null | undefined>;

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

export const crudApi = {
  list<T>(endpoint: string, params: QueryParams): Promise<PaginatedResponse<T>> {
    return apiClient.get<PaginatedResponse<T>>(`${endpoint}${buildQuery(params)}`);
  },

  get<T>(endpoint: string, id: string): Promise<T> {
    return apiClient.get<T>(`${endpoint}/${id}`);
  },

  create<T>(endpoint: string, body: unknown): Promise<T> {
    return apiClient.post<T>(endpoint, body);
  },

  update<T>(endpoint: string, id: string, body: unknown): Promise<T> {
    return apiClient.put<T>(`${endpoint}/${id}`, body);
  },

  setStatus<T>(endpoint: string, id: string, isActive: boolean): Promise<T> {
    return apiClient.patch<T>(`${endpoint}/${id}/status`, { isActive });
  },
};
import { apiClient } from './apiClient';
import type { PaginatedResponse } from './crudApi';

export interface User {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  isEmailConfirmed: boolean;
  lockoutEnd: string | null;
  roles: string[];
}

export interface CreateUserPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

const usersPath = '/api/v1/users';

export const usersApi = {
  list(params: { page: number; pageSize: number; search?: string }): Promise<PaginatedResponse<User>> {
    const query = new URLSearchParams({ page: String(params.page), pageSize: String(params.pageSize) });
    if (params.search) query.set('search', params.search);
    return apiClient.get<PaginatedResponse<User>>(`${usersPath}?${query.toString()}`);
  },

  create(payload: CreateUserPayload): Promise<User> {
    return apiClient.post<User>(usersPath, payload);
  },

  assignRoles(id: string, roles: string[]): Promise<User> {
    return apiClient.put<User>(`${usersPath}/${id}/roles`, { roles });
  },

  unlock(id: string): Promise<User> {
    return apiClient.post<User>(`${usersPath}/${id}/unlock`, {});
  },
};

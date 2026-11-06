import { apiClient } from './apiClient';
import type { AuthenticationResponse, CurrentUser } from './authService';

export interface LoginPayload {
  email: string;
  password: string;
}

export interface RegisterPayload {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
}

export interface ForgotPasswordPayload {
  email: string;
}

export interface ResetPasswordPayload {
  token: string;
  password: string;
}

export interface ConfirmEmailPayload {
  token: string;
}

export interface SimpleResponse {
  success: boolean;
  message: string;
}

export interface RegistrationResponse {
  accepted: boolean;
  message: string;
}

export async function login(payload: LoginPayload): Promise<AuthenticationResponse> {
  return apiClient.post<AuthenticationResponse>('/api/v1/auth/login', payload, {
    skipAuthRefresh: true,
  });
}

export async function register(payload: RegisterPayload): Promise<RegistrationResponse> {
  return apiClient.post<RegistrationResponse>('/api/v1/auth/register', payload, {
    skipAuthRefresh: true,
  });
}

export async function logout(): Promise<SimpleResponse> {
  return apiClient.post<SimpleResponse>('/api/v1/auth/logout', {}, {
    skipAuthRefresh: true,
  });
}

export async function forgotPassword(payload: ForgotPasswordPayload): Promise<SimpleResponse> {
  return apiClient.post<SimpleResponse>('/api/v1/auth/forgot-password', payload, {
    skipAuthRefresh: true,
  });
}

export async function resetPassword(payload: ResetPasswordPayload): Promise<SimpleResponse> {
  return apiClient.post<SimpleResponse>('/api/v1/auth/reset-password', payload, {
    skipAuthRefresh: true,
  });
}

export async function confirmEmail(payload: ConfirmEmailPayload): Promise<SimpleResponse> {
  return apiClient.post<SimpleResponse>('/api/v1/auth/confirm-email', payload, {
    skipAuthRefresh: true,
  });
}

export async function getCurrentUser(): Promise<CurrentUser> {
  return apiClient.get<CurrentUser>('/api/v1/auth/me');
}
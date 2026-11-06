import { apiClient } from './apiClient';

export interface Patient {
  id: string;
  userId?: string | null;
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  gender: string;
  identificationNumber?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  emergencyContact?: string | null;
  emergencyPhone?: string | null;
  bloodType: string;
  insuranceProvider?: string | null;
  insuranceNumber?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SelfPatientPayload {
  firstName: string;
  lastName: string;
  dateOfBirth?: string | null;
  gender: string;
  identificationNumber?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  emergencyContact?: string | null;
  emergencyPhone?: string | null;
  bloodType: string;
  insuranceProvider?: string | null;
  insuranceNumber?: string | null;
}

export const patientsApi = {
  getMe(): Promise<Patient> {
    return apiClient.get<Patient>('/api/v1/patients/me');
  },

  createSelf(payload: SelfPatientPayload): Promise<Patient> {
    return apiClient.post<Patient>('/api/v1/patients/me', payload);
  },

  updateSelf(payload: SelfPatientPayload): Promise<Patient> {
    return apiClient.put<Patient>('/api/v1/patients/me', payload);
  },
};
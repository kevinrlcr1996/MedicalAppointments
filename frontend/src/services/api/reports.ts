import { apiClient } from './apiClient';

export interface AppointmentReport {
  fromDate: string;
  toDate: string;
  totalAppointments: number;
  cancelledAppointments: number;
  noShowAppointments: number;
  newPatients: number;
  availableMinutes: number;
  reservedMinutes: number;
  occupancyPercentage: number | null;
  appointmentRequests: number;
  byStatus: { status: string; count: number }[];
  byDay: { dateUtc: string; count: number }[];
  byDoctor: ReportGroup[];
  bySpecialty: ReportGroup[];
}

export interface ReportGroup {
  id: string;
  name: string;
  count: number;
}

export const reportsApi = {
  appointments(fromDate: string, toDate: string): Promise<AppointmentReport> {
    const query = new URLSearchParams({ fromDate, toDate });
    return apiClient.get(`/api/v1/reports/appointments?${query.toString()}`);
  },

  exportAppointments(fromDate: string, toDate: string): Promise<Blob> {
    const query = new URLSearchParams({ fromDate, toDate });
    return apiClient.downloadBlob(`/api/v1/reports/appointments/export?${query.toString()}`);
  },
};

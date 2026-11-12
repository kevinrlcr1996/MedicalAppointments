import { apiClient } from './apiClient';

export interface ClinicalDocument {
  id: string;
  prescriptionId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  generatedByUserId: string;
  generatedAt: string;
}

export const clinicalDocumentsApi = {
  listByPrescription(prescriptionId: string): Promise<ClinicalDocument[]> {
    return apiClient.get(`/api/v1/clinical-records/prescriptions/${prescriptionId}/documents`);
  },

  generateForPrescription(prescriptionId: string): Promise<ClinicalDocument> {
    return apiClient.post(`/api/v1/clinical-records/prescriptions/${prescriptionId}/documents`, {});
  },

  download(documentId: string): Promise<Blob> {
    return apiClient.downloadBlob(`/api/v1/clinical-records/documents/${documentId}/download`);
  },
};

import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Download, FileText } from 'lucide-react';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Spinner } from '../../components/ui/Spinner';
import { clinicalDocumentsApi, type ClinicalDocument } from '../../services/api/clinicalDocuments';
import { ApiError } from '../../services/api/apiClient';
import { formatDateTime } from '../../utils/formatDate';

export function PrescriptionDocumentsPanel({
  prescriptionId,
  canGenerate,
}: {
  prescriptionId: string;
  canGenerate: boolean;
}) {
  const queryClient = useQueryClient();
  const [downloadingId, setDownloadingId] = useState<string | null>(null);
  const [downloadError, setDownloadError] = useState<string | null>(null);
  const query = useQuery({
    queryKey: ['clinicalDocuments', prescriptionId],
    queryFn: () => clinicalDocumentsApi.listByPrescription(prescriptionId),
  });
  const generateMutation = useMutation({
    mutationFn: () => clinicalDocumentsApi.generateForPrescription(prescriptionId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['clinicalDocuments', prescriptionId] });
    },
  });

  const download = async (clinicalDocument: ClinicalDocument) => {
    setDownloadingId(clinicalDocument.id);
    try {
      const blob = await clinicalDocumentsApi.download(clinicalDocument.id);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = clinicalDocument.fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.setTimeout(() => URL.revokeObjectURL(url), 1000);
      setDownloadError(null);
    } catch (error) {
      setDownloadError(error instanceof ApiError ? error.message : 'No se pudo descargar el documento.');
    } finally {
      setDownloadingId(null);
    }
  };

  return (
    <section className="mt-3 border-t border-slate-200 pt-3 dark:border-slate-800" aria-label="Documentos de receta">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h4 className="flex items-center gap-2 text-sm font-semibold text-slate-800 dark:text-slate-200">
          <FileText className="h-4 w-4" /> Documentos
        </h4>
        {canGenerate ? (
          <Button
            size="sm"
            variant="secondary"
            loading={generateMutation.isPending}
            onClick={() => generateMutation.mutate()}
            leftIcon={<FileText className="h-3 w-3" />}
          >
            Generar documento
          </Button>
        ) : null}
      </div>

      {query.isLoading ? <Spinner label="Cargando documentos..." /> : null}
      {query.isError ? (
        <Alert variant="error" title="No se pudieron cargar los documentos">
          {query.error instanceof ApiError ? query.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}
      {generateMutation.isError ? (
        <Alert variant="error" title="No se pudo generar el documento">
          {generateMutation.error instanceof ApiError ? generateMutation.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}
      {downloadError ? (
        <Alert variant="error" title="No se pudo descargar el documento">{downloadError}</Alert>
      ) : null}
      {query.isSuccess && query.data.length === 0 ? (
        <p className="mt-2 text-xs text-slate-500 dark:text-slate-400">Aún no se han generado documentos.</p>
      ) : null}
      {query.data?.length ? (
        <ul className="mt-2 space-y-2">
          {query.data.map((clinicalDocument) => (
            <li key={clinicalDocument.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-slate-50 px-3 py-2 dark:bg-slate-900">
              <div className="min-w-0">
                <p className="truncate text-sm text-slate-800 dark:text-slate-200">{clinicalDocument.fileName}</p>
                <p className="text-xs text-slate-500 dark:text-slate-400">
                  {formatDateTime(clinicalDocument.generatedAt)} · {(clinicalDocument.sizeBytes / 1024).toFixed(1)} KB
                </p>
              </div>
              <Button
                size="sm"
                variant="secondary"
                loading={downloadingId === clinicalDocument.id}
                onClick={() => void download(clinicalDocument)}
                leftIcon={<Download className="h-3 w-3" />}
              >
                Descargar
              </Button>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}

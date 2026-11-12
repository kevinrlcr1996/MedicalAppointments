import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { BarChart3, Download, RefreshCw } from 'lucide-react';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { PageHeader } from '../../components/ui/PageHeader';
import { Spinner } from '../../components/ui/Spinner';
import { ApiError } from '../../services/api/apiClient';
import { reportsApi, type ReportGroup } from '../../services/api/reports';

function utcToday(): string {
  return new Date().toISOString().slice(0, 10);
}

function utcDaysAgo(days: number): string {
  const date = new Date();
  date.setUTCDate(date.getUTCDate() - days);
  return date.toISOString().slice(0, 10);
}

const statusLabels: Record<string, string> = {
  Pending: 'Pendiente',
  Confirmed: 'Confirmada',
  CheckedIn: 'En recepción',
  InProgress: 'En consulta',
  Completed: 'Completada',
  Cancelled: 'Cancelada',
  NoShow: 'Ausencia',
  Rescheduled: 'Reprogramada',
};

function Breakdown({ title, groups }: { title: string; groups: ReportGroup[] }) {
  return (
    <Card>
      <h2 className="font-semibold text-slate-900 dark:text-slate-50">{title}</h2>
      {groups.length === 0 ? (
        <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">Sin datos en este período.</p>
      ) : (
        <ul className="mt-3 divide-y divide-slate-200 dark:divide-slate-800">
          {groups.map((item) => (
            <li key={item.id} className="flex items-center justify-between gap-4 py-2 text-sm">
              <span className="text-slate-700 dark:text-slate-300">{item.name}</span>
              <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">{item.count}</span>
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

export function ReportsPage() {
  const [fromDate, setFromDate] = useState(() => utcDaysAgo(29));
  const [toDate, setToDate] = useState(utcToday);
  const validRange = Boolean(fromDate && toDate && fromDate <= toDate);
  const [isExporting, setIsExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);
  const report = useQuery({
    queryKey: ['reports', 'appointments', fromDate, toDate],
    queryFn: () => reportsApi.appointments(fromDate, toDate),
    enabled: validRange,
  });

  async function exportCsv() {
    setIsExporting(true);
    setExportError(null);
    try {
      const blob = await reportsApi.exportAppointments(fromDate, toDate);
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `citas-${fromDate}-${toDate}.csv`;
      document.body.append(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
    } catch (error) {
      setExportError(error instanceof ApiError ? error.message : 'No se pudo exportar el reporte.');
    } finally {
      setIsExporting(false);
    }
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Reportes de citas"
        description="Resumen agregado por fechas UTC. Las cancelaciones se agrupan según la fecha original de la cita."
      />

      <Card>
        <div className="grid gap-3 sm:grid-cols-[1fr_1fr_auto_auto] sm:items-end">
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 dark:text-slate-300">
            Desde (UTC)
            <Input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} />
          </label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 dark:text-slate-300">
            Hasta (UTC)
            <Input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} />
          </label>
          <Button variant="secondary" onClick={() => void report.refetch()} disabled={!validRange} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Actualizar
          </Button>
          <Button variant="secondary" onClick={() => void exportCsv()} disabled={!validRange || isExporting} leftIcon={<Download className="h-4 w-4" />}>
            {isExporting ? 'Exportando...' : 'Exportar CSV'}
          </Button>
        </div>
        {!validRange ? <p className="mt-2 text-sm text-rose-600">La fecha inicial debe ser anterior o igual a la fecha final.</p> : null}
      </Card>

      {exportError ? <Alert variant="error" title="No se pudo exportar el reporte">{exportError}</Alert> : null}

      {report.isError ? (
        <Alert variant="error" title="No se pudieron cargar los reportes">
          {report.error instanceof ApiError ? report.error.message : 'Error inesperado.'}
        </Alert>
      ) : null}

      {report.isLoading ? <Spinner label="Cargando reportes..." /> : null}

      {report.data ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {[
              { label: 'Citas en el período', value: report.data.totalAppointments },
              { label: 'Canceladas', value: report.data.cancelledAppointments },
              { label: 'Ausencias', value: report.data.noShowAppointments },
              { label: 'Pacientes nuevos', value: report.data.newPatients },
              { label: 'Solicitudes de cita', value: report.data.appointmentRequests },
              { label: 'Ocupación de agenda', value: report.data.occupancyPercentage === null ? 'Sin agenda' : `${report.data.occupancyPercentage.toFixed(1)}%` },
            ].map((metric) => (
              <Card key={metric.label}>
                <p className="text-sm text-slate-500 dark:text-slate-400">{metric.label}</p>
                <p className="mt-2 text-3xl font-semibold tabular-nums text-slate-900 dark:text-slate-50">{metric.value}</p>
              </Card>
            ))}
          </div>
          <p className="-mt-4 text-xs text-slate-500 dark:text-slate-400">Ocupación = minutos reservados / minutos disponibles antes de citas. Canceladas y reprogramadas no reservan minutos; ausencias sí. Solicitudes = citas creadas más registros de lista de espera, en el período.</p>

          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <h2 className="flex items-center gap-2 font-semibold text-slate-900 dark:text-slate-50"><BarChart3 className="h-4 w-4" /> Citas por estado</h2>
              {report.data.byStatus.length === 0 ? (
                <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">Sin datos en este período.</p>
              ) : (
                <ul className="mt-3 divide-y divide-slate-200 dark:divide-slate-800">
                  {report.data.byStatus.map((item) => (
                    <li key={item.status} className="flex justify-between py-2 text-sm">
                      <span className="text-slate-700 dark:text-slate-300">{statusLabels[item.status] ?? item.status}</span>
                      <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">{item.count}</span>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
            <Card>
              <h2 className="font-semibold text-slate-900 dark:text-slate-50">Citas por día (UTC)</h2>
              {report.data.byDay.length === 0 ? (
                <p className="mt-3 text-sm text-slate-500 dark:text-slate-400">Sin datos en este período.</p>
              ) : (
                <ul className="mt-3 divide-y divide-slate-200 dark:divide-slate-800">
                  {report.data.byDay.map((item) => (
                    <li key={item.dateUtc} className="flex justify-between py-2 text-sm">
                      <time dateTime={item.dateUtc} className="text-slate-700 dark:text-slate-300">{item.dateUtc}</time>
                      <span className="font-semibold tabular-nums text-slate-900 dark:text-slate-100">{item.count}</span>
                    </li>
                  ))}
                </ul>
              )}
            </Card>
            <Breakdown title="Citas por médico" groups={report.data.byDoctor} />
            <div className="space-y-2">
              <Breakdown title="Citas por especialidad" groups={report.data.bySpecialty} />
              <p className="text-xs text-slate-500 dark:text-slate-400">Si un médico tiene varias especialidades, cada cita se cuenta en cada una de ellas.</p>
            </div>
          </div>
        </>
      ) : null}
    </div>
  );
}

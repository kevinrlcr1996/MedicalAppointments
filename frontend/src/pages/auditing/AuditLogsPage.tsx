import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { RefreshCw, ShieldCheck } from 'lucide-react';
import { Alert } from '../../components/ui/Alert';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pagination } from '../../components/ui/Pagination';
import { Spinner } from '../../components/ui/Spinner';
import { ApiError } from '../../services/api/apiClient';
import { auditLogsApi } from '../../services/api/auditLogs';

function utcDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function initialFromDate(): string {
  const date = new Date();
  date.setUTCDate(date.getUTCDate() - 6);
  return utcDate(date);
}

const isGuid = (value: string) => !value || /^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
const utcDateTimeFormatter = new Intl.DateTimeFormat('es-CR', {
  dateStyle: 'medium', timeStyle: 'medium', hour12: true, timeZone: 'UTC',
});

export function AuditLogsPage() {
  const [fromDate, setFromDate] = useState(initialFromDate);
  const [toDate, setToDate] = useState(() => utcDate(new Date()));
  const [action, setAction] = useState('');
  const [entityName, setEntityName] = useState('');
  const [userId, setUserId] = useState('');
  const [entityId, setEntityId] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(25);

  const validRange = Boolean(fromDate && toDate && fromDate <= toDate);
  const validIds = isGuid(userId.trim()) && isGuid(entityId.trim());
  const query = useQuery({
    queryKey: ['audit-logs', fromDate, toDate, action, entityName, userId, entityId, page, pageSize],
    queryFn: () => auditLogsApi.list({
      fromDate,
      toDate,
      page,
      pageSize,
      action: action.trim() || undefined,
      entityName: entityName.trim() || undefined,
      userId: userId.trim() || undefined,
      entityId: entityId.trim() || undefined,
    }),
    enabled: validRange && validIds,
  });

  return (
    <div className="space-y-6">
      <PageHeader title="Auditoría" description="Consulta actividad administrativa y clínica registrada en el sistema." />
      <Alert variant="info" title="Datos protegidos">
        Esta vista muestra metadatos del evento. No incluye valores clínicos, datos anteriores o nuevos, direcciones IP ni agentes de usuario.
      </Alert>

      <Card>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 dark:text-slate-300">Desde (UTC)<Input type="date" value={fromDate} onChange={(event) => { setFromDate(event.target.value); setPage(1); }} /></label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 dark:text-slate-300">Hasta (UTC)<Input type="date" value={toDate} onChange={(event) => { setToDate(event.target.value); setPage(1); }} /></label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 dark:text-slate-300">Acción<Input value={action} onChange={(event) => { setAction(event.target.value); setPage(1); }} placeholder="Ej. ClinicalRecords." /></label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 dark:text-slate-300">Entidad<Input value={entityName} onChange={(event) => { setEntityName(event.target.value); setPage(1); }} placeholder="Ej. Prescription" /></label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 dark:text-slate-300">Usuario (GUID)<Input value={userId} onChange={(event) => { setUserId(event.target.value); setPage(1); }} /></label>
          <label className="flex flex-col gap-1 text-sm font-medium text-slate-700 dark:text-slate-300">Entidad (GUID)<Input value={entityId} onChange={(event) => { setEntityId(event.target.value); setPage(1); }} /></label>
          <div className="flex items-end lg:col-span-2">
            <Button variant="secondary" onClick={() => void query.refetch()} disabled={!validRange || !validIds} leftIcon={<RefreshCw className="h-4 w-4" />}>Actualizar</Button>
          </div>
        </div>
        {!validRange ? <p className="mt-2 text-sm text-rose-600">La fecha inicial debe ser anterior o igual a la final.</p> : null}
        {!validIds ? <p className="mt-2 text-sm text-rose-600">Los identificadores deben ser GUID válidos.</p> : null}
      </Card>

      {query.isError ? <Alert variant="error" title="No se pudo cargar la auditoría">{query.error instanceof ApiError ? query.error.message : 'Error inesperado.'}</Alert> : null}
      {query.isLoading ? <Spinner label="Cargando auditoría..." /> : null}
      {query.data ? (
        <Card>
          <div className="mb-4 flex items-center gap-2 font-semibold text-slate-900 dark:text-slate-50"><ShieldCheck className="h-5 w-5" /> Eventos ({query.data.totalCount})</div>
          {query.data.items.length === 0 ? <p className="py-8 text-center text-sm text-slate-500">No hay eventos para los filtros seleccionados.</p> : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[800px] text-left text-sm">
                <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-800"><tr><th className="px-3 py-2">Fecha (UTC)</th><th className="px-3 py-2">Acción</th><th className="px-3 py-2">Entidad</th><th className="px-3 py-2">Id de entidad</th><th className="px-3 py-2">Id de usuario</th><th className="px-3 py-2">Correlación</th></tr></thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-800">{query.data.items.map((item) => (
                  <tr key={item.id} className="align-top text-slate-700 dark:text-slate-300"><td className="whitespace-nowrap px-3 py-3">{utcDateTimeFormatter.format(new Date(item.timestamp))}</td><td className="px-3 py-3 font-medium">{item.action}</td><td className="px-3 py-3">{item.entityName}</td><td className="px-3 py-3 font-mono text-xs">{item.entityId}</td><td className="px-3 py-3 font-mono text-xs">{item.userId ?? 'Sistema'}</td><td className="px-3 py-3 font-mono text-xs">{item.correlationId ?? '—'}</td></tr>
                ))}</tbody>
              </table>
            </div>
          )}
          <div className="mt-4"><Pagination page={page} pageSize={pageSize} totalCount={query.data.totalCount} onPageChange={setPage} onPageSizeChange={(size) => { setPageSize(size); setPage(1); }} /></div>
        </Card>
      ) : null}
    </div>
  );
}

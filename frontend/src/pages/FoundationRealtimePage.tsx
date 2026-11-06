import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { LogOut, RadioTower, RefreshCcw, Send } from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { useFoundationRealtime } from '../hooks/useFoundationRealtime';
import { publishTestEvent } from '../features/foundation/api';
import { testEventSchema, type TestEventFormValues } from '../features/foundation/testEventSchema';
import { ApiError } from '../services/api/apiClient';
import { formatDateTime } from '../utils/formatDate';
import { Alert } from '../components/ui/Alert';
import { Badge } from '../components/ui/Badge';
import { Button } from '../components/ui/Button';
import { Card } from '../components/ui/Card';
import { EmptyState } from '../components/ui/EmptyState';
import { FormField } from '../components/ui/FormField';
import { Input } from '../components/ui/Input';
import { PageHeader } from '../components/ui/PageHeader';
import { StatusBadge } from '../components/ui/StatusBadge';

export function FoundationRealtimePage() {
  const { user, signOut } = useAuth();
  const { events, state, connect, clearEvents } = useFoundationRealtime();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<TestEventFormValues>({
    resolver: zodResolver(testEventSchema),
    defaultValues: {
      message: 'Hola desde React + Identity + SignalR + Outbox',
    },
  });

  const mutation = useMutation({
    mutationFn: publishTestEvent,
    onSuccess: () => {
      reset({ message: 'Hola desde React + Identity + SignalR + Outbox' });
    },
  });

  return (
    <div className="min-h-screen bg-slate-50 p-4 sm:p-6 lg:p-8 dark:bg-slate-950">
      <div className="mx-auto max-w-6xl space-y-6">
        <PageHeader
          title="Foundation Realtime"
          description="Prueba vertical: REST autenticado → Outbox → Worker → SignalR user group → UI."
          actions={
            <>
              <StatusBadge state={state} />
              <Button variant="secondary" onClick={() => void connect()} leftIcon={<RefreshCcw className="h-4 w-4" />}>
                Reconectar
              </Button>
              <Button variant="ghost" onClick={() => void signOut()} leftIcon={<LogOut className="h-4 w-4" />}>
                Salir
              </Button>
            </>
          }
        />

        {user ? (
          <Card>
            <div className="space-y-3">
              <div>
                <p className="text-sm font-medium text-slate-900 dark:text-slate-50">{user.email}</p>
                <p className="text-sm text-slate-600 dark:text-slate-400">
                  {user.firstName || user.lastName
                    ? `${user.firstName ?? ''} ${user.lastName ?? ''}`.trim()
                    : 'Usuario sin nombre'}
                </p>
              </div>

              <div className="flex flex-wrap gap-2">
                {user.roles.map((role) => (
                  <Badge key={role} variant="info">
                    {role}
                  </Badge>
                ))}
              </div>

              <div className="flex flex-wrap gap-2">
                {user.permissions.slice(0, 12).map((permission) => (
                  <Badge key={permission} variant="neutral">
                    {permission}
                  </Badge>
                ))}
                {user.permissions.length > 12 ? (
                  <Badge variant="neutral">+{user.permissions.length - 12}</Badge>
                ) : null}
              </div>
            </div>
          </Card>
        ) : null}

        {state === 'error' ? (
          <Alert variant="error" title="Error de SignalR">
            No se pudo establecer la conexión. Verifica login, proxy, WebSockets y que la API esté activa.
          </Alert>
        ) : null}

        <div className="grid gap-6 lg:grid-cols-2">
          <Card>
            <div className="space-y-4">
              <div>
                <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-50">Publicar evento de prueba</h2>
                <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
                  La petición REST autenticada persiste un OutboxMessage dirigido al usuario actual.
                </p>
              </div>

              <form className="space-y-4" onSubmit={handleSubmit((values) => mutation.mutate(values.message))}>
                <FormField label="Mensaje" htmlFor="message" required error={errors.message?.message}>
                  <Input
                    id="message"
                    {...register('message')}
                    invalid={Boolean(errors.message)}
                    aria-describedby={errors.message ? 'message-error' : undefined}
                  />
                </FormField>

                {mutation.isError ? (
                  <Alert variant="error" title="No se pudo encolar el evento">
                    {mutation.error instanceof ApiError ? mutation.error.message : 'Error inesperado.'}
                  </Alert>
                ) : null}

                {mutation.isSuccess ? (
                  <Alert variant="success" title="Evento encolado">
                    OutboxMessageId: <code className="break-all">{mutation.data.outboxMessageId}</code>
                  </Alert>
                ) : null}

                <Button
                  type="submit"
                  loading={mutation.isPending}
                  leftIcon={<Send className="h-4 w-4" />}
                  disabled={state !== 'connected'}
                >
                  Publicar
                </Button>
              </form>
            </div>
          </Card>

          <Card>
            <div className="space-y-4">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <h2 className="text-lg font-semibold text-slate-900 dark:text-slate-50">Eventos recibidos</h2>
                  <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
                    Solo este usuario debe ver sus eventos dirigidos a su grupo.
                  </p>
                </div>

                <Button variant="ghost" size="sm" onClick={clearEvents} disabled={events.length === 0}>
                  Limpiar
                </Button>
              </div>

              {events.length === 0 ? (
                <EmptyState
                  icon={<RadioTower className="h-8 w-8" />}
                  title="Sin eventos todavía"
                  description="Publica un evento de prueba para ver la actualización en tiempo real."
                />
              ) : (
                <ul className="space-y-3">
                  {events.map((event) => (
                    <li
                      key={event.id}
                      className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-800 dark:bg-slate-900"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <p className="truncate font-medium text-slate-900 dark:text-slate-50">{event.message}</p>
                          <p className="mt-1 text-xs text-slate-500 dark:text-slate-400">
                            {formatDateTime(event.occurredAt)}
                          </p>
                        </div>

                        <span className="shrink-0 rounded-full bg-sky-100 px-2 py-1 text-xs font-medium text-sky-800 dark:bg-sky-950 dark:text-sky-200">
                          TestEvent
                        </span>
                      </div>

                      {event.correlationId ? (
                        <p className="mt-3 break-all text-xs text-slate-500 dark:text-slate-400">
                          CorrelationId: {event.correlationId}
                        </p>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}
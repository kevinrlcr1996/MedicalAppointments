import type { ConnectionState } from '../../services/realtime/signalrTypes';
import { Badge } from './Badge';

const config: Record<ConnectionState, { label: string; variant: 'neutral' | 'success' | 'warning' | 'danger' | 'info' }> =
  {
    disconnected: { label: 'Desconectado', variant: 'neutral' },
    connecting: { label: 'Conectando', variant: 'info' },
    connected: { label: 'Conectado', variant: 'success' },
    reconnecting: { label: 'Reconectando', variant: 'warning' },
    reconnected: { label: 'Reconectado', variant: 'success' },
    error: { label: 'Error', variant: 'danger' },
  };

export function StatusBadge({ state }: { state: ConnectionState }) {
  const item = config[state];

  return (
    <Badge variant={item.variant}>
      <span className="h-2 w-2 rounded-full bg-current opacity-70" aria-hidden="true" />
      {item.label}
    </Badge>
  );
}

import { useCallback, useEffect, useState } from 'react';
import type { ConnectionState } from '../services/realtime/signalrTypes';
import type { SignalRClient } from '../services/realtime/signalrClient';

export function useSignalR(client: SignalRClient) {
  const [state, setState] = useState<ConnectionState>(
    () => client.getState(),
  );

  useEffect(() => client.subscribe(setState), [client]);

  const connect = useCallback(() => client.connect(), [client]);

  const disconnect = useCallback(
    () => client.disconnect(),
    [client],
  );

  return {
    state,
    connect,
    disconnect,
  };
}

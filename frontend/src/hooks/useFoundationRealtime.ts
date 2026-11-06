
import { useEffect, useState } from 'react';
import { SignalREvents } from '../services/realtime/signalrEvents';
import {
  foundationSignalRClient,
} from '../services/realtime/signalrClient';
import type { FoundationTestEventPayload } from '../services/realtime/signalrTypes';
import { useSignalR } from './useSignalR';

export function useFoundationRealtime() {
  const [events, setEvents] = useState<FoundationTestEventPayload[]>([]);
  const { state, connect, disconnect } = useSignalR(foundationSignalRClient);

  useEffect(() => {
    const unsubscribe = foundationSignalRClient.on(
      SignalREvents.TestEvent,
      (payload) => {
        setEvents((current) => [
          payload as FoundationTestEventPayload,
          ...current,
        ]);
      },
    );

    return unsubscribe;
  }, []);

  const clearEvents = () => {
    setEvents([]);
  };

  return {
    events,
    state,
    connect,
    disconnect,
    clearEvents,
  };
}

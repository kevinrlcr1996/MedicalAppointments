import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function useClinicalRecordsRealtime() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const off = notificationSignalRClient.on(SignalREvents.ClinicalNoteUpdated, () => {
      void queryClient.invalidateQueries({ queryKey: ['clinical-records'] });
    });
    return off;
  }, [queryClient]);
}
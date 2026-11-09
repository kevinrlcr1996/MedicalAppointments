import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function useClinicalAttachmentsRealtime() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const off = notificationSignalRClient.on(SignalREvents.ClinicalAttachmentUpdated, () => {
      void queryClient.invalidateQueries({ queryKey: ['clinical-attachments'] });
      void queryClient.invalidateQueries({ queryKey: ['clinical-records'] });
    });
    return off;
  }, [queryClient]);
}
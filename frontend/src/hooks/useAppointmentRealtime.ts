import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function useAppointmentRealtime() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const events = [
      SignalREvents.AppointmentCreated,
      SignalREvents.AppointmentCancelled,
      SignalREvents.AppointmentRescheduled,
      SignalREvents.AppointmentStatusChanged,
    ];

    const unsubscribes = events.map((event) =>
      notificationSignalRClient.on(event, () => {
        void queryClient.invalidateQueries({ queryKey: ['appointments'] });
        void queryClient.invalidateQueries({ queryKey: ['availability'] });
      }),
    );

    return () => {
      for (const unsubscribe of unsubscribes) {
        unsubscribe();
      }
    };
  }, [queryClient]);
}
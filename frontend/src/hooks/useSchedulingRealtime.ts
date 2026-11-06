import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function useSchedulingRealtime() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const unsubscribeSchedule = notificationSignalRClient.on(SignalREvents.ScheduleChanged, () => {
      void queryClient.invalidateQueries({ queryKey: ['doctor-schedules'] });
      void queryClient.invalidateQueries({ queryKey: ['availability'] });
    });

    const unsubscribeAvailability = notificationSignalRClient.on(SignalREvents.AvailabilityChanged, () => {
      void queryClient.invalidateQueries({ queryKey: ['availability'] });
    });

    return () => {
      unsubscribeSchedule();
      unsubscribeAvailability();
    };
  }, [queryClient]);
}
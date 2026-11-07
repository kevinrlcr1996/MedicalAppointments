import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function useWaitingRoomRealtime() {
    const queryClient = useQueryClient();

    useEffect(() => {
        const events = [SignalREvents.WaitingRoomUpdated, SignalREvents.PatientCalled, SignalREvents.PatientRecalled];
        const unsubscribes = events.map((event) =>
            notificationSignalRClient.on(event, () => {
                void queryClient.invalidateQueries({ queryKey: ['waiting-room'] });
                void queryClient.invalidateQueries({ queryKey: ['appointments'] });
            }),
        );
        return () => {
            for (const unsubscribe of unsubscribes) unsubscribe();
        };
    }, [queryClient]);
}
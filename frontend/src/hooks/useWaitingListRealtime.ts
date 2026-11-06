import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function useWaitingListRealtime() {
    const queryClient = useQueryClient();

    useEffect(() => {
        const events = [SignalREvents.WaitingListOfferCreated, SignalREvents.WaitingListUpdated];
        const unsubscribes = events.map((event) =>
            notificationSignalRClient.on(event, () => {
                void queryClient.invalidateQueries({ queryKey: ['waiting-list'] });
            }),
        );
        return () => {
            for (const unsubscribe of unsubscribes) unsubscribe();
        };
    }, [queryClient]);
}
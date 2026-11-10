import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function useVitalSignsRealtime() {
    const queryClient = useQueryClient();
    useEffect(() => {
        const off = notificationSignalRClient.on(SignalREvents.VitalSignsUpdated, () => {
            void queryClient.invalidateQueries({ queryKey: ['vital-signs'] });
        });
        return off;
    }, [queryClient]);
}
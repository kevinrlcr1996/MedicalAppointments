import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function usePrescriptionsRealtime() {
    const queryClient = useQueryClient();

    useEffect(() => {
        const off = notificationSignalRClient.on(
            SignalREvents.PrescriptionUpdated,
            () => {
                void queryClient.invalidateQueries({ queryKey: ['prescriptions'] });
            },
        );

        const offDocuments = notificationSignalRClient.on(
            SignalREvents.ClinicalDocumentUpdated,
            () => {
                void queryClient.invalidateQueries({ queryKey: ['clinicalDocuments'] });
            },
        );

        return () => {
            off();
            offDocuments();
        };
    }, [queryClient]);
}

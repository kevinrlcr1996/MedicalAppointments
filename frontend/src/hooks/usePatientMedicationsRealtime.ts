import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function usePatientMedicationsRealtime() {
    const queryClient = useQueryClient();
    useEffect(() => {
        const off = notificationSignalRClient.on(SignalREvents.PatientMedicationUpdated, () => {
            void queryClient.invalidateQueries({ queryKey: ['patient-medications'] });
        });
        return off;
    }, [queryClient]);
}
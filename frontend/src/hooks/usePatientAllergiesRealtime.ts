import { useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { SignalREvents } from '../services/realtime/signalrEvents';
import { notificationSignalRClient } from '../services/realtime/signalrClient';

export function usePatientAllergiesRealtime() {
    const queryClient = useQueryClient();
    useEffect(() => {
        const off = notificationSignalRClient.on(SignalREvents.PatientAllergyUpdated, () => {
            void queryClient.invalidateQueries({ queryKey: ['patient-allergies'] });
        });
        return off;
    }, [queryClient]);
}
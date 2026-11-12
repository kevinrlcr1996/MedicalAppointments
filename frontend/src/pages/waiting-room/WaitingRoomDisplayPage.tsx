import { useQuery } from '@tanstack/react-query';
import { Volume2 } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { Spinner } from '../../components/ui/Spinner';
import { useAuth } from '../../contexts/AuthContext';
import { hasAnyPermission, Permissions } from '../../lib/permissions';
import { apiClient } from '../../services/api/apiClient';
import { waitingRoomApi, type PatientCalledPayload, type WaitingRoomBoard } from '../../services/api/waitingRoom';
import { notificationSignalRClient } from '../../services/realtime/signalrClient';
import { SignalREvents } from '../../services/realtime/signalrEvents';

interface DoctorClinicRef {
    clinicId: string;
    clinicName: string;
    isPrimary?: boolean;
}

interface DoctorOption {
    id: string;
    firstName: string;
    lastName: string;
    clinics?: DoctorClinicRef[];
}

export function WaitingRoomDisplayPage() {
    const { user } = useAuth();
    const [doctorId, setDoctorId] = useState('');
    const [clinicId, setClinicId] = useState('');
    const [localDate, setLocalDate] = useState(() => new Date().toISOString().slice(0, 10));
    const [lastCall, setLastCall] = useState<PatientCalledPayload | null>(null);
    const dateInputRef = useRef<HTMLInputElement>(null);

    const canView = hasAnyPermission(user, [Permissions.WaitingRoomRead, Permissions.WaitingRoomManage]);

    const doctorsQuery = useQuery({
        queryKey: ['doctors-options'],
        queryFn: () => apiClient.get<{ items: DoctorOption[] }>('/api/v1/doctors?page=1&pageSize=100&status=Active'),
        enabled: canView,
    });

    const effectiveDoctorId = doctorId || doctorsQuery.data?.items?.[0]?.id || '';

    const selectedDoctorQuery = useQuery({
        queryKey: ['doctor-detail', effectiveDoctorId],
        queryFn: () => apiClient.get<DoctorOption>(`/api/v1/doctors/${effectiveDoctorId}`),
        enabled: canView && Boolean(effectiveDoctorId),
    });

    const effectiveClinicId = clinicId || selectedDoctorQuery.data?.clinics?.[0]?.clinicId || '';

    const boardQuery = useQuery({
        queryKey: ['waiting-room', 'board', effectiveDoctorId, effectiveClinicId, localDate],
        queryFn: () => waitingRoomApi.getBoard(effectiveDoctorId, effectiveClinicId, localDate),
        enabled: canView && Boolean(effectiveDoctorId && effectiveClinicId && localDate),
        refetchInterval: 15000,
    });

    useEffect(() => {
        if (!canView) {
            return;
        }
        const off = notificationSignalRClient.on(SignalREvents.PatientCalled, (payload: unknown) => {
            const p = payload as PatientCalledPayload;
            if (p.doctorId === effectiveDoctorId && p.clinicId === effectiveClinicId) {
                setLastCall(p);
                playChime();
            }
        });
        return off;
    }, [canView, effectiveDoctorId, effectiveClinicId]);

    /*     function openDatePicker() {
            const el = dateInputRef.current;
            if (!el) {
                return;
            }
            const picker = el as HTMLInputElement & { showPicker?: () => void };
            picker.showPicker?.();
        } */

    if (!canView) {
        return (
            <div className="flex min-h-screen items-center justify-center bg-slate-950 p-6 text-slate-400">
                La pantalla de sala requiere <code className="mx-1 text-slate-200">waiting_room.read</code>.
            </div>
        );
    }

    if (doctorsQuery.isLoading) {
        return (
            <div className="flex min-h-screen items-center justify-center bg-slate-950">
                <Spinner label="Cargando..." />
            </div>
        );
    }

    const board: WaitingRoomBoard | undefined = boardQuery.data;
    const doctorOptions = (doctorsQuery.data?.items ?? []).map((d) => ({
        value: d.id,
        label: `${d.firstName} ${d.lastName}`,
    }));
    const clinicOptions = (selectedDoctorQuery.data?.clinics ?? []).map((c) => ({
        value: c.clinicId,
        label: c.clinicName,
    }));

    const ready = Boolean(effectiveDoctorId && effectiveClinicId && localDate);

    return (
        <div className="min-h-screen bg-slate-950 p-6 text-white">
            <div className="mx-auto max-w-6xl space-y-6">
                <header className="flex flex-wrap items-center justify-between gap-4">
                    <h1 className="text-2xl font-bold">{board?.clinicName ?? 'Sala de espera'}</h1>
                    <span className="flex items-center gap-2 text-sm text-slate-400">
                        <Volume2 className="h-4 w-4" /> {board?.doctorFullName}
                    </span>
                </header>

                <div className="flex flex-wrap items-end gap-3 rounded-2xl border border-slate-800 bg-slate-900/60 p-3">
                    <label className="flex flex-col gap-1 text-xs text-slate-400">
                        Médico
                        <select
                            value={effectiveDoctorId}
                            onChange={(e) => {
                                setDoctorId(e.target.value);
                                setClinicId('');
                            }}
                            className="h-9 rounded-lg border border-slate-700 bg-slate-950 px-2 text-sm text-white"
                        >
                            <option value="">Seleccionar...</option>
                            {doctorOptions.map((o) => (
                                <option key={o.value} value={o.value}>
                                    {o.label}
                                </option>
                            ))}
                        </select>
                    </label>

                    <label className="flex flex-col gap-1 text-xs text-slate-400">
                        Clínica
                        <select
                            value={effectiveClinicId}
                            onChange={(e) => setClinicId(e.target.value)}
                            disabled={!effectiveDoctorId}
                            className="h-9 rounded-lg border border-slate-700 bg-slate-950 px-2 text-sm text-white disabled:opacity-50"
                        >
                            <option value="">Seleccionar...</option>
                            {clinicOptions.map((o) => (
                                <option key={o.value} value={o.value}>
                                    {o.label}
                                </option>
                            ))}
                        </select>
                    </label>

                    {/* FIX icono fecha: oculto el widget nativo (appearance-none) y pongo mi propio
                    icono blanco clickeable que abre el calendario con showPicker(). Determinista
                    en Chrome/Edge/Safari/Firefox, sin depender del alfa del indicador del navegador. */}
                    <label className="flex flex-col gap-1 text-xs text-slate-400">
                        Fecha
                        <div className="relative">
                            <input
                                ref={dateInputRef}
                                type="date"
                                value={localDate}
                                onChange={(e) => setLocalDate(e.target.value)}
                                className="h-9 w-full appearance-none rounded-lg border border-slate-700 bg-slate-950 px-2 pr-9 text-sm text-white [color-scheme:dark] focus:border-sky-500 focus:outline-none"
                            />
                        </div>
                    </label>

                    <p className="ml-auto max-w-xs text-right text-xs text-slate-500">
                        Elige el <strong>mismo</strong> médico, clínica y fecha que tienes en el tablero de sala.
                    </p>
                </div>

                {!ready ? (
                    <div className="rounded-3xl border border-slate-800 p-10 text-center text-slate-400">
                        Selecciona médico, clínica y fecha para mostrar esta cola.
                    </div>
                ) : boardQuery.isFetching && !board ? (
                    <div className="flex justify-center py-10">
                        <Spinner label="Cargando cola..." />
                    </div>
                ) : (
                    <>
                        {lastCall ? (
                            <div className="rounded-3xl bg-sky-600 p-10 text-center shadow-2xl">
                                <p className="text-sm uppercase tracking-widest text-sky-200">Llamando a</p>
                                <p className="mt-2 text-6xl font-black">{lastCall.patientFullName}</p>
                                <p className="mt-3 text-2xl text-sky-100">
                                    Turno #{lastCall.sequenceNumber} · {lastCall.roomName ?? 'consultorio asignado'}
                                </p>
                            </div>
                        ) : (
                            <div className="rounded-3xl border border-slate-800 p-10 text-center text-slate-400">
                                En espera de llamados…
                            </div>
                        )}

                        <section>
                            <h2 className="mb-3 text-lg font-semibold text-slate-300">En espera</h2>
                            <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-5">
                                {(board?.queued ?? []).map((c) => (
                                    <div key={c.callId} className="rounded-2xl bg-slate-900 p-4 text-center">
                                        <p className="text-3xl font-bold text-sky-400">#{c.sequenceNumber}</p>
                                        <p className="mt-1 truncate text-sm">{c.patientFullName}</p>
                                    </div>
                                ))}
                                {(board?.queued.length ?? 0) === 0 ? (
                                    <p className="col-span-full text-center text-slate-500">Nadie en espera</p>
                                ) : null}
                            </div>
                        </section>

                        <section>
                            <h2 className="mb-3 text-lg font-semibold text-slate-300">En consulta</h2>
                            <div className="grid gap-3 sm:grid-cols-3">
                                {(board?.inConsultation ?? []).map((c) => (
                                    <div key={c.callId} className="rounded-2xl bg-emerald-900/40 p-4 text-center">
                                        <p className="text-sm text-emerald-300">{c.roomName ?? 'consultorio'}</p>
                                        <p className="truncate font-semibold">{c.patientFullName}</p>
                                    </div>
                                ))}
                                {(board?.inConsultation.length ?? 0) === 0 ? (
                                    <p className="col-span-full text-center text-slate-500">Nadie en consulta</p>
                                ) : null}
                            </div>
                        </section>
                    </>
                )}
            </div>
        </div>
    );
}

function playChime() {
    try {
        const ctx = new (window.AudioContext ||
            (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.frequency.value = 880;
        gain.gain.value = 0.15;
        osc.start();
        osc.stop(ctx.currentTime + 0.35);
    } catch {
        /* autoplay bloqueado: el display visual sigue siendo la fuente */
    }
}

using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Application.MedicalStructure.Rooms;
using MedicalAppointments.Application.WaitingRoom.Auth;
using MedicalAppointments.Application.WaitingRoom.Common;
using MedicalAppointments.Application.WaitingRoom.Stores;
using MedicalAppointments.Domain.WaitingRoom;

namespace MedicalAppointments.Application.WaitingRoom;

public sealed class WaitingRoomBoardService : IWaitingRoomBoardService
{
    private readonly IWaitingRoomCallStore _store;
    private readonly IWaitingRoomAccessService _access;
    private readonly IAppointmentStore _appointmentStore;
    private readonly IPatientStore _patientStore;
    private readonly IDoctorStore _doctorStore;
    private readonly IClinicStore _clinicStore;
    private readonly IRoomStore _roomStore;

    public WaitingRoomBoardService(
        IWaitingRoomCallStore store,
        IWaitingRoomAccessService access,
        IAppointmentStore appointmentStore,
        IPatientStore patientStore,
        IDoctorStore doctorStore,
        IClinicStore clinicStore,
        IRoomStore roomStore)
    {
        _store = store;
        _access = access;
        _appointmentStore = appointmentStore;
        _patientStore = patientStore;
        _doctorStore = doctorStore;
        _clinicStore = clinicStore;
        _roomStore = roomStore;
    }

    public async Task<WaitingRoomBoardDto> GetBoardAsync(
        WaitingRoomBoardQuery query,
        CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);

        if (!_access.CanViewBoard(query.DoctorId, subject))
        {
            throw new ForbiddenException("You do not have access to this waiting room.");
        }

        var calls = await _store.GetBoardAsync(query.DoctorId, query.ClinicId, query.LocalDate, cancellationToken);

        if (calls.Count == 0)
        {
            var doctor = await _doctorStore.GetTrackedWithRelationsAsync(query.DoctorId, cancellationToken);
            var clinic = await _clinicStore.GetTrackedByIdAsync(query.ClinicId, cancellationToken);

            return new WaitingRoomBoardDto(
                query.DoctorId,
                doctor is null ? string.Empty : $"{doctor.FirstName} {doctor.LastName}",
                query.ClinicId,
                clinic?.Name ?? string.Empty,
                query.LocalDate,
                [], [], [], [], []);
        }

        var appointmentIds = calls.Select(x => x.AppointmentId).Distinct().ToArray();
        var patientIds = calls.Select(x => x.AppointmentId).Distinct().ToArray(); // resolvia via citas

        // Cargamos citas con relaciones para nombre de paciente y hora de cita.
        var appointments = new Dictionary<Guid, AppointmentDto>();
        foreach (var id in appointmentIds)
        {
            var appt = await _appointmentStore.GetWithRelationsAsync(id, cancellationToken);
            if (appt is not null)
            {
                appointments[id] = AppointmentQueryService.Map(appt);
            }
        }

        var roomIds = calls.Select(x => x.RoomId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var rooms = new Dictionary<Guid, string>();
        if (roomIds.Length > 0)
        {
            foreach (var rid in roomIds)
            {
                var room = await _roomStore.GetTrackedByIdAsync(rid, cancellationToken);
                if (room is not null)
                {
                    rooms[rid] = room.Name;
                }
            }
        }

        var doctorEntity = await _doctorStore.GetTrackedWithRelationsAsync(query.DoctorId, cancellationToken);
        var clinicEntity = await _clinicStore.GetTrackedByIdAsync(query.ClinicId, cancellationToken);
        var doctorFullName = doctorEntity is null ? string.Empty : $"{doctorEntity.FirstName} {doctorEntity.LastName}";
        var clinicName = clinicEntity?.Name ?? string.Empty;

        var cards = calls
            .Select(call =>
            {
                appointments.TryGetValue(call.AppointmentId, out var appt);
                return new WaitingRoomCardDto(
                    call.Id,
                    call.AppointmentId,
                    call.SequenceNumber,
                    appt?.PatientId ?? Guid.Empty,
                    appt?.PatientFullName ?? string.Empty,
                    call.DoctorId,
                    doctorFullName,
                    call.ClinicId,
                    clinicName,
                    call.RoomId,
                    call.RoomId.HasValue && rooms.TryGetValue(call.RoomId.Value, out var rn) ? rn : null,
                    call.LocalDate,
                    call.Status,
                    call.RecallCount,
                    call.CalledAt,
                    call.AnsweredAt,
                    call.SkippedAt,
                    call.CompletedAt,
                    appt?.StartAtUtc ?? DateTimeOffset.MinValue,
                    call.CreatedAt,
                    call.Note);
            })
            .ToList();

        return new WaitingRoomBoardDto(
            query.DoctorId,
            doctorFullName,
            query.ClinicId,
            clinicName,
            query.LocalDate,
            cards.Where(x => x.Status == WaitingRoomCallStatus.Queued).OrderBy(x => x.SequenceNumber).ToList(),
            cards.Where(x => x.Status == WaitingRoomCallStatus.Called).OrderBy(x => x.SequenceNumber).ToList(),
            cards.Where(x => x.Status == WaitingRoomCallStatus.InConsultation).OrderBy(x => x.SequenceNumber).ToList(),
            cards.Where(x => x.Status == WaitingRoomCallStatus.Skipped).OrderBy(x => x.SequenceNumber).ToList(),
            cards.Where(x => x.Status == WaitingRoomCallStatus.Completed).OrderByDescending(x => x.SequenceNumber).ToList());
    }
}
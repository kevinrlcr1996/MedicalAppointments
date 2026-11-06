using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Application.Scheduling.Availability;
using MedicalAppointments.Application.WaitingList.Outbox;
using MedicalAppointments.Application.WaitingList.Stores;
using MedicalAppointments.Domain.WaitingList;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.WaitingList.Matching;

public sealed class WaitingListMatchingService : IWaitingListMatchingService
{
    private readonly IWaitingListEntryStore _entryStore;
    private readonly IWaitingListOfferStore _offerStore;
    private readonly IAvailabilityService _availabilityService;
    private readonly IPatientStore _patientStore;
    private readonly IBookingCoordinator _bookingCoordinator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWaitingListEventNotifier _notifier;
    private readonly TimeProvider _timeProvider;
    private readonly WaitingListOptions _options;
    private readonly ILogger<WaitingListMatchingService> _logger;

    public WaitingListMatchingService(
        IWaitingListEntryStore entryStore,
        IWaitingListOfferStore offerStore,
        IAvailabilityService availabilityService,
        IPatientStore patientStore,
        IBookingCoordinator bookingCoordinator,
        IUnitOfWork unitOfWork,
        IWaitingListEventNotifier notifier,
        TimeProvider timeProvider,
        IOptions<WaitingListOptions> optionsAccessor,
        ILogger<WaitingListMatchingService> logger)
    {
        _entryStore = entryStore;
        _offerStore = offerStore;
        _availabilityService = availabilityService;
        _patientStore = patientStore;
        _bookingCoordinator = bookingCoordinator;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _timeProvider = timeProvider;
        _options = optionsAccessor.Value;
        _logger = logger;
    }

    public async Task MatchAsync(SlotFreedPayload slot, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        // El slot liberado debe seguir siendo futuro y válido.
        if (slot.SlotStartUtc <= now)
        {
            return;
        }

        if (!slot.ClinicId.HasValue)
        {
            // Sin clínica no podemos revalidar disponibilidad (es por médico+clínica).
            return;
        }

        var lockResources = BookingLockResources.ForInterval(
            slot.DoctorId,
            slot.ClinicId.Value,
            slot.RoomId,
            slot.SlotStartUtc,
            slot.SlotEndUtc);

        await _bookingCoordinator.ExecuteInTransactionAsync(
            lockResources,
            async () =>
            {
                // Revalida que el slot sigue disponible (agenda + excepciones + bloqueos + citas activas).
                var availability = await _availabilityService.GetDayAsync(
                    new Scheduling.Availability.AvailabilityQuery(
                        slot.DoctorId, slot.ClinicId.Value, slot.LocalDate, null),
                    cancellationToken);

                var stillFree = availability.Slots.Any(s =>
                    s.StartAtUtc == slot.SlotStartUtc && s.EndAtUtc == slot.SlotEndUtc);

                if (!stillFree)
                {
                    _logger.LogDebug("Slot {SlotStart} no longer available; skipping matching.", slot.SlotStartUtc);
                    return false;
                }

                var candidates = await _entryStore.GetWaitingCandidatesAsync(
                    slot.DoctorId, slot.ClinicId.Value, slot.SlotStartUtc, slot.SlotEndUtc, cancellationToken);

                foreach (var candidate in candidates)
                {
                    // Constraint (EntryId, SlotStartUtc): no re-ofrecer el mismo slot al mismo candidato.
                    if (await _offerStore.ExistsForSlotAsync(candidate.Id, slot.SlotStartUtc, cancellationToken))
                    {
                        continue;
                    }

                    var patientUserId = await _patientStore.GetUserIdAsync(candidate.PatientId, cancellationToken);

                    var offer = WaitingListOffer.Create(
                        entryId: candidate.Id,
                        patientId: candidate.PatientId,
                        doctorId: slot.DoctorId,
                        clinicId: slot.ClinicId,
                        roomId: slot.RoomId,
                        localDate: slot.LocalDate,
                        slotStartUtc: slot.SlotStartUtc,
                        slotEndUtc: slot.SlotEndUtc,
                        expiresAt: now + _options.OfferTtl,
                        now: now);

                    await _offerStore.AddAsync(offer, cancellationToken);

                    _notifier.EnqueueOfferCreated(offer, patientUserId, now, null);
                    _notifier.EnqueueUpdated(
                        candidate.Id, offer.Id, candidate.PatientId, patientUserId,
                        slot.DoctorId, slot.ClinicId, "OfferCreated",
                        candidate.Status, offer.Status, now, null);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation(
                        "Created waiting list offer {OfferId} for entry {EntryId} on slot {SlotStart}.",
                        offer.Id, candidate.Id, slot.SlotStartUtc);

                    return true;
                }

                return false;
            },
            cancellationToken);
    }
}
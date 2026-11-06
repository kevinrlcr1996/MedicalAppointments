using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.WaitingList;

public sealed class WaitingListOffer : AggregateRoot
{
    private WaitingListOffer()
    {
    }

    public Guid EntryId { get; private set; }

    public Guid PatientId { get; private set; }

    public Guid DoctorId { get; private set; }

    public Guid? ClinicId { get; private set; }

    public Guid? RoomId { get; private set; }

    public DateOnly LocalDate { get; private set; }

    public DateTimeOffset SlotStartUtc { get; private set; }

    public DateTimeOffset SlotEndUtc { get; private set; }

    public WaitingListOfferStatus Status { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    public WaitingListEntry Entry { get; private set; } = null!;

    public Patient Patient { get; private set; } = null!;

    public Doctor Doctor { get; private set; } = null!;

    public Clinic? Clinic { get; private set; }

    public Room? Room { get; private set; }

    public static WaitingListOffer Create(
        Guid entryId,
        Guid patientId,
        Guid doctorId,
        Guid? clinicId,
        Guid? roomId,
        DateOnly localDate,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        if (entryId == Guid.Empty)
        {
            throw new DomainException("Entry is required.");
        }

        if (slotEndUtc <= slotStartUtc)
        {
            throw new DomainException("Offer slot end must be later than start.");
        }

        if (expiresAt <= now)
        {
            throw new DomainException("Offer expiry must be in the future.");
        }

        return new WaitingListOffer
        {
            Id = Guid.CreateVersion7(),
            EntryId = entryId,
            PatientId = patientId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            RoomId = roomId,
            LocalDate = localDate,
            SlotStartUtc = slotStartUtc,
            SlotEndUtc = slotEndUtc,
            Status = WaitingListOfferStatus.Pending,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool IsPendingAndAlive(DateTimeOffset now)
    {
        return Status == WaitingListOfferStatus.Pending && now < ExpiresAt;
    }

    public void MarkAccepted(DateTimeOffset now)
    {
        EnsurePending(now);
        Status = WaitingListOfferStatus.Accepted;
        RespondedAt = now;
        Touch(now);
    }

    public void MarkDeclined(DateTimeOffset now)
    {
        EnsurePending(now);
        Status = WaitingListOfferStatus.Declined;
        RespondedAt = now;
        Touch(now);
    }

    public void MarkExpired(DateTimeOffset now)
    {
        if (Status != WaitingListOfferStatus.Pending)
        {
            return;
        }

        Status = WaitingListOfferStatus.Expired;
        RespondedAt = now;
        Touch(now);
    }

    private void EnsurePending(DateTimeOffset now)
    {
        if (Status != WaitingListOfferStatus.Pending)
        {
            throw new DomainException($"Cannot respond to offer from status {Status}.");
        }

        if (now >= ExpiresAt)
        {
            throw new DomainException("Offer has expired.");
        }
    }
}
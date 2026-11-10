namespace MedicalAppointments.Application.Outbox;

public static class OutboxTypes
{
    public const string FoundationTestEvent = "Foundation.TestEvent";
    public const string ScheduleChanged = "Scheduling.ScheduleChanged";
    public const string AvailabilityChanged = "Scheduling.AvailabilityChanged";

    public const string AppointmentCreated = "Appointments.Created";
    public const string AppointmentCancelled = "Appointments.Cancelled";
    public const string AppointmentRescheduled = "Appointments.Rescheduled";
    public const string AppointmentStatusChanged = "Appointments.StatusChanged";

    public const string WaitingListOfferCreated = "WaitingList.OfferCreated";
    public const string WaitingListUpdated = "WaitingList.Updated";
    public const string WaitingListSlotFreed = "WaitingList.SlotFreed";

    public const string WaitingRoomUpdated = "WaitingRoom.Updated";
    public const string WaitingRoomPatientCalled = "WaitingRoom.PatientCalled";
    public const string WaitingRoomPatientRecalled = "WaitingRoom.PatientRecalled";

    public const string ClinicalNoteCreated = "ClinicalRecords.NoteCreated";
    public const string ClinicalNoteUpdated = "ClinicalRecords.NoteUpdated";
    public const string ClinicalNoteFinalized = "ClinicalRecords.NoteFinalized";
    public const string ClinicalAttachmentUploaded = "ClinicalRecords.AttachmentUploaded";

    public const string VitalSignsCreated = "ClinicalRecords.VitalSignsCreated";
    public const string VitalSignsUpdated = "ClinicalRecords.VitalSignsUpdated";
    public const string VitalSignsFinalized = "ClinicalRecords.VitalSignsFinalized";

    public const string DiagnosisCreated = "ClinicalRecords.DiagnosisCreated";
    public const string DiagnosisUpdated = "ClinicalRecords.DiagnosisUpdated";
    public const string DiagnosisFinalized = "ClinicalRecords.DiagnosisFinalized";

    public const string AllergyCreated = "ClinicalRecords.AllergyCreated";
    public const string AllergyUpdated = "ClinicalRecords.AllergyUpdated";
    public const string AllergyFinalized = "ClinicalRecords.AllergyFinalized";
    public const string AllergyResolved = "ClinicalRecords.AllergyResolved";

    public const string MedicationCreated = "ClinicalRecords.MedicationCreated";
    public const string MedicationUpdated = "ClinicalRecords.MedicationUpdated";
    public const string MedicationFinalized = "ClinicalRecords.MedicationFinalized";
    public const string MedicationStatusChanged = "ClinicalRecords.MedicationStatusChanged";
}
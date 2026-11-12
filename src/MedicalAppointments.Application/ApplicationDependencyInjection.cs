using FluentValidation;
using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Appointments.Auth;
using MedicalAppointments.Application.Appointments.Outbox;
using MedicalAppointments.Application.Auth;
using MedicalAppointments.Application.ClinicalRecords;
using MedicalAppointments.Application.ClinicalRecords.Auth;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Foundation;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Organizations;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Application.MedicalStructure.Resources;
using MedicalAppointments.Application.MedicalStructure.Rooms;
using MedicalAppointments.Application.MedicalStructure.Specialties;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Application.Reports;
using MedicalAppointments.Application.Scheduling.Availability;
using MedicalAppointments.Application.Scheduling.Blockouts;
using MedicalAppointments.Application.Scheduling.DoctorSchedules;
using MedicalAppointments.Application.Scheduling.Holidays;
using MedicalAppointments.Application.Scheduling.Outbox;
using MedicalAppointments.Application.Scheduling.ScheduleExceptions;
using MedicalAppointments.Application.Users;
using MedicalAppointments.Application.WaitingList;
using MedicalAppointments.Application.WaitingList.Common;
using MedicalAppointments.Application.WaitingList.Matching;
using MedicalAppointments.Application.WaitingList.Outbox;
using MedicalAppointments.Application.WaitingRoom;
using MedicalAppointments.Application.WaitingRoom.Auth;
using MedicalAppointments.Application.WaitingRoom.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointments.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationDependencyInjection).Assembly);

        services.AddSingleton(JsonDefaults.Options);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IAppointmentReportsService, AppointmentReportsService>();

        services.AddScoped<IFoundationRealtimeTestService, FoundationRealtimeTestService>();

        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IOutboxHandler, FoundationTestRealtimeOutboxHandler>();
        services.AddScoped<IOutboxHandler, ScheduleChangedOutboxHandler>();
        services.AddScoped<IOutboxHandler, AvailabilityChangedOutboxHandler>();

        services.AddScoped<ISpecialtyService, SpecialtyService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IPatientService, PatientService>();

        services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
        services.AddScoped<IScheduleExceptionService, ScheduleExceptionService>();
        services.AddScoped<IBlockoutService, BlockoutService>();
        services.AddScoped<IHolidayService, HolidayService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<ISchedulingEventNotifier, SchedulingEventNotifier>();
        services.AddScoped<IRealtimeAccessService, RealtimeAccessService>();

        services.AddScoped<IAppointmentAccessService, AppointmentAccessService>();
        services.AddScoped<IAppointmentQueryService, AppointmentQueryService>();
        services.AddScoped<IAppointmentBookingService, AppointmentBookingService>();
        services.AddScoped<IAppointmentWorkflowService, AppointmentWorkflowService>();
        services.AddScoped<IAppointmentEventNotifier, AppointmentEventNotifier>();

        services.AddScoped<IOutboxHandler, AppointmentCreatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, AppointmentCancelledOutboxHandler>();
        services.AddScoped<IOutboxHandler, AppointmentRescheduledOutboxHandler>();
        services.AddScoped<IOutboxHandler, AppointmentStatusChangedOutboxHandler>();

        services.Configure<WaitingListOptions>(configuration.GetSection("WaitingList"));

        services.AddScoped<IWaitingListAccessService, WaitingListAccessService>();
        services.AddScoped<IWaitingListService, WaitingListService>();
        services.AddScoped<IWaitingListMatchingService, WaitingListMatchingService>();
        services.AddScoped<IWaitingListEventNotifier, WaitingListEventNotifier>();

        services.AddScoped<IOutboxHandler, WaitingListOfferCreatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, WaitingListUpdatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, WaitingListSlotFreedOutboxHandler>();

        services.AddScoped<IWaitingRoomAccessService, WaitingRoomAccessService>();
        services.AddScoped<IWaitingRoomBoardService, WaitingRoomBoardService>();
        services.AddScoped<IWaitingRoomCallService, WaitingRoomCallService>();
        services.AddScoped<IWaitingRoomEventNotifier, WaitingRoomEventNotifier>();

        services.AddScoped<IOutboxHandler, WaitingRoomUpdatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, PatientCalledOutboxHandler>();
        services.AddScoped<IOutboxHandler, PatientRecalledOutboxHandler>();

        services.AddScoped<IClinicalRecordsAccessService, ClinicalRecordsAccessService>();
        services.AddScoped<IClinicalRecordsService, ClinicalRecordsService>();
        services.AddScoped<IClinicalRecordsEventNotifier, ClinicalRecordsEventNotifier>();

        services.AddScoped<IValidator<MedicalAppointments.Application.ClinicalRecords.Common.CreateClinicalNoteCommand>, CreateClinicalNoteCommandValidator>();
        services.AddScoped<IValidator<MedicalAppointments.Application.ClinicalRecords.Common.UpdateClinicalNoteCommand>, UpdateClinicalNoteCommandValidator>();
        services.AddScoped<IValidator<MedicalAppointments.Application.ClinicalRecords.Common.CorrectClinicalNoteCommand>, CorrectClinicalNoteCommandValidator>();

        services.AddScoped<IOutboxHandler, ClinicalNoteCreatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, ClinicalNoteUpdatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, ClinicalNoteFinalizedOutboxHandler>();

        services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();

        services.AddScoped<IClinicalAttachmentService, ClinicalAttachmentService>();
        services.AddScoped<IOutboxHandler, ClinicalAttachmentUploadedOutboxHandler>();

        services.AddScoped<IVitalSignsService, VitalSignsService>();
        services.AddScoped<IOutboxHandler, VitalSignsCreatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, VitalSignsUpdatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, VitalSignsFinalizedOutboxHandler>();

        services.AddScoped<IDiagnosisService, DiagnosisService>();
        services.AddScoped<IOutboxHandler, DiagnosisCreatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, DiagnosisUpdatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, DiagnosisFinalizedOutboxHandler>();

        services.AddScoped<IPatientAllergyService, PatientAllergyService>();
        services.AddScoped<IOutboxHandler, AllergyCreatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, AllergyUpdatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, AllergyFinalizedOutboxHandler>();
        services.AddScoped<IOutboxHandler, AllergyResolvedOutboxHandler>();

        services.AddScoped<IPatientMedicationService, PatientMedicationService>();
        services.AddScoped<IOutboxHandler, MedicationCreatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, MedicationUpdatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, MedicationFinalizedOutboxHandler>();
        services.AddScoped<IOutboxHandler, MedicationStatusChangedOutboxHandler>();

        services.AddScoped<IPrescriptionsAccessService, PrescriptionsAccessService>();
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IClinicalDocumentService, ClinicalDocumentService>();
        services.AddScoped<IOutboxHandler, PrescriptionCreatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, PrescriptionUpdatedOutboxHandler>();
        services.AddScoped<IOutboxHandler, PrescriptionIssuedOutboxHandler>();
        services.AddScoped<IOutboxHandler, PrescriptionCorrectedOutboxHandler>();
        services.AddScoped<IOutboxHandler, PrescriptionRenewedOutboxHandler>();
        services.AddScoped<IOutboxHandler, PrescriptionCancelledOutboxHandler>();
        services.AddScoped<IOutboxHandler, PrescriptionDispensedOutboxHandler>();
        services.AddScoped<IOutboxHandler, PrescriptionExpiredOutboxHandler>();

        services.AddScoped<IOutboxHandler, ClinicalDocumentGeneratedOutboxHandler>();

        return services;
    }
}

using FluentValidation;
using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Appointments.Auth;
using MedicalAppointments.Application.Appointments.Outbox;
using MedicalAppointments.Application.Auth;
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

        return services;
    }
}
using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Auth;
using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Features;
using MedicalAppointments.Application.Idempotency;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Organizations;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Application.MedicalStructure.Resources;
using MedicalAppointments.Application.MedicalStructure.Rooms;
using MedicalAppointments.Application.MedicalStructure.Specialties;
using MedicalAppointments.Application.Notifications;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Application.Scheduling.Blockouts;
using MedicalAppointments.Application.Scheduling.Common;
using MedicalAppointments.Application.Scheduling.DoctorSchedules;
using MedicalAppointments.Application.Scheduling.Holidays;
using MedicalAppointments.Application.Scheduling.ScheduleExceptions;
using MedicalAppointments.Application.WaitingList.Stores;
using MedicalAppointments.Application.WaitingRoom.Stores;
using MedicalAppointments.Infrastructure.Appointments.Persistence;
using MedicalAppointments.Infrastructure.Auditing;
using MedicalAppointments.Infrastructure.Auth;
using MedicalAppointments.Infrastructure.BackgroundServices;
using MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;
using MedicalAppointments.Infrastructure.Idempotency.Persistence;
using MedicalAppointments.Infrastructure.Identity.Email;
using MedicalAppointments.Infrastructure.Identity.Persistence;
using MedicalAppointments.Infrastructure.Identity.Security;
using MedicalAppointments.Infrastructure.Identity.Seeding;
using MedicalAppointments.Infrastructure.MedicalStructure.Persistence;
using MedicalAppointments.Infrastructure.MedicalStructure.Seeding;
using MedicalAppointments.Infrastructure.Notifications;
using MedicalAppointments.Infrastructure.Persistence;
using MedicalAppointments.Infrastructure.Realtime;
using MedicalAppointments.Infrastructure.Scheduling.Occupancy;
using MedicalAppointments.Infrastructure.Scheduling.Persistence;
using MedicalAppointments.Infrastructure.Scheduling.Time;
using MedicalAppointments.Infrastructure.Services;
using MedicalAppointments.Infrastructure.WaitingList.Jobs;
using MedicalAppointments.Infrastructure.WaitingList.Persistence;
using MedicalAppointments.Infrastructure.WaitingRoom.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointments.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxWriter, EfOutboxWriter>();
        services.AddScoped<IOutboxMessageStore, EfOutboxMessageStore>();

        services.AddScoped<IUserStore, EfUserStore>();
        services.AddScoped<IRefreshTokenStore, EfRefreshTokenStore>();
        services.AddScoped<IdentitySeeder>();

        services.AddScoped<ISpecialtyStore, EfSpecialtyStore>();
        services.AddScoped<IOrganizationStore, EfOrganizationStore>();
        services.AddScoped<IClinicStore, EfClinicStore>();
        services.AddScoped<IRoomStore, EfRoomStore>();
        services.AddScoped<IResourceStore, EfResourceStore>();
        services.AddScoped<IDoctorStore, EfDoctorStore>();
        services.AddScoped<IPatientStore, EfPatientStore>();
        services.AddScoped<MedicalStructureSeeder>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISecureTokenGenerator, CryptographicSecureTokenGenerator>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<IIdentityEmailLinkBuilder, ConfigurationIdentityEmailLinkBuilder>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddSingleton<IFeatureFlagService, ConfigurationFeatureFlagService>();

        services.AddSingleton(TimeProvider.System);

        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.Configure<Application.Auth.JwtOptions>(configuration.GetSection("Auth:Jwt"));
        services.Configure<PasswordOptions>(configuration.GetSection("Auth:Password"));
        services.Configure<RefreshTokenOptions>(configuration.GetSection("Auth:RefreshToken"));
        services.Configure<EmailTokenOptions>(configuration.GetSection("Auth:Email"));

        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.Configure<OutboxProcessingOptions>(configuration.GetSection("Outbox"));
        services.Configure<OutboxWorkerOptions>(configuration.GetSection("Outbox"));

        services.AddHostedService<OutboxProcessorWorker>();

        services.AddSignalR();
        services.AddSingleton<IRealtimePublisher, SignalRRealtimePublisher>();

        services.AddScoped<IDoctorScheduleStore, EfDoctorScheduleStore>();
        services.AddScoped<IScheduleExceptionStore, EfScheduleExceptionStore>();
        services.AddScoped<IBlockoutStore, EfBlockoutStore>();
        services.AddScoped<IHolidayStore, EfHolidayStore>();
        services.AddScoped<ISchedulingClinicLookup, EfSchedulingClinicLookup>();

        services.AddSingleton<ITimeZoneConverter, TimeZoneInfoConverter>();
        services.AddScoped<IAppointmentOccupancyProvider, EfAppointmentOccupancyProvider>();

        services.AddScoped<IAppointmentStore, EfAppointmentStore>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<IAuditWriter, EfAuditWriter>();
        services.AddScoped<IBookingCoordinator, SqlBookingCoordinator>();
        services.AddScoped<ICurrentPrincipalProvider, CurrentPrincipalProvider>();
        services.AddScoped<IRequestMetadataProvider, RequestMetadataProvider>();
        services.AddScoped<IAppointmentOccupancyProvider, EfAppointmentOccupancyProvider>();

        services.AddScoped<IWaitingListEntryStore, EfWaitingListEntryStore>();
        services.AddScoped<IWaitingListOfferStore, EfWaitingListOfferStore>();
        services.AddHostedService<WaitingListOfferExpirationWorker>();

        services.AddScoped<IWaitingRoomCallStore, EfWaitingRoomCallStore>();

        services.AddScoped<IClinicalNoteStore, EfClinicalNoteStore>();

        ConfigureNotifications(services, configuration);

        return services;
    }

    private static void ConfigureNotifications(
        IServiceCollection services,
        IConfiguration configuration)
    {
        var emailProvider = configuration.GetValue<string>("Notifications:Email:Provider") ?? "Development";

        if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
            services.AddSingleton<IEmailService, SmtpEmailService>();
        }
        else
        {
            services.AddSingleton<IEmailService, DevelopmentEmailService>();
        }

        services.AddSingleton<ISmsService, DevelopmentSmsService>();
        services.AddSingleton<IWhatsAppService, DevelopmentWhatsAppService>();
        services.AddSingleton<IPushNotificationService, DevelopmentPushNotificationService>();
    }
}
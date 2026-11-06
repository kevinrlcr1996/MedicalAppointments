using MedicalAppointments.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace MedicalAppointments.Api.Authorization;

public static class AuthorizationDependencyInjection
{
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy =>
                    policy.Requirements.Add(new PermissionRequirement(permission)));
            }

            options.AddPolicy(Policies.RealtimeTest, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.RealtimeTest)));

            options.AddPolicy(Policies.FoundationTestWrite, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.FoundationTestWrite)));

            options.AddPolicy(Policies.AppointmentsRead, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.AppointmentsReadAll) ||
                    context.User.HasPermission(Permissions.AppointmentsReadAssigned) ||
                    context.User.HasPermission(Permissions.AppointmentsReadOwn)));

            options.AddPolicy(Policies.AppointmentsCreate, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.AppointmentsCreateAll) ||
                    context.User.HasPermission(Permissions.AppointmentsCreateOwn)));

            options.AddPolicy(Policies.AppointmentsCancel, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.AppointmentsCancelAll) ||
                    context.User.HasPermission(Permissions.AppointmentsCancelAssigned) ||
                    context.User.HasPermission(Permissions.AppointmentsCancelOwn)));

            options.AddPolicy(Policies.AppointmentsReschedule, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.AppointmentsRescheduleAll) ||
                    context.User.HasPermission(Permissions.AppointmentsRescheduleAssigned) ||
                    context.User.HasPermission(Permissions.AppointmentsRescheduleOwn)));

            options.AddPolicy(Policies.AppointmentsCheckIn, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.AppointmentsCheckIn)));

            options.AddPolicy(Policies.AppointmentsComplete, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.AppointmentsComplete)));

            options.AddPolicy(Policies.AppointmentsMarkNoShow, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.AppointmentsMarkNoShow)));
        });

        return services;
    }
}
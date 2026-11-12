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

            options.AddPolicy(Policies.WaitingListRead, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.WaitingListReadAll) ||
                    context.User.HasPermission(Permissions.WaitingListReadAssigned) ||
                    context.User.HasPermission(Permissions.WaitingListReadOwn)));

            options.AddPolicy(Policies.WaitingListManage, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.WaitingListManageAll) ||
                    context.User.HasPermission(Permissions.WaitingListManageOwn)));

            options.AddPolicy(Policies.WaitingRoomRead, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.WaitingRoomRead) ||
                    context.User.HasPermission(Permissions.WaitingRoomManage) ||
                    context.User.HasPermission(Permissions.AppointmentsReadAssigned)));

            options.AddPolicy(Policies.WaitingRoomOperate, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.WaitingRoomManage) ||
                    context.User.HasPermission(Permissions.AppointmentsCheckIn) ||
                    context.User.HasPermission(Permissions.AppointmentsComplete) ||
                    context.User.HasPermission(Permissions.AppointmentsMarkNoShow)));

            options.AddPolicy(Policies.ClinicalRecordsRead, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.ClinicalRecordsRead)));

            options.AddPolicy(Policies.ClinicalRecordsManage, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.ClinicalRecordsCreate) ||
                    context.User.HasPermission(Permissions.ClinicalRecordsUpdate) ||
                    context.User.HasPermission(Permissions.ClinicalRecordsFinalize) ||
                    context.User.HasPermission(Permissions.ClinicalRecordsCorrect) ||
                    context.User.HasPermission(Permissions.ClinicalRecordsManageAll)));

            options.AddPolicy(Policies.PrescriptionsRead, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.PrescriptionsRead)));

            options.AddPolicy(Policies.PrescriptionsManage, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.PrescriptionsCreate) ||
                    context.User.HasPermission(Permissions.PrescriptionsIssue) ||
                    context.User.HasPermission(Permissions.PrescriptionsCorrect) ||
                    context.User.HasPermission(Permissions.PrescriptionsRenew) ||
                    context.User.HasPermission(Permissions.PrescriptionsCancel) ||
                    context.User.HasPermission(Permissions.PrescriptionsManageAll)));

            options.AddPolicy(Policies.DocumentsRead, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.DocumentsRead)));

            options.AddPolicy(Policies.DocumentsManage, policy =>
                policy.RequireAssertion(context =>
                    context.User.HasPermission(Permissions.DocumentsManage) ||
                    context.User.HasPermission(Permissions.PrescriptionsManageAll)));
        });

        return services;
    }
}

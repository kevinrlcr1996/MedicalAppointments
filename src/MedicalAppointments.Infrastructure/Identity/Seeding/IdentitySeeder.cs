using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Domain.Identity;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedicalAppointments.Infrastructure.Identity.Seeding;

public sealed class IdentitySeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        AppDbContext dbContext,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<IdentitySeeder> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        await SeedPermissionsAsync(now, cancellationToken);
        await SeedRolesAsync(now, cancellationToken);
        await SeedRolePermissionsAsync(now, cancellationToken);
        await SeedDefaultSuperAdminAsync(now, cancellationToken);
    }

    private async Task SeedPermissionsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Permissions
            .Select(x => x.NormalizedName)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var permissionName in Permissions.All)
        {
            var normalized = permissionName.ToUpperInvariant();

            if (existingSet.Contains(normalized))
            {
                continue;
            }

            var group = permissionName.Contains('.')
                ? permissionName.Split('.')[0]
                : "general";

            _dbContext.Permissions.Add(Permission.Create(
                name: permissionName,
                normalizedName: normalized,
                description: null,
                group: group,
                now: now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Roles
            .Select(x => x.NormalizedName)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var roles = new[]
        {
            (Name: Roles.SuperAdmin, Description: "Full system access", IsSystem: true),
            (Name: Roles.Admin, Description: "Administrative access", IsSystem: true),
            (Name: Roles.Doctor, Description: "Doctor access", IsSystem: true),
            (Name: Roles.Receptionist, Description: "Reception access", IsSystem: true),
            (Name: Roles.Patient, Description: "Patient access", IsSystem: true)
        };

        foreach (var role in roles)
        {
            var normalized = role.Name.ToUpperInvariant();

            if (existingSet.Contains(normalized))
            {
                continue;
            }

            _dbContext.Roles.Add(Role.Create(
                name: role.Name,
                normalizedName: normalized,
                description: role.Description,
                isSystem: role.IsSystem,
                now: now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolePermissionsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var permissions = await _dbContext.Permissions.ToDictionaryAsync(
            x => x.NormalizedName,
            x => x.Id,
            StringComparer.OrdinalIgnoreCase,
            cancellationToken);

        // FIX: Include(x => x.Permissions) para que GrantPermission sea idempotente
        // sobre roles que ya existen en BD. Sin esto, re-seedear fallaba por duplicados
        // y los grants nuevos (appointments.*_own, patients.manage_own) no se aplicaban.
        var roles = await _dbContext.Roles
            .Include(x => x.Permissions)
            .ToDictionaryAsync(
                x => x.NormalizedName,
                x => x,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        await GrantAsync(Roles.SuperAdmin, Permissions.All, now, cancellationToken);

        await GrantAsync(Roles.Admin, new[]
        {
            Permissions.UsersRead,
            Permissions.UsersManage,
            Permissions.PermissionsRead,
            Permissions.PatientsRead,
            Permissions.PatientsCreate,
            Permissions.PatientsUpdate,
            Permissions.DoctorsRead,
            Permissions.SpecialtiesRead,
            Permissions.SpecialtiesManage,
            Permissions.OrganizationsRead,
            Permissions.ClinicsRead,
            Permissions.RoomsRead,
            Permissions.ResourcesRead,
            Permissions.SchedulesRead,
            Permissions.SchedulesManage,
            Permissions.AvailabilityRead,
            Permissions.AppointmentsReadAll,
            Permissions.AppointmentsCreateAll,
            Permissions.AppointmentsUpdateAll,
            Permissions.AppointmentsCancelAll,
            Permissions.AppointmentsRescheduleAll,
            Permissions.AppointmentsCheckIn,
            Permissions.AppointmentsComplete,
            Permissions.AppointmentsMarkNoShow,
            Permissions.AppointmentsOverridePolicy,
            Permissions.WaitingListRead,
            Permissions.WaitingRoomRead,
            Permissions.WaitingRoomManage,
            Permissions.NotificationsRead,
            Permissions.ReportsRead,
            Permissions.SettingsManage,
            Permissions.RealtimeTest,
            Permissions.FoundationTestWrite,
            Permissions.WaitingListReadAll,
            Permissions.WaitingListManageAll,
            Permissions.ClinicalRecordsRead,
            Permissions.ClinicalRecordsManageAll,
        }, now, cancellationToken);

        await GrantAsync(Roles.Doctor, new[]
        {
            Permissions.AppointmentsReadAssigned,
            Permissions.AppointmentsCreateAssigned,
            Permissions.AppointmentsUpdateAssigned,
            Permissions.AppointmentsCancelAssigned,
            Permissions.AppointmentsRescheduleAssigned,
            Permissions.AppointmentsCheckIn,
            Permissions.AppointmentsComplete,
            Permissions.AppointmentsMarkNoShow,
            Permissions.PatientsRead,
            Permissions.MedicalRecordsRead,
            Permissions.MedicalRecordsCreate,
            Permissions.MedicalRecordsUpdate,
            Permissions.PrescriptionsRead,
            Permissions.PrescriptionsCreate,
            Permissions.DocumentsRead,
            Permissions.NotificationsRead,
            Permissions.RealtimeTest,
            Permissions.WaitingListReadAssigned,
            Permissions.ClinicalRecordsRead,
            Permissions.ClinicalRecordsCreate,
            Permissions.ClinicalRecordsUpdate,
            Permissions.ClinicalRecordsFinalize,
            Permissions.ClinicalRecordsCorrect,
            Permissions.DoctorsRead,
        }, now, cancellationToken);

        await GrantAsync(Roles.Receptionist, new[]
        {
            Permissions.AppointmentsReadAll,
            Permissions.AppointmentsCreateAll,
            Permissions.AppointmentsUpdateAll,
            Permissions.AppointmentsCancelAll,
            Permissions.AppointmentsRescheduleAll,
            Permissions.AppointmentsCheckIn,
            Permissions.AppointmentsComplete,
            Permissions.AppointmentsMarkNoShow,
            Permissions.PatientsRead,
            Permissions.PatientsCreate,
            Permissions.PatientsUpdate,
            Permissions.WaitingRoomRead,
            Permissions.WaitingRoomManage,
            Permissions.NotificationsRead,
            Permissions.RealtimeTest,
            Permissions.WaitingListReadAll,
            Permissions.WaitingListManageAll,
            Permissions.ClinicalRecordsRead,
        }, now, cancellationToken);

        await GrantAsync(Roles.Patient, new[]
        {
            Permissions.DoctorsRead,
            Permissions.SpecialtiesRead,
            Permissions.AvailabilityRead,

            Permissions.PatientsManageOwn,

            Permissions.AppointmentsReadOwn,
            Permissions.AppointmentsCreateOwn,
            Permissions.AppointmentsCancelOwn,
            Permissions.AppointmentsRescheduleOwn,

            Permissions.NotificationsRead,
            Permissions.RealtimeTest,

            Permissions.WaitingListReadOwn,
            Permissions.WaitingListManageOwn,
        }, now, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        async Task GrantAsync(string roleName, IEnumerable<string> permissionNames, DateTimeOffset timestamp, CancellationToken ct)
        {
            if (!roles.TryGetValue(roleName.ToUpperInvariant(), out var role))
            {
                return;
            }

            foreach (var permissionName in permissionNames)
            {
                var normalizedPermission = permissionName.ToUpperInvariant();

                if (!permissions.TryGetValue(normalizedPermission, out var permissionId))
                {
                    continue;
                }

                role.GrantPermission(permissionId, timestamp);
            }
        }
    }

    private async Task SeedDefaultSuperAdminAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var enabled = _configuration.GetValue<bool>("Seed:Enabled");

        if (!enabled)
        {
            return;
        }

        var email = _configuration["Seed:SuperAdminEmail"];
        var password = _configuration["Seed:SuperAdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Seed is enabled but Seed:SuperAdminEmail or Seed:SuperAdminPassword is missing.");
            return;
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();

        var exists = await _dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (exists)
        {
            return;
        }

        var hasher = new Security.Pbkdf2PasswordHasher();
        var passwordHash = hasher.Hash(password);

        var user = User.Create(
            email: email,
            normalizedEmail: normalizedEmail,
            passwordHash: passwordHash,
            firstName: "Super",
            lastName: "Admin",
            isEmailConfirmed: true,
            now: now);

        _dbContext.Users.Add(user);

        var superAdminRole = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.NormalizedName == Roles.SuperAdmin.ToUpperInvariant(), cancellationToken);

        if (superAdminRole is not null)
        {
            user.AssignRole(superAdminRole.Id, now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Default SuperAdmin seeded for {Email}. Change the password immediately.", email);
    }
}
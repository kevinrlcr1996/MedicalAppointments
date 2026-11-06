using MedicalAppointments.Infrastructure.Identity.Seeding;
using MedicalAppointments.Infrastructure.MedicalStructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MedicalAppointments.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider serviceProvider,
        bool migrate,
        bool seed,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DbInitializer));

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (migrate)
        {
            logger.LogInformation("Applying EF Core migrations.");
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        if (seed)
        {
            logger.LogInformation("Seeding database.");

            var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            await identitySeeder.SeedAsync(cancellationToken);

            var medicalSeeder = scope.ServiceProvider.GetRequiredService<MedicalStructureSeeder>();
            await medicalSeeder.SeedAsync(cancellationToken);
        }
    }
}
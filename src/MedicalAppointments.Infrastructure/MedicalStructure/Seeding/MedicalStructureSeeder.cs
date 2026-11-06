using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MedicalAppointments.Infrastructure.MedicalStructure.Seeding;

public sealed class MedicalStructureSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MedicalStructureSeeder> _logger;

    public MedicalStructureSeeder(
        AppDbContext dbContext,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<MedicalStructureSeeder> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        await SeedSpecialtiesAsync(now, cancellationToken);

        if (_configuration.GetValue<bool>("Seed:DemoData"))
        {
            await SeedDemoStructureAsync(now, cancellationToken);
        }
    }

    private async Task SeedSpecialtiesAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Specialties
            .Select(x => x.NormalizedName)
            .ToListAsync(cancellationToken);

        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var specialties = new[]
        {
            ("Medicina General", "GEN", "Atención primaria y medicina general"),
            ("Pediatría", "PED", "Salud infantil"),
            ("Cardiología", "CAR", "Enfermedades cardiovasculares"),
            ("Dermatología", "DER", "Enfermedades de la piel"),
            ("Ginecología", "GIN", "Salud femenina"),
            ("Neurología", "NEU", "Sistema nervioso"),
            ("Traumatología", "TRA", "Sistema musculoesquelético"),
            ("Odontología", "ODO", "Salud dental")
        };

        foreach (var (name, code, description) in specialties)
        {
            var normalizedName = name.ToUpperInvariant();

            if (existingSet.Contains(normalizedName))
            {
                continue;
            }

            _dbContext.Specialties.Add(Specialty.Create(
                name: name,
                code: code,
                description: description,
                isActive: true,
                now: now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDemoStructureAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var organization = await _dbContext.Organizations
            .FirstOrDefaultAsync(x => x.NormalizedName == "CLINICA DEMO", cancellationToken);

        if (organization is null)
        {
            organization = Organization.Create(
                name: "Clínica Demo",
                legalName: "Clínica Demo S.A.",
                taxId: "DEMO123456",
                email: "demo@clinicademo.local",
                phone: "+000000000000",
                address: "Calle Ficticia 123",
                city: "Ciudad Demo",
                country: "ES",
                isActive: true,
                now: now);

            _dbContext.Organizations.Add(organization);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var clinic = await _dbContext.Clinics
            .FirstOrDefaultAsync(x => x.OrganizationId == organization.Id && x.NormalizedCode == "MAIN", cancellationToken);

        if (clinic is null)
        {
            clinic = Clinic.Create(
                organizationId: organization.Id,
                name: "Sede Principal",
                code: "MAIN",
                description: "Sede de demostración",
                address: "Avenida Demo 456",
                city: "Ciudad Demo",
                country: "ES",
                phone: "+000000000001",
                email: "main@clinicademo.local",
                timeZoneId: "Europe/Madrid",
                isActive: true,
                now: now);

            _dbContext.Clinics.Add(clinic);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var room = await _dbContext.Rooms
            .FirstOrDefaultAsync(x => x.ClinicId == clinic.Id && x.NormalizedCode == "R1", cancellationToken);

        if (room is null)
        {
            room = Room.Create(
                clinicId: clinic.Id,
                name: "Consultorio 1",
                code: "R1",
                capacity: 1,
                notes: "Consultorio de demostración",
                isActive: true,
                now: now);

            _dbContext.Rooms.Add(room);
        }

        var resource = await _dbContext.Resources
            .FirstOrDefaultAsync(x => x.ClinicId == clinic.Id && x.NormalizedCode == "EQ-US-01", cancellationToken);

        if (resource is null)
        {
            resource = Resource.Create(
                clinicId: clinic.Id,
                name: "Ecógrafo Demo",
                code: "EQ-US-01",
                kind: ResourceKind.Equipment,
                description: "Equipo de demostración",
                isActive: true,
                now: now);

            _dbContext.Resources.Add(resource);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var generalMedicine = await _dbContext.Specialties
            .FirstOrDefaultAsync(x => x.NormalizedCode == "GEN", cancellationToken);

        var doctor = await _dbContext.Doctors
            .Include(x => x.Clinics)
            .FirstOrDefaultAsync(x => x.NormalizedLicenseNumber == "DEMO-001" && x.NormalizedLicenseCountry == "ES", cancellationToken);

        if (doctor is null)
        {
            doctor = Doctor.Create(
                userId: null,
                firstName: "Demo",
                lastName: "Doctor",
                email: "demo.doctor@clinicademo.local",
                phone: "+000000000002",
                licenseNumber: "DEMO-001",
                licenseCountry: "ES",
                biography: "Médico de demostración. No es un profesional real.",
                photoUrl: null,
                gender: Gender.Unknown,
                dateOfBirth: new DateOnly(1980, 1, 1),
                status: DoctorStatus.Active,
                now: now);

            if (generalMedicine is not null)
            {
                doctor.ReplaceSpecialties([generalMedicine.Id], now);
            }

            doctor.ReplaceClinics(
                [new DoctorClinicAssignment(clinic.Id, true, null, null)],
                now);

            doctor.ReplaceLanguages(["ES", "EN"], now);

            _dbContext.Doctors.Add(doctor);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var patient = await _dbContext.Patients
            .FirstOrDefaultAsync(x => x.NormalizedIdentificationNumber == "00000000X", cancellationToken);

        if (patient is null)
        {
            patient = Patient.Create(
                userId: null,
                firstName: "Demo",
                lastName: "Paciente",
                dateOfBirth: new DateOnly(1990, 5, 20),
                gender: Gender.Unknown,
                identificationNumber: "00000000X",
                phone: "+000000000003",
                email: "demo.patient@example.local",
                address: "Calle Paciente Demo 789",
                emergencyContact: "Contacto Demo",
                emergencyPhone: "+000000000004",
                bloodType: BloodType.Unknown,
                insuranceProvider: "Seguro Demo",
                insuranceNumber: "INS-DEMO-001",
                now: now);

            _dbContext.Patients.Add(patient);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Medical structure demo data seeded. Use only fictitious data.");
    }
}
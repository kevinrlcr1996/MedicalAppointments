using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.Auditing;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Idempotency;
using MedicalAppointments.Domain.Identity;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Domain.Outbox;
using MedicalAppointments.Domain.Scheduling;
using MedicalAppointments.Domain.WaitingList;
using MedicalAppointments.Domain.WaitingRoom;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSpecialty> DoctorSpecialties => Set<DoctorSpecialty>();
    public DbSet<DoctorClinic> DoctorClinics => Set<DoctorClinic>();
    public DbSet<DoctorLanguage> DoctorLanguages => Set<DoctorLanguage>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<DoctorScheduleDay> DoctorScheduleDays => Set<DoctorScheduleDay>();
    public DbSet<ScheduleException> ScheduleExceptions => Set<ScheduleException>();
    public DbSet<Blockout> Blockouts => Set<Blockout>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<WaitingListEntry> WaitingListEntries => Set<WaitingListEntry>();
    public DbSet<WaitingListOffer> WaitingListOffers => Set<WaitingListOffer>();

    public DbSet<WaitingRoomCall> WaitingRoomCalls => Set<WaitingRoomCall>();

    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<ClinicalAttachment> ClinicalAttachments => Set<ClinicalAttachment>();

    public DbSet<VitalSigns> VitalSigns => Set<VitalSigns>();

    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();

    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();

    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
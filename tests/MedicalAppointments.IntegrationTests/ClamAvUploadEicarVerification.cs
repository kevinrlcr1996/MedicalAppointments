using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MedicalAppointments.Application.ClinicalRecords.Storage;
using MedicalAppointments.Contracts.ClinicalRecords;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointments.IntegrationTests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ClamAvFactAttribute : FactAttribute
{
    public ClamAvFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("MA_TEST_CLAMAV_ENABLED"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set MA_TEST_CLAMAV_ENABLED=true and configure MA_TEST_CLAMAV_HOST/PORT to run the live ClamAV EICAR verification.";
        }
    }
}

[Collection("Api")]
public sealed class ClamAvUploadEicarVerification : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public ClamAvUploadEicarVerification(ApiTestFixture fixture) => _fixture = fixture;

    [ClamAvFact]
    public async Task Eicar_upload_returns_409_audits_and_rolls_back_storage_and_metadata()
    {
        var doctorId = Guid.Empty;
        var appointmentId = Guid.Empty;
        var noteId = Guid.Empty;

        try
        {
            using (var scope = _fixture.Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var admin = await db.Users.FirstAsync(x => x.Email == _fixture.SuperAdminEmail);
                var patientId = await db.Patients.Select(x => x.Id).FirstAsync();
                var clinicId = await db.Clinics.Select(x => x.Id).FirstAsync();
                var now = DateTimeOffset.UtcNow;

                var doctor = Doctor.Create(admin.Id, "AV Test", "Doctor", null, null,
                    $"AV-{Guid.NewGuid():N}", "CR", null, null, Gender.Unknown, null, DoctorStatus.Active, now);
                doctorId = doctor.Id;
                var appointment = Appointment.Create(patientId, doctorId, clinicId, null,
                    DateOnly.FromDateTime(now.UtcDateTime), now.AddMinutes(-30), now.AddMinutes(30),
                    "AV integration test", null, null, now);
                appointment.Confirm(now);
                appointment.CheckIn(now);
                appointment.Start(now);
                appointmentId = appointment.Id;
                db.Doctors.Add(doctor);
                db.Appointments.Add(appointment);
                await db.SaveChangesAsync();
            }

            var token = await TestAuthHelper.GetSuperAdminTokenAsync(
                _fixture.Factory, _fixture.SuperAdminEmail, _fixture.SuperAdminPassword);
            using var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);
            var noteResponse = await client.PostAsJsonAsync(
                $"/api/v1/clinical-records/appointments/{appointmentId}/notes",
                new CreateClinicalNoteRequest(ClinicalNoteCategory.Other, "AV test", "EICAR upload verification", DateTimeOffset.UtcNow));
            Assert.Equal(HttpStatusCode.Created, noteResponse.StatusCode);
            var note = JsonSerializer.Deserialize<ClinicalNoteResponse>(
                await noteResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } });
            Assert.NotNull(note);
            noteId = note!.Id;

            const string eicar = "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.ASCII.GetBytes(eicar));
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(file, "file", "eicar.pdf");
            var response = await client.PostAsync($"/api/v1/clinical-records/notes/{noteId}/attachments", form);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            using var verifyScope = _fixture.Factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.False(await verifyDb.ClinicalAttachments.AnyAsync(x => x.NoteId == noteId));
            var audit = await verifyDb.AuditLogs.FirstOrDefaultAsync(x =>
                x.Action == "ClinicalRecords.AttachmentRejectedInfected" && x.EntityId == noteId);
            Assert.NotNull(audit);
            Assert.Contains("sha256", audit!.NewValues, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("eicar.pdf", audit.NewValues, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, verifyScope.ServiceProvider.GetRequiredService<InMemoryTestFileStorage>().FileCount);
        }
        finally
        {
            using var cleanupScope = _fixture.Factory.Services.CreateScope();
            var db = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (noteId != Guid.Empty)
            {
                var notes = await db.ClinicalNotes.Where(x => x.Id == noteId).ToListAsync();
                db.ClinicalNotes.RemoveRange(notes);
                await db.SaveChangesAsync();
            }
            if (appointmentId != Guid.Empty)
            {
                var notes = await db.ClinicalNotes.Where(x => x.AppointmentId == appointmentId).ToListAsync();
                db.ClinicalNotes.RemoveRange(notes);
                await db.SaveChangesAsync();
                var appointment = await db.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId);
                if (appointment is not null) db.Appointments.Remove(appointment);
                await db.SaveChangesAsync();
            }
            if (doctorId != Guid.Empty)
            {
                var doctor = await db.Doctors.FirstOrDefaultAsync(x => x.Id == doctorId);
                if (doctor is not null) db.Doctors.Remove(doctor);
                await db.SaveChangesAsync();
            }
        }
    }
}

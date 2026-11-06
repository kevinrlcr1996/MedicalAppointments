using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.UnitTests.Domain;

public class DoctorTests
{
    [Fact]
    public void ReplaceClinics_Should_Throw_When_Multiple_Primary_Clinics()
    {
        var now = DateTimeOffset.UtcNow;

        var doctor = Doctor.Create(
            userId: null,
            firstName: "Test",
            lastName: "Doctor",
            email: null,
            phone: null,
            licenseNumber: "LIC-1",
            licenseCountry: "ES",
            biography: null,
            photoUrl: null,
            gender: Gender.Unknown,
            dateOfBirth: null,
            status: DoctorStatus.Active,
            now: now);

        Assert.Throws<DomainException>(() =>
            doctor.ReplaceClinics(
                [
                    new DoctorClinicAssignment(Guid.NewGuid(), true, null, null),
                    new DoctorClinicAssignment(Guid.NewGuid(), true, null, null)
                ],
                now));
    }

    [Fact]
    public void ReplaceSpecialties_Should_Remove_Duplicates()
    {
        var now = DateTimeOffset.UtcNow;
        var specialtyId = Guid.NewGuid();

        var doctor = Doctor.Create(
            null,
            "Test",
            "Doctor",
            null,
            null,
            "LIC-1",
            "ES",
            null,
            null,
            Gender.Unknown,
            null,
            DoctorStatus.Active,
            now);

        doctor.ReplaceSpecialties([specialtyId, specialtyId], now);

        Assert.Single(doctor.Specialties);
    }
}
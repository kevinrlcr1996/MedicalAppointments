using MedicalAppointments.Application.ClinicalRecords.Auth;

namespace MedicalAppointments.UnitTests.Application.ClinicalRecords;

public sealed class PrescriptionsAccessServiceTests
{
    private readonly PrescriptionsAccessService _service = new(null!, null!);

    [Fact]
    public void Read_permission_without_doctor_profile_does_not_grant_patient_scope()
    {
        var subject = Subject(canRead: true, doctorId: null);

        Assert.False(_service.CanReadPrescriptions(subject));
    }

    [Fact]
    public void Read_permission_with_doctor_profile_allows_scoped_reads()
    {
        var subject = Subject(canRead: true, doctorId: Guid.NewGuid());

        Assert.True(_service.CanReadPrescriptions(subject));
    }

    [Fact]
    public void Manage_all_allows_global_reads_without_doctor_profile()
    {
        var subject = Subject(canRead: true, doctorId: null, canManageAll: true);

        Assert.True(_service.CanReadPrescriptions(subject));
    }

    [Fact]
    public void Appointments_read_all_allows_global_prescription_reads()
    {
        var subject = Subject(canRead: true, doctorId: null, canReadAll: true);

        Assert.True(_service.CanReadPrescriptions(subject));
    }

    [Fact]
    public void Pharmacy_permission_allows_operational_reads_without_doctor_profile()
    {
        var subject = Subject(canRead: true, doctorId: null, canDispense: true, canExpire: true);

        Assert.True(_service.CanReadPrescriptions(subject));
        Assert.True(_service.CanReadOperationalPrescriptions(subject));
        Assert.False(_service.CanCreateForPatient(subject));
    }

    private static PrescriptionsAccessSubject Subject(bool canRead, Guid? doctorId, bool canManageAll = false, bool canReadAll = false,
        bool canDispense = false, bool canExpire = false)
        => new(Guid.NewGuid(), doctorId, canRead, false, false, false, false, false,
            canManageAll, canReadAll, canDispense, canExpire, false, false);
}

using MedicalAppointments.Application.ClinicalRecords;
using MedicalAppointments.Domain.ClinicalRecords;
using System.Reflection;

namespace MedicalAppointments.ArchitectureTests;

public class ClinicalRecordsLayerTests
{
    private static readonly Assembly DomainAssembly = typeof(ClinicalNote).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IClinicalRecordsService).Assembly;
    private static readonly Assembly AttachmentDomainAssembly = typeof(MedicalAppointments.Domain.ClinicalRecords.ClinicalAttachment).Assembly;
    private static readonly Assembly AttachmentAppAssembly = typeof(MedicalAppointments.Application.ClinicalRecords.IClinicalAttachmentService).Assembly;
    private static readonly Assembly VitalSignsDomainAssembly = typeof(MedicalAppointments.Domain.ClinicalRecords.VitalSigns).Assembly;
    private static readonly Assembly VitalSignsAppAssembly = typeof(MedicalAppointments.Application.ClinicalRecords.IVitalSignsService).Assembly;
    private static readonly Assembly DiagnosisDomainAssembly = typeof(MedicalAppointments.Domain.ClinicalRecords.Diagnosis).Assembly;
    private static readonly Assembly DiagnosisAppAssembly = typeof(MedicalAppointments.Application.ClinicalRecords.IDiagnosisService).Assembly;
    private static readonly Assembly AllergyDomainAssembly = typeof(MedicalAppointments.Domain.ClinicalRecords.PatientAllergy).Assembly;
    private static readonly Assembly AllergyAppAssembly = typeof(MedicalAppointments.Application.ClinicalRecords.IPatientAllergyService).Assembly;
    private static readonly Assembly MedicationDomainAssembly = typeof(MedicalAppointments.Domain.ClinicalRecords.PatientMedication).Assembly;
    private static readonly Assembly MedicationAppAssembly = typeof(MedicalAppointments.Application.ClinicalRecords.IPatientMedicationService).Assembly;

    // Mismas cadenas prohibidas que los dos [Fact] originales, extraídas a arrays (DRY):
    // el contenido y el orden son idénticos, solo que ahora un solo lugar las mantiene
    // y los 8 [Fact] las reutilizan. No se cambió la política de capas.
    private static readonly string[] DomainForbidden =
    [
        "MedicalAppointments.Application",
        "MedicalAppointments.Infrastructure",
        "MedicalAppointments.Api",
        "MedicalAppointments.Contracts",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore.App",
        "MediatR"
    ];

    private static readonly string[] ApplicationForbidden =
    [
        "MedicalAppointments.Infrastructure",
        "MedicalAppointments.Api",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore.App",
        "MediatR"
    ];

    [Fact]
    public void Domain_ClinicalRecords_Should_Not_Depend_On_Outer_Layers()
    {
        AssertNoReferences(DomainAssembly, DomainForbidden);
    }

    [Fact]
    public void Application_ClinicalRecords_Should_Not_Depend_On_Infrastructure_Or_Frameworks()
    {
        AssertNoReferences(ApplicationAssembly, ApplicationForbidden);
    }

    [Fact]
    public void Domain_ClinicalAttachments_Should_Not_Depend_On_Outer_Layers()
    {
        AssertNoReferences(AttachmentDomainAssembly, DomainForbidden);
    }

    [Fact]
    public void Application_ClinicalAttachments_Should_Not_Depend_On_Infrastructure_Or_Frameworks()
    {
        AssertNoReferences(AttachmentAppAssembly, ApplicationForbidden);
    }

    [Fact]
    public void Domain_VitalSigns_Should_Not_Depend_On_Outer_Layers()
    {
        AssertNoReferences(VitalSignsDomainAssembly, DomainForbidden);
    }

    [Fact]
    public void Application_VitalSigns_Should_Not_Depend_On_Infrastructure_Or_Frameworks()
    {
        AssertNoReferences(VitalSignsAppAssembly, ApplicationForbidden);
    }

    [Fact]
    public void Domain_Diagnoses_Should_Not_Depend_On_Outer_Layers()
    {
        AssertNoReferences(DiagnosisDomainAssembly, DomainForbidden);
    }

    [Fact]
    public void Application_Diagnoses_Should_Not_Depend_On_Infrastructure_Or_Frameworks()
    {
        AssertNoReferences(DiagnosisAppAssembly, ApplicationForbidden);
    }

    [Fact] public void Domain_PatientAllergies_Should_Not_Depend_On_Outer_Layers() => AssertNoReferences(AllergyDomainAssembly, DomainForbidden);
    [Fact] public void Application_PatientAllergies_Should_Not_Depend_On_Infrastructure_Or_Frameworks() => AssertNoReferences(AllergyAppAssembly, ApplicationForbidden);
    [Fact] public void Domain_PatientMedications_Should_Not_Depend_On_Outer_Layers() => AssertNoReferences(MedicationDomainAssembly, DomainForbidden);
    [Fact] public void Application_PatientMedications_Should_Not_Depend_On_Infrastructure_Or_Frameworks() => AssertNoReferences(MedicationAppAssembly, ApplicationForbidden);

    // Reescrito fully-qualified (HashSet/StringComparer con prefijo, foreach en vez de LINQ)
    // para que compile con o sin <ImplicitUsings> y sin warnings CS0105. Semántica idéntica:
    // "ninguna referencia cruzada a capa/framework prohibida". El Assert.Fail por cadena
    // cruza da diagnóstico específico (cuál assembly + cuál cadena), no un Assert.Empty genérico.
    private static void AssertNoReferences(Assembly assembly, params string[] forbiddenNames)
    {
        var referenced = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var name in assembly.GetReferencedAssemblies())
        {
            if (!string.IsNullOrWhiteSpace(name.Name))
            {
                referenced.Add(name.Name!);
            }
        }

        foreach (var forbidden in forbiddenNames)
        {
            if (referenced.Contains(forbidden))
            {
                Assert.Fail(
                    $"Assembly '{assembly.GetName().Name}' references forbidden layer/framework '{forbidden}'. " +
                    "Domain must not reference application/infrastructure/api/contracts/EF/AspNetCore/MediatR; " +
                    "application must not reference infrastructure/api/EF/AspNetCore/MediatR.");
            }
        }
    }
}
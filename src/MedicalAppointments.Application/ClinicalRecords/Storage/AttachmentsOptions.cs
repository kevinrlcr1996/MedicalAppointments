namespace MedicalAppointments.Application.ClinicalRecords.Storage;

public sealed class AttachmentsOptions
{
    public const string SectionName = "Attachments";

    public string StorageRoot { get; set; } = "uploads/clinical";
    public long MaxBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
    public string[] AllowedContentTypes { get; set; } =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public ScannerOptions Scanner { get; set; } = new();

    public sealed class ScannerOptions
    {
        public bool Enabled { get; set; }
        public string ClamAvHost { get; set; } = "127.0.0.1";
        public int ClamAvPort { get; set; } = 3310;
        public int TimeoutMilliseconds { get; set; } = 15000;
    }
}
using System.Net;
using System.Net.Sockets;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.ClinicalRecords.Storage;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.ClinicalRecords.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.IntegrationTests;

public sealed class ClamAvFileScannerTests
{
    [Fact]
    public async Task Disabled_scanner_returns_skipped_explicitly()
    {
        var scanner = CreateScanner(enabled: false, host: "127.0.0.1", port: 1);

        var result = await scanner.ScanAsync(new MemoryStream([1, 2, 3]), "sample.pdf");

        Assert.Equal(ClinicalAttachmentScanStatus.Skipped, result.Status);
    }

    [Fact]
    public async Task Enabled_scanner_throws_when_clamd_is_unavailable()
    {
        var port = ReserveAndReleaseLocalPort();
        var scanner = CreateScanner(enabled: true, host: "127.0.0.1", port);

        await Assert.ThrowsAsync<AttachmentScannerUnavailableException>(
            () => scanner.ScanAsync(new MemoryStream([1, 2, 3]), "sample.pdf"));
    }

    private static ClamAvFileScanner CreateScanner(bool enabled, string host, int port)
    {
        var options = Options.Create(new AttachmentsOptions
        {
            Scanner = new AttachmentsOptions.ScannerOptions
            {
                Enabled = enabled,
                ClamAvHost = host,
                ClamAvPort = port,
                TimeoutMilliseconds = 500
            }
        });

        return new ClamAvFileScanner(options, NullLogger<ClamAvFileScanner>.Instance);
    }

    private static int ReserveAndReleaseLocalPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

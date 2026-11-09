using System.Net;
using System.Net.Sockets;
using System.Text;
using MedicalAppointments.Application.ClinicalRecords.Storage;
using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Storage;

public sealed class ClamAvFileScanner : IFileScanner
{
    private readonly AttachmentsOptions.ScannerOptions _options;
    private readonly ILogger<ClamAvFileScanner> _logger;

    public ClamAvFileScanner(IOptions<AttachmentsOptions> optionsAccessor, ILogger<ClamAvFileScanner> logger)
    {
        _options = optionsAccessor.Value.Scanner;
        _logger = logger;
    }

    public async Task<ScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return new ScanResult(ClinicalAttachmentScanStatus.Skipped, "Scanner disabled by configuration.");
        }

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.TimeoutMilliseconds);

            await client.ConnectAsync(_options.ClamAvHost, _options.ClamAvPort, timeoutCts.Token);
            await using var netStream = client.GetStream();

            // Protocolo clamd: INSTREAM + \n + bytes + \n.\0 (fin de stream)
            var handshake = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await netStream.WriteAsync(handshake, timeoutCts.Token);

            content.Position = 0;
            var buffer = new byte[8192];
            int read;
            while ((read = await content.ReadAsync(buffer, timeoutCts.Token)) > 0)
            {
                var chunkLen = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(read));
                await netStream.WriteAsync(chunkLen, timeoutCts.Token);
                await netStream.WriteAsync(buffer.AsMemory(0, read), timeoutCts.Token);
            }

            await netStream.WriteAsync(new byte[4], timeoutCts.Token); // longitud 0 = fin
            await netStream.FlushAsync(timeoutCts.Token);

            var response = await ReadLineAsync(netStream, timeoutCts.Token);
            content.Position = 0;

            if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
            {
                return new ScanResult(ClinicalAttachmentScanStatus.Infected, response.Trim());
            }

            if (response.Contains("OK", StringComparison.OrdinalIgnoreCase))
            {
                return new ScanResult(ClinicalAttachmentScanStatus.Clean, response.Trim());
            }

            // Respuesta inesperada: no bloqueamos (decisión de config), marcamos Skipped + log.
            _logger.LogWarning("ClamAV returned unexpected response for {File}: {Response}", fileName, response);
            return new ScanResult(ClinicalAttachmentScanStatus.Skipped, "Unexpected scanner response.");
        }
        catch (Exception ex)
        {
            // AV indisponible: NO bloqueamos el upload (config explícita), Skipped + log.
            _logger.LogWarning(ex, "ClamAV scan unavailable for {File}; accepted as unscanned.", fileName);
            return new ScanResult(ClinicalAttachmentScanStatus.Skipped, "Scanner unavailable.");
        }
    }

    private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        int b;
        while ((b = stream.ReadByte()) is not (-1) and not 0)
        {
            sb.Append((char)b);
        }

        return sb.ToString();
    }
}
using System.Net.Sockets;
using System.Text;
using LinkVault.Application.Abstractions;
using LinkVault.Domain.Enums;

namespace LinkVault.Infrastructure.Storage;

public sealed class ClamAvVirusScanService(
    string host,
    int port,
    IQuarantineStorageService quarantineStorage) : IVirusScanService
{
    private readonly string _host = host;
    private readonly int _port = port;
    private readonly IQuarantineStorageService _quarantineStorage = quarantineStorage;

    public async Task<FileScanStatus> ScanAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileBytes = await _quarantineStorage.ReadAsync(storedFileName, cancellationToken);

            using var tcp = new TcpClient();
            await tcp.ConnectAsync(_host, _port, cancellationToken);

            await using var stream = tcp.GetStream();

            var command = Encoding.ASCII.GetBytes("zINSTREAM\0");
            await stream.WriteAsync(command, cancellationToken);

            const int chunkSize = 2048;
            var offset = 0;

            while (offset < fileBytes.Length)
            {
                var length = Math.Min(chunkSize, fileBytes.Length - offset);

                var lengthBytes = BitConverter.GetBytes(length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                await stream.WriteAsync(lengthBytes, cancellationToken);
                await stream.WriteAsync(fileBytes.AsMemory(offset, length), cancellationToken);
                offset += length;
            }

            await stream.WriteAsync(new byte[4], cancellationToken);
            await stream.FlushAsync(cancellationToken);

            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
            var response = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim('\0', '\n');

            return response.Contains("OK") ? FileScanStatus.Clean : FileScanStatus.Infected;
        }
        catch (Exception)
        {
            return FileScanStatus.Failed;
        }
    }
}
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace DualSenseXboxBridge.Backends;

/// <summary>
/// Thin client for the VIIPER TCP API. Requests are null terminated, the server closes the
/// connection after each response, and errors arrive as RFC 7807 style JSON.
/// </summary>
internal static class ViiperApi
{
    public static async Task<JsonDocument> SendAsync(string host, int port, string request, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        using var client = new TcpClient { NoDelay = true };
        await client.ConnectAsync(host, port, timeout.Token).ConfigureAwait(false);
        await using var stream = client.GetStream();

        var payload = Encoding.UTF8.GetBytes(request + "\0");
        await stream.WriteAsync(payload, timeout.Token).ConfigureAwait(false);
        await stream.FlushAsync(timeout.Token).ConfigureAwait(false);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, timeout.Token).ConfigureAwait(false);

        var text = Encoding.UTF8.GetString(buffer.ToArray()).Trim('\0', '\r', '\n', ' ');
        if (string.IsNullOrWhiteSpace(text))
        {
            return JsonDocument.Parse("{}");
        }

        var document = JsonDocument.Parse(text);
        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("status", out var status) &&
            status.ValueKind == JsonValueKind.Number &&
            status.GetInt32() >= 400)
        {
            var detail = document.RootElement.TryGetProperty("detail", out var detailElement)
                ? detailElement.GetString()
                : null;
            document.Dispose();
            throw new InvalidOperationException(detail ?? $"VIIPER rejected '{request}' (status {status.GetInt32()}).");
        }

        return document;
    }
}

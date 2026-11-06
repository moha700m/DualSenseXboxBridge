using System.Net;
using System.Net.Sockets;
using System.Text;
using DualSenseXboxBridge.Core.Viiper;
using Xunit;

namespace DualSenseXboxBridge.Core.Tests;

public class ViiperApiTests
{
    [Fact]
    public async Task SendAsync_WritesNullTerminatedRequestAndParsesResponse()
    {
        await using var server = new FakeViiperServer(_ => "{\"server\":\"VIIPER\",\"version\":\"9.9.9\"}");

        using var document = await ViiperApi.SendAsync("127.0.0.1", server.Port, "ping", CancellationToken.None);

        Assert.Equal("VIIPER", document.RootElement.GetProperty("server").GetString());
        Assert.Equal("9.9.9", document.RootElement.GetProperty("version").GetString());
        Assert.Equal("ping\0", server.LastRequest);
    }

    [Fact]
    public async Task SendAsync_SendsDeviceAddPayloadVerbatim()
    {
        await using var server = new FakeViiperServer(_ => "{\"busId\":1,\"devId\":\"1\"}");
        const string request = "bus/1/add {\"type\":\"xbox360\"}";

        using var document = await ViiperApi.SendAsync("127.0.0.1", server.Port, request, CancellationToken.None);

        Assert.Equal("1", document.RootElement.GetProperty("devId").GetString());
        Assert.Equal(request + "\0", server.LastRequest);
    }

    [Fact]
    public async Task SendAsync_ThrowsOnProblemDetailsResponse()
    {
        await using var server = new FakeViiperServer(
            _ => "{\"status\":400,\"title\":\"Bad Request\",\"detail\":\"missing payload\"}");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ViiperApi.SendAsync("127.0.0.1", server.Port, "bus/create", CancellationToken.None));

        Assert.Contains("missing payload", exception.Message);
    }

    [Fact]
    public async Task SendAsync_ReturnsEmptyObjectForEmptyResponse()
    {
        await using var server = new FakeViiperServer(_ => string.Empty);

        using var document = await ViiperApi.SendAsync("127.0.0.1", server.Port, "bus/remove 1", CancellationToken.None);

        Assert.Equal(System.Text.Json.JsonValueKind.Object, document.RootElement.ValueKind);
    }

    private sealed class FakeViiperServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly Func<string, string> _handler;
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Task _loop;
        private readonly List<string> _requests = new();

        public FakeViiperServer(Func<string, string> handler)
        {
            _handler = handler;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _loop = Task.Run(AcceptLoopAsync);
        }

        public int Port { get; }

        public string LastRequest
        {
            get
            {
                lock (_requests)
                {
                    return _requests.Count == 0 ? string.Empty : _requests[^1];
                }
            }
        }

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    using var client = await _listener.AcceptTcpClientAsync(_cancellation.Token);
                    await HandleClientAsync(client);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using var stream = client.GetStream();
            var buffer = new List<byte>();
            var single = new byte[1];

            while (true)
            {
                var read = await stream.ReadAsync(single.AsMemory(0, 1), _cancellation.Token);
                if (read == 0)
                {
                    break;
                }

                buffer.Add(single[0]);
                if (single[0] == 0)
                {
                    break;
                }
            }

            var request = Encoding.UTF8.GetString(buffer.ToArray());
            lock (_requests)
            {
                _requests.Add(request);
            }

            var body = _handler(request.TrimEnd('\0'));
            if (body.Length > 0)
            {
                var bytes = Encoding.UTF8.GetBytes(body);
                await stream.WriteAsync(bytes, _cancellation.Token);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cancellation.Cancel();
            _listener.Stop();
            try
            {
                await _loop;
            }
            catch
            {
            }

            _cancellation.Dispose();
        }
    }
}

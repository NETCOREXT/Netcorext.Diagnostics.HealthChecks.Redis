using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Netcorext.Diagnostics.HealthChecks.Redis;

internal class RedisClient : IDisposable
{
    private const int DEFAULT_PORT = 6379;
    private const string CMD_PING = "*1\r\n$4\r\nPING\r\n";
    private const string CMD_PONG = "+PONG\r\n";

    private Socket _client;
    private readonly string _host;
    private readonly int? _port;

    public RedisClient(string host, int? port = DEFAULT_PORT)
    {
        _host = host;
        _port = port;
    }

    public int CommandTimeout { get; set; } = 5 * 1000;
    public int ConnectTimeout { get; set; } = 5 * 1000;

    public async Task ConnectAsync()
    {
        if (!IPAddress.TryParse(_host, out var ip))
            ip = (await Dns.GetHostAddressesAsync(_host)).FirstOrDefault();

        if (ip == null)
            throw new ArgumentException($"Unknown Host({_host})");

        _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                  {
                      SendTimeout = CommandTimeout,
                      ReceiveTimeout = CommandTimeout
                  };

        var result = _client.ConnectAsync(ip, _port ?? DEFAULT_PORT);

        var index = Task.WaitAny(new[] { result }, TimeSpan.FromMilliseconds(ConnectTimeout));

        if (index == -1)
        {
            if (!_client.Connected)
            {
                _client.Close();
            }

            throw new TimeoutException("Connect timeout");
        }
    }

    public async Task<(bool Success, string Result)> PingAsync()
    {
        var bytes = Encoding.UTF8.GetBytes(CMD_PING);

        await _client.SendAsync(new ArraySegment<byte>(bytes), SocketFlags.None);

        var receiveBuffer = new byte[1024];

        var offset = await _client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), SocketFlags.None);

        using var sr = new StreamReader(new MemoryStream(receiveBuffer, 0, offset), Encoding.UTF8);

        var result = await sr.ReadToEndAsync();

        return (!string.IsNullOrWhiteSpace(result) && result.Equals(CMD_PONG, StringComparison.OrdinalIgnoreCase), result);
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisposeAsync().Wait();
    }
}
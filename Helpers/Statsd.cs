using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RealistikOsu.Cron.Helpers;

/// <summary>
/// Minimal DogStatsD sender. Does nothing while the host is empty, and never throws, so metrics
/// can't take the service down. The agent's address is looked up again every minute, because the
/// agent container usually starts after this service and can change address.
/// </summary>
public sealed class Statsd(IConfiguration configuration) : IDisposable
{
    private static readonly TimeSpan ResolveEvery = TimeSpan.FromMinutes(1);

    private readonly UdpClient _udp = new();
    private readonly string _host = configuration["STATSD_HOST"] ?? string.Empty;
    private readonly int _port = int.TryParse(configuration["STATSD_PORT"], out var port) ? port : 8125;
    private IPEndPoint? _endpoint;
    private DateTime _nextResolve = DateTime.MinValue;

    public void Gauge(string name, double value, string? tags = null) => Send(name, value, "g", tags);

    public void Timing(string name, double milliseconds, string? tags = null) => Send(name, milliseconds, "ms", tags);

    private IPEndPoint? Endpoint()
    {
        if (DateTime.UtcNow < _nextResolve) return _endpoint;

        _nextResolve = DateTime.UtcNow + ResolveEvery;
        try
        {
            var address = Dns.GetHostAddresses(_host).FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            if (address != null) _endpoint = new IPEndPoint(address, _port);
        }
        catch (SocketException)
        {
            // Not up yet. Keep the last address and try again next minute.
        }

        return _endpoint;
    }

    private void Send(string name, double value, string type, string? tags)
    {
        if (_host.Length == 0) return;

        var endpoint = Endpoint();
        if (endpoint == null) return;

        var line = string.Create(CultureInfo.InvariantCulture, $"{name}:{value}|{type}");
        if (tags != null) line += "|#" + tags;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(line);
            _udp.Send(bytes, bytes.Length, endpoint);
        }
        catch (SocketException)
        {
            // The agent being down is not worth a log line per cycle.
        }
    }

    public void Dispose() => _udp.Dispose();
}

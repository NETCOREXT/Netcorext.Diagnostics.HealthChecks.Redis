using System.Text.RegularExpressions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Netcorext.Diagnostics.HealthChecks.Redis;

public class RedisHealthChecker : IHealthCheck
{
    private const int DEFAULT_PORT = 6379;

    private readonly RedisHealthCheckOptions _options;
    private readonly string _host;
    private readonly int? _port;
    private readonly Regex _regex = new Regex("([^:]+):(\\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public RedisHealthChecker(RedisHealthCheckOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.Connection)) throw new ArgumentNullException(nameof(options.Connection));

        _options = options;

        var m = _regex.Match(_options.Connection);

        if (!m.Success || m.Groups.Count < 1) throw new ArgumentNullException(nameof(options.Connection));

        _host = m.Groups[1].Value;

        if (m.Groups.Count == 3 && int.TryParse(m.Groups[2].Value, out var port))
            _port = port;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            await using var client = new RedisClient(_host, _port)
                                     {
                                         ConnectTimeout = _options.ConnectTimeout,
                                         CommandTimeout = _options.CommandTimeout
                                     };

            await client.ConnectAsync();

            var (success, result) = await client.PingAsync();

            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        { nameof(result), result }
                    };

            return success ? HealthCheckResult.Healthy(data: d) : HealthCheckResult.Unhealthy(data: d);
        }
        catch (Exception e)
        {
            return HealthCheckResult.Unhealthy(e.Message, e);
        }
    }
}
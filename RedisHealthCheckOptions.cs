namespace Netcorext.Diagnostics.HealthChecks.Redis;

public class RedisHealthCheckOptions
{
    public string Connection { get; set; }
    public int ConnectTimeout { get; set; } = 5 * 1000;
    public int CommandTimeout { get; set; } = 5 * 1000;
}
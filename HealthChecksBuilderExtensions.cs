using Microsoft.Extensions.Diagnostics.HealthChecks;
using Netcorext.Diagnostics.HealthChecks.Redis;

namespace Microsoft.Extensions.DependencyInjection;

public static class HealthChecksBuilderExtensions
{
    private const string NAME = "Redis";

    public static IHealthChecksBuilder AddRedis(this IHealthChecksBuilder builder, Func<IServiceProvider, RedisHealthCheckOptions> factory, string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
    {
        return AddRedis<RedisHealthChecker>(builder, factory, name, failureStatus, tags, timeout);
    }

    public static IHealthChecksBuilder AddRedis<THealthChecker>(this IHealthChecksBuilder builder, Func<IServiceProvider, RedisHealthCheckOptions> factory, string name = default, HealthStatus? failureStatus = default, IEnumerable<string> tags = default, TimeSpan? timeout = default)
    {
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        return builder.Add(new HealthCheckRegistration(name ?? NAME + "-" + typeof(THealthChecker).Name,
                                                       provider => (IHealthCheck)Activator.CreateInstance(typeof(THealthChecker), factory.Invoke(provider)),
                                                       failureStatus,
                                                       tags,
                                                       timeout));
    }
}
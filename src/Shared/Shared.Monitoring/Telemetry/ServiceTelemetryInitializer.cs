using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Shared.Monitoring.Telemetry;

/// <summary>
/// Sets cloud role name and instance on all telemetry items so that
/// Application Insights Application Map groups each microservice correctly.
/// </summary>
public class ServiceTelemetryInitializer : ITelemetryInitializer
{
    private readonly string _roleName;
    private readonly string _roleInstance;

    public ServiceTelemetryInitializer(string roleName)
    {
        _roleName = roleName;
        _roleInstance = Environment.MachineName;
    }

    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = _roleName;
        telemetry.Context.Cloud.RoleInstance = _roleInstance;
    }
}

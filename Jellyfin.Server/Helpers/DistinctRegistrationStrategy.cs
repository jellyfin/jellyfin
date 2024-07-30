using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;

namespace Jellyfin.Server.Helpers;

/// <summary>
/// Skip registering the descriptor if it exists.
/// </summary>
public class DistinctRegistrationStrategy : RegistrationStrategy
{
    /// <summary>
    /// The distinct registration strategy instance.
    /// </summary>
    public static readonly DistinctRegistrationStrategy Instance = new();

    /// <inheritdoc />
    public override void Apply(IServiceCollection services, ServiceDescriptor descriptor)
    {
        if (services.Any(service => service.ServiceType == descriptor.ServiceType && service.ImplementationType == descriptor.ImplementationType))
        {
            return;
        }

        services.Add(descriptor);
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace AnomaliImportTool.Core.Application.DependencyInjection;

/// <summary>
/// Attribute to mark services for automatic registration with dependency injection container
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ServiceRegistrationAttribute : Attribute
{
    public ServiceLifetime Lifetime { get; }
    public Type? ServiceType { get; }

    /// <summary>
    /// Register service with automatic interface detection
    /// </summary>
    /// <param name="lifetime">Service lifetime</param>
    public ServiceRegistrationAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// Register service with specific interface type
    /// </summary>
    /// <param name="serviceType">Interface type to register</param>
    /// <param name="lifetime">Service lifetime</param>
    public ServiceRegistrationAttribute(Type serviceType, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ServiceType = serviceType;
        Lifetime = lifetime;
    }
} 
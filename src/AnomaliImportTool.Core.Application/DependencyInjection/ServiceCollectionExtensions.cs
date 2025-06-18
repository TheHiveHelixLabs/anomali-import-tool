using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AnomaliImportTool.Core.Application.DependencyInjection;

/// <summary>
/// Extension methods for automatic service registration using assembly scanning
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Automatically register services from assemblies using ServiceRegistrationAttribute
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServicesFromAssemblies(this IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            services.AddServicesFromAssembly(assembly);
        }
        return services;
    }

    /// <summary>
    /// Automatically register services from a single assembly
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly to scan</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServicesFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var serviceTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetCustomAttribute<ServiceRegistrationAttribute>() != null)
            .ToList();

        foreach (var implementationType in serviceTypes)
        {
            var attribute = implementationType.GetCustomAttribute<ServiceRegistrationAttribute>()!;
            var serviceType = attribute.ServiceType ?? GetServiceInterface(implementationType);

            if (serviceType != null)
            {
                RegisterService(services, serviceType, implementationType, attribute.Lifetime);
            }
        }

        return services;
    }

    /// <summary>
    /// Register all implementations of a specific interface from assemblies
    /// </summary>
    /// <typeparam name="TInterface">Interface type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="lifetime">Service lifetime</param>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddImplementationsOf<TInterface>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        params Assembly[] assemblies)
    {
        var interfaceType = typeof(TInterface);
        
        foreach (var assembly in assemblies)
        {
            var implementations = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => interfaceType.IsAssignableFrom(type))
                .ToList();

            foreach (var implementation in implementations)
            {
                RegisterService(services, interfaceType, implementation, lifetime);
            }
        }

        return services;
    }

    /// <summary>
    /// Register services by convention (IServiceName -> ServiceName)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="lifetime">Service lifetime</param>
    /// <param name="assemblies">Assemblies to scan</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddServicesByConvention(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var serviceTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .ToList();

            foreach (var implementationType in serviceTypes)
            {
                var conventionInterface = GetConventionInterface(implementationType);
                if (conventionInterface != null)
                {
                    RegisterService(services, conventionInterface, implementationType, lifetime);
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Register decorators for a service type
    /// </summary>
    /// <typeparam name="TService">Service interface</typeparam>
    /// <typeparam name="TDecorator">Decorator implementation</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="lifetime">Service lifetime</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDecorator<TService, TDecorator>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TDecorator : class, TService
    {
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TService));
        if (serviceDescriptor != null)
        {
            services.Remove(serviceDescriptor);
            
            services.Add(new ServiceDescriptor(
                typeof(TDecorator),
                serviceDescriptor.ImplementationType!,
                serviceDescriptor.Lifetime));

            services.Add(new ServiceDescriptor(
                typeof(TService),
                provider => ActivatorUtilities.CreateInstance<TDecorator>(provider, provider.GetRequiredService<TDecorator>()),
                lifetime));
        }

        return services;
    }

    private static Type? GetServiceInterface(Type implementationType)
    {
        // First, try to find interface with same name pattern (IServiceName)
        var conventionInterface = GetConventionInterface(implementationType);
        if (conventionInterface != null)
        {
            return conventionInterface;
        }

        // Fallback to first interface that's not in System namespace
        return implementationType.GetInterfaces()
            .FirstOrDefault(i => !i.Namespace?.StartsWith("System") == true);
    }

    private static Type? GetConventionInterface(Type implementationType)
    {
        var expectedInterfaceName = $"I{implementationType.Name}";
        return implementationType.GetInterfaces()
            .FirstOrDefault(i => i.Name == expectedInterfaceName);
    }

    private static void RegisterService(IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        var serviceDescriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
        services.Add(serviceDescriptor);
    }
} 
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AnomaliImportTool.Core.Application.DependencyInjection;

/// <summary>
/// Application layer service registration module
/// </summary>
public static class ApplicationServiceRegistration
{
    /// <summary>
    /// Register all application layer services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register services using attribute-based registration
        services.AddServicesFromAssembly(assembly);

        // Register specific application services
        services.AddApplicationUseCases(assembly);
        services.AddApplicationValidators(assembly);
        services.AddApplicationMappers(assembly);

        return services;
    }

    /// <summary>
    /// Register use case handlers and command/query handlers
    /// </summary>
    private static IServiceCollection AddApplicationUseCases(this IServiceCollection services, Assembly assembly)
    {
        // Register command handlers
        services.AddImplementationsOf<ICommandHandler>(ServiceLifetime.Scoped, assembly);
        
        // Register query handlers  
        services.AddImplementationsOf<IQueryHandler>(ServiceLifetime.Scoped, assembly);
        
        // Register use case handlers
        services.AddImplementationsOf<IUseCaseHandler>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register validation services
    /// </summary>
    private static IServiceCollection AddApplicationValidators(this IServiceCollection services, Assembly assembly)
    {
        // Register validators using convention
        services.AddServicesByConvention(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register mapping services
    /// </summary>
    private static IServiceCollection AddApplicationMappers(this IServiceCollection services, Assembly assembly)
    {
        // Register mappers using convention
        var mapperTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.Name.EndsWith("Mapper") || type.Name.EndsWith("Profile"))
            .ToList();

        foreach (var mapperType in mapperTypes)
        {
            services.AddScoped(mapperType);
        }

        return services;
    }
}

// Marker interfaces for service discovery
public interface ICommandHandler { }
public interface IQueryHandler { }
public interface IUseCaseHandler { } 
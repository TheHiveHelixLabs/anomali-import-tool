using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using AnomaliImportTool.Core.Application.DependencyInjection;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;
using AnomaliImportTool.Core.Application.Interfaces.Repositories;

namespace AnomaliImportTool.Infrastructure.DependencyInjection;

/// <summary>
/// Infrastructure layer service registration module
/// </summary>
public static class InfrastructureServiceRegistration
{
    /// <summary>
    /// Register all infrastructure layer services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register services using attribute-based registration
        services.AddServicesFromAssembly(assembly);

        // Register infrastructure services by interface
        services.AddInfrastructureRepositories(assembly);
        services.AddInfrastructureServices(assembly);
        services.AddExternalServices(assembly);

        return services;
    }

    /// <summary>
    /// Register repository implementations
    /// </summary>
    private static IServiceCollection AddInfrastructureRepositories(this IServiceCollection services, Assembly assembly)
    {
        // Register all repository implementations
        services.AddImplementationsOf<IDocumentRepository>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IThreatBulletinRepository>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register infrastructure service implementations
    /// </summary>
    private static IServiceCollection AddInfrastructureServices(this IServiceCollection services, Assembly assembly)
    {
        // Register infrastructure service implementations
        services.AddImplementationsOf<IFileProcessingService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<ISecurityService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register external service integrations
    /// </summary>
    private static IServiceCollection AddExternalServices(this IServiceCollection services, Assembly assembly)
    {
        // Register external API services
        services.AddImplementationsOf<IAnomaliApiService>(ServiceLifetime.Scoped, assembly);

        // Register HTTP clients with policies
        services.AddHttpClients();

        return services;
    }

    /// <summary>
    /// Configure HTTP clients with resilience policies
    /// </summary>
    private static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("AnomaliApi", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "AnomaliImportTool/1.0");
        });

        services.AddHttpClient("Default", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        return services;
    }

    /// <summary>
    /// Register specialized infrastructure modules
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSpecializedInfrastructure(this IServiceCollection services)
    {
        // Register services from specialized infrastructure assemblies
        var securityAssembly = GetAssemblyByName("AnomaliImportTool.Security");
        var documentProcessingAssembly = GetAssemblyByName("AnomaliImportTool.DocumentProcessing");
        var apiAssembly = GetAssemblyByName("AnomaliImportTool.Api");
        var gitAssembly = GetAssemblyByName("AnomaliImportTool.Git");

        var assemblies = new[] { securityAssembly, documentProcessingAssembly, apiAssembly, gitAssembly }
            .Where(a => a != null)
            .Cast<Assembly>()
            .ToArray();

        if (assemblies.Length > 0)
        {
            services.AddServicesFromAssemblies(assemblies);
        }

        return services;
    }

    private static Assembly? GetAssemblyByName(string assemblyName)
    {
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            return null;
        }
    }
} 
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using AnomaliImportTool.Core.Application.DependencyInjection;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

namespace AnomaliImportTool.Api.DependencyInjection;

/// <summary>
/// API service registration module for Anomali ThreatStream integration
/// </summary>
public static class ApiServiceRegistration
{
    /// <summary>
    /// Register all API services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register services using attribute-based registration
        services.AddServicesFromAssembly(assembly);

        // Register API-specific services
        services.AddAnomaliApiServices(assembly);
        services.AddHttpClientServices(assembly);
        services.AddApiAuthenticationServices(assembly);
        services.AddApiResponseServices(assembly);

        return services;
    }

    /// <summary>
    /// Register Anomali ThreatStream API services
    /// </summary>
    private static IServiceCollection AddAnomaliApiServices(this IServiceCollection services, Assembly assembly)
    {
        // Register Anomali API implementations
        services.AddImplementationsOf<IAnomaliApiService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IThreatStreamApiService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IBulletinApiService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IIndicatorApiService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IIntelligenceApiService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register HTTP client services
    /// </summary>
    private static IServiceCollection AddHttpClientServices(this IServiceCollection services, Assembly assembly)
    {
        // Register HTTP client services
        services.AddImplementationsOf<IHttpClientService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IApiRequestService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IApiResponseService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IRateLimitingService>(ServiceLifetime.Scoped, assembly);

        // Configure named HTTP clients
        services.AddHttpClient("AnomaliThreatStream", client =>
        {
            client.BaseAddress = new Uri("https://api.threatstream.com/");
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.Add("User-Agent", "AnomaliImportTool/1.0");
        });

        services.AddHttpClient("AnomaliOptic", client =>
        {
            client.BaseAddress = new Uri("https://optic.threatstream.com/");
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.Add("User-Agent", "AnomaliImportTool/1.0");
        });

        return services;
    }

    /// <summary>
    /// Register API authentication services
    /// </summary>
    private static IServiceCollection AddApiAuthenticationServices(this IServiceCollection services, Assembly assembly)
    {
        // Register authentication services
        services.AddImplementationsOf<IApiAuthenticationService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IApiKeyService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IOAuthService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<ITokenRefreshService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register API response handling services
    /// </summary>
    private static IServiceCollection AddApiResponseServices(this IServiceCollection services, Assembly assembly)
    {
        // Register response handling services
        services.AddImplementationsOf<IApiResponseParserService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IApiErrorHandlingService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IApiRetryService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IApiCacheService>(ServiceLifetime.Scoped, assembly);

        return services;
    }
}

// API service marker interfaces
public interface IThreatStreamApiService { }
public interface IBulletinApiService { }
public interface IIndicatorApiService { }
public interface IIntelligenceApiService { }
public interface IHttpClientService { }
public interface IApiRequestService { }
public interface IApiResponseService { }
public interface IRateLimitingService { }
public interface IApiAuthenticationService { }
public interface IApiKeyService { }
public interface IOAuthService { }
public interface ITokenRefreshService { }
public interface IApiResponseParserService { }
public interface IApiErrorHandlingService { }
public interface IApiRetryService { }
public interface IApiCacheService { } 
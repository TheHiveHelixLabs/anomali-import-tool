using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using AnomaliImportTool.Core.Application.DependencyInjection;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

namespace AnomaliImportTool.Security.DependencyInjection;

/// <summary>
/// Security service registration module
/// </summary>
public static class SecurityServiceRegistration
{
    /// <summary>
    /// Register all security services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register services using attribute-based registration
        services.AddServicesFromAssembly(assembly);

        // Register security-specific services
        services.AddCryptographyServices(assembly);
        services.AddAuthenticationServices(assembly);
        services.AddAuthorizationServices(assembly);
        services.AddAuditServices(assembly);

        return services;
    }

    /// <summary>
    /// Register cryptography services
    /// </summary>
    private static IServiceCollection AddCryptographyServices(this IServiceCollection services, Assembly assembly)
    {
        // Register encryption/decryption services
        services.AddImplementationsOf<IEncryptionService>(ServiceLifetime.Singleton, assembly);
        services.AddImplementationsOf<IHashingService>(ServiceLifetime.Singleton, assembly);
        services.AddImplementationsOf<IDigitalSignatureService>(ServiceLifetime.Singleton, assembly);

        return services;
    }

    /// <summary>
    /// Register authentication services
    /// </summary>
    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services, Assembly assembly)
    {
        // Register authentication providers
        services.AddImplementationsOf<IAuthenticationService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<ITokenService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<ICredentialService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register authorization services
    /// </summary>
    private static IServiceCollection AddAuthorizationServices(this IServiceCollection services, Assembly assembly)
    {
        // Register authorization providers
        services.AddImplementationsOf<IAuthorizationService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IPolicyEvaluationService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IPermissionService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register audit and logging services
    /// </summary>
    private static IServiceCollection AddAuditServices(this IServiceCollection services, Assembly assembly)
    {
        // Register security audit services
        services.AddImplementationsOf<ISecurityAuditService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<ISecurityEventService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IThreatDetectionService>(ServiceLifetime.Scoped, assembly);

        return services;
    }
}

// Security service marker interfaces
public interface IEncryptionService { }
public interface IHashingService { }
public interface IDigitalSignatureService { }
public interface IAuthenticationService { }
public interface ITokenService { }
public interface ICredentialService { }
public interface IAuthorizationService { }
public interface IPolicyEvaluationService { }
public interface IPermissionService { }
public interface ISecurityAuditService { }
public interface ISecurityEventService { }
public interface IThreatDetectionService { } 
using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using AnomaliImportTool.Core.Application.Interfaces.Repositories;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;
using AnomaliImportTool.Core.Application.Interfaces.Services;

namespace AnomaliImportTool.Core.Application.DependencyInjection;

/// <summary>
/// Service lifetime configuration system for Microsoft.Extensions.DependencyInjection
/// Provides comprehensive guidance and patterns for proper service lifetime management
/// </summary>
public static class ServiceLifetimeConfiguration
{
    /// <summary>
    /// Configure all services with appropriate lifetimes based on their characteristics
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection ConfigureServiceLifetimes(this IServiceCollection services)
    {
        // Configure singleton services (application-wide, thread-safe, expensive to create)
        services.ConfigureSingletonServices();
        
        // Configure scoped services (per request/operation, maintains state within scope)
        services.ConfigureScopedServices();
        
        // Configure transient services (stateless, lightweight, created on demand)
        services.ConfigureTransientServices();
        
        // Configure factory patterns for complex object creation
        services.ConfigureFactoryServices();
        
        // Configure conditional service registration
        services.ConfigureConditionalServices();

        return services;
    }

    /// <summary>
    /// Configure singleton services - Use for:
    /// - Configuration objects
    /// - Logging services
    /// - Caching services
    /// - Thread-safe stateless services
    /// - Expensive-to-create services
    /// </summary>
    private static IServiceCollection ConfigureSingletonServices(this IServiceCollection services)
    {
        // Configuration services (immutable, shared across application)
        // Note: These will be registered by their respective modules when implemented
        // services.AddSingleton<IApplicationConfiguration, ApplicationConfiguration>();
        // services.AddSingleton<ISecurityConfiguration, SecurityConfiguration>();
        
        // Caching services (shared state, thread-safe)
        // Note: These will be registered by Infrastructure module when implemented
        // services.AddSingleton<IMemoryCache, MemoryCache>();
        // services.AddSingleton<ICacheService, CacheService>();
        
        // HTTP clients (expensive to create, thread-safe)
        // Note: These will be registered by Infrastructure module when implemented
        // services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
        
        // Metrics and monitoring (shared across application)
        // Note: These will be registered by Infrastructure module when implemented
        // services.AddSingleton<IMetricsCollector, MetricsCollector>();
        // services.AddSingleton<IPerformanceCounter, PerformanceCounter>();
        
        // Event aggregators (shared messaging infrastructure)
        // Note: These will be registered by Infrastructure module when implemented
        // services.AddSingleton<IEventAggregator, EventAggregator>();
        
        return services;
    }

    /// <summary>
    /// Configure scoped services - Use for:
    /// - Database contexts
    /// - Unit of work patterns
    /// - Request-specific services
    /// - Services that maintain state within a scope
    /// </summary>
    private static IServiceCollection ConfigureScopedServices(this IServiceCollection services)
    {
        // Data access services (per-operation context)
        // Note: These will be registered by their respective modules
        // services.AddScoped<IDocumentRepository, DocumentRepository>();
        // services.AddScoped<IThreatBulletinRepository, ThreatBulletinRepository>();
        
        // Business logic services (maintain state within operation)
        // Note: These will be registered by their respective modules
        // services.AddScoped<IFileProcessingService, FileProcessingService>();
        // services.AddScoped<IAnomaliApiService, AnomaliApiService>();
        // services.AddScoped<IGitService, GitService>();
        // services.AddScoped<ISecurityService, SecurityService>();
        
        // Example scoped service registrations (demonstrate patterns)
        // Note: These will be registered by their respective modules when implemented
        // services.AddScoped<IUnitOfWork, UnitOfWork>();
        // services.AddScoped<IThreatAnalysisService, ThreatAnalysisService>();
        // services.AddScoped<IImportOrchestrationService, ImportOrchestrationService>();
        // services.AddScoped<IAuthenticationService, AuthenticationService>();
        // services.AddScoped<IAuthorizationService, AuthorizationService>();
        // services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        // services.AddScoped<IApiRateLimitService, ApiRateLimitService>();
        // services.AddScoped<IWorkflowEngine, WorkflowEngine>();
        // services.AddScoped<ITaskScheduler, TaskScheduler>();
        
        return services;
    }

    /// <summary>
    /// Configure transient services - Use for:
    /// - Stateless services
    /// - Lightweight objects
    /// - Services that are cheap to create
    /// - Services with short lifespans
    /// </summary>
    private static IServiceCollection ConfigureTransientServices(this IServiceCollection services)
    {
        // Validation services (stateless, lightweight)
        // Note: These will be registered by their respective modules when implemented
        // services.AddTransient<IDocumentValidator, DocumentValidator>();
        // services.AddTransient<IFileFormatValidator, FileFormatValidator>();
        // services.AddTransient<IInputSanitizer, InputSanitizer>();
        
        // Mapping services (stateless transformations)
        // Note: These will be registered by their respective modules when implemented
        // services.AddTransient<IDocumentMapper, DocumentMapper>();
        // services.AddTransient<IThreatBulletinMapper, ThreatBulletinMapper>();
        // services.AddTransient<IApiResponseMapper, ApiResponseMapper>();
        
        // Utility services (stateless helpers)
        // Note: These will be registered by their respective modules when implemented
        // services.AddTransient<IFileHashCalculator, FileHashCalculator>();
        // services.AddTransient<IPathValidator, PathValidator>();
        // services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        
        // Command and query handlers (stateless, per-request)
        // Note: These will be registered by their respective modules when implemented
        // services.AddTransient<ICommandDispatcher, CommandDispatcher>();
        // services.AddTransient<IQueryDispatcher, QueryDispatcher>();
        
        // Notification services (stateless, short-lived)
        // Note: INotificationService will be registered by Application module
        // services.AddTransient<IEmailService, EmailService>();
        
        // Cryptographic services (stateless, security-sensitive)
        // Note: These will be registered by their respective modules when implemented
        // services.AddTransient<IEncryptionService, EncryptionService>();
        // services.AddTransient<IHashingService, HashingService>();
        // services.AddTransient<IDigitalSignatureService, DigitalSignatureService>();
        
        return services;
    }

    /// <summary>
    /// Configure factory services for complex object creation scenarios
    /// </summary>
    private static IServiceCollection ConfigureFactoryServices(this IServiceCollection services)
    {
        // Document processor factory (creates different processors based on file type)
        // Note: These will be registered by their respective modules when implemented
        // services.AddSingleton<IDocumentProcessorFactory, DocumentProcessorFactory>();
        // services.AddTransient<IPdfDocumentProcessor, PdfDocumentProcessor>();
        // services.AddTransient<IWordDocumentProcessor, WordDocumentProcessor>();
        // services.AddTransient<IExcelDocumentProcessor, ExcelDocumentProcessor>();
        
        // Security provider factory (creates different security providers)
        // Note: These will be registered by their respective modules when implemented
        // services.AddSingleton<ISecurityProviderFactory, SecurityProviderFactory>();
        // services.AddTransient<IWindowsSecurityProvider, WindowsSecurityProvider>();
        // services.AddTransient<IOAuthSecurityProvider, OAuthSecurityProvider>();
        
        // API client factory (creates different API clients)
        // Note: These will be registered by their respective modules when implemented
        // services.AddSingleton<IApiClientFactory, ApiClientFactory>();
        // services.AddTransient<IAnomaliApiClient, AnomaliApiClient>();
        // services.AddTransient<IMispApiClient, MispApiClient>();
        
        return services;
    }

    /// <summary>
    /// Configure conditional service registration based on environment or configuration
    /// </summary>
    private static IServiceCollection ConfigureConditionalServices(this IServiceCollection services)
    {
        // Development-specific services
        // Note: These will be registered by their respective modules when implemented
        // services.AddConditionalService<IFileWatcherService, DevelopmentFileWatcherService>(
        //     condition: IsDebugEnvironment,
        //     lifetime: ServiceLifetime.Singleton);
        
        // Production-specific services
        // Note: These will be registered by their respective modules when implemented
        // services.AddConditionalService<IFileWatcherService, ProductionFileWatcherService>(
        //     condition: IsProductionEnvironment,
        //     lifetime: ServiceLifetime.Singleton);
        
        // Feature flag based services
        // Note: These will be registered by their respective modules when implemented
        // services.AddConditionalService<IOcrService, AdvancedOcrService>(
        //     condition: IsAdvancedOcrEnabled,
        //     lifetime: ServiceLifetime.Scoped);
        
        // services.AddConditionalService<IOcrService, BasicOcrService>(
        //     condition: () => !IsAdvancedOcrEnabled(),
        //     lifetime: ServiceLifetime.Scoped);
        
        return services;
    }

    /// <summary>
    /// Add service with condition
    /// </summary>
    private static IServiceCollection AddConditionalService<TInterface, TImplementation>(
        this IServiceCollection services,
        Func<bool> condition,
        ServiceLifetime lifetime)
        where TImplementation : class, TInterface
        where TInterface : class
    {
        if (condition())
        {
            services.Add(new ServiceDescriptor(typeof(TInterface), typeof(TImplementation), lifetime));
        }
        
        return services;
    }

    // Environment detection methods
    private static bool IsDebugEnvironment() => 
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    
    private static bool IsProductionEnvironment() => 
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";
    
    private static bool IsAdvancedOcrEnabled() => 
        bool.Parse(Environment.GetEnvironmentVariable("ENABLE_ADVANCED_OCR") ?? "false");

    /// <summary>
    /// Validate service lifetime configurations
    /// </summary>
    /// <param name="services">Service collection to validate</param>
    /// <returns>Validation results</returns>
    public static ServiceLifetimeValidationResult ValidateServiceLifetimes(this IServiceCollection services)
    {
        var result = new ServiceLifetimeValidationResult();
        
        // Check for common anti-patterns
        ValidateSingletonAntiPatterns(services, result);
        ValidateScopedAntiPatterns(services, result);
        ValidateTransientAntiPatterns(services, result);
        ValidateDependencyChains(services, result);
        
        return result;
    }

    private static void ValidateSingletonAntiPatterns(IServiceCollection services, ServiceLifetimeValidationResult result)
    {
        var singletonServices = services.Where(s => s.Lifetime == ServiceLifetime.Singleton).ToList();
        
        foreach (var service in singletonServices)
        {
            // Check if singleton depends on scoped or transient services
            var dependencies = GetServiceDependencies(service.ImplementationType);
            var problematicDependencies = dependencies
                .Where(dep => services.Any(s => s.ServiceType == dep && s.Lifetime != ServiceLifetime.Singleton))
                .ToList();
            
            if (problematicDependencies.Any())
            {
                result.AddWarning($"Singleton service {service.ServiceType.Name} depends on non-singleton services: {string.Join(", ", problematicDependencies.Select(d => d.Name))}");
            }
        }
    }

    private static void ValidateScopedAntiPatterns(IServiceCollection services, ServiceLifetimeValidationResult result)
    {
        var scopedServices = services.Where(s => s.Lifetime == ServiceLifetime.Scoped).ToList();
        
        foreach (var service in scopedServices)
        {
            // Check for potential memory leaks in scoped services
            if (service.ImplementationType?.GetInterfaces().Any(i => i == typeof(IDisposable)) == true)
            {
                result.AddInfo($"Scoped service {service.ServiceType.Name} implements IDisposable - ensure proper cleanup");
            }
        }
    }

    private static void ValidateTransientAntiPatterns(IServiceCollection services, ServiceLifetimeValidationResult result)
    {
        var transientServices = services.Where(s => s.Lifetime == ServiceLifetime.Transient).ToList();
        
        foreach (var service in transientServices)
        {
            // Check for expensive transient services
            if (IsExpensiveService(service.ImplementationType))
            {
                result.AddWarning($"Transient service {service.ServiceType.Name} appears to be expensive to create - consider Scoped or Singleton lifetime");
            }
        }
    }

    private static void ValidateDependencyChains(IServiceCollection services, ServiceLifetimeValidationResult result)
    {
        // Check for circular dependencies and complex dependency chains
        var dependencyGraph = BuildDependencyGraph(services);
        var circularDependencies = DetectCircularDependencies(dependencyGraph);
        
        foreach (var cycle in circularDependencies)
        {
            result.AddError($"Circular dependency detected: {string.Join(" -> ", cycle)}");
        }
    }

    private static IEnumerable<Type> GetServiceDependencies(Type? serviceType)
    {
        if (serviceType == null) return Enumerable.Empty<Type>();
        
        var constructors = serviceType.GetConstructors();
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();
        
        return primaryConstructor?.GetParameters().Select(p => p.ParameterType) ?? Enumerable.Empty<Type>();
    }

    private static bool IsExpensiveService(Type? serviceType)
    {
        if (serviceType == null) return false;
        
        // Heuristics for expensive services
        var expensivePatterns = new[]
        {
            "Client", "Factory", "Provider", "Connection", "Context", "Cache"
        };
        
        return expensivePatterns.Any(pattern => serviceType.Name.Contains(pattern));
    }

    private static Dictionary<Type, List<Type>> BuildDependencyGraph(IServiceCollection services)
    {
        var graph = new Dictionary<Type, List<Type>>();
        
        foreach (var service in services)
        {
            if (service.ImplementationType != null)
            {
                var dependencies = GetServiceDependencies(service.ImplementationType).ToList();
                graph[service.ServiceType] = dependencies;
            }
        }
        
        return graph;
    }

    private static List<List<Type>> DetectCircularDependencies(Dictionary<Type, List<Type>> graph)
    {
        // Simplified circular dependency detection
        var cycles = new List<List<Type>>();
        var visited = new HashSet<Type>();
        var recursionStack = new HashSet<Type>();
        
        foreach (var node in graph.Keys)
        {
            if (!visited.Contains(node))
            {
                DetectCyclesRecursive(node, graph, visited, recursionStack, new List<Type>(), cycles);
            }
        }
        
        return cycles;
    }

    private static void DetectCyclesRecursive(
        Type node,
        Dictionary<Type, List<Type>> graph,
        HashSet<Type> visited,
        HashSet<Type> recursionStack,
        List<Type> currentPath,
        List<List<Type>> cycles)
    {
        visited.Add(node);
        recursionStack.Add(node);
        currentPath.Add(node);
        
        if (graph.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    DetectCyclesRecursive(neighbor, graph, visited, recursionStack, currentPath, cycles);
                }
                else if (recursionStack.Contains(neighbor))
                {
                    // Found a cycle
                    var cycleStart = currentPath.IndexOf(neighbor);
                    var cycle = currentPath.Skip(cycleStart).ToList();
                    cycle.Add(neighbor); // Complete the cycle
                    cycles.Add(cycle);
                }
            }
        }
        
        recursionStack.Remove(node);
        currentPath.Remove(node);
    }
}

/// <summary>
/// Service lifetime validation result
/// </summary>
public class ServiceLifetimeValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
    public List<string> Info { get; } = new();
    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
    public void AddInfo(string info) => Info.Add(info);

    public string GetSummary()
    {
        return $"Validation Results: {Errors.Count} errors, {Warnings.Count} warnings, {Info.Count} info messages";
    }
}

// Note: Placeholder interface registrations removed to maintain clean architecture compliance
// In real implementation, these interfaces would be defined in their respective interface directories
// and concrete implementations would be in the Infrastructure layer
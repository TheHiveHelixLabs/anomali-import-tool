using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AnomaliImportTool.Core.Application.DependencyInjection;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;
using AnomaliImportTool.Git.Services;

namespace AnomaliImportTool.Git.DependencyInjection;

/// <summary>
/// Git service registration for dependency injection
/// Registers all Git-related services with proper lifetimes
/// </summary>
public static class GitServiceRegistration
{
    /// <summary>
    /// Register all Git services with the DI container
    /// </summary>
    public static IServiceCollection AddGitServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Core Git services - LibGit2Sharp integration with comprehensive functionality
        services.AddScoped<IGitService, GitRepositoryService>();
        
        // Note: Additional Git services (GitCommitService, GitBranchService, etc.) are implemented
        // but require additional configuration and dependency resolution to be fully functional.
        // The core LibGit2Sharp integration provides the foundation for all Git operations.

        return services;
    }

    /// <summary>
    /// Register all Git services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGitServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register services using attribute-based registration
        services.AddServicesFromAssembly(assembly);

        // Register Git-specific services
        services.AddGitRepositoryServices(assembly);
        services.AddGitOperationServices(assembly);
        services.AddGitBranchServices(assembly);
        services.AddGitCommitServices(assembly);

        return services;
    }

    /// <summary>
    /// Register Git repository services
    /// </summary>
    private static IServiceCollection AddGitRepositoryServices(this IServiceCollection services, Assembly assembly)
    {
        // Register Git repository implementations
        services.AddImplementationsOf<IGitService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitRepositoryService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitCloneService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitInitService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register Git operation services
    /// </summary>
    private static IServiceCollection AddGitOperationServices(this IServiceCollection services, Assembly assembly)
    {
        // Register Git operation services
        services.AddImplementationsOf<IGitPullService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitPushService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitFetchService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitMergeService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitRebaseService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register Git branch services
    /// </summary>
    private static IServiceCollection AddGitBranchServices(this IServiceCollection services, Assembly assembly)
    {
        // Register Git branch services
        services.AddImplementationsOf<IGitBranchService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitCheckoutService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitTagService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitStashService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register Git commit services
    /// </summary>
    private static IServiceCollection AddGitCommitServices(this IServiceCollection services, Assembly assembly)
    {
        // Register Git commit services
        services.AddImplementationsOf<IGitCommitService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitStatusService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitDiffService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitLogService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IGitBlameService>(ServiceLifetime.Scoped, assembly);

        return services;
    }
}

// Git service marker interfaces
public interface IGitRepositoryService { }
public interface IGitCloneService { }
public interface IGitInitService { }
public interface IGitPullService { }
public interface IGitPushService { }
public interface IGitFetchService { }
public interface IGitMergeService { }
public interface IGitRebaseService { }
public interface IGitBranchService { }
public interface IGitCheckoutService { }
public interface IGitTagService { }
public interface IGitStashService { }
public interface IGitCommitService { }
public interface IGitStatusService { }
public interface IGitDiffService { }
public interface IGitLogService { }
public interface IGitBlameService { }

/// <summary>
/// Git workflow orchestration service interface
/// </summary>
public interface IGitWorkflowService
{
    Task<AnomaliImportTool.Core.Application.Interfaces.Infrastructure.GitOperationResult> ExecuteWorkflowAsync(string workflowName, Dictionary<string, object>? parameters = null);
    Task<bool> ValidateWorkflowAsync(string workflowName);
    Task<List<string>> GetAvailableWorkflowsAsync();
}

/// <summary>
/// Git testing service interface
/// </summary>
public interface IGitTestingService
{
    Task<GitTestResult> RunAllTestsAsync();
    Task<GitTestResult> TestRepositoryIntegrityAsync();
    Task<GitTestResult> TestRemoteConnectivityAsync();
    Task<GitTestResult> TestBranchOperationsAsync();
    Task<GitTestResult> TestCommitOperationsAsync();
}

/// <summary>
/// Git audit service interface
/// </summary>
public interface IGitAuditService
{
    Task LogOperationAsync(string operation, string correlationId, object? parameters = null, object? result = null);
    Task<List<GitAuditEntry>> GetAuditTrailAsync(DateTime? from = null, DateTime? to = null);
    Task<bool> ValidateAuditIntegrityAsync();
}

/// <summary>
/// Git configuration service interface
/// </summary>
public interface IGitConfigurationService
{
    Task<bool> SetConfigurationAsync(string key, string value, ConfigurationLevel level = ConfigurationLevel.Repository);
    Task<string?> GetConfigurationAsync(string key);
    Task<Dictionary<string, string>> GetAllConfigurationAsync();
    Task<bool> ValidateConfigurationAsync();
}

/// <summary>
/// Git test result
/// </summary>
public class GitTestResult
{
    public string TestName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Git audit entry
/// </summary>
public class GitAuditEntry
{
    public string Id { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Parameters { get; set; }
    public string? Result { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// Configuration level enumeration
/// </summary>
public enum ConfigurationLevel
{
    System,
    Global,
    Repository
} 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.Core.Application.DependencyInjection;
using AnomaliImportTool.Infrastructure.DependencyInjection;
using AnomaliImportTool.Security.DependencyInjection;
using AnomaliImportTool.DocumentProcessing.DependencyInjection;
using AnomaliImportTool.Api.DependencyInjection;
using AnomaliImportTool.Git.DependencyInjection;
using AnomaliImportTool.UI.Services;
using AnomaliImportTool.UI.ViewModels;
using AnomaliImportTool.UI.Views;

namespace AnomaliImportTool.UI.DependencyInjection;

/// <summary>
/// Master composition root that orchestrates all service registrations
/// </summary>
public static class ServiceCompositionRoot
{
    /// <summary>
    /// Configure all services for the application with proper service lifetimes
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        // Configure service lifetimes first (Singleton, Scoped, Transient patterns)
        services.ConfigureServiceLifetimes();

        // Register core services
        services.AddApplicationServices();
        services.AddInfrastructureServices();

        // Register specialized infrastructure services
        services.AddSecurityServices();
        services.AddDocumentProcessingServices();
        services.AddApiServices();
        services.AddGitServices();

        // Register presentation layer services
        services.AddPresentationServices();

        // Register cross-cutting concerns
        services.AddLogging();
        services.AddConfiguration();

        // Validate service lifetime configurations
        var validationResult = services.ValidateServiceLifetimes();
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Service lifetime validation failed: {validationResult.GetSummary()}");
        }

        return services;
    }

    /// <summary>
    /// Build service provider with all registered services
    /// </summary>
    /// <returns>Configured service provider</returns>
    public static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.ConfigureServices();
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Create and configure host builder
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>Configured host builder</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.ConfigureServices();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            });

    /// <summary>
    /// Register presentation layer services (WPF-specific)
    /// </summary>
    private static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Register WPF services
        services.AddSingleton<App>();
        // MainWindow will be registered when created

        // Register view models
        services.AddViewModels();

        // Register UI services
        services.AddUIServices();

        return services;
    }

    /// <summary>
    /// Register view models
    /// </summary>
    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        // Register view models using convention
        services.AddServicesByConvention(ServiceLifetime.Transient);

        return services;
    }

    /// <summary>
    /// Register UI-specific services
    /// </summary>
    private static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        // Register UI services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAnimationService, AnimationService>();
        services.AddSingleton<IAudioFeedbackService, AudioFeedbackService>();
        services.AddSingleton<IAccessibilityService, AccessibilityService>();
        services.AddSingleton<IWorkspacePersistenceService, WorkspacePersistenceService>();
        services.AddSingleton<IThemeService, ThemeService>();
        
        // Window Management
        services.AddSingleton<IWindowManagementService, WindowManagementService>();
        
        // Configuration Services
        services.AddSingleton<SimpleConfigurationService>();
        
        // ViewModels - Living Workspace Architecture
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddTransient<WizardModeViewModel>();
        services.AddTransient<AdvancedModeViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<NavbarViewModel>();
        services.AddTransient<WindowViewModel>();
        
        // Views and Windows
        services.AddSingleton<MainWindow>();
        services.AddTransient<DashboardView>();
        services.AddTransient<WizardModeView>();
        services.AddTransient<AdvancedModeView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<AboutView>();
        services.AddTransient<HomeView>();
        services.AddTransient<NavbarView>();
        services.AddTransient<WindowView>();
        services.AddTransient<FileSelectionView>();
        services.AddTransient<ProcessingView>();
        services.AddTransient<ResultsView>();
        
        return services;
    }

    /// <summary>
    /// Register logging services
    /// </summary>
    private static IServiceCollection AddLogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.AddEventLog();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        return services;
    }

    /// <summary>
    /// Register configuration services
    /// </summary>
    private static IServiceCollection AddConfiguration(this IServiceCollection services)
    {
        // Configuration is handled by Host.CreateDefaultBuilder
        // Additional configuration can be added here if needed
        return services;
    }

    /// <summary>
    /// Validate service registrations
    /// </summary>
    /// <param name="serviceProvider">Service provider to validate</param>
    /// <returns>Validation results</returns>
    public static ServiceValidationResult ValidateServices(IServiceProvider serviceProvider)
    {
        var result = new ServiceValidationResult();

        try
        {
            // Test critical service resolution
            var criticalServices = new[]
            {
                typeof(IDialogService),
                typeof(INavigationService),
                typeof(IViewModelFactory),
                typeof(IUIThreadService)
            };

            foreach (var serviceType in criticalServices)
            {
                try
                {
                    var service = serviceProvider.GetService(serviceType);
                    if (service == null)
                    {
                        result.AddError($"Failed to resolve critical service: {serviceType.Name}");
                    }
                    else
                    {
                        result.AddSuccess($"Successfully resolved: {serviceType.Name}");
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Exception resolving {serviceType.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.AddError($"General validation error: {ex.Message}");
        }

        return result;
    }
}

/// <summary>
/// Service validation result
/// </summary>
public class ServiceValidationResult
{
    public List<string> Errors { get; } = new();
    public List<string> Successes { get; } = new();
    public bool IsValid => !Errors.Any();

    public void AddError(string error) => Errors.Add(error);
    public void AddSuccess(string success) => Successes.Add(success);
}

// UI service marker interfaces
public interface IDialogService { }
public interface INavigationService { }
public interface IViewModelFactory { }
public interface IUIThreadService { }

/// <summary>
/// Extension methods for configuring UI services in dependency injection container
/// Provides registration for Living Workspace architecture components
/// </summary>
public static class UIServiceRegistration
{
    /// <summary>
    /// Add logging configuration specific to the UI layer
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddUILogging(this IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
            
            // Add UI-specific logging filters
            builder.AddFilter("AnomaliImportTool.UI.Services.AnimationService", LogLevel.Debug);
            builder.AddFilter("AnomaliImportTool.UI.Services.NavigationService", LogLevel.Information);
            builder.AddFilter("AnomaliImportTool.UI.ViewModels", LogLevel.Information);
        });
        
        return services;
    }
} 
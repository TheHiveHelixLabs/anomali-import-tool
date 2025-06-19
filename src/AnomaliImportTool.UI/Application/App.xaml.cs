using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.UI.DependencyInjection;
using AnomaliImportTool.UI.ViewModels;
using AnomaliImportTool.UI.Views;
using AnomaliImportTool.Infrastructure.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AnomaliImportTool.UI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// Main entry point for the Anomali Import Tool Living Workspace.
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    private MainWindow? _mainWindow;

    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        
        // Initialize logging and dependency injection
        InitializeServices();
    }

    /// <summary>
    /// Gets the current App instance in use
    /// </summary>
    public new static App Current => (App)Application.Current;

    /// <summary>
    /// Gets the service provider for dependency injection
    /// </summary>
    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Services not initialized");

    /// <summary>
    /// Invoked when the application is launched normally by the end user.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        try
        {
            // Create and activate main window
            _mainWindow = Services.GetRequiredService<MainWindow>();
            _mainWindow.Activate();

            // Log successful startup
            var logger = Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation("Anomali Import Tool - Living Workspace started successfully");
        }
        catch (Exception ex)
        {
            // Handle startup errors gracefully
            HandleStartupError(ex);
        }
    }

    /// <summary>
    /// Initialize dependency injection and services
    /// </summary>
    private void InitializeServices()
    {
        var builder = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register infrastructure services
                services.AddInfrastructureServices();
                
                // Register UI services
                services.AddUIServices();
                
                // Register ViewModels
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<DashboardViewModel>();
                services.AddTransient<WizardModeViewModel>();
                services.AddTransient<AdvancedModeViewModel>();
                services.AddTransient<SettingsViewModel>();
                
                // Register Views/Windows
                services.AddSingleton<MainWindow>();
                services.AddTransient<DashboardView>();
                services.AddTransient<WizardModeView>();
                services.AddTransient<AdvancedModeView>();
                services.AddTransient<SettingsView>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        _host = builder.Build();
    }

    /// <summary>
    /// Handle startup errors with user-friendly messaging
    /// </summary>
    /// <param name="ex">The startup exception</param>
    private void HandleStartupError(Exception ex)
    {
        // Create a simple error window if main window failed
        var errorWindow = new Window()
        {
            Title = "Startup Error - Anomali Import Tool"
        };

        // Show error message to user
        var content = new Microsoft.UI.Xaml.Controls.StackPanel
        {
            Margin = new Thickness(20),
            Children =
            {
                new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = "An error occurred while starting the application:",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                },
                new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = ex.Message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                },
                new Microsoft.UI.Xaml.Controls.Button
                {
                    Content = "Exit",
                    HorizontalAlignment = HorizontalAlignment.Center
                }
            }
        };

        // Handle exit button click
        ((Microsoft.UI.Xaml.Controls.Button)content.Children[2]).Click += (s, e) => errorWindow.Close();

        errorWindow.Content = content;
        errorWindow.Activate();

        // Log the error if possible
        try
        {
            var logger = Services?.GetService<ILogger<App>>();
            logger?.LogCritical(ex, "Failed to start Anomali Import Tool");
        }
        catch
        {
            // Ignore logging errors during startup failure
        }
    }

    /// <summary>
    /// Clean up resources when application exits
    /// </summary>
    /// <param name="e">Exit event args</param>
    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            var logger = Services?.GetService<ILogger<App>>();
            logger?.LogInformation("Anomali Import Tool shutting down");
            
            _host?.Dispose();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
        }
        
        base.OnExit(e);
    }
}

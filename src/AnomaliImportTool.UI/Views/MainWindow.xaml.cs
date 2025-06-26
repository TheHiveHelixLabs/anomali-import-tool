using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Numerics;
using Windows.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.UI.ViewModels;
using AnomaliImportTool.UI.Services;
using System;
using System.Threading.Tasks;

namespace AnomaliImportTool.UI.Views;

/// <summary>
/// Main window for the Anomali Import Tool Living Workspace
/// Provides WinUI 3 integration with corporate branding and advanced UI features
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly INavigationService _navigationService;
    private readonly IWindowManagementService _windowManagementService;
    private readonly IAnimationService _animationService;
    private readonly IAccessibilityService _accessibilityService;
    private readonly MainWindowViewModel _viewModel;
    
    /// <summary>
    /// Initializes a new instance of the MainWindow
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
        
        // Get services from DI container
        var services = App.Current.Services;
        _logger = services.GetRequiredService<ILogger<MainWindow>>();
        _navigationService = services.GetRequiredService<INavigationService>();
        _windowManagementService = services.GetRequiredService<IWindowManagementService>();
        _animationService = services.GetRequiredService<IAnimationService>();
        _accessibilityService = services.GetRequiredService<IAccessibilityService>();
        _viewModel = services.GetRequiredService<MainWindowViewModel>();
        
        // Set DataContext
        this.DataContext = _viewModel;
        
        // Initialize window
        InitializeWindow();
        
        _logger.LogInformation("MainWindow initialized successfully");
    }
    
    /// <summary>
    /// Initialize window properties and navigation
    /// </summary>
    private async void InitializeWindow()
    {
        try
        {
            // Configure window properties
            await _windowManagementService.ConfigureMainWindowAsync(this);
            
            // Setup accessibility
            await _accessibilityService.ConfigureMainWindowAsync(this);
            
            // Navigate to dashboard as starting page
            await _navigationService.NavigateToAsync(typeof(DashboardView));
            
            // Announce window ready
            await _accessibilityService.AnnounceAsync(
                "Anomali Import Tool Living Workspace ready. Use Tab to navigate or press Alt+D for Dashboard.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize main window");
            await ShowErrorDialogAsync("Initialization Error", ex.Message);
        }
    }
    
    #region Window Controls Event Handlers
    
    /// <summary>
    /// Handle minimize button click
    /// </summary>
    private async void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _windowManagementService.MinimizeWindowAsync(this);
            await _accessibilityService.AnnounceAsync("Window minimized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to minimize window");
        }
    }
    
    /// <summary>
    /// Handle maximize/restore button click
    /// </summary>
    private async void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var isMaximized = await _windowManagementService.ToggleMaximizeRestoreAsync(this);
            
            // Update button icon and accessibility announcement
            var announcement = isMaximized ? "Window maximized" : "Window restored";
            await _accessibilityService.AnnounceAsync(announcement);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle window state");
        }
    }
    
    /// <summary>
    /// Handle close button click
    /// </summary>
    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if there are unsaved changes
            if (_viewModel.HasUnsavedChanges)
            {
                var dialog = new ContentDialog
                {
                    Title = "Unsaved Changes",
                    Content = "You have unsaved changes. Do you want to save before closing?",
                    PrimaryButtonText = "Save and Close",
                    SecondaryButtonText = "Close Without Saving",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.Content.XamlRoot
                };
                
                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    await _viewModel.SaveChangesAsync();
                }
                else if (result == ContentDialogResult.None)
                {
                    return; // Cancel close
                }
            }
            
            this.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close window");
        }
    }
    
    #endregion
    
    #region Navigation Event Handlers
    
    /// <summary>
    /// Handle navigation failures
    /// </summary>
    private async void MainContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        try
        {
            _logger.LogError("Navigation failed to {SourcePageType}: {Exception}", 
                e.SourcePageType?.Name, e.Exception);
            
            await ShowErrorDialogAsync("Navigation Error", 
                $"Failed to navigate to {e.SourcePageType?.Name}. Please try again.");
            
            // Navigate back to dashboard as fallback
            await _navigationService.NavigateToAsync(typeof(DashboardView));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle navigation failure");
        }
    }
    
    #endregion
    
    #region UI Helper Methods
    
    /// <summary>
    /// Show error dialog to user
    /// </summary>
    private async Task ShowErrorDialogAsync(string title, string message)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show error dialog");
        }
    }
    
    /// <summary>
    /// Update status text in the UI
    /// </summary>
    public void UpdateStatusText(string status)
    {
        try
        {
            _viewModel.StatusText = status;
            _logger.LogDebug("Status updated: {Status}", status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status text");
        }
    }
    
    /// <summary>
    /// Set progress indicator visibility and text
    /// </summary>
    public void SetProgressIndicator(bool isVisible, string? statusText = null)
    {
        try
        {
            _viewModel.IsProgressVisible = isVisible;
            
            if (!string.IsNullOrEmpty(statusText))
            {
                _viewModel.ProgressText = statusText;
            }
            
            _logger.LogDebug("Progress indicator: {IsVisible}, Text: {StatusText}", 
                isVisible, statusText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set progress indicator");
        }
    }
    
    /// <summary>
    /// Update mode indicator (Simple/Advanced)
    /// </summary>
    public void UpdateModeIndicator(string mode)
    {
        try
        {
            _viewModel.CurrentMode = mode;
            _logger.LogDebug("Mode updated: {Mode}", mode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update mode indicator");
        }
    }
    
    #endregion
    
    #region Window Lifecycle
    
    /// <summary>
    /// Handle window closed event
    /// </summary>
    protected override void OnClosed(WindowEventArgs args)
    {
        try
        {
            _logger.LogInformation("MainWindow closing");
            
            // Cleanup resources
            _viewModel?.Dispose();
            
            base.OnClosed(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during window close");
        }
    }
    
    #endregion
} 
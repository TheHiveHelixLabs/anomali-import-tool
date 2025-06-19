using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Win2D.UI.Xaml;
using Win2D;
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
/// Provides WinUI 3 + Win2D integration with corporate branding and advanced UI features
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ILogger<MainWindow> _logger;
    private readonly INavigationService _navigationService;
    private readonly IWindowManagementService _windowManagementService;
    private readonly IAnimationService _animationService;
    private readonly IAccessibilityService _accessibilityService;
    private readonly MainWindowViewModel _viewModel;
    
    // Win2D Resources
    private CanvasRenderTarget? _backgroundRenderTarget;
    private bool _isResourcesCreated = false;
    
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
    
    #region Win2D Graphics Integration
    
    /// <summary>
    /// Create Win2D resources for GPU-accelerated graphics
    /// </summary>
    private void CanvasControl_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        try
        {
            // Create background render target for subtle animations
            _backgroundRenderTarget = new CanvasRenderTarget(
                sender, 
                (float)sender.ActualWidth, 
                (float)sender.ActualHeight);
            
            _isResourcesCreated = true;
            _logger.LogDebug("Win2D resources created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Win2D resources");
        }
    }
    
    /// <summary>
    /// Draw Win2D graphics for background effects
    /// </summary>
    private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (!_isResourcesCreated || _backgroundRenderTarget == null)
            return;
            
        try
        {
            var session = args.DrawingSession;
            
            // Draw subtle background gradient
            DrawBackgroundEffects(session, sender);
            
            // Draw connection lines if in advanced mode
            if (_viewModel.IsAdvancedMode)
            {
                DrawConnectionLines(session);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during Win2D drawing");
        }
    }
    
    /// <summary>
    /// Draw subtle background effects
    /// </summary>
    private void DrawBackgroundEffects(CanvasDrawingSession session, CanvasControl control)
    {
        // Create subtle grid pattern for professional look
        var gridColor = Color.FromArgb(10, 59, 130, 246); // Very light corporate blue
        var gridSize = 50f;
        
        for (float x = 0; x < control.ActualWidth; x += gridSize)
        {
            session.DrawLine(x, 0, x, (float)control.ActualHeight, gridColor, 0.5f);
        }
        
        for (float y = 0; y < control.ActualHeight; y += gridSize)
        {
            session.DrawLine(0, y, (float)control.ActualWidth, y, gridColor, 0.5f);
        }
    }
    
    /// <summary>
    /// Draw connection lines for advanced mode
    /// </summary>
    private void DrawConnectionLines(CanvasDrawingSession session)
    {
        // Draw animated connection lines between document cards
        // This will be expanded when document cards are implemented
        var connectionColor = Color.FromArgb(100, 59, 130, 246);
        
        // Placeholder for future connection line drawing
        _logger.LogDebug("Drawing connection lines for advanced mode");
    }
    
    #endregion
    
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
            
            // Update button icon
            MaximizeRestoreButton.Content = isMaximized ? "\uE923" : "\uE922"; // Restore vs Maximize icon
            
            // Update tooltip
            ToolTipService.SetToolTip(MaximizeRestoreButton, isMaximized ? "Restore" : "Maximize");
            
            await _accessibilityService.AnnounceAsync(isMaximized ? "Window maximized" : "Window restored");
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
            var canClose = await _viewModel.CanCloseAsync();
            
            if (canClose)
            {
                await _accessibilityService.AnnounceAsync("Application closing");
                this.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during window close");
        }
    }
    
    #endregion
    
    #region Navigation Event Handlers
    
    /// <summary>
    /// Handle navigation failure
    /// </summary>
    private async void MainContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        _logger.LogError(e.Exception, "Navigation failed to {SourcePageType}", e.SourcePageType);
        
        await ShowErrorDialogAsync(
            "Navigation Error", 
            $"Failed to navigate to {e.SourcePageType.Name}. Please try again.");
            
        // Try to navigate back to dashboard
        try
        {
            await _navigationService.NavigateToAsync(typeof(DashboardView));
        }
        catch (Exception navEx)
        {
            _logger.LogCritical(navEx, "Failed to navigate to dashboard after navigation error");
        }
    }
    
    #endregion
    
    #region Utility Methods
    
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
    /// Update status bar text
    /// </summary>
    public void UpdateStatusText(string status)
    {
        try
        {
            StatusText.Text = status;
            _logger.LogDebug("Status updated: {Status}", status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update status text");
        }
    }
    
    /// <summary>
    /// Show/hide progress indicator
    /// </summary>
    public void SetProgressIndicator(bool isVisible, string? statusText = null)
    {
        try
        {
            StatusProgressRing.IsActive = isVisible;
            StatusProgressRing.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            
            if (statusText != null)
            {
                UpdateStatusText(statusText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set progress indicator");
        }
    }
    
    /// <summary>
    /// Update mode indicator
    /// </summary>
    public void UpdateModeIndicator(string mode)
    {
        try
        {
            ModeIndicatorText.Text = $"{mode} Mode";
            _logger.LogDebug("Mode indicator updated: {Mode}", mode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update mode indicator");
        }
    }
    
    #endregion
    
    #region Cleanup
    
    /// <summary>
    /// Clean up resources when window is closing
    /// </summary>
    protected override void OnClosed(WindowEventArgs args)
    {
        try
        {
            // Dispose Win2D resources
            _backgroundRenderTarget?.Dispose();
            
            _logger.LogInformation("MainWindow resources cleaned up");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during resource cleanup");
        }
        
        base.OnClosed(args);
    }
    
    #endregion
} 
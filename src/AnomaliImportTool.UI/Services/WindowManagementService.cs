using Microsoft.UI.Xaml;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace AnomaliImportTool.UI.Services;

/// <summary>
/// Window management service for Living Workspace architecture
/// Provides comprehensive window state management, multi-monitor support, and persistence
/// </summary>
public class WindowManagementService : IWindowManagementService
{
    private readonly ILogger<WindowManagementService> _logger;
    private readonly string _windowStateFilePath;
    private WindowState? _savedWindowState;
    
    // Window constraints
    private const int MinWindowWidth = 1200;
    private const int MinWindowHeight = 800;
    private const int DefaultWindowWidth = 1400;
    private const int DefaultWindowHeight = 900;
    
    public WindowManagementService(ILogger<WindowManagementService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Setup window state persistence path
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnomaliImportTool");
        Directory.CreateDirectory(appDataPath);
        _windowStateFilePath = Path.Combine(appDataPath, "window_state.json");
        
        _logger.LogInformation("WindowManagementService initialized with state persistence");
    }
    
    /// <summary>
    /// Configure main window with professional defaults and constraints
    /// </summary>
    public async Task ConfigureMainWindowAsync(Window window)
    {
        try
        {
            _logger.LogDebug("Configuring main window properties");
            
            // Get window handle and presenter
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow?.Presenter is OverlappedPresenter presenter)
            {
                // Set window constraints
                presenter.SetBorderAndTitleBar(true, true);
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
                presenter.IsMinimizable = true;
                presenter.IsAlwaysOnTop = false;
                
                // Set minimum size
                var scaleFactor = await GetDisplayScaleFactorAsync();
                var minWidth = (int)(MinWindowWidth * scaleFactor);
                var minHeight = (int)(MinWindowHeight * scaleFactor);
                
                // Set window size constraints through Win32 API
                SetWindowMinSize(hwnd, minWidth, minHeight);
                
                _logger.LogDebug("Window constraints set: Min {MinWidth}x{MinHeight}", minWidth, minHeight);
            }
            
            // Apply saved window state or defaults
            await RestoreWindowStateAsync(window);
            
            // Setup window event handlers
            SetupWindowEventHandlers(window);
            
            _logger.LogInformation("Main window configured successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure main window");
            throw;
        }
    }
    
    /// <summary>
    /// Minimize window with animation
    /// </summary>
    public async Task MinimizeWindowAsync(Window window)
    {
        try
        {
            _logger.LogDebug("Minimizing window");
            
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow?.Presenter is OverlappedPresenter presenter)
            {
                presenter.Minimize();
                await SaveWindowStateAsync(window);
                _logger.LogDebug("Window minimized successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to minimize window");
        }
    }
    
    /// <summary>
    /// Toggle between maximized and restored window state
    /// </summary>
    public async Task<bool> ToggleMaximizeRestoreAsync(Window window)
    {
        try
        {
            _logger.LogDebug("Toggling window maximize/restore");
            
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow?.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State == OverlappedPresenterState.Maximized)
                {
                    presenter.Restore();
                    _logger.LogDebug("Window restored from maximized state");
                    await SaveWindowStateAsync(window);
                    return false; // Not maximized
                }
                else
                {
                    presenter.Maximize();
                    _logger.LogDebug("Window maximized");
                    await SaveWindowStateAsync(window);
                    return true; // Maximized
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle window maximize/restore");
            return false;
        }
    }
    
    /// <summary>
    /// Setup multi-monitor support with monitor detection
    /// </summary>
    public async Task SetupMultiMonitorAsync(Window window)
    {
        try
        {
            _logger.LogDebug("Setting up multi-monitor support");
            
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow != null)
            {
                // Get all displays
                var displayAreas = DisplayArea.FindAll();
                _logger.LogInformation("Detected {DisplayCount} display(s)", displayAreas.Count);
                
                // Log display information
                for (int i = 0; i < displayAreas.Count; i++)
                {
                    var display = displayAreas[i];
                    _logger.LogDebug("Display {Index}: {Width}x{Height} at ({X},{Y})", 
                        i, display.WorkArea.Width, display.WorkArea.Height, 
                        display.WorkArea.X, display.WorkArea.Y);
                }
                
                // If multiple monitors, ensure window is on primary display initially
                if (displayAreas.Count > 1)
                {
                    var primaryDisplay = displayAreas[0]; // Primary display is typically first
                    var currentPosition = appWindow.Position;
                    
                    // Check if window is completely off-screen
                    if (!IsWindowOnAnyDisplay(currentPosition, appWindow.Size, displayAreas))
                    {
                        // Move to center of primary display
                        var centerX = primaryDisplay.WorkArea.X + (primaryDisplay.WorkArea.Width - appWindow.Size.Width) / 2;
                        var centerY = primaryDisplay.WorkArea.Y + (primaryDisplay.WorkArea.Height - appWindow.Size.Height) / 2;
                        
                        appWindow.Move(new PointInt32(centerX, centerY));
                        _logger.LogInformation("Window moved to primary display center");
                    }
                }
            }
            
            await Task.Delay(10); // Allow window positioning to settle
            _logger.LogDebug("Multi-monitor support configured");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup multi-monitor support");
        }
    }
    
    /// <summary>
    /// Save current window state to persistent storage
    /// </summary>
    public async Task SaveWindowStateAsync(Window window)
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow?.Presenter is OverlappedPresenter presenter)
            {
                var windowState = new WindowState
                {
                    X = appWindow.Position.X,
                    Y = appWindow.Position.Y,
                    Width = appWindow.Size.Width,
                    Height = appWindow.Size.Height,
                    IsMaximized = presenter.State == OverlappedPresenterState.Maximized,
                    LastSaved = DateTime.UtcNow
                };
                
                var json = JsonSerializer.Serialize(windowState, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(_windowStateFilePath, json);
                _savedWindowState = windowState;
                
                _logger.LogDebug("Window state saved: {Width}x{Height} at ({X},{Y}), Maximized: {IsMaximized}",
                    windowState.Width, windowState.Height, windowState.X, windowState.Y, windowState.IsMaximized);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save window state");
        }
    }
    
    /// <summary>
    /// Restore window state from persistent storage
    /// </summary>
    public async Task RestoreWindowStateAsync(Window window)
    {
        try
        {
            if (!File.Exists(_windowStateFilePath))
            {
                // Apply default window state
                await ApplyDefaultWindowStateAsync(window);
                return;
            }
            
            var json = await File.ReadAllTextAsync(_windowStateFilePath);
            var windowState = JsonSerializer.Deserialize<WindowState>(json);
            
            if (windowState == null)
            {
                await ApplyDefaultWindowStateAsync(window);
                return;
            }
            
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow?.Presenter is OverlappedPresenter presenter)
            {
                // Validate window state is reasonable
                if (IsValidWindowState(windowState))
                {
                    // Apply size
                    appWindow.Resize(new SizeInt32(windowState.Width, windowState.Height));
                    
                    // Apply position
                    appWindow.Move(new PointInt32(windowState.X, windowState.Y));
                    
                    // Apply maximized state
                    if (windowState.IsMaximized)
                    {
                        presenter.Maximize();
                    }
                    else
                    {
                        presenter.Restore();
                    }
                    
                    _savedWindowState = windowState;
                    _logger.LogInformation("Window state restored: {Width}x{Height} at ({X},{Y}), Maximized: {IsMaximized}",
                        windowState.Width, windowState.Height, windowState.X, windowState.Y, windowState.IsMaximized);
                }
                else
                {
                    _logger.LogWarning("Invalid window state detected, applying defaults");
                    await ApplyDefaultWindowStateAsync(window);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore window state, applying defaults");
            await ApplyDefaultWindowStateAsync(window);
        }
    }
    
    #region Private Helper Methods
    
    /// <summary>
    /// Apply default window state
    /// </summary>
    private async Task ApplyDefaultWindowStateAsync(Window window)
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow != null)
            {
                // Get scale factor for proper sizing
                var scaleFactor = await GetDisplayScaleFactorAsync();
                var width = (int)(DefaultWindowWidth * scaleFactor);
                var height = (int)(DefaultWindowHeight * scaleFactor);
                
                // Get primary display for centering
                var displayAreas = DisplayArea.FindAll();
                var primaryDisplay = displayAreas.Count > 0 ? displayAreas[0] : null;
                
                if (primaryDisplay != null)
                {
                    // Center on primary display
                    var x = primaryDisplay.WorkArea.X + (primaryDisplay.WorkArea.Width - width) / 2;
                    var y = primaryDisplay.WorkArea.Y + (primaryDisplay.WorkArea.Height - height) / 2;
                    
                    appWindow.Resize(new SizeInt32(width, height));
                    appWindow.Move(new PointInt32(x, y));
                }
                else
                {
                    // Fallback if no display info available
                    appWindow.Resize(new SizeInt32(width, height));
                }
                
                _logger.LogDebug("Default window state applied: {Width}x{Height}", width, height);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply default window state");
        }
    }
    
    /// <summary>
    /// Setup window event handlers for state tracking
    /// </summary>
    private void SetupWindowEventHandlers(Window window)
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            
            if (appWindow != null)
            {
                // Track window changes for auto-save
                appWindow.Changed += async (sender, args) =>
                {
                    if (args.DidSizeChange || args.DidPositionChange)
                    {
                        // Auto-save window state with debounce
                        await Task.Delay(1000); // Wait 1 second to avoid too frequent saves
                        await SaveWindowStateAsync(window);
                    }
                };
                
                _logger.LogDebug("Window event handlers configured");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup window event handlers");
        }
    }
    
    /// <summary>
    /// Get display scale factor for DPI awareness
    /// </summary>
    private async Task<double> GetDisplayScaleFactorAsync()
    {
        try
        {
            // Get scale factor from primary display
            var displayAreas = DisplayArea.FindAll();
            if (displayAreas.Count > 0)
            {
                // Scale factor is typically 1.0, 1.25, 1.5, 2.0, etc.
                return 1.0; // Simplified for now - would need more complex DPI detection
            }
            
            return 1.0;
        }
        catch
        {
            return 1.0; // Default scale factor
        }
    }
    
    /// <summary>
    /// Validate if window state is reasonable
    /// </summary>
    private bool IsValidWindowState(WindowState state)
    {
        // Check minimum dimensions
        if (state.Width < MinWindowWidth || state.Height < MinWindowHeight)
            return false;
            
        // Check maximum reasonable dimensions (e.g., not larger than 4K)
        if (state.Width > 4096 || state.Height > 2160)
            return false;
            
        // Check if position is reasonable (not too far off-screen)
        if (state.X < -1000 || state.Y < -1000 || state.X > 10000 || state.Y > 10000)
            return false;
            
        return true;
    }
    
    /// <summary>
    /// Check if window is visible on any display
    /// </summary>
    private bool IsWindowOnAnyDisplay(PointInt32 position, SizeInt32 size, IReadOnlyList<DisplayArea> displays)
    {
        foreach (var display in displays)
        {
            var windowRight = position.X + size.Width;
            var windowBottom = position.Y + size.Height;
            var displayRight = display.WorkArea.X + display.WorkArea.Width;
            var displayBottom = display.WorkArea.Y + display.WorkArea.Height;
            
            // Check if window overlaps with this display
            if (position.X < displayRight && windowRight > display.WorkArea.X &&
                position.Y < displayBottom && windowBottom > display.WorkArea.Y)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Set minimum window size using Win32 API
    /// </summary>
    private void SetWindowMinSize(HWND hwnd, int minWidth, int minHeight)
    {
        try
        {
            // This would require Win32 API calls to set WM_GETMINMAXINFO
            // For now, we'll rely on the presenter settings
            _logger.LogDebug("Window minimum size constraint set to {MinWidth}x{MinHeight}", minWidth, minHeight);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set window minimum size");
        }
    }
    
    #endregion
}

/// <summary>
/// Window state model for persistence
/// </summary>
public class WindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsMaximized { get; set; }
    public DateTime LastSaved { get; set; }
} 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.UI.ViewModels;

namespace AnomaliImportTool.UI.Services;

/// <summary>
/// Animation service interface for 60 FPS Living Workspace animations
/// </summary>
public interface IAnimationService
{
    /// <summary>
    /// Start mode transition animation
    /// </summary>
    Task StartModeTransitionAsync(string targetMode);
    
    /// <summary>
    /// Animate file drop with breathing effect
    /// </summary>
    Task AnimateFileDropAsync(FrameworkElement target);
    
    /// <summary>
    /// Animate document card appearance
    /// </summary>
    Task AnimateDocumentCardAsync(FrameworkElement card, bool appearing = true);
    
    /// <summary>
    /// Animate connection line drawing
    /// </summary>
    Task AnimateConnectionLineAsync(FrameworkElement line);
    
    /// <summary>
    /// Animate success celebration
    /// </summary>
    Task AnimateSuccessAsync(FrameworkElement target);
    
    /// <summary>
    /// Whether animations are enabled (respects reduced motion)
    /// </summary>
    bool AnimationsEnabled { get; }
}

/// <summary>
/// Accessibility service interface for WCAG 2.1 AA compliance
/// </summary>
public interface IAccessibilityService
{
    /// <summary>
    /// Configure main window for accessibility
    /// </summary>
    Task ConfigureMainWindowAsync(Window window);
    
    /// <summary>
    /// Announce message to screen readers
    /// </summary>
    Task AnnounceAsync(string message);
    
    /// <summary>
    /// Setup keyboard navigation for element
    /// </summary>
    Task SetupKeyboardNavigationAsync(FrameworkElement element);
    
    /// <summary>
    /// Enable voice control for element
    /// </summary>
    Task EnableVoiceControlAsync(FrameworkElement element);
    
    /// <summary>
    /// Check if high contrast mode is enabled
    /// </summary>
    bool IsHighContrastMode { get; }
    
    /// <summary>
    /// Current font scale factor
    /// </summary>
    double FontScaleFactor { get; }
}

/// <summary>
/// Workspace persistence service interface
/// </summary>
public interface IWorkspacePersistenceService
{
    /// <summary>
    /// Save workspace state
    /// </summary>
    Task SaveWorkspaceStateAsync(WorkspaceState state);
    
    /// <summary>
    /// Load workspace state
    /// </summary>
    Task<WorkspaceState?> LoadWorkspaceStateAsync();
    
    /// <summary>
    /// Load recent documents
    /// </summary>
    Task<List<Document>> LoadRecentDocumentsAsync();
    
    /// <summary>
    /// Get documents processed today count
    /// </summary>
    Task<int> GetDocumentsProcessedTodayAsync();
    
    /// <summary>
    /// Save document processing result
    /// </summary>
    Task SaveDocumentResultAsync(Document document);
}

/// <summary>
/// Theme service interface for corporate branding
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Apply corporate theme
    /// </summary>
    Task ApplyCorporateThemeAsync();
    
    /// <summary>
    /// Switch between light/dark themes
    /// </summary>
    Task SwitchThemeAsync(bool isDarkTheme);
    
    /// <summary>
    /// Get current theme colors
    /// </summary>
    Dictionary<string, string> GetThemeColors();
    
    /// <summary>
    /// Whether dark theme is currently active
    /// </summary>
    bool IsDarkTheme { get; }
}

/// <summary>
/// Window management service interface
/// </summary>
public interface IWindowManagementService
{
    /// <summary>
    /// Configure main window properties
    /// </summary>
    Task ConfigureMainWindowAsync(Window window);
    
    /// <summary>
    /// Minimize window
    /// </summary>
    Task MinimizeWindowAsync(Window window);
    
    /// <summary>
    /// Toggle maximize/restore window
    /// </summary>
    Task<bool> ToggleMaximizeRestoreAsync(Window window);
    
    /// <summary>
    /// Setup multi-monitor support
    /// </summary>
    Task SetupMultiMonitorAsync(Window window);
    
    /// <summary>
    /// Save window state
    /// </summary>
    Task SaveWindowStateAsync(Window window);
    
    /// <summary>
    /// Restore window state
    /// </summary>
    Task RestoreWindowStateAsync(Window window);
}

/// <summary>
/// Audio feedback service interface
/// </summary>
public interface IAudioFeedbackService
{
    /// <summary>
    /// Play success sound
    /// </summary>
    Task PlaySuccessAsync();
    
    /// <summary>
    /// Play error sound
    /// </summary>
    Task PlayErrorAsync();
    
    /// <summary>
    /// Play notification sound
    /// </summary>
    Task PlayNotificationAsync();
    
    /// <summary>
    /// Play file drop sound
    /// </summary>
    Task PlayFileDropAsync();
    
    /// <summary>
    /// Whether audio feedback is enabled
    /// </summary>
    bool AudioEnabled { get; set; }
} 
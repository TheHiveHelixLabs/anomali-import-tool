using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;

namespace AnomaliImportTool.UI.Services;

/// <summary>
/// Navigation service interface for Living Workspace architecture
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigate to a specific page type
    /// </summary>
    Task<bool> NavigateToAsync(Type pageType, object? parameter = null);
    
    /// <summary>
    /// Show a dialog/modal
    /// </summary>
    Task<ContentDialogResult> ShowDialogAsync(Type dialogType, object? parameter = null);
    
    /// <summary>
    /// Navigate back if possible
    /// </summary>
    Task<bool> GoBackAsync();
    
    /// <summary>
    /// Check if navigation back is possible
    /// </summary>
    bool CanGoBack { get; }
    
    /// <summary>
    /// Current page type
    /// </summary>
    Type? CurrentPageType { get; }
    
    /// <summary>
    /// Navigation history
    /// </summary>
    IReadOnlyList<Type> NavigationHistory { get; }
    
    /// <summary>
    /// Event raised when navigation occurs
    /// </summary>
    event EventHandler<NavigationEventArgs>? NavigationCompleted;
}

/// <summary>
/// Enhanced navigation service for Living Workspace architecture
/// Provides workflow step progression, professional transitions, and state persistence
/// </summary>
public class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;
    private readonly IAnimationService _animationService;
    private Frame? _mainFrame;
    private readonly List<NavigationHistoryItem> _navigationHistory = new();
    private readonly Dictionary<Type, Type> _viewModelToViewMap = new();
    private readonly Dictionary<string, WorkflowStep> _workflowSteps = new();
    private readonly string _navigationStateFilePath;
    private string _currentWorkflowMode = "Dashboard";
    private int _currentWorkflowStep = 0;
    
    /// <summary>
    /// Current page type
    /// </summary>
    public Type? CurrentPageType { get; private set; }
    
    /// <summary>
    /// Navigation history (read-only)
    /// </summary>
    public IReadOnlyList<Type> NavigationHistory => _navigationHistory.Select(h => h.PageType).ToList().AsReadOnly();
    
    /// <summary>
    /// Breadcrumb navigation items
    /// </summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs => GenerateBreadcrumbs();
    
    /// <summary>
    /// Whether navigation back is possible
    /// </summary>
    public bool CanGoBack => _mainFrame?.CanGoBack == true && _navigationHistory.Count > 1;
    
    /// <summary>
    /// Whether navigation forward is possible
    /// </summary>
    public bool CanGoForward => _mainFrame?.CanGoForward == true;
    
    /// <summary>
    /// Current workflow mode (Dashboard, Wizard, Advanced)
    /// </summary>
    public string CurrentWorkflowMode => _currentWorkflowMode;
    
    /// <summary>
    /// Current workflow step (for Wizard mode)
    /// </summary>
    public int CurrentWorkflowStep => _currentWorkflowStep;
    
    /// <summary>
    /// Total workflow steps (for Wizard mode)
    /// </summary>
    public int TotalWorkflowSteps => _workflowSteps.Count;
    
    /// <summary>
    /// Event raised when navigation completes
    /// </summary>
    public event EventHandler<NavigationEventArgs>? NavigationCompleted;
    
    /// <summary>
    /// Event raised when workflow step changes
    /// </summary>
    public event EventHandler<WorkflowStepChangedEventArgs>? WorkflowStepChanged;
    
    /// <summary>
    /// Event raised when workflow mode changes
    /// </summary>
    public event EventHandler<WorkflowModeChangedEventArgs>? WorkflowModeChanged;
    
    /// <summary>
    /// Event raised when breadcrumbs update
    /// </summary>
    public event EventHandler<BreadcrumbUpdatedEventArgs>? BreadcrumbsUpdated;
    
    /// <summary>
    /// Initialize navigation service
    /// </summary>
    public NavigationService(ILogger<NavigationService> logger, IAnimationService animationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        
        // Setup navigation state persistence path
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnomaliImportTool");
        Directory.CreateDirectory(appDataPath);
        _navigationStateFilePath = Path.Combine(appDataPath, "navigation_state.json");
        
        // Setup view mapping for Living Workspace
        SetupViewMapping();
        
        // Setup workflow steps
        SetupWorkflowSteps();
        
        _logger.LogInformation("NavigationService initialized with workflow support");
    }
    
    /// <summary>
    /// Initialize with main frame reference
    /// </summary>
    public void Initialize(Frame mainFrame)
    {
        _mainFrame = mainFrame ?? throw new ArgumentNullException(nameof(mainFrame));
        
        // Subscribe to frame navigation events
        _mainFrame.Navigated += OnFrameNavigated;
        _mainFrame.NavigationFailed += OnFrameNavigationFailed;
        
        // Restore navigation state
        _ = Task.Run(RestoreNavigationStateAsync);
        
        _logger.LogInformation("NavigationService initialized with main frame");
    }
    
    /// <summary>
    /// Navigate to specified page type with professional animation
    /// </summary>
    public async Task<bool> NavigateToAsync(Type pageType, object? parameter = null)
    {
        try
        {
            if (_mainFrame == null)
            {
                _logger.LogWarning("Navigation attempted before frame initialization");
                return false;
            }
            
            _logger.LogDebug("Navigating to {PageType} with parameter: {Parameter}", pageType.Name, parameter);
            
            // Start navigation animation
            if (_animationService.AnimationsEnabled)
            {
                await _animationService.StartModeTransitionAsync(pageType.Name);
            }
            
            // Perform navigation
            var result = _mainFrame.Navigate(pageType, parameter);
            
            if (result)
            {
                // Add to navigation history
                var historyItem = new NavigationHistoryItem
                {
                    PageType = pageType,
                    Parameter = parameter,
                    NavigatedAt = DateTime.UtcNow,
                    WorkflowMode = _currentWorkflowMode,
                    WorkflowStep = _currentWorkflowStep
                };
                
                _navigationHistory.Add(historyItem);
                
                // Update current page
                CurrentPageType = pageType;
                
                // Update workflow step if in workflow mode
                await UpdateWorkflowProgressAsync(pageType);
                
                // Save navigation state
                await SaveNavigationStateAsync();
                
                _logger.LogInformation("Successfully navigated to {PageType}", pageType.Name);
            }
            else
            {
                _logger.LogWarning("Failed to navigate to {PageType}", pageType.Name);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during navigation to {PageType}", pageType.Name);
            return false;
        }
    }
    
    /// <summary>
    /// Navigate back with animation
    /// </summary>
    public async Task<bool> GoBackAsync()
    {
        try
        {
            if (!CanGoBack)
            {
                _logger.LogDebug("Back navigation not available");
                return false;
            }
            
            _logger.LogDebug("Navigating back");
            
            // Start back animation
            if (_animationService.AnimationsEnabled)
            {
                await _animationService.StartModeTransitionAsync("Back");
            }
            
            // Remove current item from history
            if (_navigationHistory.Count > 0)
            {
                _navigationHistory.RemoveAt(_navigationHistory.Count - 1);
            }
            
            // Perform back navigation
            _mainFrame!.GoBack();
            
            // Update current page
            if (_navigationHistory.Count > 0)
            {
                var lastItem = _navigationHistory[_navigationHistory.Count - 1];
                CurrentPageType = lastItem.PageType;
                _currentWorkflowMode = lastItem.WorkflowMode;
                _currentWorkflowStep = lastItem.WorkflowStep;
            }
            
            // Update breadcrumbs
            BreadcrumbsUpdated?.Invoke(this, new BreadcrumbUpdatedEventArgs(GenerateBreadcrumbs()));
            
            await SaveNavigationStateAsync();
            
            _logger.LogInformation("Successfully navigated back");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during back navigation");
            return false;
        }
    }
    
    /// <summary>
    /// Show dialog/modal with professional presentation
    /// </summary>
    public async Task<ContentDialogResult> ShowDialogAsync(Type dialogType, object? parameter = null)
    {
        try
        {
            _logger.LogDebug("Showing dialog: {DialogType}", dialogType.Name);
            
            // Create dialog instance (would need dependency injection setup)
            // For now, return mock result
            await Task.Delay(100); // Simulate dialog display time
            
            _logger.LogDebug("Dialog {DialogType} completed", dialogType.Name);
            return ContentDialogResult.Primary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing dialog {DialogType}", dialogType.Name);
            return ContentDialogResult.None;
        }
    }
    
    /// <summary>
    /// Switch workflow mode with enhanced state management
    /// </summary>
    public async Task SwitchWorkflowModeAsync(string mode)
    {
        try
        {
            _logger.LogInformation("Switching workflow mode from {CurrentMode} to {NewMode}", _currentWorkflowMode, mode);
            
            var previousMode = _currentWorkflowMode;
            _currentWorkflowMode = mode;
            _currentWorkflowStep = 0;
            
            // Clear workflow-specific history for fresh start
            _navigationHistory.Clear();
            
            // Navigate to mode-appropriate starting page
            await NavigateToModeStartPageAsync(mode);
            
            // Raise workflow step changed event
            WorkflowStepChanged?.Invoke(this, new WorkflowStepChangedEventArgs(mode, 0, GetWorkflowStepName(mode, 0)));
            
            // Raise workflow mode changed event
            WorkflowModeChanged?.Invoke(this, new WorkflowModeChangedEventArgs(previousMode, mode, DateTime.UtcNow));
            
            await SaveNavigationStateAsync();
            
            _logger.LogInformation("Workflow mode switched to {Mode}", mode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching workflow mode to {Mode}", mode);
            throw;
        }
    }

    /// <summary>
    /// Set workflow step for state restoration
    /// </summary>
    public async Task SetWorkflowStepAsync(int step)
    {
        try
        {
            if (step < 0 || step >= TotalWorkflowSteps)
            {
                _logger.LogWarning("Invalid workflow step: {Step}", step);
                return;
            }

            _currentWorkflowStep = step;
            var stepName = GetWorkflowStepName(_currentWorkflowMode, step);
            var pageType = GetPageTypeForWorkflowStep(_currentWorkflowMode, step);

            _logger.LogInformation("Setting workflow step to {Step}: {StepName}", step, stepName);

            // Navigate to step page
            await NavigateToAsync(pageType);

            // Raise workflow step changed event
            WorkflowStepChanged?.Invoke(this, new WorkflowStepChangedEventArgs(_currentWorkflowMode, step, stepName));

            await SaveNavigationStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set workflow step to {Step}", step);
        }
    }
    
    /// <summary>
    /// Advance to next workflow step
    /// </summary>
    public async Task<bool> NextWorkflowStepAsync()
    {
        try
        {
            if (_currentWorkflowMode != "Wizard")
            {
                _logger.LogDebug("Next step only available in Wizard mode");
                return false;
            }
            
            if (_currentWorkflowStep >= TotalWorkflowSteps - 1)
            {
                _logger.LogDebug("Already at last workflow step");
                return false;
            }
            
            _currentWorkflowStep++;
            var stepName = GetWorkflowStepName(_currentWorkflowMode, _currentWorkflowStep);
            var pageType = GetPageTypeForWorkflowStep(_currentWorkflowMode, _currentWorkflowStep);
            
            _logger.LogInformation("Advancing to workflow step {Step}: {StepName}", _currentWorkflowStep, stepName);
            
            // Navigate to step page
            await NavigateToAsync(pageType);
            
            // Raise workflow step changed event
            WorkflowStepChanged?.Invoke(this, new WorkflowStepChangedEventArgs(_currentWorkflowMode, _currentWorkflowStep, stepName));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error advancing to next workflow step");
            return false;
        }
    }
    
    /// <summary>
    /// Go back to previous workflow step
    /// </summary>
    public async Task<bool> PreviousWorkflowStepAsync()
    {
        try
        {
            if (_currentWorkflowMode != "Wizard")
            {
                _logger.LogDebug("Previous step only available in Wizard mode");
                return false;
            }
            
            if (_currentWorkflowStep <= 0)
            {
                _logger.LogDebug("Already at first workflow step");
                return false;
            }
            
            _currentWorkflowStep--;
            var stepName = GetWorkflowStepName(_currentWorkflowMode, _currentWorkflowStep);
            var pageType = GetPageTypeForWorkflowStep(_currentWorkflowMode, _currentWorkflowStep);
            
            _logger.LogInformation("Going back to workflow step {Step}: {StepName}", _currentWorkflowStep, stepName);
            
            // Navigate to step page
            await NavigateToAsync(pageType);
            
            // Raise workflow step changed event
            WorkflowStepChanged?.Invoke(this, new WorkflowStepChangedEventArgs(_currentWorkflowMode, _currentWorkflowStep, stepName));
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error going back to previous workflow step");
            return false;
        }
    }
    
    #region Private Helper Methods
    
    /// <summary>
    /// Setup view model to view mapping
    /// </summary>
    private void SetupViewMapping()
    {
        // Living Workspace view mappings would be registered here
        _logger.LogDebug("View mapping configured");
    }
    
    /// <summary>
    /// Setup workflow step definitions
    /// </summary>
    private void SetupWorkflowSteps()
    {
        // Wizard workflow steps
        var wizardSteps = new Dictionary<int, WorkflowStep>
        {
            { 0, new WorkflowStep { Name = "File Selection", PageType = typeof(Views.FileSelectionView), Description = "Select documents to process" } },
            { 1, new WorkflowStep { Name = "Document Preview", PageType = typeof(Views.DocumentPreviewView), Description = "Review selected documents" } },
            { 2, new WorkflowStep { Name = "Configuration", PageType = typeof(Views.ConfigurationView), Description = "Configure processing options" } },
            { 3, new WorkflowStep { Name = "Processing", PageType = typeof(Views.ProcessingView), Description = "Process documents" } },
            { 4, new WorkflowStep { Name = "Review Results", PageType = typeof(Views.ResultsView), Description = "Review processing results" } },
            { 5, new WorkflowStep { Name = "Upload to Anomali", PageType = typeof(Views.UploadView), Description = "Upload to Anomali ThreatStream" } }
        };
        
        foreach (var step in wizardSteps)
        {
            _workflowSteps[$"Wizard_{step.Key}"] = step.Value;
        }
        
        _logger.LogDebug("Workflow steps configured: {StepCount} wizard steps", wizardSteps.Count);
    }
    
    /// <summary>
    /// Navigate to mode-appropriate starting page
    /// </summary>
    private async Task NavigateToModeStartPageAsync(string mode)
    {
        Type startPageType = mode switch
        {
            "Dashboard" => typeof(Views.DashboardView),
            "Wizard" => typeof(Views.FileSelectionView),
            "Advanced" => typeof(Views.AdvancedModeView),
            _ => typeof(Views.DashboardView)
        };
        
        await NavigateToAsync(startPageType);
    }
    
    /// <summary>
    /// Update workflow progress based on current page
    /// </summary>
    private async Task UpdateWorkflowProgressAsync(Type pageType)
    {
        if (_currentWorkflowMode == "Wizard")
        {
            // Find step for current page type
            var step = _workflowSteps
                .Where(kvp => kvp.Key.StartsWith("Wizard_"))
                .FirstOrDefault(kvp => kvp.Value.PageType == pageType);
            
            if (!step.Equals(default(KeyValuePair<string, WorkflowStep>)))
            {
                var stepNumber = int.Parse(step.Key.Split('_')[1]);
                if (stepNumber != _currentWorkflowStep)
                {
                    _currentWorkflowStep = stepNumber;
                    WorkflowStepChanged?.Invoke(this, new WorkflowStepChangedEventArgs(_currentWorkflowMode, _currentWorkflowStep, step.Value.Name));
                }
            }
        }
    }
    
    /// <summary>
    /// Get workflow step name
    /// </summary>
    private string GetWorkflowStepName(string mode, int step)
    {
        var key = $"{mode}_{step}";
        return _workflowSteps.TryGetValue(key, out var workflowStep) ? workflowStep.Name : "Unknown Step";
    }
    
    /// <summary>
    /// Get page type for workflow step
    /// </summary>
    private Type GetPageTypeForWorkflowStep(string mode, int step)
    {
        var key = $"{mode}_{step}";
        return _workflowSteps.TryGetValue(key, out var workflowStep) ? workflowStep.PageType : typeof(Views.DashboardView);
    }
    
    /// <summary>
    /// Generate breadcrumb navigation items
    /// </summary>
    private List<BreadcrumbItem> GenerateBreadcrumbs()
    {
        var breadcrumbs = new List<BreadcrumbItem>();
        
        if (_currentWorkflowMode == "Wizard")
        {
            // Add wizard breadcrumbs
            for (int i = 0; i <= _currentWorkflowStep && i < TotalWorkflowSteps; i++)
            {
                var stepName = GetWorkflowStepName(_currentWorkflowMode, i);
                breadcrumbs.Add(new BreadcrumbItem
                {
                    Name = stepName,
                    Step = i,
                    IsCurrentStep = i == _currentWorkflowStep,
                    IsCompleted = i < _currentWorkflowStep,
                    IsClickable = i <= _currentWorkflowStep
                });
            }
        }
        else
        {
            // Add simple breadcrumb for non-wizard modes
            breadcrumbs.Add(new BreadcrumbItem
            {
                Name = _currentWorkflowMode,
                Step = 0,
                IsCurrentStep = true,
                IsCompleted = false,
                IsClickable = false
            });
        }
        
        return breadcrumbs;
    }
    
    /// <summary>
    /// Save navigation state to persistent storage
    /// </summary>
    private async Task SaveNavigationStateAsync()
    {
        try
        {
            var navigationState = new NavigationState
            {
                CurrentWorkflowMode = _currentWorkflowMode,
                CurrentWorkflowStep = _currentWorkflowStep,
                CurrentPageType = CurrentPageType?.FullName,
                LastSaved = DateTime.UtcNow
            };
            
            var json = JsonSerializer.Serialize(navigationState, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            await File.WriteAllTextAsync(_navigationStateFilePath, json);
            
            _logger.LogDebug("Navigation state saved: Mode={Mode}, Step={Step}", _currentWorkflowMode, _currentWorkflowStep);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save navigation state");
        }
    }
    
    /// <summary>
    /// Restore navigation state from persistent storage
    /// </summary>
    private async Task RestoreNavigationStateAsync()
    {
        try
        {
            if (!File.Exists(_navigationStateFilePath))
            {
                _logger.LogDebug("No navigation state file found, using defaults");
                return;
            }
            
            var json = await File.ReadAllTextAsync(_navigationStateFilePath);
            var navigationState = JsonSerializer.Deserialize<NavigationState>(json);
            
            if (navigationState != null)
            {
                _currentWorkflowMode = navigationState.CurrentWorkflowMode ?? "Dashboard";
                _currentWorkflowStep = navigationState.CurrentWorkflowStep;
                
                _logger.LogInformation("Navigation state restored: Mode={Mode}, Step={Step}", _currentWorkflowMode, _currentWorkflowStep);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore navigation state, using defaults");
            _currentWorkflowMode = "Dashboard";
            _currentWorkflowStep = 0;
        }
    }
    
    /// <summary>
    /// Handle frame navigation completed
    /// </summary>
    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        _logger.LogDebug("Frame navigated to {PageType}", e.SourcePageType.Name);
        
        // Update breadcrumbs
        BreadcrumbsUpdated?.Invoke(this, new BreadcrumbUpdatedEventArgs(GenerateBreadcrumbs()));
        
        // Raise navigation completed event
        NavigationCompleted?.Invoke(this, e);
    }
    
    /// <summary>
    /// Handle frame navigation failed
    /// </summary>
    private void OnFrameNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        _logger.LogError("Frame navigation failed to {PageType}: {Exception}", e.SourcePageType.Name, e.Exception);
    }
    
    #endregion
}

// Supporting classes and interfaces

/// <summary>
/// Navigation history item
/// </summary>
public class NavigationHistoryItem
{
    public Type PageType { get; set; } = null!;
    public object? Parameter { get; set; }
    public DateTime NavigatedAt { get; set; }
    public string WorkflowMode { get; set; } = "";
    public int WorkflowStep { get; set; }
}

/// <summary>
/// Workflow step definition
/// </summary>
public class WorkflowStep
{
    public string Name { get; set; } = "";
    public Type PageType { get; set; } = null!;
    public string Description { get; set; } = "";
}

/// <summary>
/// Breadcrumb navigation item
/// </summary>
public class BreadcrumbItem
{
    public string Name { get; set; } = "";
    public int Step { get; set; }
    public bool IsCurrentStep { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsClickable { get; set; }
}

/// <summary>
/// Navigation state for persistence
/// </summary>
public class NavigationState
{
    public string? CurrentWorkflowMode { get; set; }
    public int CurrentWorkflowStep { get; set; }
    public string? CurrentPageType { get; set; }
    public DateTime LastSaved { get; set; }
}

/// <summary>
/// Workflow step changed event arguments
/// </summary>
public class WorkflowStepChangedEventArgs : EventArgs
{
    public string WorkflowMode { get; }
    public int Step { get; }
    public string StepName { get; }
    
    public WorkflowStepChangedEventArgs(string workflowMode, int step, string stepName)
    {
        WorkflowMode = workflowMode;
        Step = step;
        StepName = stepName;
    }
}

/// <summary>
/// Breadcrumb updated event arguments
/// </summary>
public class BreadcrumbUpdatedEventArgs : EventArgs
{
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; }
    
    public BreadcrumbUpdatedEventArgs(IReadOnlyList<BreadcrumbItem> breadcrumbs)
    {
        Breadcrumbs = breadcrumbs;
    }
}

/// <summary>
/// Workflow mode changed event arguments
/// </summary>
public class WorkflowModeChangedEventArgs : EventArgs
{
    public string PreviousMode { get; }
    public string NewMode { get; }
    public DateTime ChangedAt { get; }

    public WorkflowModeChangedEventArgs(string previousMode, string newMode, DateTime changedAt)
    {
        PreviousMode = previousMode;
        NewMode = newMode;
        ChangedAt = changedAt;
    }
}

/// <summary>
/// Custom navigation event args
/// </summary>
public class NavigationEventArgs : EventArgs
{
    public Type SourcePageType { get; }
    public object? Parameter { get; }
    public DateTime NavigationTime { get; }

    public NavigationEventArgs(Type sourcePageType, object? parameter = null)
    {
        SourcePageType = sourcePageType;
        Parameter = parameter;
        NavigationTime = DateTime.UtcNow;
    }
} 
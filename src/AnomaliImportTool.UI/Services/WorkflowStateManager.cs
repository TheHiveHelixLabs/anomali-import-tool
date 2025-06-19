using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.UI.ViewModels;
using System.Collections.ObjectModel;

namespace AnomaliImportTool.UI.Services;

/// <summary>
/// Professional Workflow State Manager for Living Workspace
/// Handles mid-workflow mode switching with comprehensive state preservation
/// </summary>
public interface IWorkflowStateManager
{
    /// <summary>
    /// Current workflow mode
    /// </summary>
    string CurrentMode { get; }

    /// <summary>
    /// Whether workflow state can be switched
    /// </summary>
    bool CanSwitchMode { get; }

    /// <summary>
    /// Switch workflow mode with state preservation
    /// </summary>
    Task<bool> SwitchModeAsync(string targetMode, bool preserveState = true);

    /// <summary>
    /// Save current workflow state
    /// </summary>
    Task SaveWorkflowStateAsync();

    /// <summary>
    /// Restore workflow state for specified mode
    /// </summary>
    Task<bool> RestoreWorkflowStateAsync(string mode);

    /// <summary>
    /// Get workflow state summary
    /// </summary>
    WorkflowStateSummary GetWorkflowStateSummary();

    /// <summary>
    /// Clear workflow state for specified mode
    /// </summary>
    Task ClearWorkflowStateAsync(string mode);

    /// <summary>
    /// Event raised when workflow mode changes
    /// </summary>
    event EventHandler<WorkflowModeChangedEventArgs>? WorkflowModeChanged;

    /// <summary>
    /// Event raised when workflow state is saved
    /// </summary>
    event EventHandler<WorkflowStateSavedEventArgs>? WorkflowStateSaved;
}

/// <summary>
/// Professional Workflow State Manager Implementation
/// </summary>
public class WorkflowStateManager : IWorkflowStateManager
{
    private readonly ILogger<WorkflowStateManager> _logger;
    private readonly INavigationService _navigationService;
    private readonly IWorkspacePersistenceService _workspacePersistenceService;
    private readonly IAudioFeedbackService _audioFeedbackService;
    private readonly IAccessibilityService _accessibilityService;
    private readonly string _stateFilePath;

    private string _currentMode = "Dashboard";
    private bool _canSwitchMode = true;
    private readonly Dictionary<string, WorkflowState> _workflowStates = new();
    private readonly Dictionary<string, DateTime> _lastStateSave = new();
    private readonly Dictionary<string, UserPreferences> _userPreferences = new();

    /// <summary>
    /// Initialize Workflow State Manager
    /// </summary>
    public WorkflowStateManager(
        ILogger<WorkflowStateManager> logger,
        INavigationService navigationService,
        IWorkspacePersistenceService workspacePersistenceService,
        IAudioFeedbackService audioFeedbackService,
        IAccessibilityService accessibilityService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _workspacePersistenceService = workspacePersistenceService ?? throw new ArgumentNullException(nameof(workspacePersistenceService));
        _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));
        _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));

        // Setup state persistence path
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnomaliImportTool");
        Directory.CreateDirectory(appDataPath);
        _stateFilePath = Path.Combine(appDataPath, "workflow-states.json");

        // Initialize workflow states
        InitializeWorkflowStates();

        // Load saved states
        _ = LoadWorkflowStatesAsync();

        _logger.LogInformation("WorkflowStateManager initialized with state persistence");
    }

    #region Public Properties

    /// <summary>
    /// Current workflow mode
    /// </summary>
    public string CurrentMode => _currentMode;

    /// <summary>
    /// Whether workflow mode can be switched
    /// </summary>
    public bool CanSwitchMode => _canSwitchMode;

    #endregion

    #region Public Events

    /// <summary>
    /// Event raised when workflow mode changes
    /// </summary>
    public event EventHandler<WorkflowModeChangedEventArgs>? WorkflowModeChanged;

    /// <summary>
    /// Event raised when workflow state is saved
    /// </summary>
    public event EventHandler<WorkflowStateSavedEventArgs>? WorkflowStateSaved;

    #endregion

    #region Public Methods

    /// <summary>
    /// Switch workflow mode with comprehensive state preservation
    /// </summary>
    public async Task<bool> SwitchModeAsync(string targetMode, bool preserveState = true)
    {
        try
        {
            if (!CanSwitchMode)
            {
                _logger.LogWarning("Mode switching is currently disabled");
                return false;
            }

            if (_currentMode == targetMode)
            {
                _logger.LogDebug("Already in target mode: {Mode}", targetMode);
                return true;
            }

            _logger.LogInformation("Switching workflow mode from {CurrentMode} to {TargetMode} (preserve state: {PreserveState})", 
                _currentMode, targetMode, preserveState);

            _canSwitchMode = false;

            try
            {
                // Step 1: Save current mode state if requested
                if (preserveState)
                {
                    await SaveCurrentModeStateAsync();
                }

                // Step 2: Prepare target mode
                await PrepareTargetModeAsync(targetMode);

                // Step 3: Update user preferences
                await UpdateUserPreferencesAsync(targetMode);

                // Step 4: Perform navigation
                var navigationSuccess = await PerformModeNavigationAsync(targetMode);
                if (!navigationSuccess)
                {
                    _logger.LogError("Navigation failed during mode switch to {TargetMode}", targetMode);
                    return false;
                }

                // Step 5: Restore target mode state if available
                if (preserveState)
                {
                    await RestoreTargetModeStateAsync(targetMode);
                }

                // Step 6: Update current mode
                var previousMode = _currentMode;
                _currentMode = targetMode;

                // Step 7: Notify listeners
                await NotifyModeChangeAsync(previousMode, targetMode);

                // Step 8: Save workflow states
                await SaveWorkflowStatesAsync();

                _logger.LogInformation("Successfully switched to {TargetMode} mode", targetMode);
                return true;
            }
            finally
            {
                _canSwitchMode = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch workflow mode to {TargetMode}", targetMode);
            await _audioFeedbackService.PlayErrorAsync();
            _canSwitchMode = true;
            return false;
        }
    }

    /// <summary>
    /// Save current workflow state
    /// </summary>
    public async Task SaveWorkflowStateAsync()
    {
        try
        {
            await SaveCurrentModeStateAsync();
            await SaveWorkflowStatesAsync();
            
            _logger.LogDebug("Workflow state saved for mode: {Mode}", _currentMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workflow state for mode: {Mode}", _currentMode);
        }
    }

    /// <summary>
    /// Restore workflow state for specified mode
    /// </summary>
    public async Task<bool> RestoreWorkflowStateAsync(string mode)
    {
        try
        {
            if (!_workflowStates.ContainsKey(mode))
            {
                _logger.LogDebug("No saved state found for mode: {Mode}", mode);
                return false;
            }

            var state = _workflowStates[mode];
            var restored = await ApplyWorkflowStateAsync(mode, state);

            if (restored)
            {
                _logger.LogInformation("Successfully restored workflow state for mode: {Mode}", mode);
                await _audioFeedbackService.PlayNotificationAsync();
            }

            return restored;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore workflow state for mode: {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Get comprehensive workflow state summary
    /// </summary>
    public WorkflowStateSummary GetWorkflowStateSummary()
    {
        var summary = new WorkflowStateSummary
        {
            CurrentMode = _currentMode,
            CanSwitchMode = _canSwitchMode,
            AvailableModes = new List<string> { "Dashboard", "Wizard", "Advanced" },
            SavedStates = new Dictionary<string, WorkflowStateInfo>()
        };

        foreach (var kvp in _workflowStates)
        {
            var stateInfo = new WorkflowStateInfo
            {
                Mode = kvp.Key,
                HasSavedState = kvp.Value.HasData,
                LastSaved = _lastStateSave.GetValueOrDefault(kvp.Key),
                FileCount = kvp.Value.SelectedFiles?.Count ?? 0,
                CurrentStep = kvp.Value.CurrentStep,
                IsCompleted = kvp.Value.IsCompleted
            };

            summary.SavedStates[kvp.Key] = stateInfo;
        }

        return summary;
    }

    /// <summary>
    /// Clear workflow state for specified mode
    /// </summary>
    public async Task ClearWorkflowStateAsync(string mode)
    {
        try
        {
            if (_workflowStates.ContainsKey(mode))
            {
                _workflowStates[mode] = new WorkflowState { Mode = mode };
                _lastStateSave.Remove(mode);
                
                await SaveWorkflowStatesAsync();
                
                _logger.LogInformation("Cleared workflow state for mode: {Mode}", mode);
                await _audioFeedbackService.PlayNotificationAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear workflow state for mode: {Mode}", mode);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initialize workflow states for all modes
    /// </summary>
    private void InitializeWorkflowStates()
    {
        var modes = new[] { "Dashboard", "Wizard", "Advanced" };
        
        foreach (var mode in modes)
        {
            _workflowStates[mode] = new WorkflowState { Mode = mode };
            _userPreferences[mode] = new UserPreferences { Mode = mode };
        }

        _logger.LogDebug("Initialized workflow states for {ModeCount} modes", modes.Length);
    }

    /// <summary>
    /// Save current mode state
    /// </summary>
    private async Task SaveCurrentModeStateAsync()
    {
        try
        {
            var state = await CaptureCurrentModeStateAsync();
            if (state != null)
            {
                _workflowStates[_currentMode] = state;
                _lastStateSave[_currentMode] = DateTime.UtcNow;
                
                _logger.LogDebug("Captured state for mode: {Mode}", _currentMode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save current mode state: {Mode}", _currentMode);
        }
    }

    /// <summary>
    /// Capture current mode state from active ViewModels
    /// </summary>
    private async Task<WorkflowState?> CaptureCurrentModeStateAsync()
    {
        try
        {
            var state = new WorkflowState
            {
                Mode = _currentMode,
                CapturedAt = DateTime.UtcNow,
                HasData = true
            };

            switch (_currentMode)
            {
                case "Dashboard":
                    state = await CaptureDashboardStateAsync(state);
                    break;
                case "Wizard":
                    state = await CaptureWizardStateAsync(state);
                    break;
                case "Advanced":
                    state = await CaptureAdvancedStateAsync(state);
                    break;
            }

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture current mode state");
            return null;
        }
    }

    /// <summary>
    /// Capture Dashboard mode state
    /// </summary>
    private async Task<WorkflowState> CaptureDashboardStateAsync(WorkflowState state)
    {
        // Dashboard state is typically minimal - just user preferences
        state.UserPreferences = new Dictionary<string, object>
        {
            ["LastActiveCard"] = "ProcessDocuments",
            ["PreferredStartMode"] = "Wizard",
            ["ShowRecentProjects"] = true
        };

        await Task.Delay(10); // Simulate state capture
        return state;
    }

    /// <summary>
    /// Capture Wizard mode state
    /// </summary>
    private async Task<WorkflowState> CaptureWizardStateAsync(WorkflowState state)
    {
        // Capture wizard-specific state
        state.CurrentStep = _navigationService.CurrentWorkflowStep;
        state.SelectedFiles = new List<Document>(); // Would get from WizardModeViewModel
        state.ProcessingConfiguration = new Dictionary<string, object>
        {
            ["ExtractIOCs"] = true,
            ["ExtractTTPs"] = true,
            ["GenerateMetadata"] = true,
            ["OutputFormat"] = "JSON"
        };
        state.IsInProgress = state.CurrentStep > 0;

        await Task.Delay(10); // Simulate state capture
        return state;
    }

    /// <summary>
    /// Capture Advanced mode state
    /// </summary>
    private async Task<WorkflowState> CaptureAdvancedStateAsync(WorkflowState state)
    {
        // Capture advanced mode state
        state.SelectedFiles = new List<Document>(); // Would get from AdvancedModeViewModel
        state.WorkspaceConfiguration = new Dictionary<string, object>
        {
            ["CurrentWorkspace"] = "Default",
            ["SelectedTab"] = "Files",
            ["IsExpertMode"] = false,
            ["AutoProcessNewFiles"] = false
        };
        state.IsInProgress = (state.SelectedFiles?.Count ?? 0) > 0;

        await Task.Delay(10); // Simulate state capture
        return state;
    }

    /// <summary>
    /// Prepare target mode for switching
    /// </summary>
    private async Task PrepareTargetModeAsync(string targetMode)
    {
        try
        {
            _logger.LogDebug("Preparing target mode: {TargetMode}", targetMode);

            // Mode-specific preparation
            switch (targetMode)
            {
                case "Dashboard":
                    await PrepareDashboardModeAsync();
                    break;
                case "Wizard":
                    await PrepareWizardModeAsync();
                    break;
                case "Advanced":
                    await PrepareAdvancedModeAsync();
                    break;
            }

            await Task.Delay(50); // Simulate preparation time
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare target mode: {TargetMode}", targetMode);
        }
    }

    /// <summary>
    /// Prepare Dashboard mode
    /// </summary>
    private async Task PrepareDashboardModeAsync()
    {
        // Prepare dashboard-specific resources
        await Task.Delay(10);
        _logger.LogDebug("Dashboard mode prepared");
    }

    /// <summary>
    /// Prepare Wizard mode
    /// </summary>
    private async Task PrepareWizardModeAsync()
    {
        // Prepare wizard-specific resources
        await Task.Delay(10);
        _logger.LogDebug("Wizard mode prepared");
    }

    /// <summary>
    /// Prepare Advanced mode
    /// </summary>
    private async Task PrepareAdvancedModeAsync()
    {
        // Prepare advanced mode resources
        await Task.Delay(10);
        _logger.LogDebug("Advanced mode prepared");
    }

    /// <summary>
    /// Update user preferences for target mode
    /// </summary>
    private async Task UpdateUserPreferencesAsync(string targetMode)
    {
        try
        {
            if (!_userPreferences.ContainsKey(targetMode))
            {
                _userPreferences[targetMode] = new UserPreferences { Mode = targetMode };
            }

            var prefs = _userPreferences[targetMode];
            prefs.LastUsed = DateTime.UtcNow;
            prefs.UsageCount++;

            // Update mode-specific preferences
            switch (targetMode)
            {
                case "Wizard":
                    prefs.PreferredStartStep = 0;
                    break;
                case "Advanced":
                    prefs.PreferredTab = "Files";
                    break;
            }

            await Task.Delay(10); // Simulate preference update
            _logger.LogDebug("Updated user preferences for mode: {TargetMode}", targetMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user preferences for mode: {TargetMode}", targetMode);
        }
    }

    /// <summary>
    /// Perform actual navigation to target mode
    /// </summary>
    private async Task<bool> PerformModeNavigationAsync(string targetMode)
    {
        try
        {
            _logger.LogDebug("Performing navigation to mode: {TargetMode}", targetMode);

            var success = await _navigationService.SwitchWorkflowModeAsync(targetMode);
            
            if (success)
            {
                await _accessibilityService.AnnounceAsync($"Switched to {targetMode} mode");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform navigation to mode: {TargetMode}", targetMode);
            return false;
        }
    }

    /// <summary>
    /// Restore target mode state
    /// </summary>
    private async Task RestoreTargetModeStateAsync(string targetMode)
    {
        try
        {
            if (_workflowStates.ContainsKey(targetMode))
            {
                var state = _workflowStates[targetMode];
                if (state.HasData)
                {
                    await ApplyWorkflowStateAsync(targetMode, state);
                    _logger.LogDebug("Restored state for mode: {TargetMode}", targetMode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore target mode state: {TargetMode}", targetMode);
        }
    }

    /// <summary>
    /// Apply workflow state to target mode
    /// </summary>
    private async Task<bool> ApplyWorkflowStateAsync(string mode, WorkflowState state)
    {
        try
        {
            switch (mode)
            {
                case "Dashboard":
                    return await ApplyDashboardStateAsync(state);
                case "Wizard":
                    return await ApplyWizardStateAsync(state);
                case "Advanced":
                    return await ApplyAdvancedStateAsync(state);
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply workflow state for mode: {Mode}", mode);
            return false;
        }
    }

    /// <summary>
    /// Apply Dashboard state
    /// </summary>
    private async Task<bool> ApplyDashboardStateAsync(WorkflowState state)
    {
        // Apply dashboard-specific state
        await Task.Delay(10);
        return true;
    }

    /// <summary>
    /// Apply Wizard state
    /// </summary>
    private async Task<bool> ApplyWizardStateAsync(WorkflowState state)
    {
        // Apply wizard-specific state
        if (state.CurrentStep > 0)
        {
            // Would restore wizard to specific step
            await _navigationService.SetWorkflowStepAsync(state.CurrentStep);
        }
        
        await Task.Delay(10);
        return true;
    }

    /// <summary>
    /// Apply Advanced state
    /// </summary>
    private async Task<bool> ApplyAdvancedStateAsync(WorkflowState state)
    {
        // Apply advanced mode state
        if (state.WorkspaceConfiguration != null)
        {
            // Would restore workspace configuration
        }
        
        await Task.Delay(10);
        return true;
    }

    /// <summary>
    /// Notify listeners of mode change
    /// </summary>
    private async Task NotifyModeChangeAsync(string previousMode, string newMode)
    {
        try
        {
            var args = new WorkflowModeChangedEventArgs(previousMode, newMode, DateTime.UtcNow);
            WorkflowModeChanged?.Invoke(this, args);

            var stateSavedArgs = new WorkflowStateSavedEventArgs(newMode, DateTime.UtcNow);
            WorkflowStateSaved?.Invoke(this, stateSavedArgs);

            await _audioFeedbackService.PlayNotificationAsync();
            
            _logger.LogDebug("Notified listeners of mode change: {PreviousMode} -> {NewMode}", previousMode, newMode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify listeners of mode change");
        }
    }

    /// <summary>
    /// Load workflow states from persistent storage
    /// </summary>
    private async Task LoadWorkflowStatesAsync()
    {
        try
        {
            if (!File.Exists(_stateFilePath))
            {
                _logger.LogDebug("No workflow states file found, using defaults");
                return;
            }

            var json = await File.ReadAllTextAsync(_stateFilePath);
            var persistedData = JsonSerializer.Deserialize<WorkflowStatePersistence>(json);

            if (persistedData != null)
            {
                // Restore workflow states
                foreach (var kvp in persistedData.WorkflowStates)
                {
                    _workflowStates[kvp.Key] = kvp.Value;
                }

                // Restore last save times
                foreach (var kvp in persistedData.LastStateSave)
                {
                    _lastStateSave[kvp.Key] = kvp.Value;
                }

                // Restore user preferences
                foreach (var kvp in persistedData.UserPreferences)
                {
                    _userPreferences[kvp.Key] = kvp.Value;
                }

                _logger.LogInformation("Loaded workflow states for {StateCount} modes", _workflowStates.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow states, using defaults");
        }
    }

    /// <summary>
    /// Save workflow states to persistent storage
    /// </summary>
    private async Task SaveWorkflowStatesAsync()
    {
        try
        {
            var persistedData = new WorkflowStatePersistence
            {
                WorkflowStates = _workflowStates,
                LastStateSave = _lastStateSave,
                UserPreferences = _userPreferences,
                LastSaved = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(persistedData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(_stateFilePath, json);
            
            _logger.LogDebug("Saved workflow states to persistent storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workflow states");
        }
    }

    #endregion
}

#region Supporting Models

/// <summary>
/// Comprehensive workflow state model
/// </summary>
public class WorkflowState
{
    public string Mode { get; set; } = "";
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public bool HasData { get; set; } = false;
    public bool IsInProgress { get; set; } = false;
    public bool IsCompleted { get; set; } = false;
    public int CurrentStep { get; set; } = 0;
    public List<Document>? SelectedFiles { get; set; }
    public Dictionary<string, object>? ProcessingConfiguration { get; set; }
    public Dictionary<string, object>? WorkspaceConfiguration { get; set; }
    public Dictionary<string, object>? UserPreferences { get; set; }
    public Dictionary<string, object>? CustomData { get; set; }
}

/// <summary>
/// User preferences for workflow modes
/// </summary>
public class UserPreferences
{
    public string Mode { get; set; } = "";
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    public int UsageCount { get; set; } = 0;
    public int PreferredStartStep { get; set; } = 0;
    public string PreferredTab { get; set; } = "";
    public Dictionary<string, object> CustomPreferences { get; set; } = new();
}

/// <summary>
/// Workflow state summary
/// </summary>
public class WorkflowStateSummary
{
    public string CurrentMode { get; set; } = "";
    public bool CanSwitchMode { get; set; } = true;
    public List<string> AvailableModes { get; set; } = new();
    public Dictionary<string, WorkflowStateInfo> SavedStates { get; set; } = new();
}

/// <summary>
/// Workflow state information
/// </summary>
public class WorkflowStateInfo
{
    public string Mode { get; set; } = "";
    public bool HasSavedState { get; set; } = false;
    public DateTime LastSaved { get; set; }
    public int FileCount { get; set; } = 0;
    public int CurrentStep { get; set; } = 0;
    public bool IsCompleted { get; set; } = false;
}

/// <summary>
/// Workflow state persistence model
/// </summary>
public class WorkflowStatePersistence
{
    public Dictionary<string, WorkflowState> WorkflowStates { get; set; } = new();
    public Dictionary<string, DateTime> LastStateSave { get; set; } = new();
    public Dictionary<string, UserPreferences> UserPreferences { get; set; } = new();
    public DateTime LastSaved { get; set; } = DateTime.UtcNow;
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
/// Workflow state saved event arguments
/// </summary>
public class WorkflowStateSavedEventArgs : EventArgs
{
    public string Mode { get; }
    public DateTime SavedAt { get; }

    public WorkflowStateSavedEventArgs(string mode, DateTime savedAt)
    {
        Mode = mode;
        SavedAt = savedAt;
    }
}

#endregion 
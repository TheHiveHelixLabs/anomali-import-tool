using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.UI.Services;
using AnomaliImportTool.Core.Models;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace AnomaliImportTool.UI.ViewModels;

/// <summary>
/// Main window ViewModel for the Living Workspace architecture
/// Manages window state, navigation, and mode switching with ReactiveUI
/// </summary>
public class MainWindowViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly IWorkspacePersistenceService _workspacePersistenceService;
    private readonly IWorkflowStateManager _workflowStateManager;
    private readonly IAnimationService _animationService;
    private readonly IAccessibilityService _accessibilityService;

    // Private backing fields
    private string _statusText = "Ready - Living Workspace";
    private bool _isProgressVisible = false;
    private string _currentMode = "Dashboard";
    private bool _isAdvancedMode = false;
    private bool _isWizardMode = false;
    private bool _isDashboardMode = true;
    private string _windowTitle = "Anomali Import Tool";
    private bool _hasUnsavedChanges = false;
    private ObservableCollection<Document> _recentDocuments = new();
    private int _documentsProcessedToday = 0;
    private string _userGreeting = "";
    private bool _isNavigating = false;

    /// <summary>
    /// Initializes a new instance of MainWindowViewModel
    /// </summary>
    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        INavigationService navigationService,
        IWorkspacePersistenceService workspacePersistenceService,
        IWorkflowStateManager workflowStateManager,
        IAnimationService animationService,
        IAccessibilityService accessibilityService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _workspacePersistenceService = workspacePersistenceService ?? throw new ArgumentNullException(nameof(workspacePersistenceService));
        _workflowStateManager = workflowStateManager ?? throw new ArgumentNullException(nameof(workflowStateManager));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));

        // Initialize ViewModelActivator
        Activator = new ViewModelActivator();

        // Setup reactive commands
        SetupCommands();

        // Setup reactive properties
        SetupReactiveProperties();

        // Initialize data
        InitializeAsync();

        _logger.LogInformation("MainWindowViewModel initialized with ReactiveUI");
    }

    #region Reactive Properties

    /// <summary>
    /// Current status text displayed in status bar
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    /// <summary>
    /// Whether progress indicator is visible
    /// </summary>
    public bool IsProgressVisible
    {
        get => _isProgressVisible;
        set => this.RaiseAndSetIfChanged(ref _isProgressVisible, value);
    }

    /// <summary>
    /// Current interface mode (Dashboard, Wizard, Advanced)
    /// </summary>
    public string CurrentMode
    {
        get => _currentMode;
        set => this.RaiseAndSetIfChanged(ref _currentMode, value);
    }

    /// <summary>
    /// Whether currently in Advanced Mode
    /// </summary>
    public bool IsAdvancedMode
    {
        get => _isAdvancedMode;
        set => this.RaiseAndSetIfChanged(ref _isAdvancedMode, value);
    }

    /// <summary>
    /// Whether currently in Wizard Mode
    /// </summary>
    public bool IsWizardMode
    {
        get => _isWizardMode;
        set => this.RaiseAndSetIfChanged(ref _isWizardMode, value);
    }

    /// <summary>
    /// Whether currently in Dashboard Mode
    /// </summary>
    public bool IsDashboardMode
    {
        get => _isDashboardMode;
        set => this.RaiseAndSetIfChanged(ref _isDashboardMode, value);
    }

    /// <summary>
    /// Window title with dynamic content
    /// </summary>
    public string WindowTitle
    {
        get => _windowTitle;
        set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
    }

    /// <summary>
    /// Whether there are unsaved changes
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    /// Collection of recently processed documents
    /// </summary>
    public ObservableCollection<Document> RecentDocuments
    {
        get => _recentDocuments;
        set => this.RaiseAndSetIfChanged(ref _recentDocuments, value);
    }

    /// <summary>
    /// Number of documents processed today
    /// </summary>
    public int DocumentsProcessedToday
    {
        get => _documentsProcessedToday;
        set => this.RaiseAndSetIfChanged(ref _documentsProcessedToday, value);
    }

    /// <summary>
    /// Personalized user greeting
    /// </summary>
    public string UserGreeting
    {
        get => _userGreeting;
        set => this.RaiseAndSetIfChanged(ref _userGreeting, value);
    }

    /// <summary>
    /// Whether navigation is in progress
    /// </summary>
    public bool IsNavigating
    {
        get => _isNavigating;
        set => this.RaiseAndSetIfChanged(ref _isNavigating, value);
    }

    #endregion

    #region Reactive Commands

    /// <summary>
    /// Command to switch to Dashboard Mode
    /// </summary>
    public ReactiveCommand<Unit, Unit> SwitchToDashboardCommand { get; private set; } = null!;

    /// <summary>
    /// Command to switch to Wizard Mode
    /// </summary>
    public ReactiveCommand<Unit, Unit> SwitchToWizardCommand { get; private set; } = null!;

    /// <summary>
    /// Command to switch to Advanced Mode
    /// </summary>
    public ReactiveCommand<Unit, Unit> SwitchToAdvancedCommand { get; private set; } = null!;

    /// <summary>
    /// Command to refresh workspace data
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to save workspace state
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to show settings
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowSettingsCommand { get; private set; } = null!;

    /// <summary>
    /// Command to show about dialog
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; private set; } = null!;

    #endregion

    #region Setup Methods

    /// <summary>
    /// Setup reactive commands with proper conditions and error handling
    /// </summary>
    private void SetupCommands()
    {
        // Mode switching commands - only available when not navigating
        var canNavigate = this.WhenAnyValue(x => x.IsNavigating, navigating => !navigating);

        SwitchToDashboardCommand = ReactiveCommand.CreateFromTask(
            SwitchToDashboardAsync, 
            canNavigate,
            RxApp.MainThreadScheduler);

        SwitchToWizardCommand = ReactiveCommand.CreateFromTask(
            SwitchToWizardAsync, 
            canNavigate,
            RxApp.MainThreadScheduler);

        SwitchToAdvancedCommand = ReactiveCommand.CreateFromTask(
            SwitchToAdvancedAsync, 
            canNavigate,
            RxApp.MainThreadScheduler);

        // Workspace commands
        RefreshWorkspaceCommand = ReactiveCommand.CreateFromTask(
            RefreshWorkspaceAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        SaveWorkspaceCommand = ReactiveCommand.CreateFromTask(
            SaveWorkspaceAsync,
            this.WhenAnyValue(x => x.HasUnsavedChanges),
            RxApp.MainThreadScheduler);

        // Dialog commands
        ShowSettingsCommand = ReactiveCommand.CreateFromTask(
            ShowSettingsAsync,
            canNavigate,
            RxApp.MainThreadScheduler);

        ShowAboutCommand = ReactiveCommand.CreateFromTask(
            ShowAboutAsync,
            canNavigate,
            RxApp.MainThreadScheduler);

        // Setup command error handling
        SetupCommandErrorHandling();
    }

    /// <summary>
    /// Setup reactive properties with subscriptions and computed values
    /// </summary>
    private void SetupReactiveProperties()
    {
        // Update window title when mode changes
        this.WhenAnyValue(x => x.CurrentMode, x => x.HasUnsavedChanges)
            .Select(tuple => $"Anomali Import Tool - {tuple.Item1}{(tuple.Item2 ? "*" : "")}")
            .ToPropertyEx(this, x => x.WindowTitle);

        // Update status text based on mode and activity
        this.WhenAnyValue(x => x.CurrentMode, x => x.IsNavigating, x => x.DocumentsProcessedToday)
            .Select(tuple => 
            {
                if (tuple.Item2) return "Switching workspace mode...";
                return $"Ready - {tuple.Item1} Mode | {tuple.Item3} documents processed today";
            })
            .Subscribe(status => StatusText = status);

        // Update mode flags when CurrentMode changes
        this.WhenAnyValue(x => x.CurrentMode)
            .Subscribe(mode =>
            {
                IsDashboardMode = mode == "Dashboard";
                IsWizardMode = mode == "Wizard";
                IsAdvancedMode = mode == "Advanced";
            });

        // Generate user greeting based on time of day
        this.WhenAnyValue(x => x.CurrentMode)
            .Where(mode => mode == "Dashboard")
            .Select(_ => GenerateUserGreeting())
            .Subscribe(greeting => UserGreeting = greeting);
    }

    /// <summary>
    /// Setup error handling for reactive commands
    /// </summary>
    private void SetupCommandErrorHandling()
    {
        // Global command exception handling
        var allCommands = new[]
        {
            SwitchToDashboardCommand,
            SwitchToWizardCommand,
            SwitchToAdvancedCommand,
            RefreshWorkspaceCommand,
            SaveWorkspaceCommand,
            ShowSettingsCommand,
            ShowAboutCommand
        };

        foreach (var command in allCommands)
        {
            command.ThrownExceptions
                .SelectMany(ex => HandleCommandException(ex))
                .Subscribe();
        }
    }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Switch to Dashboard Mode with state preservation
    /// </summary>
    private async Task SwitchToDashboardAsync()
    {
        try
        {
            IsNavigating = true;
            
            // Use WorkflowStateManager for intelligent mode switching
            var success = await _workflowStateManager.SwitchModeAsync("Dashboard", preserveState: true);
            
            if (success)
            {
                CurrentMode = "Dashboard";
                await _accessibilityService.AnnounceAsync("Switched to Dashboard Mode");
                _logger.LogInformation("Successfully switched to Dashboard Mode");
            }
            else
            {
                _logger.LogWarning("Failed to switch to Dashboard Mode");
            }
        }
        finally
        {
            IsNavigating = false;
        }
    }

    /// <summary>
    /// Switch to Wizard Mode with state preservation
    /// </summary>
    private async Task SwitchToWizardAsync()
    {
        try
        {
            IsNavigating = true;
            
            // Use WorkflowStateManager for intelligent mode switching
            var success = await _workflowStateManager.SwitchModeAsync("Wizard", preserveState: true);
            
            if (success)
            {
                CurrentMode = "Wizard";
                await _accessibilityService.AnnounceAsync("Switched to Wizard Mode - Step-by-step guided workflow");
                _logger.LogInformation("Successfully switched to Wizard Mode");
            }
            else
            {
                _logger.LogWarning("Failed to switch to Wizard Mode");
            }
        }
        finally
        {
            IsNavigating = false;
        }
    }

    /// <summary>
    /// Switch to Advanced Mode with state preservation
    /// </summary>
    private async Task SwitchToAdvancedAsync()
    {
        try
        {
            IsNavigating = true;
            
            // Use WorkflowStateManager for intelligent mode switching
            var success = await _workflowStateManager.SwitchModeAsync("Advanced", preserveState: true);
            
            if (success)
            {
                CurrentMode = "Advanced";
                await _accessibilityService.AnnounceAsync("Switched to Advanced Mode - Power user interface with full features");
                _logger.LogInformation("Successfully switched to Advanced Mode");
            }
            else
            {
                _logger.LogWarning("Failed to switch to Advanced Mode");
            }
        }
        finally
        {
            IsNavigating = false;
        }
    }

    /// <summary>
    /// Refresh workspace data
    /// </summary>
    private async Task RefreshWorkspaceAsync()
    {
        try
        {
            IsProgressVisible = true;
            StatusText = "Refreshing workspace data...";
            
            // Load recent documents
            var recentDocs = await _workspacePersistenceService.LoadRecentDocumentsAsync();
            RecentDocuments.Clear();
            foreach (var doc in recentDocs)
            {
                RecentDocuments.Add(doc);
            }
            
            // Update statistics
            DocumentsProcessedToday = await _workspacePersistenceService.GetDocumentsProcessedTodayAsync();
            
            await _accessibilityService.AnnounceAsync($"Workspace refreshed. {DocumentsProcessedToday} documents processed today.");
            _logger.LogInformation("Workspace data refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh workspace data");
            StatusText = "Failed to refresh workspace data";
        }
        finally
        {
            IsProgressVisible = false;
        }
    }

    /// <summary>
    /// Save current workspace state
    /// </summary>
    private async Task SaveWorkspaceAsync()
    {
        try
        {
            IsProgressVisible = true;
            StatusText = "Saving workspace...";
            
            await _workspacePersistenceService.SaveWorkspaceStateAsync(new WorkspaceState
            {
                CurrentMode = CurrentMode,
                LastSaved = DateTime.UtcNow,
                RecentDocuments = RecentDocuments.ToList(),
                DocumentsProcessedToday = DocumentsProcessedToday
            });
            
            HasUnsavedChanges = false;
            await _accessibilityService.AnnounceAsync("Workspace saved successfully");
            
            _logger.LogInformation("Workspace state saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workspace state");
            StatusText = "Failed to save workspace";
        }
        finally
        {
            IsProgressVisible = false;
        }
    }

    /// <summary>
    /// Show settings dialog
    /// </summary>
    private async Task ShowSettingsAsync()
    {
        try
        {
            await _navigationService.ShowDialogAsync(typeof(Views.SettingsView));
            _logger.LogInformation("Settings dialog shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show settings dialog");
        }
    }

    /// <summary>
    /// Show about dialog
    /// </summary>
    private async Task ShowAboutAsync()
    {
        try
        {
            await _navigationService.ShowDialogAsync(typeof(Views.AboutView));
            _logger.LogInformation("About dialog shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show about dialog");
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Initialize ViewModel data asynchronously
    /// </summary>
    private async void InitializeAsync()
    {
        try
        {
            await RefreshWorkspaceAsync();
            
            // Load saved workspace state
            var savedState = await _workspacePersistenceService.LoadWorkspaceStateAsync();
            if (savedState != null)
            {
                CurrentMode = savedState.CurrentMode;
                DocumentsProcessedToday = savedState.DocumentsProcessedToday;
            }
            
            UserGreeting = GenerateUserGreeting();
            
            _logger.LogInformation("MainWindowViewModel initialization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MainWindowViewModel");
        }
    }

    /// <summary>
    /// Generate personalized user greeting based on time of day
    /// </summary>
    private string GenerateUserGreeting()
    {
        var hour = DateTime.Now.Hour;
        var timeGreeting = hour switch
        {
            >= 6 and < 12 => "Good morning",
            >= 12 and < 17 => "Good afternoon",
            >= 17 and < 22 => "Good evening",
            _ => "Good night"
        };
        
        return $"{timeGreeting}! Welcome to your security workspace.";
    }

    /// <summary>
    /// Handle command exceptions with user-friendly messaging
    /// </summary>
    private IObservable<Unit> HandleCommandException(Exception exception)
    {
        return Observable.Start(() =>
        {
            _logger.LogError(exception, "Command execution failed");
            StatusText = $"Error: {exception.Message}";
            
            // Reset navigation state if needed
            if (IsNavigating)
            {
                IsNavigating = false;
            }
        }, RxApp.MainThreadScheduler);
    }

    /// <summary>
    /// Check if window can be closed (prompt for unsaved changes)
    /// </summary>
    public async Task<bool> CanCloseAsync()
    {
        if (!HasUnsavedChanges)
            return true;
            
        // In a real implementation, this would show a dialog
        // For now, auto-save workspace
        try
        {
            await SaveWorkspaceAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workspace before closing");
            return false; // Don't close if save failed
        }
    }

    #endregion

    #region IActivatableViewModel

    /// <summary>
    /// ViewModelActivator for ReactiveUI lifecycle management
    /// </summary>
    public ViewModelActivator Activator { get; }

    /// <summary>
    /// Setup subscriptions when ViewModel becomes active
    /// </summary>
    private void SetupActivation()
    {
        this.WhenActivated(disposables =>
        {
            // Auto-refresh workspace data every 5 minutes
            Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(5))
                .SelectMany(_ => RefreshWorkspaceCommand.Execute())
                .Subscribe()
                .DisposeWith(disposables);

            // Auto-save when changes occur
            this.WhenAnyValue(x => x.HasUnsavedChanges)
                .Where(hasChanges => hasChanges)
                .Throttle(TimeSpan.FromSeconds(30))
                .SelectMany(_ => SaveWorkspaceCommand.Execute())
                .Subscribe()
                .DisposeWith(disposables);

            _logger.LogDebug("MainWindowViewModel activated with auto-refresh and auto-save");
        });
    }

    #endregion
}

/// <summary>
/// Workspace state model for persistence
/// </summary>
public class WorkspaceState
{
    public string CurrentMode { get; set; } = "Dashboard";
    public DateTime LastSaved { get; set; }
    public List<Document> RecentDocuments { get; set; } = new();
    public int DocumentsProcessedToday { get; set; }
} 
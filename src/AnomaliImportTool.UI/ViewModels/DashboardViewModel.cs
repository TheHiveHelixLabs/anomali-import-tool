using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.UI.Services;
using AnomaliImportTool.Core.Models;
using System.Reactive.Disposables;
using System.Linq;

namespace AnomaliImportTool.UI.ViewModels;

/// <summary>
/// Professional Dashboard ViewModel for Living Workspace
/// Provides quick start cards, recent projects, statistics, and mode selection
/// </summary>
public class DashboardViewModel : BaseViewModel
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly IWorkspacePersistenceService _workspacePersistenceService;
    private readonly IAudioFeedbackService _audioFeedbackService;
    private readonly IAnimationService _animationService;
    private readonly IAccessibilityService _accessibilityService;

    // Private backing fields
    private string _userGreeting = "";
    private string _currentTimeDisplay = "";
    private int _documentsProcessedToday = 0;
    private int _totalDocumentsProcessed = 0;
    private int _recentProjectsCount = 0;
    private string _lastActivityTime = "";
    private bool _hasRecentProjects = false;
    private string _selectedMode = "Dashboard";
    private bool _isLoadingData = false;
    private string _systemStatus = "All systems operational";
    private double _dailyProgress = 0.0;
    private string _nextRecommendedAction = "";
    
    // Observable collections
    private ObservableCollection<QuickStartCard> _quickStartCards = new();
    private ObservableCollection<RecentProject> _recentProjects = new();
    private ObservableCollection<SystemAlert> _systemAlerts = new();
    private ObservableCollection<string> _availableModes = new();

    /// <summary>
    /// Initialize Dashboard ViewModel
    /// </summary>
    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        INavigationService navigationService,
        IWorkspacePersistenceService workspacePersistenceService,
        IAudioFeedbackService audioFeedbackService,
        IAnimationService animationService,
        IAccessibilityService accessibilityService) : base(logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _workspacePersistenceService = workspacePersistenceService ?? throw new ArgumentNullException(nameof(workspacePersistenceService));
        _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));

        Title = "Dashboard - Anomali Import Tool";

        // Setup reactive commands
        SetupCommands();

        // Setup reactive properties and bindings
        SetupReactiveProperties();

        // Initialize data
        _ = Task.Run(InitializeDataAsync);

        _logger.LogInformation("DashboardViewModel initialized with ReactiveUI patterns");
    }

    #region Reactive Properties

    /// <summary>
    /// Personalized user greeting
    /// </summary>
    public string UserGreeting
    {
        get => _userGreeting;
        set => this.RaiseAndSetIfChanged(ref _userGreeting, value);
    }

    /// <summary>
    /// Current time display
    /// </summary>
    public string CurrentTimeDisplay
    {
        get => _currentTimeDisplay;
        set => this.RaiseAndSetIfChanged(ref _currentTimeDisplay, value);
    }

    /// <summary>
    /// Documents processed today count
    /// </summary>
    public int DocumentsProcessedToday
    {
        get => _documentsProcessedToday;
        set => this.RaiseAndSetIfChanged(ref _documentsProcessedToday, value);
    }

    /// <summary>
    /// Total documents processed count
    /// </summary>
    public int TotalDocumentsProcessed
    {
        get => _totalDocumentsProcessed;
        set => this.RaiseAndSetIfChanged(ref _totalDocumentsProcessed, value);
    }

    /// <summary>
    /// Recent projects count
    /// </summary>
    public int RecentProjectsCount
    {
        get => _recentProjectsCount;
        set => this.RaiseAndSetIfChanged(ref _recentProjectsCount, value);
    }

    /// <summary>
    /// Last activity time display
    /// </summary>
    public string LastActivityTime
    {
        get => _lastActivityTime;
        set => this.RaiseAndSetIfChanged(ref _lastActivityTime, value);
    }

    /// <summary>
    /// Whether user has recent projects
    /// </summary>
    public bool HasRecentProjects
    {
        get => _hasRecentProjects;
        set => this.RaiseAndSetIfChanged(ref _hasRecentProjects, value);
    }

    /// <summary>
    /// Currently selected mode
    /// </summary>
    public string SelectedMode
    {
        get => _selectedMode;
        set => this.RaiseAndSetIfChanged(ref _selectedMode, value);
    }

    /// <summary>
    /// Whether dashboard is loading data
    /// </summary>
    public bool IsLoadingData
    {
        get => _isLoadingData;
        set => this.RaiseAndSetIfChanged(ref _isLoadingData, value);
    }

    /// <summary>
    /// System status message
    /// </summary>
    public string SystemStatus
    {
        get => _systemStatus;
        set => this.RaiseAndSetIfChanged(ref _systemStatus, value);
    }

    /// <summary>
    /// Daily progress percentage (0-100)
    /// </summary>
    public double DailyProgress
    {
        get => _dailyProgress;
        set => this.RaiseAndSetIfChanged(ref _dailyProgress, value);
    }

    /// <summary>
    /// Next recommended action for user
    /// </summary>
    public string NextRecommendedAction
    {
        get => _nextRecommendedAction;
        set => this.RaiseAndSetIfChanged(ref _nextRecommendedAction, value);
    }

    /// <summary>
    /// Quick start cards collection
    /// </summary>
    public ObservableCollection<QuickStartCard> QuickStartCards
    {
        get => _quickStartCards;
        set => this.RaiseAndSetIfChanged(ref _quickStartCards, value);
    }

    /// <summary>
    /// Recent projects collection
    /// </summary>
    public ObservableCollection<RecentProject> RecentProjects
    {
        get => _recentProjects;
        set => this.RaiseAndSetIfChanged(ref _recentProjects, value);
    }

    /// <summary>
    /// System alerts collection
    /// </summary>
    public ObservableCollection<SystemAlert> SystemAlerts
    {
        get => _systemAlerts;
        set => this.RaiseAndSetIfChanged(ref _systemAlerts, value);
    }

    /// <summary>
    /// Available modes collection
    /// </summary>
    public ObservableCollection<string> AvailableModes
    {
        get => _availableModes;
        set => this.RaiseAndSetIfChanged(ref _availableModes, value);
    }

    #endregion

    #region Reactive Commands

    /// <summary>
    /// Command to switch to Wizard Mode
    /// </summary>
    public ReactiveCommand<Unit, Unit> SwitchToWizardCommand { get; private set; } = null!;

    /// <summary>
    /// Command to switch to Advanced Mode
    /// </summary>
    public ReactiveCommand<Unit, Unit> SwitchToAdvancedCommand { get; private set; } = null!;

    /// <summary>
    /// Command to open recent project
    /// </summary>
    public ReactiveCommand<RecentProject, Unit> OpenRecentProjectCommand { get; private set; } = null!;

    /// <summary>
    /// Command to execute quick start action
    /// </summary>
    public ReactiveCommand<QuickStartCard, Unit> ExecuteQuickStartCommand { get; private set; } = null!;

    /// <summary>
    /// Command to refresh dashboard data
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshDataCommand { get; private set; } = null!;

    /// <summary>
    /// Command to dismiss system alert
    /// </summary>
    public ReactiveCommand<SystemAlert, Unit> DismissAlertCommand { get; private set; } = null!;

    /// <summary>
    /// Command to open settings
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; private set; } = null!;

    /// <summary>
    /// Command to show help
    /// </summary>
    public ReactiveCommand<Unit, Unit> ShowHelpCommand { get; private set; } = null!;

    #endregion

    #region Private Methods

    /// <summary>
    /// Setup reactive commands with proper error handling
    /// </summary>
    private void SetupCommands()
    {
        // Switch to Wizard Mode command
        SwitchToWizardCommand = ReactiveCommand.CreateFromTask(SwitchToWizardAsync);

        // Switch to Advanced Mode command
        SwitchToAdvancedCommand = ReactiveCommand.CreateFromTask(SwitchToAdvancedAsync);

        // Open Recent Project command
        OpenRecentProjectCommand = ReactiveCommand.CreateFromTask<RecentProject>(OpenRecentProjectAsync);

        // Execute Quick Start command
        ExecuteQuickStartCommand = ReactiveCommand.CreateFromTask<QuickStartCard>(ExecuteQuickStartAsync);

        // Refresh Data command
        RefreshDataCommand = ReactiveCommand.CreateFromTask(RefreshDataAsync);

        // Dismiss Alert command
        DismissAlertCommand = ReactiveCommand.CreateFromTask<SystemAlert>(DismissAlertAsync);

        // Open Settings command
        OpenSettingsCommand = ReactiveCommand.CreateFromTask(OpenSettingsAsync);

        // Show Help command
        ShowHelpCommand = ReactiveCommand.CreateFromTask(ShowHelpAsync);

        _logger.LogDebug("Dashboard reactive commands configured");
    }

    /// <summary>
    /// Setup reactive properties and derived observables
    /// </summary>
    private void SetupReactiveProperties()
    {
        // Update time display every minute
        Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
            .Subscribe(_ => UpdateTimeDisplay());

        // Update user greeting based on time of day
        this.WhenAnyValue(x => x.CurrentTimeDisplay)
            .Subscribe(_ => UpdateUserGreeting());

        // Update progress indicators
        this.WhenAnyValue(x => x.DocumentsProcessedToday)
            .Subscribe(count => UpdateDailyProgress(count));

        // Monitor for recent projects changes
        this.WhenAnyValue(x => x.RecentProjectsCount)
            .Subscribe(count => HasRecentProjects = count > 0);

        _logger.LogDebug("Dashboard reactive properties configured");
    }

    /// <summary>
    /// Initialize dashboard data asynchronously
    /// </summary>
    private async Task InitializeDataAsync()
    {
        try
        {
            IsLoadingData = true;
            StatusMessage = "Loading dashboard data...";

            // Load statistics
            await LoadStatisticsAsync();

            // Load recent projects
            await LoadRecentProjectsAsync();

            // Setup quick start cards
            await SetupQuickStartCardsAsync();

            // Load system alerts
            await LoadSystemAlertsAsync();

            // Setup available modes
            SetupAvailableModes();

            // Update time and greeting
            UpdateTimeDisplay();
            UpdateUserGreeting();

            StatusMessage = "Dashboard ready";
            _logger.LogInformation("Dashboard data initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize dashboard data");
            StatusMessage = "Error loading dashboard data";
            HasErrors = true;
            ErrorMessage = "Failed to load dashboard. Please try refreshing.";
        }
        finally
        {
            IsLoadingData = false;
        }
    }

    /// <summary>
    /// Load user statistics
    /// </summary>
    private async Task LoadStatisticsAsync()
    {
        try
        {
            DocumentsProcessedToday = await _workspacePersistenceService.GetDocumentsProcessedTodayAsync();
            TotalDocumentsProcessed = DocumentsProcessedToday * 30; // Simulated total
            
            var lastActivity = DateTime.Now.AddHours(-2); // Simulated
            LastActivityTime = $"Last activity: {lastActivity:HH:mm}";

            _logger.LogDebug("Statistics loaded: {TodayCount} today, {TotalCount} total", 
                DocumentsProcessedToday, TotalDocumentsProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load statistics");
        }
    }

    /// <summary>
    /// Load recent projects
    /// </summary>
    private async Task LoadRecentProjectsAsync()
    {
        try
        {
            var recentDocs = await _workspacePersistenceService.LoadRecentDocumentsAsync();
            
            RecentProjects.Clear();
            
            // Convert documents to recent projects (simplified)
            foreach (var doc in recentDocs.Take(5))
            {
                RecentProjects.Add(new RecentProject
                {
                    Name = Path.GetFileNameWithoutExtension(doc.FileName),
                    LastModified = DateTime.Now.AddDays(-RecentProjects.Count),
                    DocumentCount = 1,
                    Status = "Completed",
                    FilePath = doc.FileName
                });
            }

            RecentProjectsCount = RecentProjects.Count;

            _logger.LogDebug("Loaded {ProjectCount} recent projects", RecentProjectsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent projects");
        }
    }

    /// <summary>
    /// Setup quick start cards
    /// </summary>
    private async Task SetupQuickStartCardsAsync()
    {
        QuickStartCards.Clear();

        var cards = new[]
        {
            new QuickStartCard
            {
                Title = "Process Documents",
                Description = "Start the guided wizard to process threat intelligence documents",
                Icon = "Document",
                Action = async () => await SwitchToWizardAsync(),
                Category = "Primary",
                EstimatedTime = "5-10 minutes"
            },
            new QuickStartCard
            {
                Title = "Advanced Mode",
                Description = "Access advanced features and bulk processing options",
                Icon = "Settings",
                Action = async () => await SwitchToAdvancedAsync(),
                Category = "Advanced",
                EstimatedTime = "Variable"
            },
            new QuickStartCard
            {
                Title = "View Reports",
                Description = "Review processing reports and analytics",
                Icon = "Analytics",
                Action = async () => await NavigateToReportsAsync(),
                Category = "Analysis",
                EstimatedTime = "2-5 minutes"
            },
            new QuickStartCard
            {
                Title = "Configuration",
                Description = "Configure API settings and processing options",
                Icon = "Config",
                Action = async () => await OpenSettingsAsync(),
                Category = "Setup",
                EstimatedTime = "3-7 minutes"
            }
        };

        foreach (var card in cards)
        {
            QuickStartCards.Add(card);
        }

        _logger.LogDebug("Setup {CardCount} quick start cards", QuickStartCards.Count);
        await Task.Delay(1); // Await compliance
    }

    /// <summary>
    /// Load system alerts
    /// </summary>
    private async Task LoadSystemAlertsAsync()
    {
        SystemAlerts.Clear();

        // Add sample alerts (in real app, these would come from a service)
        if (DocumentsProcessedToday == 0)
        {
            SystemAlerts.Add(new SystemAlert
            {
                Id = Guid.NewGuid(),
                Type = AlertType.Info,
                Title = "Welcome!",
                Message = "Start by processing your first document using the wizard.",
                Timestamp = DateTime.Now,
                IsActionable = true,
                ActionText = "Start Wizard",
                Action = async () => await SwitchToWizardAsync()
            });
        }

        _logger.LogDebug("Loaded {AlertCount} system alerts", SystemAlerts.Count);
        await Task.Delay(1); // Await compliance
    }

    /// <summary>
    /// Setup available modes
    /// </summary>
    private void SetupAvailableModes()
    {
        AvailableModes.Clear();
        AvailableModes.Add("Dashboard");
        AvailableModes.Add("Wizard");
        AvailableModes.Add("Advanced");

        _logger.LogDebug("Setup {ModeCount} available modes", AvailableModes.Count);
    }

    /// <summary>
    /// Update time display
    /// </summary>
    private void UpdateTimeDisplay()
    {
        CurrentTimeDisplay = DateTime.Now.ToString("dddd, MMMM dd, yyyy - HH:mm");
    }

    /// <summary>
    /// Update user greeting based on time of day
    /// </summary>
    private void UpdateUserGreeting()
    {
        var hour = DateTime.Now.Hour;
        var greeting = hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon",
            _ => "Good evening"
        };

        UserGreeting = $"{greeting}! Ready to process threat intelligence?";
    }

    /// <summary>
    /// Update daily progress calculation
    /// </summary>
    private void UpdateDailyProgress(int documentsProcessed)
    {
        // Assume target of 10 documents per day
        const int dailyTarget = 10;
        DailyProgress = Math.Min(100.0, (documentsProcessed / (double)dailyTarget) * 100.0);

        // Update recommended action
        if (documentsProcessed == 0)
        {
            NextRecommendedAction = "Start by processing your first document";
        }
        else if (DailyProgress < 50)
        {
            NextRecommendedAction = "Continue processing documents to reach your daily goal";
        }
        else if (DailyProgress >= 100)
        {
            NextRecommendedAction = "Excellent! You've exceeded your daily goal";
        }
        else
        {
            NextRecommendedAction = $"You're {DailyProgress:F0}% towards your daily goal";
        }
    }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Switch to Wizard Mode with audio feedback
    /// </summary>
    private async Task SwitchToWizardAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Switching to Wizard Mode...";

            // Play audio feedback
            await _audioFeedbackService.PlayNotificationAsync();

            // Announce to screen readers
            await _accessibilityService.AnnounceAsync("Switching to Wizard Mode - guided document processing");

            // Navigate with animation
            await _navigationService.SwitchWorkflowModeAsync("Wizard");

            _logger.LogInformation("Successfully switched to Wizard Mode from Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to Wizard Mode");
            await _audioFeedbackService.PlayErrorAsync();
            StatusMessage = "Failed to switch to Wizard Mode";
            HasErrors = true;
            ErrorMessage = "Unable to switch to Wizard Mode. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Switch to Advanced Mode with audio feedback
    /// </summary>
    private async Task SwitchToAdvancedAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Switching to Advanced Mode...";

            // Play audio feedback
            await _audioFeedbackService.PlayNotificationAsync();

            // Announce to screen readers
            await _accessibilityService.AnnounceAsync("Switching to Advanced Mode - professional document processing");

            // Navigate with animation
            await _navigationService.SwitchWorkflowModeAsync("Advanced");

            _logger.LogInformation("Successfully switched to Advanced Mode from Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to Advanced Mode");
            await _audioFeedbackService.PlayErrorAsync();
            StatusMessage = "Failed to switch to Advanced Mode";
            HasErrors = true;
            ErrorMessage = "Unable to switch to Advanced Mode. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Open recent project
    /// </summary>
    private async Task OpenRecentProjectAsync(RecentProject project)
    {
        try
        {
            _logger.LogInformation("Opening recent project: {ProjectName}", project.Name);

            // Play success sound
            await _audioFeedbackService.PlaySuccessAsync();

            // Navigate to project (simplified)
            await _navigationService.NavigateToAsync(typeof(Views.ProcessingView), project);

            StatusMessage = $"Opened project: {project.Name}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open recent project: {ProjectName}", project.Name);
            await _audioFeedbackService.PlayErrorAsync();
        }
    }

    /// <summary>
    /// Execute quick start action
    /// </summary>
    private async Task ExecuteQuickStartAsync(QuickStartCard card)
    {
        try
        {
            _logger.LogInformation("Executing quick start action: {CardTitle}", card.Title);

            // Play notification sound
            await _audioFeedbackService.PlayNotificationAsync();

            // Execute card action
            if (card.Action != null)
            {
                await card.Action();
            }

            StatusMessage = $"Executed: {card.Title}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute quick start action: {CardTitle}", card.Title);
            await _audioFeedbackService.PlayErrorAsync();
        }
    }

    /// <summary>
    /// Refresh dashboard data
    /// </summary>
    private async Task RefreshDataAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing dashboard data");
            await InitializeDataAsync();
            await _audioFeedbackService.PlaySuccessAsync();
            StatusMessage = "Dashboard refreshed successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh dashboard data");
            await _audioFeedbackService.PlayErrorAsync();
        }
    }

    /// <summary>
    /// Dismiss system alert
    /// </summary>
    private async Task DismissAlertAsync(SystemAlert alert)
    {
        try
        {
            SystemAlerts.Remove(alert);
            _logger.LogDebug("Dismissed alert: {AlertTitle}", alert.Title);
            await Task.Delay(1); // Await compliance
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss alert: {AlertTitle}", alert.Title);
        }
    }

    /// <summary>
    /// Open settings
    /// </summary>
    private async Task OpenSettingsAsync()
    {
        try
        {
            await _navigationService.NavigateToAsync(typeof(Views.SettingsView));
            _logger.LogInformation("Navigated to Settings from Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Settings");
        }
    }

    /// <summary>
    /// Show help
    /// </summary>
    private async Task ShowHelpAsync()
    {
        try
        {
            await _navigationService.ShowDialogAsync(typeof(Views.HelpDialog));
            _logger.LogInformation("Showed help dialog from Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show help dialog");
        }
    }

    /// <summary>
    /// Navigate to reports (helper method)
    /// </summary>
    private async Task NavigateToReportsAsync()
    {
        try
        {
            await _navigationService.NavigateToAsync(typeof(Views.ReportsView));
            _logger.LogInformation("Navigated to Reports from Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to Reports");
        }
    }

    #endregion
}

#region Supporting Models

/// <summary>
/// Quick start card model
/// </summary>
public class QuickStartCard
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public Func<Task>? Action { get; set; }
    public string Category { get; set; } = "";
    public string EstimatedTime { get; set; } = "";
}

/// <summary>
/// Recent project model
/// </summary>
public class RecentProject
{
    public string Name { get; set; } = "";
    public DateTime LastModified { get; set; }
    public int DocumentCount { get; set; }
    public string Status { get; set; } = "";
    public string FilePath { get; set; } = "";
}

/// <summary>
/// System alert model
/// </summary>
public class SystemAlert
{
    public Guid Id { get; set; }
    public AlertType Type { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool IsActionable { get; set; }
    public string ActionText { get; set; } = "";
    public Func<Task>? Action { get; set; }
}

/// <summary>
/// Alert type enumeration
/// </summary>
public enum AlertType
{
    Info,
    Warning,
    Error,
    Success
}

#endregion 
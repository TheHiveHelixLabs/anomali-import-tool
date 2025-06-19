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
using System.IO;
using System.Collections.Generic;

namespace AnomaliImportTool.UI.ViewModels;

/// <summary>
/// Professional Advanced Mode ViewModel for Living Workspace
/// Provides multi-panel interface for power users with bulk processing and expert features
/// </summary>
public class AdvancedModeViewModel : BaseViewModel
{
    private readonly ILogger<AdvancedModeViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly IWorkspacePersistenceService _workspacePersistenceService;
    private readonly IAudioFeedbackService _audioFeedbackService;
    private readonly IAnimationService _animationService;
    private readonly IAccessibilityService _accessibilityService;

    // Private backing fields
    private string _selectedTab = "Files";
    private bool _isProcessingBulk = false;
    private int _totalFiles = 0;
    private int _processedFiles = 0;
    private int _failedFiles = 0;
    private double _bulkProgress = 0.0;
    private string _bulkStatusMessage = "";
    private bool _showDetailedLogs = true;
    private bool _autoProcessNewFiles = false;
    private string _currentWorkspace = "";
    private bool _isExpertMode = false;
    
    // Collections
    private ObservableCollection<AdvancedFileItem> _files = new();
    private ObservableCollection<ProcessingTask> _processingQueue = new();
    private ObservableCollection<BatchOperation> _batchOperations = new();
    private ObservableCollection<LogEntry> _detailedLogs = new();
    private ObservableCollection<AdvancedConfiguration> _configurations = new();
    private ObservableCollection<ExportTemplate> _exportTemplates = new();
    private ObservableCollection<WorkspaceProfile> _workspaceProfiles = new();

    /// <summary>
    /// Initialize Advanced Mode ViewModel
    /// </summary>
    public AdvancedModeViewModel(
        ILogger<AdvancedModeViewModel> logger,
        INavigationService navigationService,
        IWorkspacePersistenceService workspacePersistenceService,
        IAudioFeedbackService audioFeedbackService,
        IAnimationService animationService,
        IAccessibilityService accessibilityService)
        : base(logger, navigationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _workspacePersistenceService = workspacePersistenceService ?? throw new ArgumentNullException(nameof(workspacePersistenceService));
        _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));

        // Setup reactive commands
        SetupCommands();

        // Setup reactive properties
        SetupReactiveProperties();

        // Initialize data
        InitializeAsync();

        _logger.LogInformation("AdvancedModeViewModel initialized with enterprise features");
    }

    #region Reactive Properties

    /// <summary>
    /// Currently selected tab (Files, Processing, Configuration, Logs, Reports)
    /// </summary>
    public string SelectedTab
    {
        get => _selectedTab;
        set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
    }

    /// <summary>
    /// Whether bulk processing is currently running
    /// </summary>
    public bool IsProcessingBulk
    {
        get => _isProcessingBulk;
        set => this.RaiseAndSetIfChanged(ref _isProcessingBulk, value);
    }

    /// <summary>
    /// Total number of files in processing queue
    /// </summary>
    public int TotalFiles
    {
        get => _totalFiles;
        set => this.RaiseAndSetIfChanged(ref _totalFiles, value);
    }

    /// <summary>
    /// Number of successfully processed files
    /// </summary>
    public int ProcessedFiles
    {
        get => _processedFiles;
        set => this.RaiseAndSetIfChanged(ref _processedFiles, value);
    }

    /// <summary>
    /// Number of files that failed processing
    /// </summary>
    public int FailedFiles
    {
        get => _failedFiles;
        set => this.RaiseAndSetIfChanged(ref _failedFiles, value);
    }

    /// <summary>
    /// Overall bulk processing progress (0-100)
    /// </summary>
    public double BulkProgress
    {
        get => _bulkProgress;
        set => this.RaiseAndSetIfChanged(ref _bulkProgress, value);
    }

    /// <summary>
    /// Current bulk processing status message
    /// </summary>
    public string BulkStatusMessage
    {
        get => _bulkStatusMessage;
        set => this.RaiseAndSetIfChanged(ref _bulkStatusMessage, value);
    }

    /// <summary>
    /// Whether to show detailed processing logs
    /// </summary>
    public bool ShowDetailedLogs
    {
        get => _showDetailedLogs;
        set => this.RaiseAndSetIfChanged(ref _showDetailedLogs, value);
    }

    /// <summary>
    /// Whether to automatically process new files when added
    /// </summary>
    public bool AutoProcessNewFiles
    {
        get => _autoProcessNewFiles;
        set => this.RaiseAndSetIfChanged(ref _autoProcessNewFiles, value);
    }

    /// <summary>
    /// Current workspace name
    /// </summary>
    public string CurrentWorkspace
    {
        get => _currentWorkspace;
        set => this.RaiseAndSetIfChanged(ref _currentWorkspace, value);
    }

    /// <summary>
    /// Whether expert mode is enabled (shows advanced options)
    /// </summary>
    public bool IsExpertMode
    {
        get => _isExpertMode;
        set => this.RaiseAndSetIfChanged(ref _isExpertMode, value);
    }

    /// <summary>
    /// Collection of files in the advanced workspace
    /// </summary>
    public ObservableCollection<AdvancedFileItem> Files
    {
        get => _files;
        set => this.RaiseAndSetIfChanged(ref _files, value);
    }

    /// <summary>
    /// Processing queue for bulk operations
    /// </summary>
    public ObservableCollection<ProcessingTask> ProcessingQueue
    {
        get => _processingQueue;
        set => this.RaiseAndSetIfChanged(ref _processingQueue, value);
    }

    /// <summary>
    /// Available batch operations
    /// </summary>
    public ObservableCollection<BatchOperation> BatchOperations
    {
        get => _batchOperations;
        set => this.RaiseAndSetIfChanged(ref _batchOperations, value);
    }

    /// <summary>
    /// Detailed processing logs
    /// </summary>
    public ObservableCollection<LogEntry> DetailedLogs
    {
        get => _detailedLogs;
        set => this.RaiseAndSetIfChanged(ref _detailedLogs, value);
    }

    /// <summary>
    /// Advanced configuration profiles
    /// </summary>
    public ObservableCollection<AdvancedConfiguration> Configurations
    {
        get => _configurations;
        set => this.RaiseAndSetIfChanged(ref _configurations, value);
    }

    /// <summary>
    /// Export templates for reports
    /// </summary>
    public ObservableCollection<ExportTemplate> ExportTemplates
    {
        get => _exportTemplates;
        set => this.RaiseAndSetIfChanged(ref _exportTemplates, value);
    }

    /// <summary>
    /// Workspace profiles for different scenarios
    /// </summary>
    public ObservableCollection<WorkspaceProfile> WorkspaceProfiles
    {
        get => _workspaceProfiles;
        set => this.RaiseAndSetIfChanged(ref _workspaceProfiles, value);
    }

    #endregion

    #region Reactive Commands

    /// <summary>
    /// Command to switch active tab
    /// </summary>
    public ReactiveCommand<string, Unit> SwitchTabCommand { get; private set; } = null!;

    /// <summary>
    /// Command to add files to workspace
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddFilesCommand { get; private set; } = null!;

    /// <summary>
    /// Command to remove selected files
    /// </summary>
    public ReactiveCommand<Unit, Unit> RemoveFilesCommand { get; private set; } = null!;

    /// <summary>
    /// Command to start bulk processing
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartBulkProcessingCommand { get; private set; } = null!;

    /// <summary>
    /// Command to pause bulk processing
    /// </summary>
    public ReactiveCommand<Unit, Unit> PauseBulkProcessingCommand { get; private set; } = null!;

    /// <summary>
    /// Command to cancel bulk processing
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelBulkProcessingCommand { get; private set; } = null!;

    /// <summary>
    /// Command to create new batch operation
    /// </summary>
    public ReactiveCommand<Unit, Unit> CreateBatchOperationCommand { get; private set; } = null!;

    /// <summary>
    /// Command to save current workspace
    /// </summary>
    public ReactiveCommand<Unit, Unit> SaveWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to load workspace
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadWorkspaceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to export processing results
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExportResultsCommand { get; private set; } = null!;

    /// <summary>
    /// Command to toggle expert mode
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleExpertModeCommand { get; private set; } = null!;

    /// <summary>
    /// Command to clear all logs
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearLogsCommand { get; private set; } = null!;

    /// <summary>
    /// Command to switch back to wizard mode
    /// </summary>
    public ReactiveCommand<Unit, Unit> SwitchToWizardCommand { get; private set; } = null!;

    /// <summary>
    /// Command to return to dashboard
    /// </summary>
    public ReactiveCommand<Unit, Unit> ReturnToDashboardCommand { get; private set; } = null!;

    #endregion

    #region Setup Methods

    /// <summary>
    /// Setup reactive commands with appropriate conditions
    /// </summary>
    private void SetupCommands()
    {
        // Tab navigation
        SwitchTabCommand = ReactiveCommand.Create<string>(SwitchTab);

        // File management
        var hasFiles = this.WhenAnyValue(x => x.Files.Count, count => count > 0);
        var notProcessing = this.WhenAnyValue(x => x.IsProcessingBulk, processing => !processing);

        AddFilesCommand = ReactiveCommand.CreateFromTask(
            AddFilesAsync,
            notProcessing,
            RxApp.MainThreadScheduler);

        RemoveFilesCommand = ReactiveCommand.CreateFromTask(
            RemoveFilesAsync,
            hasFiles.CombineLatest(notProcessing, (files, processing) => files && processing),
            RxApp.MainThreadScheduler);

        // Bulk processing
        var canStartProcessing = hasFiles.CombineLatest(notProcessing, (files, processing) => files && processing);
        var canPauseProcessing = this.WhenAnyValue(x => x.IsProcessingBulk);
        var canCancelProcessing = this.WhenAnyValue(x => x.IsProcessingBulk);

        StartBulkProcessingCommand = ReactiveCommand.CreateFromTask(
            StartBulkProcessingAsync,
            canStartProcessing,
            RxApp.MainThreadScheduler);

        PauseBulkProcessingCommand = ReactiveCommand.CreateFromTask(
            PauseBulkProcessingAsync,
            canPauseProcessing,
            RxApp.MainThreadScheduler);

        CancelBulkProcessingCommand = ReactiveCommand.CreateFromTask(
            CancelBulkProcessingAsync,
            canCancelProcessing,
            RxApp.MainThreadScheduler);

        // Workspace operations
        CreateBatchOperationCommand = ReactiveCommand.CreateFromTask(
            CreateBatchOperationAsync,
            notProcessing,
            RxApp.MainThreadScheduler);

        SaveWorkspaceCommand = ReactiveCommand.CreateFromTask(
            SaveWorkspaceAsync,
            hasFiles,
            RxApp.MainThreadScheduler);

        LoadWorkspaceCommand = ReactiveCommand.CreateFromTask(
            LoadWorkspaceAsync,
            notProcessing,
            RxApp.MainThreadScheduler);

        ExportResultsCommand = ReactiveCommand.CreateFromTask(
            ExportResultsAsync,
            hasFiles,
            RxApp.MainThreadScheduler);

        // Mode operations
        ToggleExpertModeCommand = ReactiveCommand.Create(ToggleExpertMode);

        ClearLogsCommand = ReactiveCommand.CreateFromTask(
            ClearLogsAsync,
            this.WhenAnyValue(x => x.DetailedLogs.Count, count => count > 0),
            RxApp.MainThreadScheduler);

        SwitchToWizardCommand = ReactiveCommand.CreateFromTask(
            SwitchToWizardAsync,
            notProcessing,
            RxApp.MainThreadScheduler);

        ReturnToDashboardCommand = ReactiveCommand.CreateFromTask(
            ReturnToDashboardAsync,
            notProcessing,
            RxApp.MainThreadScheduler);

        // Setup command error handling
        SetupCommandErrorHandling();
    }

    /// <summary>
    /// Setup reactive properties with subscriptions and computed values
    /// </summary>
    private void SetupReactiveProperties()
    {
        // Update totals when files collection changes
        this.WhenAnyValue(x => x.Files.Count)
            .Subscribe(count => TotalFiles = count);

        // Update status message based on processing state
        this.WhenAnyValue(x => x.IsProcessingBulk, x => x.ProcessedFiles, x => x.TotalFiles)
            .Select(tuple =>
            {
                if (!tuple.Item1) return "Ready for bulk processing";
                return $"Processing: {tuple.Item2} of {tuple.Item3} files completed";
            })
            .Subscribe(message => BulkStatusMessage = message);

        // Calculate progress
        this.WhenAnyValue(x => x.ProcessedFiles, x => x.TotalFiles)
            .Where(tuple => tuple.Item2 > 0)
            .Select(tuple => (tuple.Item1 * 100.0) / tuple.Item2)
            .Subscribe(progress => BulkProgress = progress);

        // Auto-process files if enabled
        this.WhenAnyValue(x => x.AutoProcessNewFiles, x => x.Files.Count)
            .Where(tuple => tuple.Item1 && tuple.Item2 > 0 && !IsProcessingBulk)
            .SelectMany(_ => StartBulkProcessingAsync().ToObservable())
            .Subscribe();

        // Log tab switches for analytics
        this.WhenAnyValue(x => x.SelectedTab)
            .Subscribe(tab => AddLogEntry($"Switched to {tab} tab", LogLevel.Information));
    }

    /// <summary>
    /// Setup error handling for reactive commands
    /// </summary>
    private void SetupCommandErrorHandling()
    {
        var allCommands = new ReactiveCommand<Unit, Unit>[]
        {
            AddFilesCommand,
            RemoveFilesCommand,
            StartBulkProcessingCommand,
            PauseBulkProcessingCommand,
            CancelBulkProcessingCommand,
            CreateBatchOperationCommand,
            SaveWorkspaceCommand,
            LoadWorkspaceCommand,
            ExportResultsCommand,
            ClearLogsCommand,
            SwitchToWizardCommand,
            ReturnToDashboardCommand
        };

        foreach (var command in allCommands)
        {
            command.ThrownExceptions
                .SelectMany(HandleCommandException)
                .Subscribe();
        }

        // Handle SwitchTabCommand separately as it has different signature
        SwitchTabCommand.ThrownExceptions
            .SelectMany(HandleCommandException)
            .Subscribe();
    }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Switch to specified tab
    /// </summary>
    private void SwitchTab(string tabName)
    {
        try
        {
            SelectedTab = tabName;
            _logger.LogDebug("Switched to tab: {TabName}", tabName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to tab: {TabName}", tabName);
        }
    }

    /// <summary>
    /// Add files to workspace
    /// </summary>
    private async Task AddFilesAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Adding files to workspace...";

            // Simulate file dialog (would use actual file dialog in implementation)
            await Task.Delay(500);

            // Add mock files for demonstration
            var mockFiles = new[]
            {
                new AdvancedFileItem
                {
                    FileName = "threat_analysis.pdf",
                    FilePath = @"C:\Documents\threat_analysis.pdf",
                    FileSize = 2.5,
                    FileType = "PDF",
                    Status = FileStatus.Ready,
                    Priority = ProcessingPriority.Normal,
                    Tags = new ObservableCollection<string> { "threat", "analysis" }
                },
                new AdvancedFileItem
                {
                    FileName = "ioc_report.docx",
                    FilePath = @"C:\Documents\ioc_report.docx",
                    FileSize = 1.8,
                    FileType = "Word",
                    Status = FileStatus.Ready,
                    Priority = ProcessingPriority.High,
                    Tags = new ObservableCollection<string> { "ioc", "report" }
                }
            };

            foreach (var file in mockFiles)
            {
                Files.Add(file);
            }

            await _audioFeedbackService.PlayNotificationAsync();
            StatusMessage = $"Added {mockFiles.Length} files to workspace";
            AddLogEntry($"Added {mockFiles.Length} files to workspace", LogLevel.Information);

            _logger.LogInformation("Successfully added {FileCount} files to advanced workspace", mockFiles.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add files to workspace");
            await _audioFeedbackService.PlayErrorAsync();
            HasErrors = true;
            ErrorMessage = "Failed to add files. Please try again.";
            AddLogEntry($"Error adding files: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Remove selected files from workspace
    /// </summary>
    private async Task RemoveFilesAsync()
    {
        try
        {
            var selectedFiles = Files.Where(f => f.IsSelected).ToList();
            if (!selectedFiles.Any())
            {
                StatusMessage = "No files selected for removal";
                return;
            }

            foreach (var file in selectedFiles)
            {
                Files.Remove(file);
            }

            await _audioFeedbackService.PlayNotificationAsync();
            StatusMessage = $"Removed {selectedFiles.Count} files from workspace";
            AddLogEntry($"Removed {selectedFiles.Count} files from workspace", LogLevel.Information);

            _logger.LogInformation("Removed {FileCount} files from advanced workspace", selectedFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove files from workspace");
            await _audioFeedbackService.PlayErrorAsync();
            AddLogEntry($"Error removing files: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Start bulk processing of all files
    /// </summary>
    private async Task StartBulkProcessingAsync()
    {
        try
        {
            IsProcessingBulk = true;
            ProcessedFiles = 0;
            FailedFiles = 0;
            BulkProgress = 0;

            StatusMessage = "Starting bulk processing...";
            AddLogEntry($"Starting bulk processing of {TotalFiles} files", LogLevel.Information);

            await _audioFeedbackService.PlayNotificationAsync();
            await _accessibilityService.AnnounceAsync($"Starting bulk processing of {TotalFiles} files");

            // Process files one by one
            for (int i = 0; i < Files.Count; i++)
            {
                if (!IsProcessingBulk) break; // Check if cancelled

                var file = Files[i];
                file.Status = FileStatus.Processing;

                try
                {
                    // Simulate processing
                    for (int progress = 0; progress <= 100; progress += 25)
                    {
                        file.ProcessingProgress = progress;
                        await Task.Delay(200);
                    }

                    file.Status = FileStatus.Completed;
                    file.ExtractedIOCs = 15 + i * 3; // Simulated
                    file.ExtractedTTPs = 8 + i * 2; // Simulated
                    file.ProcessingTime = TimeSpan.FromSeconds(2 + i);

                    ProcessedFiles++;
                    AddLogEntry($"Successfully processed {file.FileName}", LogLevel.Information);
                }
                catch (Exception fileEx)
                {
                    file.Status = FileStatus.Failed;
                    file.ErrorMessage = fileEx.Message;
                    FailedFiles++;
                    AddLogEntry($"Failed to process {file.FileName}: {fileEx.Message}", LogLevel.Error);
                }
            }

            if (IsProcessingBulk)
            {
                await _audioFeedbackService.PlaySuccessAsync();
                StatusMessage = $"Bulk processing completed: {ProcessedFiles} successful, {FailedFiles} failed";
                AddLogEntry($"Bulk processing completed: {ProcessedFiles} successful, {FailedFiles} failed", LogLevel.Information);
                await _accessibilityService.AnnounceAsync("Bulk processing completed successfully");
            }

            _logger.LogInformation("Bulk processing completed: {ProcessedFiles} successful, {FailedFiles} failed", 
                ProcessedFiles, FailedFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk processing failed");
            await _audioFeedbackService.PlayErrorAsync();
            HasErrors = true;
            ErrorMessage = "Bulk processing failed. Check logs for details.";
            AddLogEntry($"Bulk processing error: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsProcessingBulk = false;
        }
    }

    /// <summary>
    /// Pause bulk processing
    /// </summary>
    private async Task PauseBulkProcessingAsync()
    {
        try
        {
            IsProcessingBulk = false;
            StatusMessage = "Bulk processing paused";
            AddLogEntry("Bulk processing paused by user", LogLevel.Information);
            await _accessibilityService.AnnounceAsync("Bulk processing paused");
            _logger.LogInformation("Bulk processing paused");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause bulk processing");
        }
    }

    /// <summary>
    /// Cancel bulk processing
    /// </summary>
    private async Task CancelBulkProcessingAsync()
    {
        try
        {
            IsProcessingBulk = false;
            StatusMessage = "Bulk processing cancelled";
            AddLogEntry("Bulk processing cancelled by user", LogLevel.Warning);
            await _accessibilityService.AnnounceAsync("Bulk processing cancelled");

            // Reset file statuses
            foreach (var file in Files.Where(f => f.Status == FileStatus.Processing))
            {
                file.Status = FileStatus.Ready;
                file.ProcessingProgress = 0;
            }

            _logger.LogInformation("Bulk processing cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel bulk processing");
        }
    }

    /// <summary>
    /// Create new batch operation
    /// </summary>
    private async Task CreateBatchOperationAsync()
    {
        try
        {
            var batchOp = new BatchOperation
            {
                Name = $"Batch_{DateTime.Now:yyyyMMdd_HHmmss}",
                Description = "Custom batch operation",
                CreatedAt = DateTime.Now,
                Files = Files.Where(f => f.IsSelected).ToList(),
                Status = BatchStatus.Created
            };

            BatchOperations.Add(batchOp);
            StatusMessage = $"Created batch operation: {batchOp.Name}";
            AddLogEntry($"Created batch operation: {batchOp.Name} with {batchOp.Files.Count} files", LogLevel.Information);

            await _audioFeedbackService.PlayNotificationAsync();
            _logger.LogInformation("Created batch operation: {BatchName}", batchOp.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create batch operation");
            AddLogEntry($"Error creating batch operation: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Save current workspace
    /// </summary>
    private async Task SaveWorkspaceAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Saving workspace...";

            var workspace = new WorkspaceState
            {
                Name = CurrentWorkspace,
                Files = Files.ToList(),
                Configurations = Configurations.ToList(),
                CreatedAt = DateTime.Now,
                IsExpertMode = IsExpertMode
            };

            // Simulate save
            await Task.Delay(1000);

            StatusMessage = "Workspace saved successfully";
            AddLogEntry($"Workspace '{CurrentWorkspace}' saved successfully", LogLevel.Information);
            await _audioFeedbackService.PlaySuccessAsync();

            _logger.LogInformation("Workspace saved: {WorkspaceName}", CurrentWorkspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workspace");
            await _audioFeedbackService.PlayErrorAsync();
            HasErrors = true;
            ErrorMessage = "Failed to save workspace. Please try again.";
            AddLogEntry($"Error saving workspace: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Load workspace
    /// </summary>
    private async Task LoadWorkspaceAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading workspace...";

            // Simulate load
            await Task.Delay(1000);

            StatusMessage = "Workspace loaded successfully";
            AddLogEntry($"Workspace '{CurrentWorkspace}' loaded successfully", LogLevel.Information);
            await _audioFeedbackService.PlaySuccessAsync();

            _logger.LogInformation("Workspace loaded: {WorkspaceName}", CurrentWorkspace);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workspace");
            await _audioFeedbackService.PlayErrorAsync();
            HasErrors = true;
            ErrorMessage = "Failed to load workspace. Please try again.";
            AddLogEntry($"Error loading workspace: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Export processing results
    /// </summary>
    private async Task ExportResultsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Exporting results...";

            // Simulate export
            await Task.Delay(2000);

            StatusMessage = "Results exported successfully";
            AddLogEntry($"Exported results for {TotalFiles} files", LogLevel.Information);
            await _audioFeedbackService.PlaySuccessAsync();

            _logger.LogInformation("Exported results for {FileCount} files", TotalFiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export results");
            await _audioFeedbackService.PlayErrorAsync();
            HasErrors = true;
            ErrorMessage = "Failed to export results. Please try again.";
            AddLogEntry($"Error exporting results: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Toggle expert mode
    /// </summary>
    private void ToggleExpertMode()
    {
        IsExpertMode = !IsExpertMode;
        StatusMessage = IsExpertMode ? "Expert mode enabled" : "Expert mode disabled";
        AddLogEntry($"Expert mode {(IsExpertMode ? "enabled" : "disabled")}", LogLevel.Information);
        _logger.LogInformation("Expert mode {Status}", IsExpertMode ? "enabled" : "disabled");
    }

    /// <summary>
    /// Clear all logs
    /// </summary>
    private async Task ClearLogsAsync()
    {
        try
        {
            DetailedLogs.Clear();
            StatusMessage = "Logs cleared";
            await _audioFeedbackService.PlayNotificationAsync();
            _logger.LogInformation("Detailed logs cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear logs");
        }
    }

    /// <summary>
    /// Switch to wizard mode
    /// </summary>
    private async Task SwitchToWizardAsync()
    {
        try
        {
            StatusMessage = "Switching to Wizard Mode...";
            await _audioFeedbackService.PlayNotificationAsync();
            await _accessibilityService.AnnounceAsync("Switching to Wizard Mode - guided workflow");
            await _navigationService.SwitchWorkflowModeAsync("Wizard");
            _logger.LogInformation("Switched to Wizard Mode from Advanced Mode");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to Wizard Mode");
            await _audioFeedbackService.PlayErrorAsync();
        }
    }

    /// <summary>
    /// Return to dashboard
    /// </summary>
    private async Task ReturnToDashboardAsync()
    {
        try
        {
            StatusMessage = "Returning to Dashboard...";
            await _audioFeedbackService.PlayNotificationAsync();
            await _accessibilityService.AnnounceAsync("Returning to Dashboard");
            await _navigationService.SwitchWorkflowModeAsync("Dashboard");
            _logger.LogInformation("Returned to Dashboard from Advanced Mode");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to return to Dashboard");
            await _audioFeedbackService.PlayErrorAsync();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Initialize data and default settings
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            CurrentWorkspace = "Default";
            
            // Initialize default configurations
            InitializeDefaultConfigurations();
            
            // Initialize export templates
            InitializeExportTemplates();
            
            // Initialize workspace profiles
            InitializeWorkspaceProfiles();

            AddLogEntry("Advanced Mode initialized successfully", LogLevel.Information);
            
            _logger.LogInformation("Advanced Mode initialized with default settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Advanced Mode");
            AddLogEntry($"Initialization error: {ex.Message}", LogLevel.Error);
        }
    }

    /// <summary>
    /// Initialize default configurations
    /// </summary>
    private void InitializeDefaultConfigurations()
    {
        var defaultConfigs = new[]
        {
            new AdvancedConfiguration
            {
                Name = "Standard Processing",
                Description = "Standard configuration for document processing",
                ExtractIOCs = true,
                ExtractTTPs = true,
                EnableOCR = true,
                MaxFileSize = 100,
                ProcessingTimeout = 300
            },
            new AdvancedConfiguration
            {
                Name = "Fast Processing",
                Description = "Optimized for speed with reduced feature set",
                ExtractIOCs = true,
                ExtractTTPs = false,
                EnableOCR = false,
                MaxFileSize = 50,
                ProcessingTimeout = 120
            },
            new AdvancedConfiguration
            {
                Name = "Comprehensive Analysis",
                Description = "Full feature analysis with extended timeouts",
                ExtractIOCs = true,
                ExtractTTPs = true,
                EnableOCR = true,
                EnableDeepAnalysis = true,
                MaxFileSize = 200,
                ProcessingTimeout = 600
            }
        };

        foreach (var config in defaultConfigs)
        {
            Configurations.Add(config);
        }
    }

    /// <summary>
    /// Initialize export templates
    /// </summary>
    private void InitializeExportTemplates()
    {
        var templates = new[]
        {
            new ExportTemplate
            {
                Name = "Executive Summary",
                Description = "High-level summary for executive reporting",
                Format = ExportFormat.PDF,
                IncludeDetails = false,
                IncludeCharts = true
            },
            new ExportTemplate
            {
                Name = "Technical Report",
                Description = "Detailed technical analysis report",
                Format = ExportFormat.PDF,
                IncludeDetails = true,
                IncludeCharts = true,
                IncludeRawData = true
            },
            new ExportTemplate
            {
                Name = "CSV Data Export",
                Description = "Raw data export for further analysis",
                Format = ExportFormat.CSV,
                IncludeDetails = false,
                IncludeCharts = false,
                IncludeRawData = true
            }
        };

        foreach (var template in templates)
        {
            ExportTemplates.Add(template);
        }
    }

    /// <summary>
    /// Initialize workspace profiles
    /// </summary>
    private void InitializeWorkspaceProfiles()
    {
        var profiles = new[]
        {
            new WorkspaceProfile
            {
                Name = "Threat Analysis",
                Description = "Optimized for threat intelligence analysis",
                DefaultConfiguration = "Comprehensive Analysis",
                AutoProcessNewFiles = false,
                ShowDetailedLogs = true
            },
            new WorkspaceProfile
            {
                Name = "Bulk Import",
                Description = "High-volume document processing",
                DefaultConfiguration = "Fast Processing",
                AutoProcessNewFiles = true,
                ShowDetailedLogs = false
            },
            new WorkspaceProfile
            {
                Name = "Research Mode",
                Description = "Interactive analysis and investigation",
                DefaultConfiguration = "Standard Processing",
                AutoProcessNewFiles = false,
                ShowDetailedLogs = true,
                EnableExpertMode = true
            }
        };

        foreach (var profile in profiles)
        {
            WorkspaceProfiles.Add(profile);
        }
    }

    /// <summary>
    /// Add entry to detailed logs
    /// </summary>
    private void AddLogEntry(string message, LogLevel level)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = message
        };

        DetailedLogs.Add(entry);

        // Keep only last 1000 entries
        if (DetailedLogs.Count > 1000)
        {
            DetailedLogs.RemoveAt(0);
        }
    }

    /// <summary>
    /// Handle command exceptions
    /// </summary>
    private IObservable<Unit> HandleCommandException(Exception ex)
    {
        _logger.LogError(ex, "Command execution failed");
        AddLogEntry($"Command error: {ex.Message}", LogLevel.Error);
        HasErrors = true;
        ErrorMessage = ex.Message;
        return Observable.Return(Unit.Default);
    }

    #endregion
}

#region Supporting Models

/// <summary>
/// Advanced file item with enhanced metadata
/// </summary>
public class AdvancedFileItem
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public double FileSize { get; set; } // In MB
    public string FileType { get; set; } = "";
    public FileStatus Status { get; set; } = FileStatus.Ready;
    public ProcessingPriority Priority { get; set; } = ProcessingPriority.Normal;
    public double ProcessingProgress { get; set; } = 0;
    public TimeSpan ProcessingTime { get; set; } = TimeSpan.Zero;
    public int ExtractedIOCs { get; set; } = 0;
    public int ExtractedTTPs { get; set; } = 0;
    public string ErrorMessage { get; set; } = "";
    public bool IsSelected { get; set; } = false;
    public ObservableCollection<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// File processing status
/// </summary>
public enum FileStatus
{
    Ready,
    Processing,
    Completed,
    Failed,
    Paused
}

/// <summary>
/// Processing priority levels
/// </summary>
public enum ProcessingPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Processing task for queue management
/// </summary>
public class ProcessingTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<AdvancedFileItem> Files { get; set; } = new();
    public ProcessingPriority Priority { get; set; } = ProcessingPriority.Normal;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
}

/// <summary>
/// Batch operation for grouping files
/// </summary>
public class BatchOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<AdvancedFileItem> Files { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public BatchStatus Status { get; set; } = BatchStatus.Created;
}

/// <summary>
/// Batch operation status
/// </summary>
public enum BatchStatus
{
    Created,
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Log entry for detailed logging
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public LogLevel Level { get; set; } = LogLevel.Information;
    public string Message { get; set; } = "";
    public string Source { get; set; } = "AdvancedMode";
}

/// <summary>
/// Advanced configuration profile
/// </summary>
public class AdvancedConfiguration
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool ExtractIOCs { get; set; } = true;
    public bool ExtractTTPs { get; set; } = true;
    public bool EnableOCR { get; set; } = true;
    public bool EnableDeepAnalysis { get; set; } = false;
    public int MaxFileSize { get; set; } = 100; // MB
    public int ProcessingTimeout { get; set; } = 300; // seconds
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

/// <summary>
/// Export template for reports
/// </summary>
public class ExportTemplate
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ExportFormat Format { get; set; } = ExportFormat.PDF;
    public bool IncludeDetails { get; set; } = true;
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeRawData { get; set; } = false;
}

/// <summary>
/// Export format options
/// </summary>
public enum ExportFormat
{
    PDF,
    CSV,
    JSON,
    XML,
    Excel
}

/// <summary>
/// Workspace profile for different scenarios
/// </summary>
public class WorkspaceProfile
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string DefaultConfiguration { get; set; } = "";
    public bool AutoProcessNewFiles { get; set; } = false;
    public bool ShowDetailedLogs { get; set; } = true;
    public bool EnableExpertMode { get; set; } = false;
}

/// <summary>
/// Workspace state for persistence
/// </summary>
public class WorkspaceState
{
    public string Name { get; set; } = "";
    public List<AdvancedFileItem> Files { get; set; } = new();
    public List<AdvancedConfiguration> Configurations { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsExpertMode { get; set; } = false;
}

/// <summary>
/// Export data structure
/// </summary>
public class ExportData
{
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int FailedFiles { get; set; }
    public DateTime ExportedAt { get; set; }
    public List<AdvancedFileItem> Files { get; set; } = new();
}

#endregion 
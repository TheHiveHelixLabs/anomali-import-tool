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

namespace AnomaliImportTool.UI.ViewModels;

/// <summary>
/// Professional Wizard Mode ViewModel for Living Workspace
/// Provides 6-step guided workflow with validation, progress tracking, and state persistence
/// </summary>
public class WizardModeViewModel : BaseViewModel
{
    private readonly ILogger<WizardModeViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly IWorkspacePersistenceService _workspacePersistenceService;
    private readonly IAudioFeedbackService _audioFeedbackService;
    private readonly IAnimationService _animationService;
    private readonly IAccessibilityService _accessibilityService;

    // Private backing fields
    private int _currentStep = 0;
    private int _totalSteps = 7;
    private double _overallProgress = 0.0;
    private string _currentStepTitle = "";
    private string _currentStepDescription = "";
    private bool _canGoNext = false;
    private bool _canGoPrevious = false;
    private bool _isProcessing = false;
    private string _wizardTitle = "Document Processing Wizard";
    private WizardStep _currentStepData = new();
    
    // Step-specific data
    private ObservableCollection<string> _selectedFiles = new();
    private ObservableCollection<DocumentPreview> _documentPreviews = new();
    private ProcessingConfiguration _processingConfig = new();
    private ObservableCollection<ProcessingResult> _processingResults = new();
    private UploadConfiguration _uploadConfig = new();
    private WizardSummary _wizardSummary = new();
    private ImportTemplate? _selectedTemplate;

    /// <summary>
    /// Initialize Wizard Mode ViewModel
    /// </summary>
    public WizardModeViewModel(
        ILogger<WizardModeViewModel> logger,
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

        Title = "Wizard Mode - Anomali Import Tool";

        // Setup reactive commands
        SetupCommands();

        // Setup reactive properties
        SetupReactiveProperties();

        // Initialize wizard
        _ = Task.Run(InitializeWizardAsync);

        _logger.LogInformation("WizardModeViewModel initialized with 6-step workflow");
    }

    #region Reactive Properties

    /// <summary>
    /// Current step number (0-based)
    /// </summary>
    public int CurrentStep
    {
        get => _currentStep;
        set => this.RaiseAndSetIfChanged(ref _currentStep, value);
    }

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int TotalSteps
    {
        get => _totalSteps;
        set => this.RaiseAndSetIfChanged(ref _totalSteps, value);
    }

    /// <summary>
    /// Overall wizard progress (0-100)
    /// </summary>
    public double OverallProgress
    {
        get => _overallProgress;
        set => this.RaiseAndSetIfChanged(ref _overallProgress, value);
    }

    /// <summary>
    /// Current step title
    /// </summary>
    public string CurrentStepTitle
    {
        get => _currentStepTitle;
        set => this.RaiseAndSetIfChanged(ref _currentStepTitle, value);
    }

    /// <summary>
    /// Current step description
    /// </summary>
    public string CurrentStepDescription
    {
        get => _currentStepDescription;
        set => this.RaiseAndSetIfChanged(ref _currentStepDescription, value);
    }

    /// <summary>
    /// Whether Next button is enabled
    /// </summary>
    public bool CanGoNext
    {
        get => _canGoNext;
        set => this.RaiseAndSetIfChanged(ref _canGoNext, value);
    }

    /// <summary>
    /// Whether Previous button is enabled
    /// </summary>
    public bool CanGoPrevious
    {
        get => _canGoPrevious;
        set => this.RaiseAndSetIfChanged(ref _canGoPrevious, value);
    }

    /// <summary>
    /// Whether wizard is currently processing
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
    }

    /// <summary>
    /// Wizard title
    /// </summary>
    public string WizardTitle
    {
        get => _wizardTitle;
        set => this.RaiseAndSetIfChanged(ref _wizardTitle, value);
    }

    /// <summary>
    /// Current step data
    /// </summary>
    public WizardStep CurrentStepData
    {
        get => _currentStepData;
        set => this.RaiseAndSetIfChanged(ref _currentStepData, value);
    }

    /// <summary>
    /// Selected files collection
    /// </summary>
    public ObservableCollection<string> SelectedFiles
    {
        get => _selectedFiles;
        set => this.RaiseAndSetIfChanged(ref _selectedFiles, value);
    }

    /// <summary>
    /// Document previews collection
    /// </summary>
    public ObservableCollection<DocumentPreview> DocumentPreviews
    {
        get => _documentPreviews;
        set => this.RaiseAndSetIfChanged(ref _documentPreviews, value);
    }

    /// <summary>
    /// Processing configuration
    /// </summary>
    public ProcessingConfiguration ProcessingConfig
    {
        get => _processingConfig;
        set => this.RaiseAndSetIfChanged(ref _processingConfig, value);
    }

    /// <summary>
    /// Processing results collection
    /// </summary>
    public ObservableCollection<ProcessingResult> ProcessingResults
    {
        get => _processingResults;
        set => this.RaiseAndSetIfChanged(ref _processingResults, value);
    }

    /// <summary>
    /// Upload configuration
    /// </summary>
    public UploadConfiguration UploadConfig
    {
        get => _uploadConfig;
        set => this.RaiseAndSetIfChanged(ref _uploadConfig, value);
    }

    /// <summary>
    /// Wizard summary
    /// </summary>
    public WizardSummary WizardSummary
    {
        get => _wizardSummary;
        set => this.RaiseAndSetIfChanged(ref _wizardSummary, value);
    }

    /// <summary>
    /// Selected template
    /// </summary>
    public ImportTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set => this.RaiseAndSetIfChanged(ref _selectedTemplate, value);
    }

    #endregion

    #region Reactive Commands

    /// <summary>
    /// Command to go to next step
    /// </summary>
    public ReactiveCommand<Unit, Unit> NextStepCommand { get; private set; } = null!;

    /// <summary>
    /// Command to go to previous step
    /// </summary>
    public ReactiveCommand<Unit, Unit> PreviousStepCommand { get; private set; } = null!;

    /// <summary>
    /// Command to select files
    /// </summary>
    public ReactiveCommand<Unit, Unit> SelectFilesCommand { get; private set; } = null!;

    /// <summary>
    /// Command to remove selected file
    /// </summary>
    public ReactiveCommand<string, Unit> RemoveFileCommand { get; private set; } = null!;

    /// <summary>
    /// Command to start processing
    /// </summary>
    public ReactiveCommand<Unit, Unit> StartProcessingCommand { get; private set; } = null!;

    /// <summary>
    /// Command to cancel wizard
    /// </summary>
    public ReactiveCommand<Unit, Unit> CancelWizardCommand { get; private set; } = null!;

    /// <summary>
    /// Command to finish wizard
    /// </summary>
    public ReactiveCommand<Unit, Unit> FinishWizardCommand { get; private set; } = null!;

    /// <summary>
    /// Command to jump to specific step
    /// </summary>
    public ReactiveCommand<int, Unit> JumpToStepCommand { get; private set; } = null!;

    #endregion

    #region Private Methods

    /// <summary>
    /// Setup reactive commands with validation
    /// </summary>
    private void SetupCommands()
    {
        // Next Step command - only enabled when current step is valid
        NextStepCommand = ReactiveCommand.CreateFromTask(
            NextStepAsync, 
            this.WhenAnyValue(x => x.CanGoNext, x => x.IsProcessing, (canGo, processing) => canGo && !processing));

        // Previous Step command - only enabled when not on first step
        PreviousStepCommand = ReactiveCommand.CreateFromTask(
            PreviousStepAsync,
            this.WhenAnyValue(x => x.CanGoPrevious, x => x.IsProcessing, (canGo, processing) => canGo && !processing));

        // Select Files command
        SelectFilesCommand = ReactiveCommand.CreateFromTask(SelectFilesAsync);

        // Remove File command
        RemoveFileCommand = ReactiveCommand.CreateFromTask<string>(RemoveFileAsync);

        // Start Processing command
        StartProcessingCommand = ReactiveCommand.CreateFromTask(
            StartProcessingAsync,
            this.WhenAnyValue(x => x.SelectedFiles.Count, count => count > 0));

        // Cancel Wizard command
        CancelWizardCommand = ReactiveCommand.CreateFromTask(CancelWizardAsync);

        // Finish Wizard command
        FinishWizardCommand = ReactiveCommand.CreateFromTask(
            FinishWizardAsync,
            this.WhenAnyValue(x => x.CurrentStep, step => step == TotalSteps - 1));

        // Jump to Step command
        JumpToStepCommand = ReactiveCommand.CreateFromTask<int>(JumpToStepAsync);

        _logger.LogDebug("Wizard reactive commands configured");
    }

    /// <summary>
    /// Setup reactive properties and validation
    /// </summary>
    private void SetupReactiveProperties()
    {
        // Update navigation buttons based on current step
        this.WhenAnyValue(x => x.CurrentStep)
            .Subscribe(step =>
            {
                CanGoPrevious = step > 0;
                UpdateOverallProgress();
                UpdateCurrentStepInfo();
                ValidateCurrentStep();
            });

        // Monitor selected files for validation
        this.WhenAnyValue(x => x.SelectedFiles.Count)
            .Subscribe(_ => ValidateCurrentStep());

        // Monitor processing configuration changes
        this.WhenAnyValue(x => x.ProcessingConfig)
            .Subscribe(_ => ValidateCurrentStep());

        _logger.LogDebug("Wizard reactive properties configured");
    }

    /// <summary>
    /// Initialize wizard with step data
    /// </summary>
    private async Task InitializeWizardAsync()
    {
        try
        {
            // Load previous wizard state if available
            await LoadWizardStateAsync();

            // Update current step info
            UpdateCurrentStepInfo();

            // Validate initial step
            ValidateCurrentStep();

            // Announce wizard start to screen readers
            await _accessibilityService.AnnounceAsync($"Document Processing Wizard started. Step 1 of {TotalSteps}: {CurrentStepTitle}");

            _logger.LogInformation("Wizard initialized successfully at step {CurrentStep}", CurrentStep + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize wizard");
            HasErrors = true;
            ErrorMessage = "Failed to initialize wizard. Please try again.";
        }
    }

    /// <summary>
    /// Update current step information
    /// </summary>
    private void UpdateCurrentStepInfo()
    {
        var stepInfo = GetStepInfo(CurrentStep);
        CurrentStepTitle = stepInfo.Title;
        CurrentStepDescription = stepInfo.Description;
        CurrentStepData = stepInfo;

        _logger.LogDebug("Updated step info for step {Step}: {Title}", CurrentStep + 1, CurrentStepTitle);
    }

    /// <summary>
    /// Get step information for given step number
    /// </summary>
    private WizardStep GetStepInfo(int stepNumber)
    {
        return stepNumber switch
        {
            0 => new WizardStep
            {
                StepNumber = 1,
                Title = "File Selection",
                Description = "Select the threat intelligence documents you want to process",
                Instructions = "Click 'Select Files' to browse for PDF, DOC, or TXT files containing threat intelligence data.",
                IsCompleted = SelectedFiles.Count > 0,
                ValidationMessage = SelectedFiles.Count == 0 ? "Please select at least one file to continue" : ""
            },
            1 => new WizardStep
            {
                StepNumber = 2,
                Title = "Document Preview",
                Description = "Review the selected documents and their content",
                Instructions = "Verify the documents are correct and check the preview information before proceeding.",
                IsCompleted = DocumentPreviews.Count > 0,
                ValidationMessage = DocumentPreviews.Count == 0 ? "Document previews are being generated..." : ""
            },
            2 => new WizardStep
            {
                StepNumber = 3,
                Title = "Template Selection",
                Description = "Select or confirm the template to guide extraction",
                Instructions = "The system suggests a template automatically. You can override the selection.",
                IsCompleted = SelectedTemplate != null,
                ValidationMessage = SelectedTemplate == null ? "Please select a template to continue" : ""
            },
            3 => new WizardStep
            {
                StepNumber = 4,
                Title = "Configuration",
                Description = "Configure processing options and settings",
                Instructions = "Set your processing preferences, extraction settings, and output format options.",
                IsCompleted = ProcessingConfig.IsValid,
                ValidationMessage = !ProcessingConfig.IsValid ? "Please configure all required processing options" : ""
            },
            4 => new WizardStep
            {
                StepNumber = 5,
                Title = "Processing",
                Description = "Process the documents and extract threat intelligence",
                Instructions = "Click 'Start Processing' to begin extracting threat intelligence from your documents.",
                IsCompleted = ProcessingResults.Any(r => r.Status == ProcessingStatus.Completed),
                ValidationMessage = ""
            },
            5 => new WizardStep
            {
                StepNumber = 6,
                Title = "Review Results",
                Description = "Review the processing results and extracted data",
                Instructions = "Verify the extracted threat intelligence data and make any necessary adjustments.",
                IsCompleted = ProcessingResults.All(r => r.IsReviewed),
                ValidationMessage = ProcessingResults.Any(r => !r.IsReviewed) ? "Please review all processing results" : ""
            },
            6 => new WizardStep
            {
                StepNumber = 7,
                Title = "Upload to Anomali",
                Description = "Upload the processed data to Anomali ThreatStream",
                Instructions = "Configure upload settings and transfer your threat intelligence to Anomali ThreatStream.",
                IsCompleted = WizardSummary.IsCompleted,
                ValidationMessage = ""
            },
            _ => new WizardStep { Title = "Unknown Step", Description = "Invalid step" }
        };
    }

    /// <summary>
    /// Validate current step and update navigation
    /// </summary>
    private void ValidateCurrentStep()
    {
        var stepInfo = GetStepInfo(CurrentStep);
        
        CanGoNext = CurrentStep switch
        {
            0 => SelectedFiles.Count > 0, // File Selection
            1 => DocumentPreviews.Count > 0, // Document Preview
            2 => SelectedTemplate != null, // Template Selection
            3 => ProcessingConfig.IsValid, // Configuration
            4 => ProcessingResults.Any(r => r.Status == ProcessingStatus.Completed), // Processing
            5 => ProcessingResults.All(r => r.IsReviewed), // Review Results
            6 => true, // Upload
            _ => false
        };

        // Update current step data
        CurrentStepData = stepInfo;

        _logger.LogDebug("Step {Step} validation: CanGoNext={CanGoNext}", CurrentStep + 1, CanGoNext);
    }

    /// <summary>
    /// Update overall wizard progress
    /// </summary>
    private void UpdateOverallProgress()
    {
        OverallProgress = ((double)(CurrentStep + 1) / TotalSteps) * 100.0;
        _logger.LogDebug("Overall progress updated: {Progress}%", OverallProgress);
    }

    /// <summary>
    /// Load previous wizard state
    /// </summary>
    private async Task LoadWizardStateAsync()
    {
        try
        {
            // Load from workspace persistence service
            // This would restore previous wizard session if available
            _logger.LogDebug("Loading wizard state...");
            
            // For now, just set defaults
            ProcessingConfig = new ProcessingConfiguration
            {
                ExtractIOCs = true,
                ExtractTTPs = true,
                GenerateMetadata = true,
                OutputFormat = "JSON"
            };

            UploadConfig = new UploadConfiguration
            {
                UploadToAnomali = true,
                GenerateReport = true
            };

            await Task.Delay(100); // Simulate loading
            _logger.LogDebug("Wizard state loaded");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load wizard state");
        }
    }

    /// <summary>
    /// Save current wizard state
    /// </summary>
    private async Task SaveWizardStateAsync()
    {
        try
        {
            var wizardState = new WizardState
            {
                CurrentStep = CurrentStep,
                SelectedFiles = SelectedFiles.ToList(),
                ProcessingConfig = ProcessingConfig,
                UploadConfig = UploadConfig,
                LastSaved = DateTime.UtcNow
            };

            // Save to workspace persistence service
            await Task.Delay(50); // Simulate saving
            _logger.LogDebug("Wizard state saved at step {Step}", CurrentStep + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save wizard state");
        }
    }

    #endregion

    #region Command Implementations

    /// <summary>
    /// Go to next step with validation and animation
    /// </summary>
    private async Task NextStepAsync()
    {
        try
        {
            if (CurrentStep >= TotalSteps - 1) return;

            _logger.LogInformation("Advancing to step {NextStep}", CurrentStep + 2);

            // Save current state
            await SaveWizardStateAsync();

            // Play navigation sound
            await _audioFeedbackService.PlayNotificationAsync();

            // Animate transition
            if (_animationService.AnimationsEnabled)
            {
                await _animationService.StartModeTransitionAsync("NextStep");
            }

            // Advance step
            CurrentStep++;

            // Perform step-specific actions
            await PerformStepEntryActionsAsync();

            // Announce to screen readers
            await _accessibilityService.AnnounceAsync($"Step {CurrentStep + 1} of {TotalSteps}: {CurrentStepTitle}");

            // Navigate in navigation service
            await _navigationService.NextWorkflowStepAsync();

            _logger.LogInformation("Successfully advanced to step {CurrentStep}", CurrentStep + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to advance to next step");
            await _audioFeedbackService.PlayErrorAsync();
            HasErrors = true;
            ErrorMessage = "Failed to advance to next step. Please try again.";
        }
    }

    /// <summary>
    /// Go to previous step with animation
    /// </summary>
    private async Task PreviousStepAsync()
    {
        try
        {
            if (CurrentStep <= 0) return;

            _logger.LogInformation("Going back to step {PreviousStep}", CurrentStep);

            // Play navigation sound
            await _audioFeedbackService.PlayNotificationAsync();

            // Animate transition
            if (_animationService.AnimationsEnabled)
            {
                await _animationService.StartModeTransitionAsync("PreviousStep");
            }

            // Go back
            CurrentStep--;

            // Announce to screen readers
            await _accessibilityService.AnnounceAsync($"Returned to step {CurrentStep + 1} of {TotalSteps}: {CurrentStepTitle}");

            // Navigate in navigation service
            await _navigationService.PreviousWorkflowStepAsync();

            _logger.LogInformation("Successfully returned to step {CurrentStep}", CurrentStep + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to go to previous step");
            await _audioFeedbackService.PlayErrorAsync();
        }
    }

    /// <summary>
    /// Select files for processing
    /// </summary>
    private async Task SelectFilesAsync()
    {
        try
        {
            _logger.LogInformation("Opening file selection dialog");

            // Simulate file selection dialog
            var selectedFiles = new[]
            {
                "C:\\Documents\\ThreatReport1.pdf",
                "C:\\Documents\\IOC_Analysis.docx",
                "C:\\Documents\\ThreatIntel.txt"
            };

            SelectedFiles.Clear();
            foreach (var file in selectedFiles)
            {
                SelectedFiles.Add(file);
            }

            // Generate document previews
            await GenerateDocumentPreviewsAsync();

            await _audioFeedbackService.PlaySuccessAsync();
            StatusMessage = $"Selected {SelectedFiles.Count} files for processing";

            _logger.LogInformation("Selected {FileCount} files", SelectedFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select files");
            await _audioFeedbackService.PlayErrorAsync();
        }
    }

    /// <summary>
    /// Remove file from selection
    /// </summary>
    private async Task RemoveFileAsync(string filePath)
    {
        try
        {
            SelectedFiles.Remove(filePath);
            
            // Remove corresponding preview
            var preview = DocumentPreviews.FirstOrDefault(p => p.FilePath == filePath);
            if (preview != null)
            {
                DocumentPreviews.Remove(preview);
            }

            _logger.LogInformation("Removed file: {FilePath}", filePath);
            await Task.Delay(1); // Await compliance
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove file: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Start processing documents
    /// </summary>
    private async Task StartProcessingAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "Processing documents...";

            _logger.LogInformation("Starting document processing for {FileCount} files", SelectedFiles.Count);

            ProcessingResults.Clear();

            // Process each file
            for (int i = 0; i < SelectedFiles.Count; i++)
            {
                var file = SelectedFiles[i];
                var result = new ProcessingResult
                {
                    FileName = Path.GetFileName(file),
                    FilePath = file,
                    Status = ProcessingStatus.Processing,
                    Progress = 0.0,
                    StartTime = DateTime.Now
                };

                ProcessingResults.Add(result);

                // Simulate processing
                for (int progress = 0; progress <= 100; progress += 20)
                {
                    result.Progress = progress;
                    await Task.Delay(200);
                }

                result.Status = ProcessingStatus.Completed;
                result.EndTime = DateTime.Now;
                result.ExtractedIOCs = 15 + i * 3; // Simulated
                result.ExtractedTTPs = 8 + i * 2; // Simulated
            }

            await _audioFeedbackService.PlaySuccessAsync();
            StatusMessage = "Processing completed successfully";

            _logger.LogInformation("Document processing completed for all files");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process documents");
            await _audioFeedbackService.PlayErrorAsync();
            StatusMessage = "Processing failed";
            HasErrors = true;
            ErrorMessage = "Document processing failed. Please check your files and try again.";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Cancel wizard and return to dashboard
    /// </summary>
    private async Task CancelWizardAsync()
    {
        try
        {
            _logger.LogInformation("Canceling wizard");

            var confirmed = true; // In real app, show confirmation dialog

            if (confirmed)
            {
                await _navigationService.SwitchWorkflowModeAsync("Dashboard");
                _logger.LogInformation("Wizard canceled, returned to Dashboard");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel wizard");
        }
    }

    /// <summary>
    /// Finish wizard and complete workflow
    /// </summary>
    private async Task FinishWizardAsync()
    {
        try
        {
            _logger.LogInformation("Finishing wizard");

            // Generate wizard summary
            WizardSummary = new WizardSummary
            {
                TotalFilesProcessed = SelectedFiles.Count,
                TotalIOCsExtracted = ProcessingResults.Sum(r => r.ExtractedIOCs),
                TotalTTPsExtracted = ProcessingResults.Sum(r => r.ExtractedTTPs),
                ProcessingDuration = TimeSpan.FromMinutes(5), // Simulated
                IsCompleted = true,
                CompletedAt = DateTime.Now
            };

            // Save final state
            await SaveWizardStateAsync();

            await _audioFeedbackService.PlaySuccessAsync();
            await _accessibilityService.AnnounceAsync("Wizard completed successfully. Threat intelligence processing finished.");

            StatusMessage = "Wizard completed successfully!";

            _logger.LogInformation("Wizard completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to finish wizard");
            await _audioFeedbackService.PlayErrorAsync();
        }
    }

    /// <summary>
    /// Jump to specific step
    /// </summary>
    private async Task JumpToStepAsync(int targetStep)
    {
        try
        {
            if (targetStep < 0 || targetStep >= TotalSteps) return;
            if (targetStep == CurrentStep) return;

            _logger.LogInformation("Jumping to step {TargetStep}", targetStep + 1);

            CurrentStep = targetStep;

            await _audioFeedbackService.PlayNotificationAsync();
            await _accessibilityService.AnnounceAsync($"Jumped to step {CurrentStep + 1}: {CurrentStepTitle}");

            _logger.LogInformation("Successfully jumped to step {CurrentStep}", CurrentStep + 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to jump to step {TargetStep}", targetStep + 1);
        }
    }

    /// <summary>
    /// Perform actions when entering a step
    /// </summary>
    private async Task PerformStepEntryActionsAsync()
    {
        switch (CurrentStep)
        {
            case 1: // Document Preview
                await GenerateDocumentPreviewsAsync();
                break;
            case 2: // Template Selection
                // Ensure template is selected
                break;
            case 3: // Configuration
                // Ensure processing config is ready
                break;
            case 4: // Processing
                // Ready for processing
                break;
            case 5: // Review Results
                // Mark results as ready for review
                foreach (var result in ProcessingResults)
                {
                    result.IsReviewed = false; // Reset review status
                }
                break;
            case 6: // Upload
                // Prepare upload configuration
                break;
        }

        await Task.Delay(1); // Await compliance
    }

    /// <summary>
    /// Generate document previews
    /// </summary>
    private async Task GenerateDocumentPreviewsAsync()
    {
        DocumentPreviews.Clear();

        foreach (var filePath in SelectedFiles)
        {
            var preview = new DocumentPreview
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileSize = "1.2 MB", // Simulated
                FileType = Path.GetExtension(filePath).ToUpper(),
                PageCount = 15, // Simulated
                LastModified = DateTime.Now.AddDays(-5),
                ThumbnailPath = "",
                IsValid = true
            };

            DocumentPreviews.Add(preview);
        }

        _logger.LogDebug("Generated {PreviewCount} document previews", DocumentPreviews.Count);
        await Task.Delay(1); // Await compliance
    }

    #endregion
}

#region Supporting Models

/// <summary>
/// Wizard step information
/// </summary>
public class WizardStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Instructions { get; set; } = "";
    public bool IsCompleted { get; set; }
    public string ValidationMessage { get; set; } = "";
}

/// <summary>
/// Document preview model
/// </summary>
public class DocumentPreview
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileSize { get; set; } = "";
    public string FileType { get; set; } = "";
    public int PageCount { get; set; }
    public DateTime LastModified { get; set; }
    public string ThumbnailPath { get; set; } = "";
    public bool IsValid { get; set; } = true;
}

/// <summary>
/// Processing configuration
/// </summary>
public class ProcessingConfiguration
{
    public bool ExtractIOCs { get; set; } = true;
    public bool ExtractTTPs { get; set; } = true;
    public bool GenerateMetadata { get; set; } = true;
    public string OutputFormat { get; set; } = "JSON";
    public bool EnableOCR { get; set; } = false;
    public string Language { get; set; } = "English";
    
    public bool IsValid => !string.IsNullOrEmpty(OutputFormat);
}

/// <summary>
/// Processing result model
/// </summary>
public class ProcessingResult
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public ProcessingStatus Status { get; set; }
    public double Progress { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int ExtractedIOCs { get; set; }
    public int ExtractedTTPs { get; set; }
    public bool IsReviewed { get; set; }
    public string ErrorMessage { get; set; } = "";
}

/// <summary>
/// Processing status enumeration
/// </summary>
public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

/// <summary>
/// Upload configuration
/// </summary>
public class UploadConfiguration
{
    public bool UploadToAnomali { get; set; } = true;
    public bool GenerateReport { get; set; } = true;
    public string ReportFormat { get; set; } = "PDF";
    public bool NotifyOnCompletion { get; set; } = true;
}

/// <summary>
/// Wizard summary
/// </summary>
public class WizardSummary
{
    public int TotalFilesProcessed { get; set; }
    public int TotalIOCsExtracted { get; set; }
    public int TotalTTPsExtracted { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CompletedAt { get; set; }
}

/// <summary>
/// Wizard state for persistence
/// </summary>
public class WizardState
{
    public int CurrentStep { get; set; }
    public List<string> SelectedFiles { get; set; } = new();
    public ProcessingConfiguration ProcessingConfig { get; set; } = new();
    public UploadConfiguration UploadConfig { get; set; } = new();
    public DateTime LastSaved { get; set; }
}

#endregion 
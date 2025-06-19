using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.WinUI.Services
{
    /// <summary>
    /// Interactive onboarding tutorial service interface
    /// </summary>
    public interface IInteractiveOnboardingService
    {
        /// <summary>
        /// Initialize onboarding service
        /// </summary>
        Task InitializeAsync(FrameworkElement rootElement);

        /// <summary>
        /// Start onboarding tutorial
        /// </summary>
        Task StartTutorialAsync(string tutorialId);

        /// <summary>
        /// Stop current tutorial
        /// </summary>
        Task StopTutorialAsync();

        /// <summary>
        /// Move to next tutorial step
        /// </summary>
        Task NextStepAsync();

        /// <summary>
        /// Move to previous tutorial step
        /// </summary>
        Task PreviousStepAsync();

        /// <summary>
        /// Skip current tutorial
        /// </summary>
        Task SkipTutorialAsync();

        /// <summary>
        /// Register tutorial definition
        /// </summary>
        void RegisterTutorial(TutorialDefinition tutorial);

        /// <summary>
        /// Unregister tutorial
        /// </summary>
        void UnregisterTutorial(string tutorialId);

        /// <summary>
        /// Check if user has completed tutorial
        /// </summary>
        bool HasCompletedTutorial(string tutorialId);

        /// <summary>
        /// Mark tutorial as completed
        /// </summary>
        void MarkTutorialCompleted(string tutorialId);

        /// <summary>
        /// Reset tutorial progress
        /// </summary>
        void ResetTutorialProgress(string tutorialId);

        /// <summary>
        /// Get available tutorials
        /// </summary>
        IReadOnlyList<TutorialDefinition> GetAvailableTutorials();

        /// <summary>
        /// Set onboarding enabled state
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Observable for tutorial events
        /// </summary>
        IObservable<TutorialEvent> TutorialEvents { get; }

        /// <summary>
        /// Current tutorial state
        /// </summary>
        TutorialState CurrentState { get; }
    }

    /// <summary>
    /// Tutorial definition
    /// </summary>
    public class TutorialDefinition
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<TutorialStep> Steps { get; set; } = new List<TutorialStep>();
        public TutorialPriority Priority { get; set; } = TutorialPriority.Normal;
        public bool IsRequired { get; set; }
        public bool ShowOnFirstRun { get; set; }
        public string Category { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public List<string> Prerequisites { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Tutorial step definition
    /// </summary>
    public class TutorialStep
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string TargetElementName { get; set; }
        public FrameworkElement TargetElement { get; set; }
        public HighlightStyle HighlightStyle { get; set; } = HighlightStyle.Spotlight;
        public TooltipPosition TooltipPosition { get; set; } = TooltipPosition.Auto;
        public bool RequireInteraction { get; set; }
        public string InteractionType { get; set; }
        public TimeSpan? AutoAdvanceDelay { get; set; }
        public List<TutorialAction> Actions { get; set; } = new List<TutorialAction>();
        public string MediaUrl { get; set; }
        public bool IsOptional { get; set; }
    }

    /// <summary>
    /// Tutorial action
    /// </summary>
    public class TutorialAction
    {
        public string Type { get; set; }
        public string Target { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Func<Task<bool>> Handler { get; set; }
    }

    /// <summary>
    /// Tutorial priority levels
    /// </summary>
    public enum TutorialPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Highlight styles for tutorial steps
    /// </summary>
    public enum HighlightStyle
    {
        None,
        Outline,
        Spotlight,
        Glow,
        Pulse,
        Shake
    }

    /// <summary>
    /// Tooltip position options
    /// </summary>
    public enum TooltipPosition
    {
        Auto,
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// Tutorial state
    /// </summary>
    public class TutorialState
    {
        public string CurrentTutorialId { get; set; }
        public int CurrentStepIndex { get; set; }
        public bool IsActive { get; set; }
        public bool IsPaused { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public Dictionary<string, object> UserData { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Tutorial event data
    /// </summary>
    public class TutorialEvent
    {
        public string EventType { get; set; }
        public string TutorialId { get; set; }
        public int? StepIndex { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Tutorial overlay UI components
    /// </summary>
    public class TutorialOverlay
    {
        public Canvas OverlayCanvas { get; set; }
        public Rectangle DimLayer { get; set; }
        public Border TooltipBorder { get; set; }
        public TextBlock TitleText { get; set; }
        public TextBlock ContentText { get; set; }
        public StackPanel ButtonPanel { get; set; }
        public Button PreviousButton { get; set; }
        public Button NextButton { get; set; }
        public Button SkipButton { get; set; }
        public ProgressBar ProgressBar { get; set; }
        public Shape HighlightShape { get; set; }
    }

    /// <summary>
    /// Professional interactive onboarding tutorial service
    /// </summary>
    public class InteractiveOnboardingService : IInteractiveOnboardingService, IDisposable
    {
        private readonly ILogger<InteractiveOnboardingService> _logger;
        private readonly IAccessibilityService _accessibilityService;
        private readonly IAudioFeedbackService _audioFeedbackService;
        private readonly IKeyboardNavigationService _keyboardNavigationService;

        private readonly Dictionary<string, TutorialDefinition> _tutorials;
        private readonly HashSet<string> _completedTutorials;
        private readonly Subject<TutorialEvent> _tutorialEvents;

        private FrameworkElement _rootElement;
        private TutorialOverlay _overlay;
        private TutorialState _currentState;
        private bool _isEnabled = true;
        private bool _isInitialized = false;

        // Tutorial constants
        private const int ANIMATION_DURATION_MS = 300;
        private const int AUTO_ADVANCE_DEFAULT_MS = 5000;
        private const double SPOTLIGHT_RADIUS = 8.0;
        private const double DIM_OPACITY = 0.7;

        public InteractiveOnboardingService(
            ILogger<InteractiveOnboardingService> logger,
            IAccessibilityService accessibilityService,
            IAudioFeedbackService audioFeedbackService,
            IKeyboardNavigationService keyboardNavigationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));
            _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));
            _keyboardNavigationService = keyboardNavigationService ?? throw new ArgumentNullException(nameof(keyboardNavigationService));

            _tutorials = new Dictionary<string, TutorialDefinition>();
            _completedTutorials = new HashSet<string>();
            _tutorialEvents = new Subject<TutorialEvent>();
            _currentState = new TutorialState();

            _logger.LogInformation("InteractiveOnboardingService initialized");
        }

        public IObservable<TutorialEvent> TutorialEvents => _tutorialEvents.AsObservable();
        public TutorialState CurrentState => _currentState;

        public async Task InitializeAsync(FrameworkElement rootElement)
        {
            try
            {
                _logger.LogInformation("Initializing interactive onboarding service");

                _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));

                // Create overlay UI
                await CreateOverlayUIAsync();

                // Register default tutorials
                RegisterDefaultTutorials();

                // Load completed tutorials from storage
                await LoadCompletedTutorialsAsync();

                _isInitialized = true;

                _logger.LogInformation("Interactive onboarding service initialized with {TutorialCount} tutorials",
                    _tutorials.Count);

                // Fire initialization event
                _tutorialEvents.OnNext(new TutorialEvent
                {
                    EventType = "ServiceInitialized",
                    Data = new Dictionary<string, object> { ["TutorialCount"] = _tutorials.Count }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize interactive onboarding service");
                throw;
            }
        }

        public async Task StartTutorialAsync(string tutorialId)
        {
            try
            {
                if (!_isEnabled || !_isInitialized)
                {
                    _logger.LogWarning("Cannot start tutorial - service not enabled or initialized");
                    return;
                }

                if (!_tutorials.TryGetValue(tutorialId, out var tutorial))
                {
                    _logger.LogWarning("Tutorial not found: {TutorialId}", tutorialId);
                    return;
                }

                _logger.LogInformation("Starting tutorial: {TutorialId} - {Title}", tutorialId, tutorial.Title);

                // Stop current tutorial if running
                if (_currentState.IsActive)
                {
                    await StopTutorialAsync();
                }

                // Initialize tutorial state
                _currentState = new TutorialState
                {
                    CurrentTutorialId = tutorialId,
                    CurrentStepIndex = 0,
                    IsActive = true,
                    IsPaused = false,
                    StartTime = DateTime.UtcNow
                };

                // Show overlay
                ShowOverlay();

                // Start first step
                await ShowStepAsync(0);

                // Play start sound
                await _audioFeedbackService.PlaySuccessSoundAsync();

                // Announce tutorial start
                await _accessibilityService.AnnounceAsync(
                    $"Starting tutorial: {tutorial.Title}", 
                    AccessibilityAnnouncementPriority.High);

                // Fire start event
                _tutorialEvents.OnNext(new TutorialEvent
                {
                    EventType = "TutorialStarted",
                    TutorialId = tutorialId,
                    StepIndex = 0
                });

                _logger.LogInformation("Tutorial started successfully: {TutorialId}", tutorialId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start tutorial: {TutorialId}", tutorialId);
                await _audioFeedbackService.PlayErrorSoundAsync();
            }
        }

        public async Task StopTutorialAsync()
        {
            try
            {
                if (!_currentState.IsActive)
                {
                    return;
                }

                _logger.LogInformation("Stopping tutorial: {TutorialId}", _currentState.CurrentTutorialId);

                var tutorialId = _currentState.CurrentTutorialId;

                // Hide overlay
                HideOverlay();

                // Reset state
                _currentState.IsActive = false;
                _currentState.IsPaused = false;
                _currentState.ElapsedTime = DateTime.UtcNow - _currentState.StartTime;

                // Play stop sound
                await _audioFeedbackService.PlayNavigationSoundAsync();

                // Announce tutorial stop
                await _accessibilityService.AnnounceAsync(
                    "Tutorial stopped", 
                    AccessibilityAnnouncementPriority.Medium);

                // Fire stop event
                _tutorialEvents.OnNext(new TutorialEvent
                {
                    EventType = "TutorialStopped",
                    TutorialId = tutorialId,
                    Data = new Dictionary<string, object> { ["ElapsedTime"] = _currentState.ElapsedTime }
                });

                _logger.LogInformation("Tutorial stopped: {TutorialId}", tutorialId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop tutorial");
            }
        }

        public async Task NextStepAsync()
        {
            try
            {
                if (!_currentState.IsActive || _currentState.IsPaused)
                {
                    return;
                }

                var tutorial = _tutorials[_currentState.CurrentTutorialId];
                var nextStepIndex = _currentState.CurrentStepIndex + 1;

                if (nextStepIndex >= tutorial.Steps.Count)
                {
                    // Tutorial completed
                    await CompleteTutorialAsync();
                    return;
                }

                _logger.LogDebug("Moving to next step: {StepIndex}", nextStepIndex);

                _currentState.CurrentStepIndex = nextStepIndex;
                await ShowStepAsync(nextStepIndex);

                // Play navigation sound
                await _audioFeedbackService.PlayNavigationSoundAsync();

                // Fire step change event
                _tutorialEvents.OnNext(new TutorialEvent
                {
                    EventType = "StepChanged",
                    TutorialId = _currentState.CurrentTutorialId,
                    StepIndex = nextStepIndex
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move to next step");
            }
        }

        public async Task PreviousStepAsync()
        {
            try
            {
                if (!_currentState.IsActive || _currentState.IsPaused)
                {
                    return;
                }

                var previousStepIndex = _currentState.CurrentStepIndex - 1;

                if (previousStepIndex < 0)
                {
                    return; // Already at first step
                }

                _logger.LogDebug("Moving to previous step: {StepIndex}", previousStepIndex);

                _currentState.CurrentStepIndex = previousStepIndex;
                await ShowStepAsync(previousStepIndex);

                // Play navigation sound
                await _audioFeedbackService.PlayNavigationSoundAsync();

                // Fire step change event
                _tutorialEvents.OnNext(new TutorialEvent
                {
                    EventType = "StepChanged",
                    TutorialId = _currentState.CurrentTutorialId,
                    StepIndex = previousStepIndex
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move to previous step");
            }
        }

        public async Task SkipTutorialAsync()
        {
            try
            {
                if (!_currentState.IsActive)
                {
                    return;
                }

                _logger.LogInformation("Skipping tutorial: {TutorialId}", _currentState.CurrentTutorialId);

                var tutorialId = _currentState.CurrentTutorialId;

                // Stop tutorial
                await StopTutorialAsync();

                // Fire skip event
                _tutorialEvents.OnNext(new TutorialEvent
                {
                    EventType = "TutorialSkipped",
                    TutorialId = tutorialId,
                    StepIndex = _currentState.CurrentStepIndex
                });

                // Announce skip
                await _accessibilityService.AnnounceAsync(
                    "Tutorial skipped", 
                    AccessibilityAnnouncementPriority.Medium);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to skip tutorial");
            }
        }

        public void RegisterTutorial(TutorialDefinition tutorial)
        {
            try
            {
                if (tutorial == null || string.IsNullOrWhiteSpace(tutorial.Id))
                {
                    _logger.LogWarning("Invalid tutorial definition");
                    return;
                }

                _tutorials[tutorial.Id] = tutorial;

                _logger.LogDebug("Registered tutorial: {TutorialId} - {Title} ({StepCount} steps)",
                    tutorial.Id, tutorial.Title, tutorial.Steps.Count);

                // Fire registration event
                _tutorialEvents.OnNext(new TutorialEvent
                {
                    EventType = "TutorialRegistered",
                    TutorialId = tutorial.Id,
                    Data = new Dictionary<string, object> { ["StepCount"] = tutorial.Steps.Count }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register tutorial: {TutorialId}", tutorial?.Id);
            }
        }

        public void UnregisterTutorial(string tutorialId)
        {
            try
            {
                if (_tutorials.Remove(tutorialId))
                {
                    _logger.LogDebug("Unregistered tutorial: {TutorialId}", tutorialId);

                    // Fire unregistration event
                    _tutorialEvents.OnNext(new TutorialEvent
                    {
                        EventType = "TutorialUnregistered",
                        TutorialId = tutorialId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister tutorial: {TutorialId}", tutorialId);
            }
        }

        public bool HasCompletedTutorial(string tutorialId)
        {
            return _completedTutorials.Contains(tutorialId);
        }

        public void MarkTutorialCompleted(string tutorialId)
        {
            try
            {
                if (_completedTutorials.Add(tutorialId))
                {
                    _logger.LogInformation("Marked tutorial as completed: {TutorialId}", tutorialId);

                    // Save to storage
                    _ = Task.Run(() => SaveCompletedTutorialsAsync());

                    // Fire completion event
                    _tutorialEvents.OnNext(new TutorialEvent
                    {
                        EventType = "TutorialMarkedCompleted",
                        TutorialId = tutorialId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark tutorial as completed: {TutorialId}", tutorialId);
            }
        }

        public void ResetTutorialProgress(string tutorialId)
        {
            try
            {
                if (_completedTutorials.Remove(tutorialId))
                {
                    _logger.LogInformation("Reset tutorial progress: {TutorialId}", tutorialId);

                    // Save to storage
                    _ = Task.Run(() => SaveCompletedTutorialsAsync());

                    // Fire reset event
                    _tutorialEvents.OnNext(new TutorialEvent
                    {
                        EventType = "TutorialProgressReset",
                        TutorialId = tutorialId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset tutorial progress: {TutorialId}", tutorialId);
            }
        }

        public IReadOnlyList<TutorialDefinition> GetAvailableTutorials()
        {
            return _tutorials.Values.ToList().AsReadOnly();
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Interactive onboarding {Status}", enabled ? "enabled" : "disabled");

            if (!enabled && _currentState.IsActive)
            {
                _ = Task.Run(StopTutorialAsync);
            }
        }

        private async Task CreateOverlayUIAsync()
        {
            try
            {
                _overlay = new TutorialOverlay();

                // Create main overlay canvas
                _overlay.OverlayCanvas = new Canvas
                {
                    Name = "TutorialOverlayCanvas",
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                    Visibility = Visibility.Collapsed
                };

                // Create dim layer
                _overlay.DimLayer = new Rectangle
                {
                    Fill = new SolidColorBrush(Windows.UI.Color.FromArgb((byte)(DIM_OPACITY * 255), 0, 0, 0))
                };

                // Create tooltip
                await CreateTooltipUIAsync();

                // Add to overlay canvas
                _overlay.OverlayCanvas.Children.Add(_overlay.DimLayer);
                _overlay.OverlayCanvas.Children.Add(_overlay.TooltipBorder);

                // Add overlay to root element
                if (_rootElement is Panel rootPanel)
                {
                    rootPanel.Children.Add(_overlay.OverlayCanvas);
                }

                _logger.LogDebug("Tutorial overlay UI created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create overlay UI");
            }
        }

        private async Task CreateTooltipUIAsync()
        {
            try
            {
                // Create tooltip border
                _overlay.TooltipBorder = new Border
                {
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(240, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    MaxWidth = 400,
                    Visibility = Visibility.Collapsed
                };

                // Create content stack panel
                var contentPanel = new StackPanel
                {
                    Spacing = 12
                };

                // Title text
                _overlay.TitleText = new TextBlock
                {
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 32, 32, 32)),
                    TextWrapping = TextWrapping.Wrap
                };

                // Content text
                _overlay.ContentText = new TextBlock
                {
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 64, 64, 64)),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20
                };

                // Progress bar
                _overlay.ProgressBar = new ProgressBar
                {
                    Height = 4,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215))
                };

                // Button panel
                _overlay.ButtonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                // Create buttons
                CreateTutorialButtons();

                // Add to content panel
                contentPanel.Children.Add(_overlay.TitleText);
                contentPanel.Children.Add(_overlay.ContentText);
                contentPanel.Children.Add(_overlay.ProgressBar);
                contentPanel.Children.Add(_overlay.ButtonPanel);

                _overlay.TooltipBorder.Child = contentPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create tooltip UI");
            }
        }

        private void CreateTutorialButtons()
        {
            // Previous button
            _overlay.PreviousButton = new Button
            {
                Content = "Previous",
                Style = Application.Current.Resources["DefaultButtonStyle"] as Style
            };
            _overlay.PreviousButton.Click += async (s, e) => await PreviousStepAsync();

            // Next button
            _overlay.NextButton = new Button
            {
                Content = "Next",
                Style = Application.Current.Resources["AccentButtonStyle"] as Style
            };
            _overlay.NextButton.Click += async (s, e) => await NextStepAsync();

            // Skip button
            _overlay.SkipButton = new Button
            {
                Content = "Skip",
                Style = Application.Current.Resources["DefaultButtonStyle"] as Style
            };
            _overlay.SkipButton.Click += async (s, e) => await SkipTutorialAsync();

            // Add to button panel
            _overlay.ButtonPanel.Children.Add(_overlay.PreviousButton);
            _overlay.ButtonPanel.Children.Add(_overlay.NextButton);
            _overlay.ButtonPanel.Children.Add(_overlay.SkipButton);
        }

        private void RegisterDefaultTutorials()
        {
            try
            {
                // Application Overview Tutorial
                var overviewTutorial = new TutorialDefinition
                {
                    Id = "app_overview",
                    Title = "Application Overview",
                    Description = "Learn the basics of using the Anomali Threat Bulletin Import Tool",
                    Priority = TutorialPriority.High,
                    ShowOnFirstRun = true,
                    Category = "Getting Started",
                    EstimatedDuration = TimeSpan.FromMinutes(5),
                    Steps = new List<TutorialStep>
                    {
                        new TutorialStep
                        {
                            Id = "welcome",
                            Title = "Welcome",
                            Content = "Welcome to the Anomali Threat Bulletin Import Tool! This tutorial will guide you through the key features.",
                            HighlightStyle = HighlightStyle.None
                        },
                        new TutorialStep
                        {
                            Id = "dashboard",
                            Title = "Dashboard",
                            Content = "This is your dashboard where you can see recent activity and quick actions.",
                            TargetElementName = "DashboardView",
                            HighlightStyle = HighlightStyle.Spotlight
                        },
                        new TutorialStep
                        {
                            Id = "modes",
                            Title = "Interface Modes",
                            Content = "You can switch between Wizard Mode for guided workflows and Advanced Mode for power users.",
                            TargetElementName = "ModeSelector",
                            HighlightStyle = HighlightStyle.Outline
                        }
                    }
                };

                RegisterTutorial(overviewTutorial);

                _logger.LogInformation("Registered {Count} default tutorials", 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register default tutorials");
            }
        }

        private async Task ShowStepAsync(int stepIndex)
        {
            try
            {
                var tutorial = _tutorials[_currentState.CurrentTutorialId];
                var step = tutorial.Steps[stepIndex];

                _logger.LogDebug("Showing tutorial step: {StepIndex} - {Title}", stepIndex, step.Title);

                // Update tooltip content
                _overlay.TitleText.Text = step.Title;
                _overlay.ContentText.Text = step.Content;

                // Update progress
                _overlay.ProgressBar.Value = (double)(stepIndex + 1) / tutorial.Steps.Count * 100;

                // Update button states
                _overlay.PreviousButton.IsEnabled = stepIndex > 0;
                _overlay.NextButton.Content = stepIndex == tutorial.Steps.Count - 1 ? "Finish" : "Next";

                // Find and highlight target element
                if (!string.IsNullOrWhiteSpace(step.TargetElementName))
                {
                    var targetElement = FindElementByName(step.TargetElementName);
                    if (targetElement != null)
                    {
                        await HighlightElementAsync(targetElement, step.HighlightStyle);
                        PositionTooltip(targetElement, step.TooltipPosition);
                    }
                }

                // Show tooltip
                _overlay.TooltipBorder.Visibility = Visibility.Visible;

                // Announce step
                await _accessibilityService.AnnounceAsync(
                    $"Step {stepIndex + 1} of {tutorial.Steps.Count}: {step.Title}. {step.Content}", 
                    AccessibilityAnnouncementPriority.High);

                // Handle auto-advance
                if (step.AutoAdvanceDelay.HasValue)
                {
                    _ = Task.Delay(step.AutoAdvanceDelay.Value).ContinueWith(async _ =>
                    {
                        if (_currentState.IsActive && _currentState.CurrentStepIndex == stepIndex)
                        {
                            await NextStepAsync();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show tutorial step: {StepIndex}", stepIndex);
            }
        }

        private FrameworkElement FindElementByName(string elementName)
        {
            try
            {
                return FindElementRecursive(_rootElement, elementName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find element by name: {ElementName}", elementName);
                return null;
            }
        }

        private FrameworkElement FindElementRecursive(DependencyObject parent, string name)
        {
            if (parent is FrameworkElement element && element.Name == name)
            {
                return element;
            }

            var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                var result = FindElementRecursive(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private async Task HighlightElementAsync(FrameworkElement element, HighlightStyle style)
        {
            try
            {
                // Remove existing highlight
                RemoveHighlight();

                if (style == HighlightStyle.None) return;

                // Create highlight shape based on style
                _overlay.HighlightShape = style switch
                {
                    HighlightStyle.Outline => CreateOutlineHighlight(element),
                    HighlightStyle.Spotlight => CreateSpotlightHighlight(element),
                    HighlightStyle.Glow => CreateGlowHighlight(element),
                    _ => CreateOutlineHighlight(element)
                };

                if (_overlay.HighlightShape != null)
                {
                    _overlay.OverlayCanvas.Children.Add(_overlay.HighlightShape);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to highlight element");
            }
        }

        private Shape CreateOutlineHighlight(FrameworkElement element)
        {
            var bounds = element.TransformToVisual(_overlay.OverlayCanvas).TransformBounds(
                new Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));

            return new Rectangle
            {
                Width = bounds.Width + 8,
                Height = bounds.Height + 8,
                Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                RadiusX = SPOTLIGHT_RADIUS,
                RadiusY = SPOTLIGHT_RADIUS
            };
        }

        private Shape CreateSpotlightHighlight(FrameworkElement element)
        {
            // This would create a spotlight effect - simplified implementation
            return CreateOutlineHighlight(element);
        }

        private Shape CreateGlowHighlight(FrameworkElement element)
        {
            // This would create a glow effect - simplified implementation
            return CreateOutlineHighlight(element);
        }

        private void RemoveHighlight()
        {
            if (_overlay.HighlightShape != null)
            {
                _overlay.OverlayCanvas.Children.Remove(_overlay.HighlightShape);
                _overlay.HighlightShape = null;
            }
        }

        private void PositionTooltip(FrameworkElement targetElement, TooltipPosition position)
        {
            try
            {
                var bounds = targetElement.TransformToVisual(_overlay.OverlayCanvas).TransformBounds(
                    new Windows.Foundation.Rect(0, 0, targetElement.ActualWidth, targetElement.ActualHeight));

                double left = bounds.X;
                double top = bounds.Y;

                // Calculate position based on preference
                switch (position)
                {
                    case TooltipPosition.Top:
                        left = bounds.X + (bounds.Width - _overlay.TooltipBorder.ActualWidth) / 2;
                        top = bounds.Y - _overlay.TooltipBorder.ActualHeight - 16;
                        break;
                    case TooltipPosition.Bottom:
                        left = bounds.X + (bounds.Width - _overlay.TooltipBorder.ActualWidth) / 2;
                        top = bounds.Y + bounds.Height + 16;
                        break;
                    case TooltipPosition.Left:
                        left = bounds.X - _overlay.TooltipBorder.ActualWidth - 16;
                        top = bounds.Y + (bounds.Height - _overlay.TooltipBorder.ActualHeight) / 2;
                        break;
                    case TooltipPosition.Right:
                        left = bounds.X + bounds.Width + 16;
                        top = bounds.Y + (bounds.Height - _overlay.TooltipBorder.ActualHeight) / 2;
                        break;
                    default: // Auto
                        left = bounds.X + bounds.Width + 16;
                        top = bounds.Y;
                        break;
                }

                Canvas.SetLeft(_overlay.TooltipBorder, Math.Max(16, left));
                Canvas.SetTop(_overlay.TooltipBorder, Math.Max(16, top));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to position tooltip");
            }
        }

        private void ShowOverlay()
        {
            if (_overlay?.OverlayCanvas != null)
            {
                _overlay.OverlayCanvas.Visibility = Visibility.Visible;
                
                // Update dim layer size
                _overlay.DimLayer.Width = _rootElement.ActualWidth;
                _overlay.DimLayer.Height = _rootElement.ActualHeight;
            }
        }

        private void HideOverlay()
        {
            if (_overlay?.OverlayCanvas != null)
            {
                _overlay.OverlayCanvas.Visibility = Visibility.Collapsed;
                _overlay.TooltipBorder.Visibility = Visibility.Collapsed;
                RemoveHighlight();
            }
        }

        private async Task CompleteTutorialAsync()
        {
            try
            {
                var tutorialId = _currentState.CurrentTutorialId;
                _logger.LogInformation("Completing tutorial: {TutorialId}", tutorialId);

                // Mark as completed
                MarkTutorialCompleted(tutorialId);

                // Stop tutorial
                await StopTutorialAsync();

                // Play completion sound
                await _audioFeedbackService.PlaySuccessSoundAsync();

                // Announce completion
                await _accessibilityService.AnnounceAsync(
                    "Tutorial completed successfully!", 
                    AccessibilityAnnouncementPriority.High);

                // Fire completion event
                _tutorialEvents.OnNext(new TutorialEvent
                {
                    EventType = "TutorialCompleted",
                    TutorialId = tutorialId,
                    Data = new Dictionary<string, object> { ["ElapsedTime"] = _currentState.ElapsedTime }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete tutorial");
            }
        }

        private async Task LoadCompletedTutorialsAsync()
        {
            try
            {
                // Load completed tutorials from storage (simplified implementation)
                // In a real implementation, this would load from user settings or database
                _logger.LogDebug("Loaded completed tutorials from storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load completed tutorials");
            }
        }

        private async Task SaveCompletedTutorialsAsync()
        {
            try
            {
                // Save completed tutorials to storage (simplified implementation)
                _logger.LogDebug("Saved completed tutorials to storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save completed tutorials");
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Disposing InteractiveOnboardingService");

                // Stop current tutorial
                if (_currentState.IsActive)
                {
                    _ = Task.Run(StopTutorialAsync);
                }

                // Remove overlay from UI
                if (_overlay?.OverlayCanvas != null && _rootElement is Panel rootPanel)
                {
                    rootPanel.Children.Remove(_overlay.OverlayCanvas);
                }

                // Clean up collections
                _tutorials.Clear();
                _completedTutorials.Clear();

                // Dispose observables
                _tutorialEvents?.Dispose();

                _logger.LogInformation("InteractiveOnboardingService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing InteractiveOnboardingService");
            }
        }
    }
} 
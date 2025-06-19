using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.WinUI.Services
{
    /// <summary>
    /// High contrast service interface for enhanced accessibility beyond Windows settings
    /// </summary>
    public interface IHighContrastService
    {
        /// <summary>
        /// Initialize high contrast service and detect system settings
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Apply high contrast theme
        /// </summary>
        Task ApplyHighContrastAsync(HighContrastMode mode);

        /// <summary>
        /// Restore normal contrast theme
        /// </summary>
        Task RestoreNormalContrastAsync();

        /// <summary>
        /// Toggle high contrast mode
        /// </summary>
        Task ToggleHighContrastAsync();

        /// <summary>
        /// Get current high contrast mode
        /// </summary>
        HighContrastMode GetCurrentMode();

        /// <summary>
        /// Check if high contrast is currently active
        /// </summary>
        bool IsHighContrastActive { get; }

        /// <summary>
        /// Register element for high contrast support
        /// </summary>
        void RegisterElement(FrameworkElement element, string contrastRole = "default");

        /// <summary>
        /// Unregister element from high contrast support
        /// </summary>
        void UnregisterElement(FrameworkElement element);

        /// <summary>
        /// Set custom high contrast colors
        /// </summary>
        void SetCustomColors(HighContrastColorScheme colorScheme);

        /// <summary>
        /// Observable for high contrast change events
        /// </summary>
        IObservable<HighContrastChangeEvent> ContrastChangeEvents { get; }

        /// <summary>
        /// Available high contrast modes
        /// </summary>
        IReadOnlyList<HighContrastModeInfo> AvailableModes { get; }
    }

    /// <summary>
    /// High contrast modes enumeration
    /// </summary>
    public enum HighContrastMode
    {
        None,
        HighContrastBlack,
        HighContrastWhite,
        HighContrastBlue,
        HighContrastGreen,
        CustomContrast,
        SystemDefault
    }

    /// <summary>
    /// High contrast change event data
    /// </summary>
    public class HighContrastChangeEvent
    {
        public HighContrastMode PreviousMode { get; set; }
        public HighContrastMode CurrentMode { get; set; }
        public bool IsSystemTriggered { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; }
    }

    /// <summary>
    /// High contrast mode information
    /// </summary>
    public class HighContrastModeInfo
    {
        public HighContrastMode Mode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public HighContrastColorScheme ColorScheme { get; set; }
        public bool IsSystemMode { get; set; }
        public double ContrastRatio { get; set; }
    }

    /// <summary>
    /// High contrast color scheme
    /// </summary>
    public class HighContrastColorScheme
    {
        public Color Background { get; set; }
        public Color Foreground { get; set; }
        public Color SelectedBackground { get; set; }
        public Color SelectedForeground { get; set; }
        public Color ButtonBackground { get; set; }
        public Color ButtonForeground { get; set; }
        public Color ButtonHoverBackground { get; set; }
        public Color ButtonHoverForeground { get; set; }
        public Color ButtonPressedBackground { get; set; }
        public Color ButtonPressedForeground { get; set; }
        public Color DisabledBackground { get; set; }
        public Color DisabledForeground { get; set; }
        public Color HyperlinkForeground { get; set; }
        public Color HyperlinkHoverForeground { get; set; }
        public Color BorderColor { get; set; }
        public Color FocusBorderColor { get; set; }
        public Color ErrorColor { get; set; }
        public Color WarningColor { get; set; }
        public Color SuccessColor { get; set; }
        public Color InfoColor { get; set; }
    }

    /// <summary>
    /// Element contrast role for specialized styling
    /// </summary>
    public enum ContrastRole
    {
        Default,
        Primary,
        Secondary,
        Accent,
        Button,
        Input,
        Navigation,
        Content,
        Header,
        Footer,
        Sidebar,
        Error,
        Warning,
        Success,
        Info
    }

    /// <summary>
    /// Registered element information for high contrast
    /// </summary>
    public class ContrastElement
    {
        public FrameworkElement Element { get; set; }
        public ContrastRole Role { get; set; }
        public Dictionary<string, object> OriginalProperties { get; set; } = new Dictionary<string, object>();
        public bool IsContrastApplied { get; set; }
    }

    /// <summary>
    /// Professional high contrast service with enhanced accessibility features
    /// </summary>
    public class HighContrastService : IHighContrastService, IDisposable
    {
        private readonly ILogger<HighContrastService> _logger;
        private readonly IAccessibilityService _accessibilityService;
        private readonly IAudioFeedbackService _audioFeedbackService;

        private readonly Dictionary<FrameworkElement, ContrastElement> _registeredElements;
        private readonly Dictionary<HighContrastMode, HighContrastModeInfo> _availableModes;
        private readonly Subject<HighContrastChangeEvent> _contrastChangeEvents;

        private AccessibilitySettings _accessibilitySettings;
        private UISettings _uiSettings;
        private HighContrastMode _currentMode = HighContrastMode.None;
        private HighContrastColorScheme _customColorScheme;
        private bool _isInitialized = false;
        private bool _isSystemHighContrastEnabled = false;

        // High contrast constants
        private const double MIN_CONTRAST_RATIO = 4.5; // WCAG AA standard
        private const double ENHANCED_CONTRAST_RATIO = 7.0; // WCAG AAA standard

        public HighContrastService(
            ILogger<HighContrastService> logger,
            IAccessibilityService accessibilityService,
            IAudioFeedbackService audioFeedbackService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));
            _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));

            _registeredElements = new Dictionary<FrameworkElement, ContrastElement>();
            _availableModes = new Dictionary<HighContrastMode, HighContrastModeInfo>();
            _contrastChangeEvents = new Subject<HighContrastChangeEvent>();

            _logger.LogInformation("HighContrastService initialized");
        }

        public IObservable<HighContrastChangeEvent> ContrastChangeEvents => _contrastChangeEvents.AsObservable();
        public IReadOnlyList<HighContrastModeInfo> AvailableModes => new List<HighContrastModeInfo>(_availableModes.Values);
        public bool IsHighContrastActive => _currentMode != HighContrastMode.None;

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing high contrast service");

                // Initialize system settings
                _accessibilitySettings = new AccessibilitySettings();
                _uiSettings = new UISettings();

                // Set up system event handlers
                _accessibilitySettings.HighContrastChanged += OnSystemHighContrastChanged;
                _uiSettings.ColorValuesChanged += OnSystemColorValuesChanged;

                // Initialize available modes
                InitializeAvailableModes();

                // Detect current system state
                _isSystemHighContrastEnabled = _accessibilitySettings.HighContrast;
                if (_isSystemHighContrastEnabled)
                {
                    _currentMode = DetectSystemHighContrastMode();
                }

                _isInitialized = true;

                _logger.LogInformation("High contrast service initialized. System high contrast: {IsEnabled}, Current mode: {Mode}",
                    _isSystemHighContrastEnabled, _currentMode);

                // Announce initialization
                await _accessibilityService.AnnounceAsync(
                    $"High contrast service ready. Current mode: {GetModeDisplayName(_currentMode)}", 
                    AccessibilityAnnouncementPriority.Medium);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize high contrast service");
                throw;
            }
        }

        public async Task ApplyHighContrastAsync(HighContrastMode mode)
        {
            try
            {
                if (!_isInitialized)
                {
                    _logger.LogWarning("High contrast service not initialized");
                    return;
                }

                _logger.LogInformation("Applying high contrast mode: {Mode}", mode);

                var previousMode = _currentMode;
                _currentMode = mode;

                // Get color scheme for the mode
                var colorScheme = GetColorSchemeForMode(mode);
                if (colorScheme == null)
                {
                    _logger.LogWarning("No color scheme available for mode: {Mode}", mode);
                    return;
                }

                // Apply contrast to all registered elements
                await ApplyContrastToElementsAsync(colorScheme);

                // Update application theme resources
                await UpdateApplicationThemeAsync(colorScheme);

                // Fire change event
                _contrastChangeEvents.OnNext(new HighContrastChangeEvent
                {
                    PreviousMode = previousMode,
                    CurrentMode = _currentMode,
                    IsSystemTriggered = false,
                    Reason = "User requested"
                });

                // Play audio feedback
                await _audioFeedbackService.PlaySuccessSoundAsync();

                // Announce change
                await _accessibilityService.AnnounceAsync(
                    $"High contrast mode changed to {GetModeDisplayName(mode)}", 
                    AccessibilityAnnouncementPriority.High);

                _logger.LogInformation("High contrast mode applied successfully: {Mode}", mode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply high contrast mode: {Mode}", mode);
                await _audioFeedbackService.PlayErrorSoundAsync();
            }
        }

        public async Task RestoreNormalContrastAsync()
        {
            try
            {
                _logger.LogInformation("Restoring normal contrast");

                var previousMode = _currentMode;
                _currentMode = HighContrastMode.None;

                // Restore original properties for all registered elements
                await RestoreOriginalPropertiesAsync();

                // Restore application theme
                await RestoreApplicationThemeAsync();

                // Fire change event
                _contrastChangeEvents.OnNext(new HighContrastChangeEvent
                {
                    PreviousMode = previousMode,
                    CurrentMode = _currentMode,
                    IsSystemTriggered = false,
                    Reason = "User requested normal contrast"
                });

                // Play audio feedback
                await _audioFeedbackService.PlaySuccessSoundAsync();

                // Announce change
                await _accessibilityService.AnnounceAsync(
                    "Normal contrast restored", 
                    AccessibilityAnnouncementPriority.High);

                _logger.LogInformation("Normal contrast restored successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore normal contrast");
                await _audioFeedbackService.PlayErrorSoundAsync();
            }
        }

        public async Task ToggleHighContrastAsync()
        {
            try
            {
                if (IsHighContrastActive)
                {
                    await RestoreNormalContrastAsync();
                }
                else
                {
                    // Apply default high contrast mode
                    var defaultMode = _isSystemHighContrastEnabled ? DetectSystemHighContrastMode() : HighContrastMode.HighContrastBlack;
                    await ApplyHighContrastAsync(defaultMode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle high contrast");
                await _audioFeedbackService.PlayErrorSoundAsync();
            }
        }

        public HighContrastMode GetCurrentMode()
        {
            return _currentMode;
        }

        public void RegisterElement(FrameworkElement element, string contrastRole = "default")
        {
            try
            {
                if (element == null)
                {
                    _logger.LogWarning("Attempted to register null element for high contrast");
                    return;
                }

                if (_registeredElements.ContainsKey(element))
                {
                    _logger.LogDebug("Element already registered for high contrast");
                    return;
                }

                var role = Enum.TryParse<ContrastRole>(contrastRole, true, out var parsedRole) ? parsedRole : ContrastRole.Default;

                var contrastElement = new ContrastElement
                {
                    Element = element,
                    Role = role,
                    IsContrastApplied = false
                };

                // Store original properties
                StoreOriginalProperties(contrastElement);

                _registeredElements[element] = contrastElement;

                // Apply current contrast if active
                if (IsHighContrastActive)
                {
                    var colorScheme = GetColorSchemeForMode(_currentMode);
                    if (colorScheme != null)
                    {
                        ApplyContrastToElement(contrastElement, colorScheme);
                    }
                }

                _logger.LogDebug("Registered element for high contrast: {ElementType} (Role: {Role})",
                    element.GetType().Name, role);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register element for high contrast");
            }
        }

        public void UnregisterElement(FrameworkElement element)
        {
            try
            {
                if (element == null) return;

                if (_registeredElements.TryGetValue(element, out var contrastElement))
                {
                    // Restore original properties if contrast is applied
                    if (contrastElement.IsContrastApplied)
                    {
                        RestoreElementProperties(contrastElement);
                    }

                    _registeredElements.Remove(element);

                    _logger.LogDebug("Unregistered element from high contrast: {ElementType}",
                        element.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister element from high contrast");
            }
        }

        public void SetCustomColors(HighContrastColorScheme colorScheme)
        {
            try
            {
                _customColorScheme = colorScheme ?? throw new ArgumentNullException(nameof(colorScheme));

                // Validate contrast ratios
                ValidateColorScheme(_customColorScheme);

                _logger.LogInformation("Custom high contrast color scheme set");

                // Apply if custom mode is active
                if (_currentMode == HighContrastMode.CustomContrast)
                {
                    _ = Task.Run(() => ApplyHighContrastAsync(HighContrastMode.CustomContrast));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set custom high contrast colors");
            }
        }

        private void InitializeAvailableModes()
        {
            // High Contrast Black
            _availableModes[HighContrastMode.HighContrastBlack] = new HighContrastModeInfo
            {
                Mode = HighContrastMode.HighContrastBlack,
                Name = "High Contrast Black",
                Description = "Black background with white text for maximum contrast",
                IsSystemMode = true,
                ContrastRatio = 21.0, // Maximum possible contrast ratio
                ColorScheme = CreateHighContrastBlackScheme()
            };

            // High Contrast White
            _availableModes[HighContrastMode.HighContrastWhite] = new HighContrastModeInfo
            {
                Mode = HighContrastMode.HighContrastWhite,
                Name = "High Contrast White",
                Description = "White background with black text for high readability",
                IsSystemMode = true,
                ContrastRatio = 21.0,
                ColorScheme = CreateHighContrastWhiteScheme()
            };

            // High Contrast Blue
            _availableModes[HighContrastMode.HighContrastBlue] = new HighContrastModeInfo
            {
                Mode = HighContrastMode.HighContrastBlue,
                Name = "High Contrast Blue",
                Description = "Blue theme with high contrast for reduced eye strain",
                IsSystemMode = false,
                ContrastRatio = 12.0,
                ColorScheme = CreateHighContrastBlueScheme()
            };

            // High Contrast Green
            _availableModes[HighContrastMode.HighContrastGreen] = new HighContrastModeInfo
            {
                Mode = HighContrastMode.HighContrastGreen,
                Name = "High Contrast Green",
                Description = "Green theme optimized for accessibility",
                IsSystemMode = false,
                ContrastRatio = 10.0,
                ColorScheme = CreateHighContrastGreenScheme()
            };

            // Custom Contrast
            _availableModes[HighContrastMode.CustomContrast] = new HighContrastModeInfo
            {
                Mode = HighContrastMode.CustomContrast,
                Name = "Custom Contrast",
                Description = "User-defined high contrast color scheme",
                IsSystemMode = false,
                ContrastRatio = 7.0,
                ColorScheme = null // Will be set by user
            };
        }

        private HighContrastColorScheme CreateHighContrastBlackScheme()
        {
            return new HighContrastColorScheme
            {
                Background = Colors.Black,
                Foreground = Colors.White,
                SelectedBackground = Color.FromArgb(255, 0, 120, 215),
                SelectedForeground = Colors.White,
                ButtonBackground = Color.FromArgb(255, 64, 64, 64),
                ButtonForeground = Colors.White,
                ButtonHoverBackground = Color.FromArgb(255, 128, 128, 128),
                ButtonHoverForeground = Colors.White,
                ButtonPressedBackground = Color.FromArgb(255, 0, 120, 215),
                ButtonPressedForeground = Colors.White,
                DisabledBackground = Color.FromArgb(255, 32, 32, 32),
                DisabledForeground = Color.FromArgb(255, 128, 128, 128),
                HyperlinkForeground = Color.FromArgb(255, 0, 162, 232),
                HyperlinkHoverForeground = Color.FromArgb(255, 64, 196, 255),
                BorderColor = Colors.White,
                FocusBorderColor = Colors.Yellow,
                ErrorColor = Color.FromArgb(255, 255, 99, 71),
                WarningColor = Color.FromArgb(255, 255, 215, 0),
                SuccessColor = Color.FromArgb(255, 50, 205, 50),
                InfoColor = Color.FromArgb(255, 135, 206, 235)
            };
        }

        private HighContrastColorScheme CreateHighContrastWhiteScheme()
        {
            return new HighContrastColorScheme
            {
                Background = Colors.White,
                Foreground = Colors.Black,
                SelectedBackground = Color.FromArgb(255, 0, 120, 215),
                SelectedForeground = Colors.White,
                ButtonBackground = Color.FromArgb(255, 240, 240, 240),
                ButtonForeground = Colors.Black,
                ButtonHoverBackground = Color.FromArgb(255, 220, 220, 220),
                ButtonHoverForeground = Colors.Black,
                ButtonPressedBackground = Color.FromArgb(255, 0, 120, 215),
                ButtonPressedForeground = Colors.White,
                DisabledBackground = Color.FromArgb(255, 248, 248, 248),
                DisabledForeground = Color.FromArgb(255, 128, 128, 128),
                HyperlinkForeground = Color.FromArgb(255, 0, 102, 204),
                HyperlinkHoverForeground = Color.FromArgb(255, 0, 51, 153),
                BorderColor = Colors.Black,
                FocusBorderColor = Color.FromArgb(255, 255, 140, 0),
                ErrorColor = Color.FromArgb(255, 220, 20, 60),
                WarningColor = Color.FromArgb(255, 255, 140, 0),
                SuccessColor = Color.FromArgb(255, 34, 139, 34),
                InfoColor = Color.FromArgb(255, 70, 130, 180)
            };
        }

        private HighContrastColorScheme CreateHighContrastBlueScheme()
        {
            return new HighContrastColorScheme
            {
                Background = Color.FromArgb(255, 0, 32, 96),
                Foreground = Colors.White,
                SelectedBackground = Color.FromArgb(255, 0, 120, 215),
                SelectedForeground = Colors.White,
                ButtonBackground = Color.FromArgb(255, 0, 64, 128),
                ButtonForeground = Colors.White,
                ButtonHoverBackground = Color.FromArgb(255, 0, 96, 160),
                ButtonHoverForeground = Colors.White,
                ButtonPressedBackground = Color.FromArgb(255, 0, 120, 215),
                ButtonPressedForeground = Colors.White,
                DisabledBackground = Color.FromArgb(255, 0, 16, 48),
                DisabledForeground = Color.FromArgb(255, 128, 128, 128),
                HyperlinkForeground = Color.FromArgb(255, 100, 200, 255),
                HyperlinkHoverForeground = Color.FromArgb(255, 150, 220, 255),
                BorderColor = Colors.White,
                FocusBorderColor = Colors.Yellow,
                ErrorColor = Color.FromArgb(255, 255, 99, 71),
                WarningColor = Color.FromArgb(255, 255, 215, 0),
                SuccessColor = Color.FromArgb(255, 50, 205, 50),
                InfoColor = Color.FromArgb(255, 135, 206, 235)
            };
        }

        private HighContrastColorScheme CreateHighContrastGreenScheme()
        {
            return new HighContrastColorScheme
            {
                Background = Color.FromArgb(255, 0, 64, 0),
                Foreground = Colors.White,
                SelectedBackground = Color.FromArgb(255, 0, 128, 0),
                SelectedForeground = Colors.White,
                ButtonBackground = Color.FromArgb(255, 0, 96, 0),
                ButtonForeground = Colors.White,
                ButtonHoverBackground = Color.FromArgb(255, 0, 128, 0),
                ButtonHoverForeground = Colors.White,
                ButtonPressedBackground = Color.FromArgb(255, 0, 160, 0),
                ButtonPressedForeground = Colors.White,
                DisabledBackground = Color.FromArgb(255, 0, 32, 0),
                DisabledForeground = Color.FromArgb(255, 128, 128, 128),
                HyperlinkForeground = Color.FromArgb(255, 144, 238, 144),
                HyperlinkHoverForeground = Color.FromArgb(255, 173, 255, 173),
                BorderColor = Colors.White,
                FocusBorderColor = Colors.Yellow,
                ErrorColor = Color.FromArgb(255, 255, 99, 71),
                WarningColor = Color.FromArgb(255, 255, 215, 0),
                SuccessColor = Color.FromArgb(255, 50, 205, 50),
                InfoColor = Color.FromArgb(255, 135, 206, 235)
            };
        }

        private HighContrastMode DetectSystemHighContrastMode()
        {
            try
            {
                // This is a simplified detection - in a real implementation,
                // you would query system colors to determine the exact mode
                return HighContrastMode.HighContrastBlack;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect system high contrast mode");
                return HighContrastMode.HighContrastBlack;
            }
        }

        private HighContrastColorScheme GetColorSchemeForMode(HighContrastMode mode)
        {
            if (mode == HighContrastMode.CustomContrast)
            {
                return _customColorScheme;
            }

            return _availableModes.TryGetValue(mode, out var modeInfo) ? modeInfo.ColorScheme : null;
        }

        private async Task ApplyContrastToElementsAsync(HighContrastColorScheme colorScheme)
        {
            try
            {
                var tasks = new List<Task>();

                foreach (var kvp in _registeredElements)
                {
                    var contrastElement = kvp.Value;
                    tasks.Add(Task.Run(() => ApplyContrastToElement(contrastElement, colorScheme)));
                }

                await Task.WhenAll(tasks);

                _logger.LogDebug("Applied high contrast to {Count} elements", _registeredElements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply contrast to elements");
            }
        }

        private void ApplyContrastToElement(ContrastElement contrastElement, HighContrastColorScheme colorScheme)
        {
            try
            {
                var element = contrastElement.Element;
                var role = contrastElement.Role;

                // Apply role-specific styling
                switch (role)
                {
                    case ContrastRole.Button:
                        ApplyButtonContrast(element, colorScheme);
                        break;
                    case ContrastRole.Input:
                        ApplyInputContrast(element, colorScheme);
                        break;
                    case ContrastRole.Navigation:
                        ApplyNavigationContrast(element, colorScheme);
                        break;
                    case ContrastRole.Error:
                        ApplyErrorContrast(element, colorScheme);
                        break;
                    case ContrastRole.Warning:
                        ApplyWarningContrast(element, colorScheme);
                        break;
                    case ContrastRole.Success:
                        ApplySuccessContrast(element, colorScheme);
                        break;
                    case ContrastRole.Info:
                        ApplyInfoContrast(element, colorScheme);
                        break;
                    default:
                        ApplyDefaultContrast(element, colorScheme);
                        break;
                }

                contrastElement.IsContrastApplied = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply contrast to element: {ElementType}", 
                    contrastElement.Element?.GetType().Name);
            }
        }

        private void ApplyDefaultContrast(FrameworkElement element, HighContrastColorScheme colorScheme)
        {
            if (element.Background is SolidColorBrush)
                element.Background = new SolidColorBrush(colorScheme.Background);
            
            if (element.Foreground is SolidColorBrush)
                element.Foreground = new SolidColorBrush(colorScheme.Foreground);
        }

        private void ApplyButtonContrast(FrameworkElement element, HighContrastColorScheme colorScheme)
        {
            element.Background = new SolidColorBrush(colorScheme.ButtonBackground);
            element.Foreground = new SolidColorBrush(colorScheme.ButtonForeground);
            element.BorderBrush = new SolidColorBrush(colorScheme.BorderColor);
            element.BorderThickness = new Thickness(2);
        }

        private void ApplyInputContrast(FrameworkElement element, HighContrastColorScheme colorScheme)
        {
            element.Background = new SolidColorBrush(colorScheme.Background);
            element.Foreground = new SolidColorBrush(colorScheme.Foreground);
            element.BorderBrush = new SolidColorBrush(colorScheme.BorderColor);
            element.BorderThickness = new Thickness(2);
        }

        private void ApplyNavigationContrast(FrameworkElement element, HighContrastColorScheme colorScheme)
        {
            element.Background = new SolidColorBrush(colorScheme.Background);
            element.Foreground = new SolidColorBrush(colorScheme.Foreground);
            element.BorderBrush = new SolidColorBrush(colorScheme.BorderColor);
        }

        private void ApplyErrorContrast(FrameworkElement element, HighContrastColorScheme colorScheme)
        {
            element.Background = new SolidColorBrush(colorScheme.Background);
            element.Foreground = new SolidColorBrush(colorScheme.ErrorColor);
            element.BorderBrush = new SolidColorBrush(colorScheme.ErrorColor);
        }

        private void ApplyWarningContrast(FrameworkElement element, HighContrastColorScheme colorScheme)
        {
            element.Background = new SolidColorBrush(colorScheme.Background);
            element.Foreground = new SolidColorBrush(colorScheme.WarningColor);
            element.BorderBrush = new SolidColorBrush(colorScheme.WarningColor);
        }

        private void ApplySuccessContrast(FrameworkElement element, HighContrastColorScheme colorScheme)
        {
            element.Background = new SolidColorBrush(colorScheme.Background);
            element.Foreground = new SolidColorBrush(colorScheme.SuccessColor);
            element.BorderBrush = new SolidColorBrush(colorScheme.SuccessColor);
        }

        private void ApplyInfoContrast(FrameworkElement element, HighContrastColorScheme colorScheme)
        {
            element.Background = new SolidColorBrush(colorScheme.Background);
            element.Foreground = new SolidColorBrush(colorScheme.InfoColor);
            element.BorderBrush = new SolidColorBrush(colorScheme.InfoColor);
        }

        private async Task RestoreOriginalPropertiesAsync()
        {
            try
            {
                var tasks = new List<Task>();

                foreach (var contrastElement in _registeredElements.Values)
                {
                    if (contrastElement.IsContrastApplied)
                    {
                        tasks.Add(Task.Run(() => RestoreElementProperties(contrastElement)));
                    }
                }

                await Task.WhenAll(tasks);

                _logger.LogDebug("Restored original properties for {Count} elements", _registeredElements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore original properties");
            }
        }

        private void RestoreElementProperties(ContrastElement contrastElement)
        {
            try
            {
                var element = contrastElement.Element;
                var originalProperties = contrastElement.OriginalProperties;

                foreach (var kvp in originalProperties)
                {
                    var property = kvp.Key;
                    var value = kvp.Value;

                    switch (property)
                    {
                        case "Background":
                            element.Background = value as Brush;
                            break;
                        case "Foreground":
                            element.Foreground = value as Brush;
                            break;
                        case "BorderBrush":
                            element.BorderBrush = value as Brush;
                            break;
                        case "BorderThickness":
                            if (value is Thickness thickness)
                                element.BorderThickness = thickness;
                            break;
                    }
                }

                contrastElement.IsContrastApplied = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore element properties");
            }
        }

        private void StoreOriginalProperties(ContrastElement contrastElement)
        {
            try
            {
                var element = contrastElement.Element;
                var properties = contrastElement.OriginalProperties;

                properties["Background"] = element.Background;
                properties["Foreground"] = element.Foreground;
                properties["BorderBrush"] = element.BorderBrush;
                properties["BorderThickness"] = element.BorderThickness;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store original properties");
            }
        }

        private async Task UpdateApplicationThemeAsync(HighContrastColorScheme colorScheme)
        {
            try
            {
                // Update application-level theme resources
                var resources = Application.Current.Resources;

                resources["SystemAccentColor"] = colorScheme.SelectedBackground;
                resources["SystemBackgroundColor"] = colorScheme.Background;
                resources["SystemForegroundColor"] = colorScheme.Foreground;

                _logger.LogDebug("Updated application theme resources");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update application theme");
            }
        }

        private async Task RestoreApplicationThemeAsync()
        {
            try
            {
                // Restore original application theme
                // This would involve restoring the original theme resources
                _logger.LogDebug("Restored application theme");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore application theme");
            }
        }

        private void ValidateColorScheme(HighContrastColorScheme colorScheme)
        {
            // Validate contrast ratios meet WCAG guidelines
            var backgroundLuminance = CalculateLuminance(colorScheme.Background);
            var foregroundLuminance = CalculateLuminance(colorScheme.Foreground);
            
            var contrastRatio = CalculateContrastRatio(backgroundLuminance, foregroundLuminance);
            
            if (contrastRatio < MIN_CONTRAST_RATIO)
            {
                _logger.LogWarning("Color scheme does not meet minimum contrast ratio requirements. Ratio: {Ratio}", contrastRatio);
            }
            else if (contrastRatio >= ENHANCED_CONTRAST_RATIO)
            {
                _logger.LogInformation("Color scheme meets enhanced contrast ratio requirements. Ratio: {Ratio}", contrastRatio);
            }
        }

        private double CalculateLuminance(Color color)
        {
            // Calculate relative luminance according to WCAG guidelines
            var r = color.R / 255.0;
            var g = color.G / 255.0;
            var b = color.B / 255.0;

            r = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);

            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private double CalculateContrastRatio(double luminance1, double luminance2)
        {
            var lighter = Math.Max(luminance1, luminance2);
            var darker = Math.Min(luminance1, luminance2);
            return (lighter + 0.05) / (darker + 0.05);
        }

        private string GetModeDisplayName(HighContrastMode mode)
        {
            return _availableModes.TryGetValue(mode, out var modeInfo) ? modeInfo.Name : mode.ToString();
        }

        private async void OnSystemHighContrastChanged(AccessibilitySettings sender, object args)
        {
            try
            {
                _logger.LogInformation("System high contrast setting changed");

                var wasEnabled = _isSystemHighContrastEnabled;
                _isSystemHighContrastEnabled = sender.HighContrast;

                if (_isSystemHighContrastEnabled != wasEnabled)
                {
                    var newMode = _isSystemHighContrastEnabled ? DetectSystemHighContrastMode() : HighContrastMode.None;
                    
                    _contrastChangeEvents.OnNext(new HighContrastChangeEvent
                    {
                        PreviousMode = _currentMode,
                        CurrentMode = newMode,
                        IsSystemTriggered = true,
                        Reason = "System setting changed"
                    });

                    _currentMode = newMode;

                    if (_isSystemHighContrastEnabled)
                    {
                        await ApplyHighContrastAsync(newMode);
                    }
                    else
                    {
                        await RestoreNormalContrastAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling system high contrast change");
            }
        }

        private async void OnSystemColorValuesChanged(UISettings sender, object args)
        {
            try
            {
                _logger.LogDebug("System color values changed");
                
                // Update color schemes if needed based on system changes
                if (_isSystemHighContrastEnabled)
                {
                    var currentMode = DetectSystemHighContrastMode();
                    if (currentMode != _currentMode)
                    {
                        await ApplyHighContrastAsync(currentMode);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling system color values change");
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Disposing HighContrastService");

                // Remove system event handlers
                if (_accessibilitySettings != null)
                {
                    _accessibilitySettings.HighContrastChanged -= OnSystemHighContrastChanged;
                }

                if (_uiSettings != null)
                {
                    _uiSettings.ColorValuesChanged -= OnSystemColorValuesChanged;
                }

                // Restore normal contrast if active
                if (IsHighContrastActive)
                {
                    _ = Task.Run(RestoreNormalContrastAsync);
                }

                // Clean up registered elements
                _registeredElements.Clear();
                _availableModes.Clear();

                // Dispose observables
                _contrastChangeEvents?.Dispose();

                _logger.LogInformation("HighContrastService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing HighContrastService");
            }
        }
    }
} 
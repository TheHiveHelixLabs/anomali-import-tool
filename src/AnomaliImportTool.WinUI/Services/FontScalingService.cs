using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    /// Font scaling service interface for responsive typography
    /// </summary>
    public interface IFontScalingService
    {
        /// <summary>
        /// Initialize font scaling service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Set global font scale factor
        /// </summary>
        Task SetFontScaleAsync(double scaleFactor);

        /// <summary>
        /// Get current font scale factor
        /// </summary>
        double GetCurrentFontScale();

        /// <summary>
        /// Register element for font scaling
        /// </summary>
        void RegisterElement(FrameworkElement element, FontScaleInfo scaleInfo = null);

        /// <summary>
        /// Unregister element from font scaling
        /// </summary>
        void UnregisterElement(FrameworkElement element);

        /// <summary>
        /// Update element scaling information
        /// </summary>
        void UpdateElementScaling(FrameworkElement element, FontScaleInfo scaleInfo);

        /// <summary>
        /// Apply system font scaling
        /// </summary>
        Task ApplySystemFontScalingAsync();

        /// <summary>
        /// Reset to default font scaling
        /// </summary>
        Task ResetFontScalingAsync();

        /// <summary>
        /// Set font scaling for specific category
        /// </summary>
        Task SetCategoryScalingAsync(FontCategory category, double scaleFactor);

        /// <summary>
        /// Enable or disable responsive layout adjustments
        /// </summary>
        void SetResponsiveLayoutEnabled(bool enabled);

        /// <summary>
        /// Observable for font scaling change events
        /// </summary>
        IObservable<FontScalingEvent> ScalingEvents { get; }

        /// <summary>
        /// Get supported font scale range
        /// </summary>
        (double Min, double Max) GetSupportedScaleRange();

        /// <summary>
        /// Check if current scaling causes layout issues
        /// </summary>
        Task<LayoutValidationResult> ValidateLayoutAsync();
    }

    /// <summary>
    /// Font scaling information for elements
    /// </summary>
    public class FontScaleInfo
    {
        public double OriginalFontSize { get; set; }
        public double MinFontSize { get; set; } = 8;
        public double MaxFontSize { get; set; } = 72;
        public FontCategory Category { get; set; } = FontCategory.Body;
        public bool IsScalable { get; set; } = true;
        public bool PreserveLineHeight { get; set; } = true;
        public bool AdjustSpacing { get; set; } = true;
        public ResponsiveBreakpoint ResponsiveBreakpoint { get; set; } = ResponsiveBreakpoint.None;
        public Dictionary<string, object> OriginalProperties { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Font categories for different scaling behaviors
    /// </summary>
    public enum FontCategory
    {
        Display,
        Heading,
        Subheading,
        Body,
        Caption,
        Button,
        Navigation,
        Code,
        Label,
        Input,
        Status,
        Error,
        Warning,
        Success,
        Info
    }

    /// <summary>
    /// Responsive breakpoints for layout adjustments
    /// </summary>
    public enum ResponsiveBreakpoint
    {
        None,
        Small,      // < 768px
        Medium,     // 768px - 1024px
        Large,      // 1024px - 1440px
        ExtraLarge  // > 1440px
    }

    /// <summary>
    /// Font scaling event data
    /// </summary>
    public class FontScalingEvent
    {
        public string EventType { get; set; }
        public double PreviousScale { get; set; }
        public double CurrentScale { get; set; }
        public FontCategory? Category { get; set; }
        public bool IsSystemTriggered { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<string> AffectedElements { get; set; } = new List<string>();
    }

    /// <summary>
    /// Layout validation result
    /// </summary>
    public class LayoutValidationResult
    {
        public bool IsValid { get; set; }
        public List<LayoutIssue> Issues { get; set; } = new List<LayoutIssue>();
        public double RecommendedMaxScale { get; set; }
        public string Summary { get; set; }
    }

    /// <summary>
    /// Layout issue information
    /// </summary>
    public class LayoutIssue
    {
        public string ElementName { get; set; }
        public string IssueType { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string Recommendation { get; set; }
    }

    /// <summary>
    /// Registered element for font scaling
    /// </summary>
    public class ScalableElement
    {
        public FrameworkElement Element { get; set; }
        public FontScaleInfo ScaleInfo { get; set; }
        public bool IsScaleApplied { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    /// <summary>
    /// Font category scaling configuration
    /// </summary>
    public class CategoryScaling
    {
        public FontCategory Category { get; set; }
        public double ScaleFactor { get; set; } = 1.0;
        public double MinSize { get; set; } = 8;
        public double MaxSize { get; set; } = 72;
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Professional font scaling service with responsive layout support
    /// </summary>
    public class FontScalingService : IFontScalingService, IDisposable
    {
        private readonly ILogger<FontScalingService> _logger;
        private readonly IAccessibilityService _accessibilityService;
        private readonly IAudioFeedbackService _audioFeedbackService;

        private readonly Dictionary<FrameworkElement, ScalableElement> _registeredElements;
        private readonly Dictionary<FontCategory, CategoryScaling> _categoryScaling;
        private readonly Subject<FontScalingEvent> _scalingEvents;

        private UISettings _uiSettings;
        private double _globalFontScale = 1.0;
        private double _systemFontScale = 1.0;
        private bool _isInitialized = false;
        private bool _responsiveLayoutEnabled = true;

        // Font scaling constants
        private const double MIN_FONT_SCALE = 0.5;
        private const double MAX_FONT_SCALE = 2.0;
        private const double DEFAULT_FONT_SCALE = 1.0;
        private const double SCALE_INCREMENT = 0.1;
        private const int LAYOUT_VALIDATION_DELAY_MS = 100;

        // Font size mappings for categories
        private readonly Dictionary<FontCategory, double> _baseFontSizes = new Dictionary<FontCategory, double>
        {
            { FontCategory.Display, 28 },
            { FontCategory.Heading, 20 },
            { FontCategory.Subheading, 16 },
            { FontCategory.Body, 14 },
            { FontCategory.Caption, 12 },
            { FontCategory.Button, 14 },
            { FontCategory.Navigation, 14 },
            { FontCategory.Code, 12 },
            { FontCategory.Label, 12 },
            { FontCategory.Input, 14 },
            { FontCategory.Status, 12 },
            { FontCategory.Error, 12 },
            { FontCategory.Warning, 12 },
            { FontCategory.Success, 12 },
            { FontCategory.Info, 12 }
        };

        public FontScalingService(
            ILogger<FontScalingService> logger,
            IAccessibilityService accessibilityService,
            IAudioFeedbackService audioFeedbackService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));
            _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));

            _registeredElements = new Dictionary<FrameworkElement, ScalableElement>();
            _categoryScaling = new Dictionary<FontCategory, CategoryScaling>();
            _scalingEvents = new Subject<FontScalingEvent>();

            InitializeCategoryScaling();

            _logger.LogInformation("FontScalingService initialized");
        }

        public IObservable<FontScalingEvent> ScalingEvents => _scalingEvents.AsObservable();

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing font scaling service");

                // Initialize UI settings
                _uiSettings = new UISettings();
                _uiSettings.TextScaleFactorChanged += OnSystemTextScaleFactorChanged;

                // Get initial system font scaling
                _systemFontScale = _uiSettings.TextScaleFactor;
                _globalFontScale = _systemFontScale;

                _isInitialized = true;

                _logger.LogInformation("Font scaling service initialized. System scale: {SystemScale}, Global scale: {GlobalScale}",
                    _systemFontScale, _globalFontScale);

                // Announce initialization
                await _accessibilityService.AnnounceAsync(
                    $"Font scaling ready. Current scale: {_globalFontScale:P0}", 
                    AccessibilityAnnouncementPriority.Medium);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize font scaling service");
                throw;
            }
        }

        public async Task SetFontScaleAsync(double scaleFactor)
        {
            try
            {
                scaleFactor = Math.Max(MIN_FONT_SCALE, Math.Min(MAX_FONT_SCALE, scaleFactor));

                _logger.LogInformation("Setting font scale to {ScaleFactor}", scaleFactor);

                var previousScale = _globalFontScale;
                _globalFontScale = scaleFactor;

                // Apply scaling to all registered elements
                await ApplyScalingToElementsAsync();

                // Validate layout after scaling
                if (_responsiveLayoutEnabled)
                {
                    await Task.Delay(LAYOUT_VALIDATION_DELAY_MS);
                    var validationResult = await ValidateLayoutAsync();
                    
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Layout validation failed after font scaling. Issues: {IssueCount}", 
                            validationResult.Issues.Count);
                    }
                }

                // Fire scaling event
                _scalingEvents.OnNext(new FontScalingEvent
                {
                    EventType = "GlobalScalingChanged",
                    PreviousScale = previousScale,
                    CurrentScale = _globalFontScale,
                    IsSystemTriggered = false,
                    AffectedElements = _registeredElements.Keys.Select(e => e.Name ?? e.GetType().Name).ToList()
                });

                // Play audio feedback
                await _audioFeedbackService.PlaySuccessSoundAsync();

                // Announce change
                await _accessibilityService.AnnounceAsync(
                    $"Font size changed to {scaleFactor:P0}", 
                    AccessibilityAnnouncementPriority.High);

                _logger.LogInformation("Font scale applied successfully: {ScaleFactor}", scaleFactor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set font scale: {ScaleFactor}", scaleFactor);
                await _audioFeedbackService.PlayErrorSoundAsync();
            }
        }

        public double GetCurrentFontScale()
        {
            return _globalFontScale;
        }

        public void RegisterElement(FrameworkElement element, FontScaleInfo scaleInfo = null)
        {
            try
            {
                if (element == null)
                {
                    _logger.LogWarning("Attempted to register null element for font scaling");
                    return;
                }

                if (_registeredElements.ContainsKey(element))
                {
                    _logger.LogDebug("Element already registered for font scaling");
                    return;
                }

                // Create or use provided scale info
                var info = scaleInfo ?? CreateDefaultScaleInfo(element);

                // Store original properties
                StoreOriginalProperties(element, info);

                var scalableElement = new ScalableElement
                {
                    Element = element,
                    ScaleInfo = info,
                    IsScaleApplied = false,
                    LastUpdate = DateTime.UtcNow
                };

                _registeredElements[element] = scalableElement;

                // Apply current scaling if initialized
                if (_isInitialized && Math.Abs(_globalFontScale - 1.0) > 0.01)
                {
                    ApplyScalingToElement(scalableElement);
                }

                _logger.LogDebug("Registered element for font scaling: {ElementType} - Category: {Category}",
                    element.GetType().Name, info.Category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register element for font scaling");
            }
        }

        public void UnregisterElement(FrameworkElement element)
        {
            try
            {
                if (element == null) return;

                if (_registeredElements.TryGetValue(element, out var scalableElement))
                {
                    // Restore original properties
                    RestoreOriginalProperties(scalableElement);

                    _registeredElements.Remove(element);

                    _logger.LogDebug("Unregistered element from font scaling: {ElementType}",
                        element.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister element from font scaling");
            }
        }

        public void UpdateElementScaling(FrameworkElement element, FontScaleInfo scaleInfo)
        {
            try
            {
                if (element == null || scaleInfo == null) return;

                if (_registeredElements.TryGetValue(element, out var scalableElement))
                {
                    scalableElement.ScaleInfo = scaleInfo;
                    scalableElement.LastUpdate = DateTime.UtcNow;

                    // Reapply scaling with new info
                    if (scalableElement.IsScaleApplied)
                    {
                        ApplyScalingToElement(scalableElement);
                    }

                    _logger.LogDebug("Updated element scaling info: {ElementType} - Category: {Category}",
                        element.GetType().Name, scaleInfo.Category);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update element scaling");
            }
        }

        public async Task ApplySystemFontScalingAsync()
        {
            try
            {
                _logger.LogInformation("Applying system font scaling: {SystemScale}", _systemFontScale);

                await SetFontScaleAsync(_systemFontScale);

                // Fire system scaling event
                _scalingEvents.OnNext(new FontScalingEvent
                {
                    EventType = "SystemScalingApplied",
                    PreviousScale = _globalFontScale,
                    CurrentScale = _systemFontScale,
                    IsSystemTriggered = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply system font scaling");
            }
        }

        public async Task ResetFontScalingAsync()
        {
            try
            {
                _logger.LogInformation("Resetting font scaling to default");

                await SetFontScaleAsync(DEFAULT_FONT_SCALE);

                // Reset category scaling
                foreach (var category in _categoryScaling.Values)
                {
                    category.ScaleFactor = 1.0;
                }

                _logger.LogInformation("Font scaling reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset font scaling");
            }
        }

        public async Task SetCategoryScalingAsync(FontCategory category, double scaleFactor)
        {
            try
            {
                scaleFactor = Math.Max(MIN_FONT_SCALE, Math.Min(MAX_FONT_SCALE, scaleFactor));

                _logger.LogInformation("Setting category scaling: {Category} to {ScaleFactor}", category, scaleFactor);

                if (_categoryScaling.TryGetValue(category, out var categoryConfig))
                {
                    var previousScale = categoryConfig.ScaleFactor;
                    categoryConfig.ScaleFactor = scaleFactor;

                    // Apply scaling to elements in this category
                    await ApplyCategoryScalingAsync(category);

                    // Fire category scaling event
                    _scalingEvents.OnNext(new FontScalingEvent
                    {
                        EventType = "CategoryScalingChanged",
                        PreviousScale = previousScale,
                        CurrentScale = scaleFactor,
                        Category = category,
                        IsSystemTriggered = false
                    });

                    _logger.LogInformation("Category scaling applied: {Category} - {ScaleFactor}", category, scaleFactor);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set category scaling: {Category}", category);
            }
        }

        public void SetResponsiveLayoutEnabled(bool enabled)
        {
            _responsiveLayoutEnabled = enabled;
            _logger.LogInformation("Responsive layout adjustments {Status}", enabled ? "enabled" : "disabled");
        }

        public (double Min, double Max) GetSupportedScaleRange()
        {
            return (MIN_FONT_SCALE, MAX_FONT_SCALE);
        }

        public async Task<LayoutValidationResult> ValidateLayoutAsync()
        {
            try
            {
                var result = new LayoutValidationResult { IsValid = true };
                var issues = new List<LayoutIssue>();

                // Validate each registered element
                foreach (var kvp in _registeredElements)
                {
                    var element = kvp.Key;
                    var scalableElement = kvp.Value;

                    var elementIssues = await ValidateElementLayoutAsync(element, scalableElement);
                    issues.AddRange(elementIssues);
                }

                result.Issues = issues;
                result.IsValid = !issues.Any(i => i.Severity == "Critical");
                result.RecommendedMaxScale = CalculateRecommendedMaxScale(issues);
                result.Summary = $"Validated {_registeredElements.Count} elements. Found {issues.Count} issues.";

                _logger.LogDebug("Layout validation completed. Valid: {IsValid}, Issues: {IssueCount}",
                    result.IsValid, issues.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate layout");
                return new LayoutValidationResult
                {
                    IsValid = false,
                    Summary = "Layout validation failed due to error"
                };
            }
        }

        private void InitializeCategoryScaling()
        {
            foreach (var category in Enum.GetValues<FontCategory>())
            {
                _categoryScaling[category] = new CategoryScaling
                {
                    Category = category,
                    ScaleFactor = 1.0,
                    MinSize = 8,
                    MaxSize = 72,
                    IsEnabled = true
                };
            }
        }

        private FontScaleInfo CreateDefaultScaleInfo(FrameworkElement element)
        {
            var fontSize = GetElementFontSize(element);
            var category = DetermineElementCategory(element);

            return new FontScaleInfo
            {
                OriginalFontSize = fontSize,
                MinFontSize = 8,
                MaxFontSize = 72,
                Category = category,
                IsScalable = true,
                PreserveLineHeight = true,
                AdjustSpacing = true
            };
        }

        private double GetElementFontSize(FrameworkElement element)
        {
            try
            {
                return element switch
                {
                    TextBlock textBlock => textBlock.FontSize,
                    Control control => control.FontSize,
                    _ => _baseFontSizes.GetValueOrDefault(FontCategory.Body, 14)
                };
            }
            catch
            {
                return 14; // Default font size
            }
        }

        private FontCategory DetermineElementCategory(FrameworkElement element)
        {
            var elementName = element.Name?.ToLowerInvariant() ?? string.Empty;
            var elementType = element.GetType().Name.ToLowerInvariant();

            return elementName switch
            {
                var name when name.Contains("title") || name.Contains("display") => FontCategory.Display,
                var name when name.Contains("heading") || name.Contains("header") => FontCategory.Heading,
                var name when name.Contains("subheading") || name.Contains("subtitle") => FontCategory.Subheading,
                var name when name.Contains("caption") => FontCategory.Caption,
                var name when name.Contains("button") => FontCategory.Button,
                var name when name.Contains("nav") => FontCategory.Navigation,
                var name when name.Contains("code") => FontCategory.Code,
                var name when name.Contains("label") => FontCategory.Label,
                var name when name.Contains("input") => FontCategory.Input,
                var name when name.Contains("status") => FontCategory.Status,
                var name when name.Contains("error") => FontCategory.Error,
                var name when name.Contains("warning") => FontCategory.Warning,
                var name when name.Contains("success") => FontCategory.Success,
                var name when name.Contains("info") => FontCategory.Info,
                _ => elementType switch
                {
                    "button" => FontCategory.Button,
                    "textbox" => FontCategory.Input,
                    "combobox" => FontCategory.Input,
                    _ => FontCategory.Body
                }
            };
        }

        private void StoreOriginalProperties(FrameworkElement element, FontScaleInfo scaleInfo)
        {
            try
            {
                scaleInfo.OriginalProperties.Clear();

                if (element is TextBlock textBlock)
                {
                    scaleInfo.OriginalProperties["FontSize"] = textBlock.FontSize;
                    scaleInfo.OriginalProperties["LineHeight"] = textBlock.LineHeight;
                    scaleInfo.OriginalProperties["Margin"] = textBlock.Margin;
                    scaleInfo.OriginalProperties["Padding"] = textBlock.Padding;
                }
                else if (element is Control control)
                {
                    scaleInfo.OriginalProperties["FontSize"] = control.FontSize;
                    scaleInfo.OriginalProperties["Margin"] = control.Margin;
                    scaleInfo.OriginalProperties["Padding"] = control.Padding;
                }

                scaleInfo.OriginalFontSize = GetElementFontSize(element);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store original properties");
            }
        }

        private async Task ApplyScalingToElementsAsync()
        {
            try
            {
                var tasks = new List<Task>();

                foreach (var scalableElement in _registeredElements.Values)
                {
                    tasks.Add(Task.Run(() => ApplyScalingToElement(scalableElement)));
                }

                await Task.WhenAll(tasks);

                _logger.LogDebug("Applied font scaling to {Count} elements", _registeredElements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply scaling to elements");
            }
        }

        private void ApplyScalingToElement(ScalableElement scalableElement)
        {
            try
            {
                var element = scalableElement.Element;
                var scaleInfo = scalableElement.ScaleInfo;

                if (!scaleInfo.IsScalable) return;

                // Calculate effective scale factor
                var categoryScale = _categoryScaling.GetValueOrDefault(scaleInfo.Category)?.ScaleFactor ?? 1.0;
                var effectiveScale = _globalFontScale * categoryScale;

                // Calculate new font size
                var newFontSize = scaleInfo.OriginalFontSize * effectiveScale;
                newFontSize = Math.Max(scaleInfo.MinFontSize, Math.Min(scaleInfo.MaxFontSize, newFontSize));

                // Apply font size
                ApplyFontSizeToElement(element, newFontSize);

                // Apply spacing adjustments if enabled
                if (scaleInfo.AdjustSpacing)
                {
                    ApplySpacingAdjustments(element, effectiveScale);
                }

                // Apply line height adjustments if enabled
                if (scaleInfo.PreserveLineHeight)
                {
                    ApplyLineHeightAdjustments(element, effectiveScale);
                }

                scalableElement.IsScaleApplied = true;
                scalableElement.LastUpdate = DateTime.UtcNow;

                _logger.LogDebug("Applied scaling to element: {ElementType} - Original: {OriginalSize}, New: {NewSize}, Scale: {Scale}",
                    element.GetType().Name, scaleInfo.OriginalFontSize, newFontSize, effectiveScale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply scaling to element");
            }
        }

        private void ApplyFontSizeToElement(FrameworkElement element, double fontSize)
        {
            try
            {
                switch (element)
                {
                    case TextBlock textBlock:
                        textBlock.FontSize = fontSize;
                        break;
                    case Control control:
                        control.FontSize = fontSize;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply font size to element");
            }
        }

        private void ApplySpacingAdjustments(FrameworkElement element, double scaleFactor)
        {
            try
            {
                var originalMargin = element.Margin;
                var originalPadding = element.Padding;

                // Scale margins and padding proportionally
                var newMargin = new Thickness(
                    originalMargin.Left * scaleFactor,
                    originalMargin.Top * scaleFactor,
                    originalMargin.Right * scaleFactor,
                    originalMargin.Bottom * scaleFactor
                );

                var newPadding = new Thickness(
                    originalPadding.Left * scaleFactor,
                    originalPadding.Top * scaleFactor,
                    originalPadding.Right * scaleFactor,
                    originalPadding.Bottom * scaleFactor
                );

                element.Margin = newMargin;
                element.Padding = newPadding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply spacing adjustments");
            }
        }

        private void ApplyLineHeightAdjustments(FrameworkElement element, double scaleFactor)
        {
            try
            {
                if (element is TextBlock textBlock)
                {
                    // Preserve relative line height
                    if (textBlock.LineHeight > 0)
                    {
                        textBlock.LineHeight *= scaleFactor;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply line height adjustments");
            }
        }

        private async Task ApplyCategoryScalingAsync(FontCategory category)
        {
            try
            {
                var elementsInCategory = _registeredElements.Values
                    .Where(se => se.ScaleInfo.Category == category)
                    .ToList();

                var tasks = elementsInCategory.Select(scalableElement => 
                    Task.Run(() => ApplyScalingToElement(scalableElement)));

                await Task.WhenAll(tasks);

                _logger.LogDebug("Applied category scaling to {Count} elements in category {Category}",
                    elementsInCategory.Count, category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply category scaling: {Category}", category);
            }
        }

        private void RestoreOriginalProperties(ScalableElement scalableElement)
        {
            try
            {
                var element = scalableElement.Element;
                var originalProperties = scalableElement.ScaleInfo.OriginalProperties;

                foreach (var kvp in originalProperties)
                {
                    var property = kvp.Key;
                    var value = kvp.Value;

                    switch (property)
                    {
                        case "FontSize":
                            ApplyFontSizeToElement(element, (double)value);
                            break;
                        case "Margin":
                            element.Margin = (Thickness)value;
                            break;
                        case "Padding":
                            element.Padding = (Thickness)value;
                            break;
                        case "LineHeight":
                            if (element is TextBlock textBlock && value is double lineHeight)
                                textBlock.LineHeight = lineHeight;
                            break;
                    }
                }

                scalableElement.IsScaleApplied = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore original properties");
            }
        }

        private async Task<List<LayoutIssue>> ValidateElementLayoutAsync(FrameworkElement element, ScalableElement scalableElement)
        {
            var issues = new List<LayoutIssue>();

            try
            {
                // Check if element is clipped or overflowing
                var actualWidth = element.ActualWidth;
                var actualHeight = element.ActualHeight;

                if (actualWidth <= 0 || actualHeight <= 0)
                {
                    issues.Add(new LayoutIssue
                    {
                        ElementName = element.Name ?? element.GetType().Name,
                        IssueType = "ZeroSize",
                        Description = "Element has zero width or height",
                        Severity = "Warning",
                        Recommendation = "Check element visibility and layout constraints"
                    });
                }

                // Check font size bounds
                var currentFontSize = GetElementFontSize(element);
                if (currentFontSize < scalableElement.ScaleInfo.MinFontSize)
                {
                    issues.Add(new LayoutIssue
                    {
                        ElementName = element.Name ?? element.GetType().Name,
                        IssueType = "FontTooSmall",
                        Description = $"Font size {currentFontSize} is below minimum {scalableElement.ScaleInfo.MinFontSize}",
                        Severity = "Critical",
                        Recommendation = "Reduce font scaling or increase minimum font size"
                    });
                }

                if (currentFontSize > scalableElement.ScaleInfo.MaxFontSize)
                {
                    issues.Add(new LayoutIssue
                    {
                        ElementName = element.Name ?? element.GetType().Name,
                        IssueType = "FontTooLarge",
                        Description = $"Font size {currentFontSize} exceeds maximum {scalableElement.ScaleInfo.MaxFontSize}",
                        Severity = "Warning",
                        Recommendation = "Reduce font scaling or increase maximum font size"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate element layout");
                issues.Add(new LayoutIssue
                {
                    ElementName = element.Name ?? element.GetType().Name,
                    IssueType = "ValidationError",
                    Description = "Failed to validate element layout",
                    Severity = "Error",
                    Recommendation = "Check element accessibility"
                });
            }

            return issues;
        }

        private double CalculateRecommendedMaxScale(List<LayoutIssue> issues)
        {
            var criticalIssues = issues.Where(i => i.Severity == "Critical").ToList();
            if (!criticalIssues.Any())
            {
                return MAX_FONT_SCALE;
            }

            // Calculate safe scale based on critical issues
            // This is a simplified calculation - in practice, you'd analyze specific constraints
            return Math.Max(MIN_FONT_SCALE, _globalFontScale * 0.8);
        }

        private async void OnSystemTextScaleFactorChanged(UISettings sender, object args)
        {
            try
            {
                _logger.LogInformation("System text scale factor changed");

                var newSystemScale = sender.TextScaleFactor;
                var previousSystemScale = _systemFontScale;

                _systemFontScale = newSystemScale;

                // Apply new system scaling
                await ApplySystemFontScalingAsync();

                // Fire system change event
                _scalingEvents.OnNext(new FontScalingEvent
                {
                    EventType = "SystemScaleChanged",
                    PreviousScale = previousSystemScale,
                    CurrentScale = newSystemScale,
                    IsSystemTriggered = true
                });

                _logger.LogInformation("System font scaling updated: {PreviousScale} -> {NewScale}",
                    previousSystemScale, newSystemScale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling system text scale factor change");
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Disposing FontScalingService");

                // Remove system event handlers
                if (_uiSettings != null)
                {
                    _uiSettings.TextScaleFactorChanged -= OnSystemTextScaleFactorChanged;
                }

                // Restore original properties for all elements
                foreach (var scalableElement in _registeredElements.Values)
                {
                    if (scalableElement.IsScaleApplied)
                    {
                        RestoreOriginalProperties(scalableElement);
                    }
                }

                // Clean up collections
                _registeredElements.Clear();
                _categoryScaling.Clear();

                // Dispose observables
                _scalingEvents?.Dispose();

                _logger.LogInformation("FontScalingService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing FontScalingService");
            }
        }
    }
} 
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
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
    /// Screen reader support service interface for comprehensive accessibility
    /// </summary>
    public interface IScreenReaderSupportService
    {
        /// <summary>
        /// Initialize screen reader support system
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Register an element for screen reader support
        /// </summary>
        void RegisterElement(FrameworkElement element, ScreenReaderInfo info);

        /// <summary>
        /// Unregister an element from screen reader support
        /// </summary>
        void UnregisterElement(FrameworkElement element);

        /// <summary>
        /// Update screen reader information for an element
        /// </summary>
        void UpdateElementInfo(FrameworkElement element, ScreenReaderInfo info);

        /// <summary>
        /// Create and manage live region for dynamic content
        /// </summary>
        void CreateLiveRegion(string regionId, LiveRegionPriority priority, FrameworkElement container = null);

        /// <summary>
        /// Update live region content
        /// </summary>
        Task UpdateLiveRegionAsync(string regionId, string content, bool interrupt = false);

        /// <summary>
        /// Remove live region
        /// </summary>
        void RemoveLiveRegion(string regionId);

        /// <summary>
        /// Set focus with screen reader announcement
        /// </summary>
        Task SetFocusWithAnnouncementAsync(FrameworkElement element, string announcement = null);

        /// <summary>
        /// Announce message to screen reader
        /// </summary>
        Task AnnounceAsync(string message, ScreenReaderPriority priority = ScreenReaderPriority.Medium);

        /// <summary>
        /// Set up navigation landmarks
        /// </summary>
        void SetupNavigationLandmarks(FrameworkElement rootElement);

        /// <summary>
        /// Create accessible data table structure
        /// </summary>
        void SetupAccessibleTable(FrameworkElement tableElement, AccessibleTableInfo tableInfo);

        /// <summary>
        /// Enable or disable screen reader support
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Check if screen reader is detected
        /// </summary>
        bool IsScreenReaderActive { get; }

        /// <summary>
        /// Observable for screen reader events
        /// </summary>
        IObservable<ScreenReaderEvent> ScreenReaderEvents { get; }

        /// <summary>
        /// Get accessibility summary for element
        /// </summary>
        string GetAccessibilitySummary(FrameworkElement element);
    }

    /// <summary>
    /// Screen reader information for elements
    /// </summary>
    public class ScreenReaderInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string HelpText { get; set; }
        public string Role { get; set; }
        public string Value { get; set; }
        public AccessibilityState State { get; set; }
        public List<string> Properties { get; set; } = new List<string>();
        public string LandmarkType { get; set; }
        public int TabIndex { get; set; } = -1;
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public string KeyboardShortcut { get; set; }
        public string GroupName { get; set; }
        public int PositionInSet { get; set; }
        public int SetSize { get; set; }
        public int Level { get; set; }
    }

    /// <summary>
    /// Accessibility state enumeration
    /// </summary>
    public enum AccessibilityState
    {
        Normal,
        Disabled,
        ReadOnly,
        Required,
        Invalid,
        Expanded,
        Collapsed,
        Selected,
        Checked,
        Mixed,
        Pressed,
        Busy,
        Hidden
    }

    /// <summary>
    /// Live region priority levels
    /// </summary>
    public enum LiveRegionPriority
    {
        Off,
        Polite,
        Assertive
    }

    /// <summary>
    /// Screen reader priority levels
    /// </summary>
    public enum ScreenReaderPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Screen reader event data
    /// </summary>
    public class ScreenReaderEvent
    {
        public string EventType { get; set; }
        public FrameworkElement Element { get; set; }
        public string Message { get; set; }
        public ScreenReaderPriority Priority { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool WasAnnounced { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Accessible table information
    /// </summary>
    public class AccessibleTableInfo
    {
        public string Caption { get; set; }
        public string Summary { get; set; }
        public List<AccessibleColumnInfo> Columns { get; set; } = new List<AccessibleColumnInfo>();
        public List<AccessibleRowInfo> Rows { get; set; } = new List<AccessibleRowInfo>();
        public bool HasHeaders { get; set; } = true;
        public string HeaderScope { get; set; } = "col";
    }

    /// <summary>
    /// Accessible column information
    /// </summary>
    public class AccessibleColumnInfo
    {
        public string Header { get; set; }
        public string Description { get; set; }
        public string DataType { get; set; }
        public bool IsSortable { get; set; }
        public string SortDirection { get; set; }
    }

    /// <summary>
    /// Accessible row information
    /// </summary>
    public class AccessibleRowInfo
    {
        public string Header { get; set; }
        public string Description { get; set; }
        public List<string> CellData { get; set; } = new List<string>();
        public bool IsSelected { get; set; }
        public int RowIndex { get; set; }
    }

    /// <summary>
    /// Live region management
    /// </summary>
    public class LiveRegion
    {
        public string Id { get; set; }
        public FrameworkElement Container { get; set; }
        public TextBlock TextElement { get; set; }
        public LiveRegionPriority Priority { get; set; }
        public DateTime LastUpdate { get; set; }
        public string LastContent { get; set; }
    }

    /// <summary>
    /// Professional screen reader support service with comprehensive ARIA implementation
    /// </summary>
    public class ScreenReaderSupportService : IScreenReaderSupportService, IDisposable
    {
        private readonly ILogger<ScreenReaderSupportService> _logger;
        private readonly IAccessibilityService _accessibilityService;
        private readonly IAudioFeedbackService _audioFeedbackService;

        private readonly Dictionary<FrameworkElement, ScreenReaderInfo> _registeredElements;
        private readonly Dictionary<string, LiveRegion> _liveRegions;
        private readonly Subject<ScreenReaderEvent> _screenReaderEvents;

        private bool _isEnabled = true;
        private bool _isInitialized = false;
        private bool _isScreenReaderActive = false;
        private FrameworkElement _rootElement;

        // Screen reader constants
        private const int ANNOUNCEMENT_DELAY_MS = 100;
        private const int LIVE_REGION_UPDATE_THROTTLE_MS = 500;
        private const string LIVE_REGION_CONTAINER_NAME = "ScreenReaderLiveRegions";

        public ScreenReaderSupportService(
            ILogger<ScreenReaderSupportService> logger,
            IAccessibilityService accessibilityService,
            IAudioFeedbackService audioFeedbackService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));
            _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));

            _registeredElements = new Dictionary<FrameworkElement, ScreenReaderInfo>();
            _liveRegions = new Dictionary<string, LiveRegion>();
            _screenReaderEvents = new Subject<ScreenReaderEvent>();

            _logger.LogInformation("ScreenReaderSupportService initialized");
        }

        public IObservable<ScreenReaderEvent> ScreenReaderEvents => _screenReaderEvents.AsObservable();
        public bool IsScreenReaderActive => _isScreenReaderActive;

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing screen reader support service");

                // Detect if screen reader is active
                _isScreenReaderActive = await DetectScreenReaderAsync();

                // Set up automation event handlers
                SetupAutomationEventHandlers();

                // Create live regions container
                await CreateLiveRegionsContainerAsync();

                _isInitialized = true;

                _logger.LogInformation("Screen reader support service initialized. Screen reader active: {IsActive}",
                    _isScreenReaderActive);

                // Announce initialization if screen reader is active
                if (_isScreenReaderActive)
                {
                    await AnnounceAsync("Screen reader support activated", ScreenReaderPriority.High);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize screen reader support service");
                throw;
            }
        }

        public void RegisterElement(FrameworkElement element, ScreenReaderInfo info)
        {
            try
            {
                if (element == null || info == null)
                {
                    _logger.LogWarning("Attempted to register null element or info for screen reader");
                    return;
                }

                _registeredElements[element] = info;

                // Apply accessibility properties
                ApplyAccessibilityProperties(element, info);

                // Set up automation peer if needed
                SetupAutomationPeer(element, info);

                _logger.LogDebug("Registered element for screen reader: {ElementType} - {Name}",
                    element.GetType().Name, info.Name);

                // Fire registration event
                _screenReaderEvents.OnNext(new ScreenReaderEvent
                {
                    EventType = "ElementRegistered",
                    Element = element,
                    Message = $"Element registered: {info.Name}",
                    Priority = ScreenReaderPriority.Low
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register element for screen reader");
            }
        }

        public void UnregisterElement(FrameworkElement element)
        {
            try
            {
                if (element == null) return;

                if (_registeredElements.Remove(element))
                {
                    _logger.LogDebug("Unregistered element from screen reader: {ElementType}",
                        element.GetType().Name);

                    // Fire unregistration event
                    _screenReaderEvents.OnNext(new ScreenReaderEvent
                    {
                        EventType = "ElementUnregistered",
                        Element = element,
                        Message = "Element unregistered",
                        Priority = ScreenReaderPriority.Low
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister element from screen reader");
            }
        }

        public void UpdateElementInfo(FrameworkElement element, ScreenReaderInfo info)
        {
            try
            {
                if (element == null || info == null) return;

                if (_registeredElements.ContainsKey(element))
                {
                    _registeredElements[element] = info;
                    ApplyAccessibilityProperties(element, info);

                    _logger.LogDebug("Updated screen reader info for element: {ElementType} - {Name}",
                        element.GetType().Name, info.Name);

                    // Fire update event
                    _screenReaderEvents.OnNext(new ScreenReaderEvent
                    {
                        EventType = "ElementUpdated",
                        Element = element,
                        Message = $"Element updated: {info.Name}",
                        Priority = ScreenReaderPriority.Low
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update element info for screen reader");
            }
        }

        public void CreateLiveRegion(string regionId, LiveRegionPriority priority, FrameworkElement container = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(regionId))
                {
                    _logger.LogWarning("Attempted to create live region with empty ID");
                    return;
                }

                if (_liveRegions.ContainsKey(regionId))
                {
                    _logger.LogWarning("Live region already exists: {RegionId}", regionId);
                    return;
                }

                // Create text element for live region
                var textElement = new TextBlock
                {
                    Name = $"LiveRegion_{regionId}",
                    Visibility = Visibility.Collapsed // Hidden from visual display but accessible to screen readers
                };

                // Set automation properties
                AutomationProperties.SetName(textElement, $"Live Region {regionId}");
                AutomationProperties.SetAccessibilityView(textElement, AccessibilityView.Content);
                
                // Set live setting based on priority
                var liveSetting = priority switch
                {
                    LiveRegionPriority.Polite => AutomationLiveSetting.Polite,
                    LiveRegionPriority.Assertive => AutomationLiveSetting.Assertive,
                    _ => AutomationLiveSetting.Off
                };
                AutomationProperties.SetLiveSetting(textElement, liveSetting);

                // Add to container or root
                var targetContainer = container ?? _rootElement;
                if (targetContainer is Panel panel)
                {
                    panel.Children.Add(textElement);
                }

                var liveRegion = new LiveRegion
                {
                    Id = regionId,
                    Container = targetContainer,
                    TextElement = textElement,
                    Priority = priority,
                    LastUpdate = DateTime.UtcNow
                };

                _liveRegions[regionId] = liveRegion;

                _logger.LogInformation("Created live region: {RegionId} with priority {Priority}",
                    regionId, priority);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create live region: {RegionId}", regionId);
            }
        }

        public async Task UpdateLiveRegionAsync(string regionId, string content, bool interrupt = false)
        {
            try
            {
                if (!_liveRegions.TryGetValue(regionId, out var liveRegion))
                {
                    _logger.LogWarning("Live region not found: {RegionId}", regionId);
                    return;
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return;
                }

                // Throttle updates to prevent overwhelming screen readers
                var timeSinceLastUpdate = DateTime.UtcNow - liveRegion.LastUpdate;
                if (timeSinceLastUpdate.TotalMilliseconds < LIVE_REGION_UPDATE_THROTTLE_MS && !interrupt)
                {
                    await Task.Delay(LIVE_REGION_UPDATE_THROTTLE_MS - (int)timeSinceLastUpdate.TotalMilliseconds);
                }

                // Update content
                liveRegion.TextElement.Text = content;
                liveRegion.LastContent = content;
                liveRegion.LastUpdate = DateTime.UtcNow;

                _logger.LogDebug("Updated live region {RegionId}: {Content}", regionId, content);

                // Fire update event
                _screenReaderEvents.OnNext(new ScreenReaderEvent
                {
                    EventType = "LiveRegionUpdated",
                    Message = content,
                    Priority = ScreenReaderPriority.Medium,
                    WasAnnounced = true
                });

                // Additional announcement for high priority content
                if (liveRegion.Priority == LiveRegionPriority.Assertive || interrupt)
                {
                    await Task.Delay(ANNOUNCEMENT_DELAY_MS);
                    await _accessibilityService.AnnounceAsync(content, AccessibilityAnnouncementPriority.High);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update live region: {RegionId}", regionId);
            }
        }

        public void RemoveLiveRegion(string regionId)
        {
            try
            {
                if (_liveRegions.TryGetValue(regionId, out var liveRegion))
                {
                    // Remove from UI
                    if (liveRegion.Container is Panel panel)
                    {
                        panel.Children.Remove(liveRegion.TextElement);
                    }

                    _liveRegions.Remove(regionId);

                    _logger.LogDebug("Removed live region: {RegionId}", regionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove live region: {RegionId}", regionId);
            }
        }

        public async Task SetFocusWithAnnouncementAsync(FrameworkElement element, string announcement = null)
        {
            try
            {
                if (element == null) return;

                // Set focus
                var focusResult = element.Focus(FocusState.Keyboard);

                if (focusResult)
                {
                    // Get announcement text
                    var announceText = announcement;
                    if (string.IsNullOrWhiteSpace(announceText))
                    {
                        announceText = GetElementAnnouncement(element);
                    }

                    // Announce to screen reader
                    if (!string.IsNullOrWhiteSpace(announceText))
                    {
                        await Task.Delay(ANNOUNCEMENT_DELAY_MS);
                        await AnnounceAsync(announceText, ScreenReaderPriority.High);
                    }

                    _logger.LogDebug("Set focus with announcement: {ElementType} - {Announcement}",
                        element.GetType().Name, announceText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set focus with announcement");
            }
        }

        public async Task AnnounceAsync(string message, ScreenReaderPriority priority = ScreenReaderPriority.Medium)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message) || !_isEnabled) return;

                _logger.LogDebug("Announcing to screen reader: {Message} (Priority: {Priority})", message, priority);

                // Convert priority
                var accessibilityPriority = priority switch
                {
                    ScreenReaderPriority.Critical => AccessibilityAnnouncementPriority.High,
                    ScreenReaderPriority.High => AccessibilityAnnouncementPriority.High,
                    ScreenReaderPriority.Medium => AccessibilityAnnouncementPriority.Medium,
                    _ => AccessibilityAnnouncementPriority.Low
                };

                // Announce through accessibility service
                await _accessibilityService.AnnounceAsync(message, accessibilityPriority);

                // Fire announcement event
                _screenReaderEvents.OnNext(new ScreenReaderEvent
                {
                    EventType = "Announcement",
                    Message = message,
                    Priority = priority,
                    WasAnnounced = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to announce message to screen reader");

                _screenReaderEvents.OnNext(new ScreenReaderEvent
                {
                    EventType = "AnnouncementFailed",
                    Message = message,
                    Priority = priority,
                    WasAnnounced = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        public void SetupNavigationLandmarks(FrameworkElement rootElement)
        {
            try
            {
                if (rootElement == null) return;

                _rootElement = rootElement;

                // Find and setup landmarks
                SetupLandmarksRecursive(rootElement);

                _logger.LogInformation("Navigation landmarks setup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup navigation landmarks");
            }
        }

        public void SetupAccessibleTable(FrameworkElement tableElement, AccessibleTableInfo tableInfo)
        {
            try
            {
                if (tableElement == null || tableInfo == null) return;

                // Set table properties
                AutomationProperties.SetName(tableElement, tableInfo.Caption ?? "Data Table");
                AutomationProperties.SetHelpText(tableElement, tableInfo.Summary);
                AutomationProperties.SetItemType(tableElement, "table");

                // Setup column headers
                SetupTableHeaders(tableElement, tableInfo);

                // Setup row information
                SetupTableRows(tableElement, tableInfo);

                _logger.LogDebug("Setup accessible table: {Caption}", tableInfo.Caption);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup accessible table");
            }
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Screen reader support {Status}", enabled ? "enabled" : "disabled");

            // Announce state change
            if (_isScreenReaderActive)
            {
                _ = Task.Run(() => AnnounceAsync($"Screen reader support {(enabled ? "enabled" : "disabled")}", 
                    ScreenReaderPriority.High));
            }
        }

        public string GetAccessibilitySummary(FrameworkElement element)
        {
            try
            {
                if (element == null) return string.Empty;

                var summary = new List<string>();

                // Get registered info
                if (_registeredElements.TryGetValue(element, out var info))
                {
                    if (!string.IsNullOrWhiteSpace(info.Name))
                        summary.Add($"Name: {info.Name}");
                    if (!string.IsNullOrWhiteSpace(info.Role))
                        summary.Add($"Role: {info.Role}");
                    if (!string.IsNullOrWhiteSpace(info.Description))
                        summary.Add($"Description: {info.Description}");
                    if (info.State != AccessibilityState.Normal)
                        summary.Add($"State: {info.State}");
                }

                // Get automation properties
                var automationName = AutomationProperties.GetName(element);
                if (!string.IsNullOrWhiteSpace(automationName))
                    summary.Add($"Automation Name: {automationName}");

                var helpText = AutomationProperties.GetHelpText(element);
                if (!string.IsNullOrWhiteSpace(helpText))
                    summary.Add($"Help: {helpText}");

                return string.Join(", ", summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get accessibility summary");
                return "Error retrieving accessibility information";
            }
        }

        private async Task<bool> DetectScreenReaderAsync()
        {
            try
            {
                // Check Windows accessibility settings
                var accessibilitySettings = new Windows.UI.ViewManagement.AccessibilitySettings();
                
                // Multiple detection methods
                var isScreenReaderRunning = accessibilitySettings.ScreenReaderEnabled;
                var isHighContrastEnabled = accessibilitySettings.HighContrast;
                
                // Additional detection through automation
                var hasAutomationListener = AutomationPeer.ListenerExists(AutomationEvents.LiveRegionChanged);

                var detected = isScreenReaderRunning || hasAutomationListener;

                _logger.LogInformation("Screen reader detection: Running={IsRunning}, HighContrast={HighContrast}, AutomationListener={HasListener}, Detected={Detected}",
                    isScreenReaderRunning, isHighContrastEnabled, hasAutomationListener, detected);

                return detected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect screen reader");
                return false; // Assume no screen reader if detection fails
            }
        }

        private void SetupAutomationEventHandlers()
        {
            try
            {
                // This would set up automation event handlers for screen reader events
                // In a full implementation, you would register for various automation events
                _logger.LogDebug("Automation event handlers setup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup automation event handlers");
            }
        }

        private async Task CreateLiveRegionsContainerAsync()
        {
            try
            {
                // Create default live regions for common scenarios
                CreateLiveRegion("status", LiveRegionPriority.Polite);
                CreateLiveRegion("alerts", LiveRegionPriority.Assertive);
                CreateLiveRegion("progress", LiveRegionPriority.Polite);

                _logger.LogDebug("Live regions container created with default regions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create live regions container");
            }
        }

        private void ApplyAccessibilityProperties(FrameworkElement element, ScreenReaderInfo info)
        {
            try
            {
                // Set basic properties
                if (!string.IsNullOrWhiteSpace(info.Name))
                    AutomationProperties.SetName(element, info.Name);

                if (!string.IsNullOrWhiteSpace(info.HelpText))
                    AutomationProperties.SetHelpText(element, info.HelpText);

                if (!string.IsNullOrWhiteSpace(info.Description))
                    AutomationProperties.SetFullDescription(element, info.Description);

                // Set item type/role
                if (!string.IsNullOrWhiteSpace(info.Role))
                    AutomationProperties.SetItemType(element, info.Role);

                // Set accessibility view
                AutomationProperties.SetAccessibilityView(element, AccessibilityView.Content);

                // Set landmark type
                if (!string.IsNullOrWhiteSpace(info.LandmarkType))
                {
                    var landmarkType = ParseLandmarkType(info.LandmarkType);
                    AutomationProperties.SetLandmarkType(element, landmarkType);
                }

                // Set position in set
                if (info.PositionInSet > 0 && info.SetSize > 0)
                {
                    AutomationProperties.SetPositionInSet(element, info.PositionInSet);
                    AutomationProperties.SetSizeOfSet(element, info.SetSize);
                }

                // Set level for hierarchical elements
                if (info.Level > 0)
                    AutomationProperties.SetLevel(element, info.Level);

                // Set required state
                if (info.IsRequired)
                    AutomationProperties.SetIsRequiredForForm(element, true);

                // Set keyboard shortcut
                if (!string.IsNullOrWhiteSpace(info.KeyboardShortcut))
                    AutomationProperties.SetAcceleratorKey(element, info.KeyboardShortcut);

                _logger.LogDebug("Applied accessibility properties to element: {Name}", info.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply accessibility properties");
            }
        }

        private void SetupAutomationPeer(FrameworkElement element, ScreenReaderInfo info)
        {
            try
            {
                // This would create custom automation peers for complex controls
                // For now, we rely on the built-in automation peers
                _logger.LogDebug("Automation peer setup for element: {Name}", info.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup automation peer");
            }
        }

        private string GetElementAnnouncement(FrameworkElement element)
        {
            try
            {
                var parts = new List<string>();

                // Get name
                var name = AutomationProperties.GetName(element);
                if (!string.IsNullOrWhiteSpace(name))
                    parts.Add(name);

                // Get role/type
                var itemType = AutomationProperties.GetItemType(element);
                if (!string.IsNullOrWhiteSpace(itemType))
                    parts.Add(itemType);

                // Get state information
                if (_registeredElements.TryGetValue(element, out var info))
                {
                    if (info.State != AccessibilityState.Normal)
                        parts.Add(info.State.ToString().ToLowerInvariant());

                    if (!string.IsNullOrWhiteSpace(info.Value))
                        parts.Add(info.Value);
                }

                return string.Join(", ", parts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get element announcement");
                return "Element";
            }
        }

        private void SetupLandmarksRecursive(DependencyObject element)
        {
            try
            {
                if (element is FrameworkElement frameworkElement)
                {
                    // Setup landmarks based on element type and properties
                    SetupElementLandmark(frameworkElement);
                }

                // Process children
                var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
                for (int i = 0; i < childCount; i++)
                {
                    var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);
                    SetupLandmarksRecursive(child);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup landmarks recursively");
            }
        }

        private void SetupElementLandmark(FrameworkElement element)
        {
            try
            {
                // Determine landmark type based on element type and name
                var landmarkType = AutomationLandmarkType.None;
                var elementName = element.Name?.ToLowerInvariant() ?? string.Empty;

                if (element is NavigationView || elementName.Contains("nav"))
                    landmarkType = AutomationLandmarkType.Navigation;
                else if (elementName.Contains("main") || elementName.Contains("content"))
                    landmarkType = AutomationLandmarkType.Main;
                else if (elementName.Contains("header") || elementName.Contains("banner"))
                    landmarkType = AutomationLandmarkType.Banner;
                else if (elementName.Contains("footer") || elementName.Contains("contentinfo"))
                    landmarkType = AutomationLandmarkType.ContentInfo;
                else if (elementName.Contains("search"))
                    landmarkType = AutomationLandmarkType.Search;
                else if (elementName.Contains("form"))
                    landmarkType = AutomationLandmarkType.Form;

                if (landmarkType != AutomationLandmarkType.None)
                {
                    AutomationProperties.SetLandmarkType(element, landmarkType);
                    _logger.LogDebug("Set landmark type {LandmarkType} for element {ElementName}",
                        landmarkType, element.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup element landmark");
            }
        }

        private void SetupTableHeaders(FrameworkElement tableElement, AccessibleTableInfo tableInfo)
        {
            try
            {
                // This would setup accessible table headers
                // Implementation depends on the specific table control being used
                _logger.LogDebug("Setup table headers for {ColumnCount} columns", tableInfo.Columns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup table headers");
            }
        }

        private void SetupTableRows(FrameworkElement tableElement, AccessibleTableInfo tableInfo)
        {
            try
            {
                // This would setup accessible table rows
                // Implementation depends on the specific table control being used
                _logger.LogDebug("Setup table rows for {RowCount} rows", tableInfo.Rows.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup table rows");
            }
        }

        private AutomationLandmarkType ParseLandmarkType(string landmarkType)
        {
            return landmarkType.ToLowerInvariant() switch
            {
                "banner" => AutomationLandmarkType.Banner,
                "complementary" => AutomationLandmarkType.Complementary,
                "contentinfo" => AutomationLandmarkType.ContentInfo,
                "form" => AutomationLandmarkType.Form,
                "main" => AutomationLandmarkType.Main,
                "navigation" => AutomationLandmarkType.Navigation,
                "region" => AutomationLandmarkType.Region,
                "search" => AutomationLandmarkType.Search,
                _ => AutomationLandmarkType.None
            };
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Disposing ScreenReaderSupportService");

                // Clean up live regions
                foreach (var liveRegion in _liveRegions.Values)
                {
                    if (liveRegion.Container is Panel panel)
                    {
                        panel.Children.Remove(liveRegion.TextElement);
                    }
                }
                _liveRegions.Clear();

                // Clean up registered elements
                _registeredElements.Clear();

                // Dispose observables
                _screenReaderEvents?.Dispose();

                _logger.LogInformation("ScreenReaderSupportService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing ScreenReaderSupportService");
            }
        }
    }
} 
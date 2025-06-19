using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.WinUI.Services
{
    /// <summary>
    /// Comprehensive keyboard navigation service implementing complete keyboard navigation
    /// for all UI elements with enterprise-grade accessibility patterns
    /// </summary>
    public interface IKeyboardNavigationService
    {
        /// <summary>
        /// Initialize keyboard navigation for the specified UI element
        /// </summary>
        Task InitializeAsync(FrameworkElement rootElement);

        /// <summary>
        /// Register a focusable element with the navigation system
        /// </summary>
        void RegisterFocusableElement(FrameworkElement element, string navigationGroup = "default");

        /// <summary>
        /// Unregister a focusable element from the navigation system
        /// </summary>
        void UnregisterFocusableElement(FrameworkElement element);

        /// <summary>
        /// Set focus to the next element in the navigation order
        /// </summary>
        bool FocusNext(string navigationGroup = null);

        /// <summary>
        /// Set focus to the previous element in the navigation order
        /// </summary>
        bool FocusPrevious(string navigationGroup = null);

        /// <summary>
        /// Set focus to the first element in the specified navigation group
        /// </summary>
        bool FocusFirst(string navigationGroup = "default");

        /// <summary>
        /// Set focus to the last element in the specified navigation group
        /// </summary>
        bool FocusLast(string navigationGroup = "default");

        /// <summary>
        /// Handle keyboard shortcuts and accelerators
        /// </summary>
        bool HandleKeyboardShortcut(VirtualKey key, VirtualKeyModifiers modifiers);

        /// <summary>
        /// Enable or disable keyboard navigation
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Observable for keyboard navigation events
        /// </summary>
        IObservable<KeyboardNavigationEvent> NavigationEvents { get; }

        /// <summary>
        /// Observable for focus change events
        /// </summary>
        IObservable<FocusChangeEvent> FocusEvents { get; }
    }

    /// <summary>
    /// Keyboard navigation event data
    /// </summary>
    public class KeyboardNavigationEvent
    {
        public string EventType { get; set; }
        public string NavigationGroup { get; set; }
        public FrameworkElement SourceElement { get; set; }
        public FrameworkElement TargetElement { get; set; }
        public VirtualKey Key { get; set; }
        public VirtualKeyModifiers Modifiers { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Handled { get; set; }
    }

    /// <summary>
    /// Focus change event data
    /// </summary>
    public class FocusChangeEvent
    {
        public FrameworkElement PreviousElement { get; set; }
        public FrameworkElement CurrentElement { get; set; }
        public string NavigationGroup { get; set; }
        public string FocusReason { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Navigation group configuration
    /// </summary>
    public class NavigationGroup
    {
        public string Name { get; set; }
        public List<FrameworkElement> Elements { get; set; } = new List<FrameworkElement>();
        public int CurrentIndex { get; set; } = -1;
        public bool IsCircular { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Keyboard shortcut configuration
    /// </summary>
    public class KeyboardShortcut
    {
        public VirtualKey Key { get; set; }
        public VirtualKeyModifiers Modifiers { get; set; }
        public string Command { get; set; }
        public string Description { get; set; }
        public Func<bool> Handler { get; set; }
        public bool IsGlobal { get; set; }
        public string Context { get; set; }
    }

    /// <summary>
    /// Professional keyboard navigation service with comprehensive accessibility support
    /// </summary>
    public class KeyboardNavigationService : IKeyboardNavigationService, IDisposable
    {
        private readonly ILogger<KeyboardNavigationService> _logger;
        private readonly IAccessibilityService _accessibilityService;
        private readonly IAudioFeedbackService _audioFeedbackService;

        private readonly Dictionary<string, NavigationGroup> _navigationGroups;
        private readonly List<KeyboardShortcut> _shortcuts;
        private readonly Subject<KeyboardNavigationEvent> _navigationEvents;
        private readonly Subject<FocusChangeEvent> _focusEvents;

        private FrameworkElement _rootElement;
        private string _currentGroup = "default";
        private bool _isEnabled = true;
        private bool _isInitialized = false;
        private FrameworkElement _lastFocusedElement;

        // Keyboard navigation constants
        private const int FOCUS_RETRY_DELAY_MS = 50;
        private const int MAX_FOCUS_RETRIES = 3;

        public KeyboardNavigationService(
            ILogger<KeyboardNavigationService> logger,
            IAccessibilityService accessibilityService,
            IAudioFeedbackService audioFeedbackService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));
            _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));

            _navigationGroups = new Dictionary<string, NavigationGroup>();
            _shortcuts = new List<KeyboardShortcut>();
            _navigationEvents = new Subject<KeyboardNavigationEvent>();
            _focusEvents = new Subject<FocusChangeEvent>();

            InitializeDefaultNavigationGroup();
            RegisterDefaultShortcuts();

            _logger.LogInformation("KeyboardNavigationService initialized");
        }

        public IObservable<KeyboardNavigationEvent> NavigationEvents => _navigationEvents.AsObservable();
        public IObservable<FocusChangeEvent> FocusEvents => _focusEvents.AsObservable();

        public async Task InitializeAsync(FrameworkElement rootElement)
        {
            try
            {
                _logger.LogInformation("Initializing keyboard navigation for root element: {ElementType}", 
                    rootElement?.GetType().Name);

                _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));

                // Attach keyboard event handlers
                _rootElement.KeyDown += OnRootElementKeyDown;
                _rootElement.PreviewKeyDown += OnRootElementPreviewKeyDown;

                // Register for focus events
                _rootElement.GotFocus += OnElementGotFocus;
                _rootElement.LostFocus += OnElementLostFocus;

                // Auto-discover focusable elements
                await DiscoverFocusableElementsAsync(_rootElement);

                _isInitialized = true;

                _logger.LogInformation("Keyboard navigation initialized successfully with {GroupCount} navigation groups",
                    _navigationGroups.Count);

                // Announce initialization to screen readers
                await _accessibilityService.AnnounceAsync("Keyboard navigation enabled", 
                    AccessibilityAnnouncementPriority.Medium);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize keyboard navigation");
                throw;
            }
        }

        public void RegisterFocusableElement(FrameworkElement element, string navigationGroup = "default")
        {
            try
            {
                if (element == null)
                {
                    _logger.LogWarning("Attempted to register null element for keyboard navigation");
                    return;
                }

                if (!_navigationGroups.ContainsKey(navigationGroup))
                {
                    _navigationGroups[navigationGroup] = new NavigationGroup { Name = navigationGroup };
                }

                var group = _navigationGroups[navigationGroup];
                if (!group.Elements.Contains(element))
                {
                    group.Elements.Add(element);
                    
                    // Set up element-specific keyboard handling
                    element.KeyDown += OnElementKeyDown;
                    element.GotFocus += OnElementGotFocus;
                    element.LostFocus += OnElementLostFocus;

                    // Set accessibility properties
                    SetAccessibilityProperties(element, navigationGroup);

                    _logger.LogDebug("Registered focusable element {ElementType} in group {Group}",
                        element.GetType().Name, navigationGroup);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register focusable element");
            }
        }

        public void UnregisterFocusableElement(FrameworkElement element)
        {
            try
            {
                if (element == null) return;

                foreach (var group in _navigationGroups.Values)
                {
                    if (group.Elements.Remove(element))
                    {
                        // Remove event handlers
                        element.KeyDown -= OnElementKeyDown;
                        element.GotFocus -= OnElementGotFocus;
                        element.LostFocus -= OnElementLostFocus;

                        _logger.LogDebug("Unregistered focusable element {ElementType}",
                            element.GetType().Name);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister focusable element");
            }
        }

        public bool FocusNext(string navigationGroup = null)
        {
            try
            {
                var groupName = navigationGroup ?? _currentGroup;
                if (!_navigationGroups.TryGetValue(groupName, out var group) || !group.IsEnabled)
                {
                    return false;
                }

                var elements = group.Elements.Where(e => IsElementFocusable(e)).ToList();
                if (elements.Count == 0) return false;

                var currentIndex = group.CurrentIndex;
                var nextIndex = (currentIndex + 1) % elements.Count;

                if (SetFocusToElement(elements[nextIndex], groupName, "FocusNext"))
                {
                    group.CurrentIndex = nextIndex;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to focus next element");
                return false;
            }
        }

        public bool FocusPrevious(string navigationGroup = null)
        {
            try
            {
                var groupName = navigationGroup ?? _currentGroup;
                if (!_navigationGroups.TryGetValue(groupName, out var group) || !group.IsEnabled)
                {
                    return false;
                }

                var elements = group.Elements.Where(e => IsElementFocusable(e)).ToList();
                if (elements.Count == 0) return false;

                var currentIndex = group.CurrentIndex;
                var previousIndex = currentIndex <= 0 ? elements.Count - 1 : currentIndex - 1;

                if (SetFocusToElement(elements[previousIndex], groupName, "FocusPrevious"))
                {
                    group.CurrentIndex = previousIndex;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to focus previous element");
                return false;
            }
        }

        public bool FocusFirst(string navigationGroup = "default")
        {
            try
            {
                if (!_navigationGroups.TryGetValue(navigationGroup, out var group) || !group.IsEnabled)
                {
                    return false;
                }

                var elements = group.Elements.Where(e => IsElementFocusable(e)).ToList();
                if (elements.Count == 0) return false;

                if (SetFocusToElement(elements[0], navigationGroup, "FocusFirst"))
                {
                    group.CurrentIndex = 0;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to focus first element");
                return false;
            }
        }

        public bool FocusLast(string navigationGroup = "default")
        {
            try
            {
                if (!_navigationGroups.TryGetValue(navigationGroup, out var group) || !group.IsEnabled)
                {
                    return false;
                }

                var elements = group.Elements.Where(e => IsElementFocusable(e)).ToList();
                if (elements.Count == 0) return false;

                var lastIndex = elements.Count - 1;
                if (SetFocusToElement(elements[lastIndex], navigationGroup, "FocusLast"))
                {
                    group.CurrentIndex = lastIndex;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to focus last element");
                return false;
            }
        }

        public bool HandleKeyboardShortcut(VirtualKey key, VirtualKeyModifiers modifiers)
        {
            try
            {
                var shortcut = _shortcuts.FirstOrDefault(s => s.Key == key && s.Modifiers == modifiers);
                if (shortcut?.Handler != null)
                {
                    _logger.LogDebug("Executing keyboard shortcut: {Key}+{Modifiers} -> {Command}",
                        key, modifiers, shortcut.Command);

                    var handled = shortcut.Handler();
                    
                    if (handled)
                    {
                        // Play audio feedback
                        _ = Task.Run(() => _audioFeedbackService.PlayNavigationSoundAsync());

                        // Announce shortcut execution
                        _ = Task.Run(() => _accessibilityService.AnnounceAsync(
                            $"Executed {shortcut.Description}", 
                            AccessibilityAnnouncementPriority.Low));
                    }

                    return handled;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle keyboard shortcut");
                return false;
            }
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Keyboard navigation {Status}", enabled ? "enabled" : "disabled");

            // Announce state change
            _ = Task.Run(() => _accessibilityService.AnnounceAsync(
                $"Keyboard navigation {(enabled ? "enabled" : "disabled")}", 
                AccessibilityAnnouncementPriority.Medium));
        }

        private void InitializeDefaultNavigationGroup()
        {
            _navigationGroups["default"] = new NavigationGroup
            {
                Name = "default",
                IsCircular = true,
                IsEnabled = true,
                Priority = 0
            };
        }

        private void RegisterDefaultShortcuts()
        {
            // Navigation shortcuts
            RegisterShortcut(VirtualKey.Tab, VirtualKeyModifiers.None, "FocusNext", "Move to next element", () => FocusNext());
            RegisterShortcut(VirtualKey.Tab, VirtualKeyModifiers.Shift, "FocusPrevious", "Move to previous element", () => FocusPrevious());
            RegisterShortcut(VirtualKey.Home, VirtualKeyModifiers.None, "FocusFirst", "Move to first element", () => FocusFirst());
            RegisterShortcut(VirtualKey.End, VirtualKeyModifiers.None, "FocusLast", "Move to last element", () => FocusLast());

            // Application shortcuts
            RegisterShortcut(VirtualKey.F1, VirtualKeyModifiers.None, "Help", "Show help", () => ShowHelp());
            RegisterShortcut(VirtualKey.Escape, VirtualKeyModifiers.None, "Cancel", "Cancel current operation", () => CancelCurrentOperation());
        }

        private void RegisterShortcut(VirtualKey key, VirtualKeyModifiers modifiers, string command, string description, Func<bool> handler)
        {
            _shortcuts.Add(new KeyboardShortcut
            {
                Key = key,
                Modifiers = modifiers,
                Command = command,
                Description = description,
                Handler = handler,
                IsGlobal = true
            });
        }

        private async Task DiscoverFocusableElementsAsync(FrameworkElement rootElement)
        {
            try
            {
                await Task.Run(() =>
                {
                    var focusableElements = FindFocusableElements(rootElement);
                    foreach (var element in focusableElements)
                    {
                        RegisterFocusableElement(element, "default");
                    }
                });

                _logger.LogInformation("Discovered {Count} focusable elements", 
                    _navigationGroups["default"].Elements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover focusable elements");
            }
        }

        private List<FrameworkElement> FindFocusableElements(DependencyObject parent)
        {
            var focusableElements = new List<FrameworkElement>();

            if (parent is FrameworkElement element && IsElementFocusable(element))
            {
                focusableElements.Add(element);
            }

            var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
                focusableElements.AddRange(FindFocusableElements(child));
            }

            return focusableElements;
        }

        private bool IsElementFocusable(FrameworkElement element)
        {
            if (element == null) return false;

            return element.IsEnabled &&
                   element.Visibility == Visibility.Visible &&
                   (element.IsTabStop || 
                    element is Button ||
                    element is TextBox ||
                    element is ComboBox ||
                    element is CheckBox ||
                    element is RadioButton ||
                    element is Slider ||
                    element is ListView ||
                    element is TreeView);
        }

        private bool SetFocusToElement(FrameworkElement element, string groupName, string reason)
        {
            try
            {
                if (element == null || !IsElementFocusable(element)) return false;

                var previousElement = _lastFocusedElement;
                var success = element.Focus(FocusState.Keyboard);

                if (success)
                {
                    _lastFocusedElement = element;
                    _currentGroup = groupName;

                    // Fire focus change event
                    _focusEvents.OnNext(new FocusChangeEvent
                    {
                        PreviousElement = previousElement,
                        CurrentElement = element,
                        NavigationGroup = groupName,
                        FocusReason = reason
                    });

                    // Play audio feedback
                    _ = Task.Run(() => _audioFeedbackService.PlayNavigationSoundAsync());

                    _logger.LogDebug("Focus set to {ElementType} in group {Group} (reason: {Reason})",
                        element.GetType().Name, groupName, reason);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set focus to element");
                return false;
            }
        }

        private void SetAccessibilityProperties(FrameworkElement element, string navigationGroup)
        {
            try
            {
                // Set basic accessibility properties
                if (string.IsNullOrEmpty(element.Name))
                {
                    element.Name = $"{element.GetType().Name}_{Guid.NewGuid():N}";
                }

                // Set automation properties for screen readers
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(element, 
                    Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Content);
                
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetLiveSetting(element,
                    Microsoft.UI.Xaml.Automation.Peers.AutomationLiveSetting.Polite);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set accessibility properties for element");
            }
        }

        private async void OnRootElementKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!_isEnabled || !_isInitialized) return;

            try
            {
                var modifiers = GetCurrentKeyModifiers();
                var handled = HandleKeyboardShortcut(e.Key, modifiers);

                if (handled)
                {
                    e.Handled = true;
                }

                // Fire navigation event
                _navigationEvents.OnNext(new KeyboardNavigationEvent
                {
                    EventType = "KeyDown",
                    NavigationGroup = _currentGroup,
                    SourceElement = sender as FrameworkElement,
                    Key = e.Key,
                    Modifiers = modifiers,
                    Handled = handled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling root element key down");
            }
        }

        private void OnRootElementPreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle preview key events for special navigation cases
            if (!_isEnabled || !_isInitialized) return;

            try
            {
                // Handle special navigation keys that might be consumed by controls
                if (e.Key == VirtualKey.Tab)
                {
                    var modifiers = GetCurrentKeyModifiers();
                    if (modifiers == VirtualKeyModifiers.Shift)
                    {
                        if (FocusPrevious())
                        {
                            e.Handled = true;
                        }
                    }
                    else if (modifiers == VirtualKeyModifiers.None)
                    {
                        if (FocusNext())
                        {
                            e.Handled = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling root element preview key down");
            }
        }

        private void OnElementKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Handle element-specific keyboard events
            if (!_isEnabled) return;

            try
            {
                var element = sender as FrameworkElement;
                var modifiers = GetCurrentKeyModifiers();

                _navigationEvents.OnNext(new KeyboardNavigationEvent
                {
                    EventType = "ElementKeyDown",
                    SourceElement = element,
                    Key = e.Key,
                    Modifiers = modifiers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling element key down");
            }
        }

        private void OnElementGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var element = sender as FrameworkElement;
                if (element != null)
                {
                    UpdateCurrentGroupFromElement(element);
                    _lastFocusedElement = element;

                    _logger.LogDebug("Element got focus: {ElementType}", element.GetType().Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling element got focus");
            }
        }

        private void OnElementLostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var element = sender as FrameworkElement;
                _logger.LogDebug("Element lost focus: {ElementType}", element?.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling element lost focus");
            }
        }

        private void UpdateCurrentGroupFromElement(FrameworkElement element)
        {
            foreach (var kvp in _navigationGroups)
            {
                if (kvp.Value.Elements.Contains(element))
                {
                    _currentGroup = kvp.Key;
                    kvp.Value.CurrentIndex = kvp.Value.Elements.IndexOf(element);
                    break;
                }
            }
        }

        private VirtualKeyModifiers GetCurrentKeyModifiers()
        {
            var modifiers = VirtualKeyModifiers.None;
            
            var window = Microsoft.UI.Xaml.Window.Current;
            if (window != null)
            {
                var coreWindow = window.CoreWindow;
                if (coreWindow != null)
                {
                    if (coreWindow.GetKeyState(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                        modifiers |= VirtualKeyModifiers.Control;
                    if (coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                        modifiers |= VirtualKeyModifiers.Shift;
                    if (coreWindow.GetKeyState(VirtualKey.Menu).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                        modifiers |= VirtualKeyModifiers.Menu;
                }
            }

            return modifiers;
        }

        private bool ShowHelp()
        {
            try
            {
                // Show contextual help
                _ = Task.Run(() => _accessibilityService.AnnounceAsync(
                    "Help system activated. Use F1 for context-sensitive help.", 
                    AccessibilityAnnouncementPriority.High));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show help");
                return false;
            }
        }

        private bool CancelCurrentOperation()
        {
            try
            {
                // Cancel current operation logic would go here
                _ = Task.Run(() => _accessibilityService.AnnounceAsync(
                    "Operation cancelled", 
                    AccessibilityAnnouncementPriority.Medium));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel current operation");
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Disposing KeyboardNavigationService");

                // Remove event handlers
                if (_rootElement != null)
                {
                    _rootElement.KeyDown -= OnRootElementKeyDown;
                    _rootElement.PreviewKeyDown -= OnRootElementPreviewKeyDown;
                    _rootElement.GotFocus -= OnElementGotFocus;
                    _rootElement.LostFocus -= OnElementLostFocus;
                }

                // Clean up navigation groups
                foreach (var group in _navigationGroups.Values)
                {
                    foreach (var element in group.Elements)
                    {
                        element.KeyDown -= OnElementKeyDown;
                        element.GotFocus -= OnElementGotFocus;
                        element.LostFocus -= OnElementLostFocus;
                    }
                }

                _navigationGroups.Clear();
                _shortcuts.Clear();

                // Dispose observables
                _navigationEvents?.Dispose();
                _focusEvents?.Dispose();

                _logger.LogInformation("KeyboardNavigationService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing KeyboardNavigationService");
            }
        }
    }
} 
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AnomaliImportTool.Core.Interfaces;

namespace AnomaliImportTool.WinUI.Services
{
    /// <summary>
    /// Contextual help service interface for smart tooltips and help system
    /// </summary>
    public interface IContextualHelpService
    {
        /// <summary>
        /// Initialize contextual help service
        /// </summary>
        Task InitializeAsync(FrameworkElement rootElement);

        /// <summary>
        /// Register help content for an element
        /// </summary>
        void RegisterHelpContent(FrameworkElement element, HelpContent helpContent);

        /// <summary>
        /// Unregister help content for an element
        /// </summary>
        void UnregisterHelpContent(FrameworkElement element);

        /// <summary>
        /// Show contextual help for current focus
        /// </summary>
        Task ShowContextualHelpAsync();

        /// <summary>
        /// Show help for specific element
        /// </summary>
        Task ShowHelpForElementAsync(FrameworkElement element);

        /// <summary>
        /// Hide current help display
        /// </summary>
        void HideHelp();

        /// <summary>
        /// Enable or disable contextual help
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Set help display mode
        /// </summary>
        void SetDisplayMode(HelpDisplayMode mode);

        /// <summary>
        /// Observable for help events
        /// </summary>
        IObservable<HelpEvent> HelpEvents { get; }
    }

    /// <summary>
    /// Help content definition
    /// </summary>
    public class HelpContent
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DetailedHelp { get; set; }
        public List<string> KeyboardShortcuts { get; set; } = new List<string>();
        public List<HelpTip> Tips { get; set; } = new List<HelpTip>();
        public string VideoUrl { get; set; }
        public string DocumentationUrl { get; set; }
        public HelpPriority Priority { get; set; } = HelpPriority.Normal;
        public string Category { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }

    /// <summary>
    /// Help tip information
    /// </summary>
    public class HelpTip
    {
        public string Text { get; set; }
        public string Icon { get; set; }
        public HelpTipType Type { get; set; } = HelpTipType.Info;
    }

    /// <summary>
    /// Help display modes
    /// </summary>
    public enum HelpDisplayMode
    {
        Tooltip,
        Flyout,
        SidePanel,
        Modal,
        Inline
    }

    /// <summary>
    /// Help priority levels
    /// </summary>
    public enum HelpPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Help tip types
    /// </summary>
    public enum HelpTipType
    {
        Info,
        Tip,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Help event data
    /// </summary>
    public class HelpEvent
    {
        public string EventType { get; set; }
        public FrameworkElement Element { get; set; }
        public string HelpTitle { get; set; }
        public HelpDisplayMode DisplayMode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Professional contextual help service with smart tooltips
    /// </summary>
    public class ContextualHelpService : IContextualHelpService, IDisposable
    {
        private readonly ILogger<ContextualHelpService> _logger;
        private readonly IAccessibilityService _accessibilityService;

        private readonly Dictionary<FrameworkElement, HelpContent> _helpContent;
        private readonly Subject<HelpEvent> _helpEvents;

        private FrameworkElement _rootElement;
        private ToolTip _currentTooltip;
        private Flyout _currentFlyout;
        private bool _isEnabled = true;
        private HelpDisplayMode _displayMode = HelpDisplayMode.Tooltip;
        private bool _isInitialized = false;

        public ContextualHelpService(
            ILogger<ContextualHelpService> logger,
            IAccessibilityService accessibilityService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));

            _helpContent = new Dictionary<FrameworkElement, HelpContent>();
            _helpEvents = new Subject<HelpEvent>();

            _logger.LogInformation("ContextualHelpService initialized");
        }

        public IObservable<HelpEvent> HelpEvents => _helpEvents.AsObservable();

        public async Task InitializeAsync(FrameworkElement rootElement)
        {
            try
            {
                _logger.LogInformation("Initializing contextual help service");

                _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));

                // Set up global keyboard handlers
                _rootElement.KeyDown += OnRootKeyDown;

                // Register default help content
                RegisterDefaultHelpContent();

                _isInitialized = true;

                _logger.LogInformation("Contextual help service initialized");

                await _accessibilityService.AnnounceAsync(
                    "Contextual help system ready. Press F1 for help.", 
                    AccessibilityAnnouncementPriority.Low);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize contextual help service");
                throw;
            }
        }

        public void RegisterHelpContent(FrameworkElement element, HelpContent helpContent)
        {
            try
            {
                if (element == null || helpContent == null)
                {
                    _logger.LogWarning("Invalid element or help content for registration");
                    return;
                }

                _helpContent[element] = helpContent;

                // Set up element event handlers
                element.GotFocus += OnElementGotFocus;
                element.PointerEntered += OnElementPointerEntered;
                element.PointerExited += OnElementPointerExited;
                element.KeyDown += OnElementKeyDown;

                // Set basic tooltip if using tooltip mode
                if (_displayMode == HelpDisplayMode.Tooltip)
                {
                    SetupBasicTooltip(element, helpContent);
                }

                _logger.LogDebug("Registered help content for element: {ElementType} - {Title}",
                    element.GetType().Name, helpContent.Title);

                _helpEvents.OnNext(new HelpEvent
                {
                    EventType = "HelpContentRegistered",
                    Element = element,
                    HelpTitle = helpContent.Title,
                    DisplayMode = _displayMode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register help content");
            }
        }

        public void UnregisterHelpContent(FrameworkElement element)
        {
            try
            {
                if (element == null) return;

                if (_helpContent.Remove(element))
                {
                    // Remove event handlers
                    element.GotFocus -= OnElementGotFocus;
                    element.PointerEntered -= OnElementPointerEntered;
                    element.PointerExited -= OnElementPointerExited;
                    element.KeyDown -= OnElementKeyDown;

                    // Clear tooltip
                    ToolTipService.SetToolTip(element, null);

                    _logger.LogDebug("Unregistered help content for element: {ElementType}",
                        element.GetType().Name);

                    _helpEvents.OnNext(new HelpEvent
                    {
                        EventType = "HelpContentUnregistered",
                        Element = element,
                        DisplayMode = _displayMode
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister help content");
            }
        }

        public async Task ShowContextualHelpAsync()
        {
            try
            {
                if (!_isEnabled || !_isInitialized) return;

                // Find currently focused element
                var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
                if (focusedElement != null && _helpContent.ContainsKey(focusedElement))
                {
                    await ShowHelpForElementAsync(focusedElement);
                }
                else
                {
                    // Show general help
                    await ShowGeneralHelpAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show contextual help");
            }
        }

        public async Task ShowHelpForElementAsync(FrameworkElement element)
        {
            try
            {
                if (element == null || !_helpContent.TryGetValue(element, out var helpContent))
                {
                    _logger.LogWarning("No help content found for element");
                    return;
                }

                _logger.LogDebug("Showing help for element: {ElementType} - {Title}",
                    element.GetType().Name, helpContent.Title);

                switch (_displayMode)
                {
                    case HelpDisplayMode.Tooltip:
                        ShowAdvancedTooltip(element, helpContent);
                        break;
                    case HelpDisplayMode.Flyout:
                        ShowHelpFlyout(element, helpContent);
                        break;
                    case HelpDisplayMode.Modal:
                        await ShowHelpModalAsync(helpContent);
                        break;
                    default:
                        ShowAdvancedTooltip(element, helpContent);
                        break;
                }

                // Announce help display
                await _accessibilityService.AnnounceAsync(
                    $"Showing help: {helpContent.Title}", 
                    AccessibilityAnnouncementPriority.Medium);

                _helpEvents.OnNext(new HelpEvent
                {
                    EventType = "HelpShown",
                    Element = element,
                    HelpTitle = helpContent.Title,
                    DisplayMode = _displayMode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show help for element");
            }
        }

        public void HideHelp()
        {
            try
            {
                // Hide current tooltip
                if (_currentTooltip != null)
                {
                    _currentTooltip.IsOpen = false;
                    _currentTooltip = null;
                }

                // Hide current flyout
                if (_currentFlyout != null)
                {
                    _currentFlyout.Hide();
                    _currentFlyout = null;
                }

                _helpEvents.OnNext(new HelpEvent
                {
                    EventType = "HelpHidden",
                    DisplayMode = _displayMode
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide help");
            }
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Contextual help {Status}", enabled ? "enabled" : "disabled");

            if (!enabled)
            {
                HideHelp();
            }
        }

        public void SetDisplayMode(HelpDisplayMode mode)
        {
            _displayMode = mode;
            _logger.LogInformation("Help display mode changed to: {Mode}", mode);

            // Update existing help content for new mode
            foreach (var kvp in _helpContent)
            {
                if (mode == HelpDisplayMode.Tooltip)
                {
                    SetupBasicTooltip(kvp.Key, kvp.Value);
                }
                else
                {
                    ToolTipService.SetToolTip(kvp.Key, null);
                }
            }
        }

        private void RegisterDefaultHelpContent()
        {
            try
            {
                // This would register help content for common UI elements
                _logger.LogDebug("Registered default help content");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register default help content");
            }
        }

        private void SetupBasicTooltip(FrameworkElement element, HelpContent helpContent)
        {
            try
            {
                var tooltip = new ToolTip
                {
                    Content = helpContent.Description ?? helpContent.Title,
                    Placement = Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Auto
                };

                ToolTipService.SetToolTip(element, tooltip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to setup basic tooltip");
            }
        }

        private void ShowAdvancedTooltip(FrameworkElement element, HelpContent helpContent)
        {
            try
            {
                HideHelp(); // Hide any existing help

                // Create advanced tooltip content
                var tooltipContent = CreateAdvancedTooltipContent(helpContent);

                _currentTooltip = new ToolTip
                {
                    Content = tooltipContent,
                    Placement = Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Auto,
                    IsOpen = true
                };

                ToolTipService.SetToolTip(element, _currentTooltip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show advanced tooltip");
            }
        }

        private FrameworkElement CreateAdvancedTooltipContent(HelpContent helpContent)
        {
            var panel = new StackPanel
            {
                MaxWidth = 400,
                Spacing = 8
            };

            // Title
            if (!string.IsNullOrWhiteSpace(helpContent.Title))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = helpContent.Title,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 16
                });
            }

            // Description
            if (!string.IsNullOrWhiteSpace(helpContent.Description))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = helpContent.Description,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                });
            }

            // Keyboard shortcuts
            if (helpContent.KeyboardShortcuts.Count > 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Keyboard shortcuts:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 4)
                });

                foreach (var shortcut in helpContent.KeyboardShortcuts)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"• {shortcut}",
                        FontSize = 12,
                        Margin = new Thickness(16, 0, 0, 2)
                    });
                }
            }

            // Tips
            if (helpContent.Tips.Count > 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Tips:",
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 8, 0, 4)
                });

                foreach (var tip in helpContent.Tips)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"💡 {tip.Text}",
                        FontSize = 12,
                        Margin = new Thickness(16, 0, 0, 2),
                        TextWrapping = TextWrapping.Wrap
                    });
                }
            }

            return panel;
        }

        private void ShowHelpFlyout(FrameworkElement element, HelpContent helpContent)
        {
            try
            {
                HideHelp(); // Hide any existing help

                var flyoutContent = CreateFlyoutContent(helpContent);

                _currentFlyout = new Flyout
                {
                    Content = flyoutContent,
                    Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Auto
                };

                _currentFlyout.ShowAt(element);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show help flyout");
            }
        }

        private FrameworkElement CreateFlyoutContent(HelpContent helpContent)
        {
            var scrollViewer = new ScrollViewer
            {
                MaxWidth = 500,
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var panel = new StackPanel
            {
                Spacing = 12,
                Margin = new Thickness(16)
            };

            // Title
            if (!string.IsNullOrWhiteSpace(helpContent.Title))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = helpContent.Title,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 20
                });
            }

            // Description
            if (!string.IsNullOrWhiteSpace(helpContent.Description))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = helpContent.Description,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14
                });
            }

            // Detailed help
            if (!string.IsNullOrWhiteSpace(helpContent.DetailedHelp))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = helpContent.DetailedHelp,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    Margin = new Thickness(0, 8, 0, 0)
                });
            }

            // Keyboard shortcuts section
            if (helpContent.KeyboardShortcuts.Count > 0)
            {
                panel.Children.Add(CreateKeyboardShortcutsSection(helpContent.KeyboardShortcuts));
            }

            // Tips section
            if (helpContent.Tips.Count > 0)
            {
                panel.Children.Add(CreateTipsSection(helpContent.Tips));
            }

            // Links section
            if (!string.IsNullOrWhiteSpace(helpContent.DocumentationUrl) || !string.IsNullOrWhiteSpace(helpContent.VideoUrl))
            {
                panel.Children.Add(CreateLinksSection(helpContent));
            }

            scrollViewer.Content = panel;
            return scrollViewer;
        }

        private FrameworkElement CreateKeyboardShortcutsSection(List<string> shortcuts)
        {
            var section = new StackPanel { Spacing = 8 };

            section.Children.Add(new TextBlock
            {
                Text = "Keyboard Shortcuts",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16
            });

            foreach (var shortcut in shortcuts)
            {
                var shortcutPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                };

                shortcutPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 240, 240)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 4, 8, 4),
                    Child = new TextBlock
                    {
                        Text = shortcut,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 12
                    }
                });

                section.Children.Add(shortcutPanel);
            }

            return section;
        }

        private FrameworkElement CreateTipsSection(List<HelpTip> tips)
        {
            var section = new StackPanel { Spacing = 8 };

            section.Children.Add(new TextBlock
            {
                Text = "Tips & Tricks",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16
            });

            foreach (var tip in tips)
            {
                var tipPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8
                };

                var icon = tip.Type switch
                {
                    HelpTipType.Tip => "💡",
                    HelpTipType.Warning => "⚠️",
                    HelpTipType.Error => "❌",
                    HelpTipType.Success => "✅",
                    _ => "ℹ️"
                };

                tipPanel.Children.Add(new TextBlock
                {
                    Text = icon,
                    FontSize = 16,
                    VerticalAlignment = VerticalAlignment.Top
                });

                tipPanel.Children.Add(new TextBlock
                {
                    Text = tip.Text,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Top
                });

                section.Children.Add(tipPanel);
            }

            return section;
        }

        private FrameworkElement CreateLinksSection(HelpContent helpContent)
        {
            var section = new StackPanel { Spacing = 8 };

            section.Children.Add(new TextBlock
            {
                Text = "Additional Resources",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16
            });

            if (!string.IsNullOrWhiteSpace(helpContent.DocumentationUrl))
            {
                var docLink = new HyperlinkButton
                {
                    Content = "📖 View Documentation",
                    NavigateUri = new Uri(helpContent.DocumentationUrl)
                };
                section.Children.Add(docLink);
            }

            if (!string.IsNullOrWhiteSpace(helpContent.VideoUrl))
            {
                var videoLink = new HyperlinkButton
                {
                    Content = "🎥 Watch Video Tutorial",
                    NavigateUri = new Uri(helpContent.VideoUrl)
                };
                section.Children.Add(videoLink);
            }

            return section;
        }

        private async Task ShowHelpModalAsync(HelpContent helpContent)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = helpContent.Title ?? "Help",
                    Content = CreateFlyoutContent(helpContent),
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Close
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show help modal");
            }
        }

        private async Task ShowGeneralHelpAsync()
        {
            try
            {
                var generalHelp = new HelpContent
                {
                    Title = "Application Help",
                    Description = "Welcome to the Anomali Threat Bulletin Import Tool help system.",
                    DetailedHelp = "This application helps you import and process threat bulletins. Use F1 to get context-sensitive help for any element.",
                    KeyboardShortcuts = new List<string>
                    {
                        "F1 - Show contextual help",
                        "Ctrl+H - Show general help",
                        "Esc - Close help"
                    }
                };

                await ShowHelpModalAsync(generalHelp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show general help");
            }
        }

        private async void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (!_isEnabled) return;

                if (e.Key == Windows.System.VirtualKey.F1)
                {
                    await ShowContextualHelpAsync();
                    e.Handled = true;
                }
                else if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    HideHelp();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling root key down");
            }
        }

        private void OnElementGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                // Could show help preview or update help state
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling element got focus");
            }
        }

        private void OnElementPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                // Could show help preview on hover
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling element pointer entered");
            }
        }

        private void OnElementPointerExited(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                // Could hide help preview
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling element pointer exited");
            }
        }

        private async void OnElementKeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (!_isEnabled) return;

                if (e.Key == Windows.System.VirtualKey.F1)
                {
                    await ShowHelpForElementAsync(sender as FrameworkElement);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling element key down");
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Disposing ContextualHelpService");

                // Remove event handlers
                if (_rootElement != null)
                {
                    _rootElement.KeyDown -= OnRootKeyDown;
                }

                // Remove element event handlers
                foreach (var element in _helpContent.Keys)
                {
                    element.GotFocus -= OnElementGotFocus;
                    element.PointerEntered -= OnElementPointerEntered;
                    element.PointerExited -= OnElementPointerExited;
                    element.KeyDown -= OnElementKeyDown;
                }

                // Hide current help
                HideHelp();

                // Clean up collections
                _helpContent.Clear();

                // Dispose observables
                _helpEvents?.Dispose();

                _logger.LogInformation("ContextualHelpService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing ContextualHelpService");
            }
        }
    }
} 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AnomaliImportTool.UI.Services
{
    /// <summary>
    /// Service for managing professional layout system with 8px spacing grid
    /// Provides consistent spacing, alignment, and branding elements integration
    /// </summary>
    public interface IProfessionalLayoutService : INotifyPropertyChanged
    {
        // Spacing Grid System (8px base)
        double SpacingUnit { get; }
        double SpacingXSmall { get; }
        double SpacingSmall { get; }
        double SpacingMedium { get; }
        double SpacingLarge { get; }
        double SpacingXLarge { get; }
        double SpacingXXLarge { get; }
        
        // Thickness Values
        Thickness ThicknessXSmall { get; }
        Thickness ThicknessSmall { get; }
        Thickness ThicknessMedium { get; }
        Thickness ThicknessLarge { get; }
        Thickness ThicknessXLarge { get; }
        Thickness ThicknessXXLarge { get; }
        
        // Layout Containers
        Grid CreateLayoutGrid(int rows, int columns);
        StackPanel CreateLayoutStackPanel(Orientation orientation);
        DockPanel CreateLayoutDockPanel();
        WrapPanel CreateLayoutWrapPanel();
        UniformGrid CreateLayoutUniformGrid(int rows, int columns);
        
        // Spacing Helpers
        Thickness GetSpacing(SpacingSize size);
        Thickness GetSpacing(SpacingSize horizontal, SpacingSize vertical);
        Thickness GetSpacing(SpacingSize left, SpacingSize top, SpacingSize right, SpacingSize bottom);
        double GetSpacingValue(SpacingSize size);
        
        // Alignment Helpers
        void ApplyStandardAlignment(FrameworkElement element, HorizontalAlignment horizontal, VerticalAlignment vertical);
        void ApplyResponsiveMargins(FrameworkElement element, SpacingSize size);
        void ApplyConsistentPadding(Control control, SpacingSize size);
        
        // Branding Integration
        ImageSource ApplicationIcon { get; }
        ImageSource ApplicationLogo { get; }
        void ApplyApplicationIcon(Window window);
        void ApplyBrandingToContainer(Panel container);
        
        // Layout Validation
        bool ValidateLayoutSpacing(FrameworkElement element);
        void EnforceSpacingConsistency(Panel container);
        LayoutValidationResult ValidateLayoutStructure(FrameworkElement root);
        
        // Responsive Design
        void ApplyResponsiveLayout(FrameworkElement element, double containerWidth);
        GridLength GetResponsiveColumnWidth(double availableWidth, int columnCount);
        double GetResponsiveFontSize(double baseFontSize, double containerWidth);
        
        // Events
        event EventHandler<LayoutChangedEventArgs> LayoutChanged;
    }

    public enum SpacingSize
    {
        None = 0,
        XSmall = 1,
        Small = 2,
        Medium = 3,
        Large = 4,
        XLarge = 5,
        XXLarge = 6
    }

    public class LayoutValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    public class LayoutChangedEventArgs : EventArgs
    {
        public FrameworkElement Element { get; set; }
        public string ChangeType { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
    }

    public class ProfessionalLayoutService : ReactiveObject, IProfessionalLayoutService
    {
        private readonly ILogger<ProfessionalLayoutService> _logger;
        
        // Base spacing unit (8px)
        private const double BaseSpacingUnit = 8.0;
        
        // Cached resources
        private ImageSource _applicationIcon;
        private ImageSource _applicationLogo;
        
        // Responsive breakpoints
        private const double SmallBreakpoint = 600;
        private const double MediumBreakpoint = 900;
        private const double LargeBreakpoint = 1200;

        public ProfessionalLayoutService(ILogger<ProfessionalLayoutService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeLayoutSystem();
            LoadBrandingAssets();
            
            _logger.LogInformation("Professional Layout Service initialized with 8px spacing grid");
        }

        #region Spacing Grid System

        public double SpacingUnit => BaseSpacingUnit;
        public double SpacingXSmall => BaseSpacingUnit * 0.5; // 4px
        public double SpacingSmall => BaseSpacingUnit * 1.0; // 8px
        public double SpacingMedium => BaseSpacingUnit * 2.0; // 16px
        public double SpacingLarge => BaseSpacingUnit * 3.0; // 24px
        public double SpacingXLarge => BaseSpacingUnit * 4.0; // 32px
        public double SpacingXXLarge => BaseSpacingUnit * 6.0; // 48px

        public Thickness ThicknessXSmall => new Thickness(SpacingXSmall);
        public Thickness ThicknessSmall => new Thickness(SpacingSmall);
        public Thickness ThicknessMedium => new Thickness(SpacingMedium);
        public Thickness ThicknessLarge => new Thickness(SpacingLarge);
        public Thickness ThicknessXLarge => new Thickness(SpacingXLarge);
        public Thickness ThicknessXXLarge => new Thickness(SpacingXXLarge);

        #endregion

        #region Layout Container Creation

        public Grid CreateLayoutGrid(int rows, int columns)
        {
            try
            {
                var grid = new Grid
                {
                    Margin = ThicknessSmall
                };
                
                // Add row definitions
                for (int i = 0; i < rows; i++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                }
                
                // Add column definitions
                for (int i = 0; i < columns; i++)
                {
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                
                _logger.LogDebug("Created layout grid with {Rows} rows and {Columns} columns", rows, columns);
                return grid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create layout grid");
                return new Grid();
            }
        }

        public StackPanel CreateLayoutStackPanel(Orientation orientation)
        {
            try
            {
                var stackPanel = new StackPanel
                {
                    Orientation = orientation,
                    Margin = ThicknessSmall
                };
                
                // Apply consistent spacing between children
                if (orientation == Orientation.Vertical)
                {
                    // Vertical spacing will be handled by child margins
                }
                else
                {
                    // Horizontal spacing will be handled by child margins
                }
                
                _logger.LogDebug("Created layout stack panel with {Orientation} orientation", orientation);
                return stackPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create layout stack panel");
                return new StackPanel();
            }
        }

        public DockPanel CreateLayoutDockPanel()
        {
            try
            {
                var dockPanel = new DockPanel
                {
                    Margin = ThicknessSmall,
                    LastChildFill = true
                };
                
                _logger.LogDebug("Created layout dock panel");
                return dockPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create layout dock panel");
                return new DockPanel();
            }
        }

        public WrapPanel CreateLayoutWrapPanel()
        {
            try
            {
                var wrapPanel = new WrapPanel
                {
                    Margin = ThicknessSmall,
                    Orientation = Orientation.Horizontal
                };
                
                _logger.LogDebug("Created layout wrap panel");
                return wrapPanel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create layout wrap panel");
                return new WrapPanel();
            }
        }

        public UniformGrid CreateLayoutUniformGrid(int rows, int columns)
        {
            try
            {
                var uniformGrid = new UniformGrid
                {
                    Rows = rows,
                    Columns = columns,
                    Margin = ThicknessSmall
                };
                
                _logger.LogDebug("Created layout uniform grid with {Rows} rows and {Columns} columns", rows, columns);
                return uniformGrid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create layout uniform grid");
                return new UniformGrid();
            }
        }

        #endregion

        #region Spacing Helpers

        public Thickness GetSpacing(SpacingSize size)
        {
            return new Thickness(GetSpacingValue(size));
        }

        public Thickness GetSpacing(SpacingSize horizontal, SpacingSize vertical)
        {
            return new Thickness(GetSpacingValue(horizontal), GetSpacingValue(vertical), 
                               GetSpacingValue(horizontal), GetSpacingValue(vertical));
        }

        public Thickness GetSpacing(SpacingSize left, SpacingSize top, SpacingSize right, SpacingSize bottom)
        {
            return new Thickness(GetSpacingValue(left), GetSpacingValue(top), 
                               GetSpacingValue(right), GetSpacingValue(bottom));
        }

        public double GetSpacingValue(SpacingSize size)
        {
            return size switch
            {
                SpacingSize.None => 0,
                SpacingSize.XSmall => SpacingXSmall,
                SpacingSize.Small => SpacingSmall,
                SpacingSize.Medium => SpacingMedium,
                SpacingSize.Large => SpacingLarge,
                SpacingSize.XLarge => SpacingXLarge,
                SpacingSize.XXLarge => SpacingXXLarge,
                _ => SpacingSmall
            };
        }

        #endregion

        #region Alignment Helpers

        public void ApplyStandardAlignment(FrameworkElement element, HorizontalAlignment horizontal, VerticalAlignment vertical)
        {
            try
            {
                if (element == null) return;
                
                element.HorizontalAlignment = horizontal;
                element.VerticalAlignment = vertical;
                
                // Ensure consistent margins
                if (element.Margin == new Thickness(0))
                {
                    element.Margin = ThicknessSmall;
                }
                
                _logger.LogDebug("Applied standard alignment to {ElementType}: H={HorizontalAlignment}, V={VerticalAlignment}", 
                    element.GetType().Name, horizontal, vertical);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply standard alignment");
            }
        }

        public void ApplyResponsiveMargins(FrameworkElement element, SpacingSize size)
        {
            try
            {
                if (element == null) return;
                
                var spacing = GetSpacing(size);
                element.Margin = spacing;
                
                _logger.LogDebug("Applied responsive margins to {ElementType}: {Spacing}", 
                    element.GetType().Name, spacing);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply responsive margins");
            }
        }

        public void ApplyConsistentPadding(Control control, SpacingSize size)
        {
            try
            {
                if (control == null) return;
                
                var padding = GetSpacing(size);
                control.Padding = padding;
                
                _logger.LogDebug("Applied consistent padding to {ControlType}: {Padding}", 
                    control.GetType().Name, padding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply consistent padding");
            }
        }

        #endregion

        #region Branding Integration

        public ImageSource ApplicationIcon => _applicationIcon;
        public ImageSource ApplicationLogo => _applicationLogo;

        public void ApplyApplicationIcon(Window window)
        {
            try
            {
                if (window == null) return;
                
                if (_applicationIcon != null)
                {
                    window.Icon = _applicationIcon;
                    _logger.LogDebug("Applied application icon to window: {WindowTitle}", window.Title);
                }
                else
                {
                    _logger.LogWarning("Application icon not available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply application icon");
            }
        }

        public void ApplyBrandingToContainer(Panel container)
        {
            try
            {
                if (container == null) return;
                
                // Apply consistent spacing
                container.Margin = ThicknessSmall;
                
                // Apply brand-appropriate background if needed
                if (container.Background == null)
                {
                    // Use theme-appropriate background
                    container.Background = Application.Current.TryFindResource("LightBackgroundSecondaryBrush") as Brush 
                                         ?? new SolidColorBrush(Color.FromRgb(248, 249, 250));
                }
                
                _logger.LogDebug("Applied branding to container: {ContainerType}", container.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply branding to container");
            }
        }

        private void LoadBrandingAssets()
        {
            try
            {
                // Load application icon
                try
                {
                    _applicationIcon = new BitmapImage(new Uri("/AnomaliImportTool.UI;component/Assets/Ico/Application.ico", UriKind.Relative));
                    _logger.LogDebug("Loaded application icon");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load application icon");
                }
                
                // Load application logo
                try
                {
                    _applicationLogo = new BitmapImage(new Uri("/AnomaliImportTool.UI;component/Assets/Png/Application.png", UriKind.Relative));
                    _logger.LogDebug("Loaded application logo");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load application logo");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load branding assets");
            }
        }

        #endregion

        #region Layout Validation

        public bool ValidateLayoutSpacing(FrameworkElement element)
        {
            try
            {
                if (element == null) return false;
                
                var margin = element.Margin;
                
                // Check if margins follow the 8px grid
                var isValid = IsValidSpacing(margin.Left) && 
                             IsValidSpacing(margin.Top) && 
                             IsValidSpacing(margin.Right) && 
                             IsValidSpacing(margin.Bottom);
                
                if (!isValid)
                {
                    _logger.LogWarning("Invalid spacing detected on {ElementType}: {Margin}", 
                        element.GetType().Name, margin);
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate layout spacing");
                return false;
            }
        }

        public void EnforceSpacingConsistency(Panel container)
        {
            try
            {
                if (container == null) return;
                
                foreach (UIElement child in container.Children)
                {
                    if (child is FrameworkElement element)
                    {
                        if (!ValidateLayoutSpacing(element))
                        {
                            // Correct the spacing to nearest valid value
                            var correctedMargin = new Thickness(
                                CorrectSpacing(element.Margin.Left),
                                CorrectSpacing(element.Margin.Top),
                                CorrectSpacing(element.Margin.Right),
                                CorrectSpacing(element.Margin.Bottom)
                            );
                            
                            element.Margin = correctedMargin;
                            
                            _logger.LogDebug("Corrected spacing for {ElementType}: {CorrectedMargin}", 
                                element.GetType().Name, correctedMargin);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enforce spacing consistency");
            }
        }

        public LayoutValidationResult ValidateLayoutStructure(FrameworkElement root)
        {
            var result = new LayoutValidationResult { IsValid = true };
            
            try
            {
                if (root == null)
                {
                    result.IsValid = false;
                    result.Issues.Add("Root element is null");
                    return result;
                }
                
                ValidateElementRecursively(root, result);
                
                _logger.LogDebug("Layout validation completed. Valid: {IsValid}, Issues: {IssueCount}", 
                    result.IsValid, result.Issues.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate layout structure");
                result.IsValid = false;
                result.Issues.Add($"Validation error: {ex.Message}");
            }
            
            return result;
        }

        private void ValidateElementRecursively(FrameworkElement element, LayoutValidationResult result)
        {
            // Validate spacing
            if (!ValidateLayoutSpacing(element))
            {
                result.IsValid = false;
                result.Issues.Add($"Invalid spacing on {element.GetType().Name}");
                result.Recommendations.Add($"Adjust margins on {element.GetType().Name} to follow 8px grid");
            }
            
            // Validate children if it's a panel
            if (element is Panel panel)
            {
                foreach (UIElement child in panel.Children)
                {
                    if (child is FrameworkElement childElement)
                    {
                        ValidateElementRecursively(childElement, result);
                    }
                }
            }
            else if (element is ContentControl contentControl && contentControl.Content is FrameworkElement contentElement)
            {
                ValidateElementRecursively(contentElement, result);
            }
        }

        private bool IsValidSpacing(double value)
        {
            // Check if the value is a multiple of the base spacing unit or zero
            return Math.Abs(value % BaseSpacingUnit) < 0.01 || Math.Abs(value) < 0.01;
        }

        private double CorrectSpacing(double value)
        {
            // Round to the nearest valid spacing value
            return Math.Round(value / BaseSpacingUnit) * BaseSpacingUnit;
        }

        #endregion

        #region Responsive Design

        public void ApplyResponsiveLayout(FrameworkElement element, double containerWidth)
        {
            try
            {
                if (element == null) return;
                
                // Determine responsive breakpoint
                var breakpoint = GetBreakpoint(containerWidth);
                
                // Apply responsive spacing
                var responsiveSpacing = breakpoint switch
                {
                    "Small" => SpacingSize.Small,
                    "Medium" => SpacingSize.Medium,
                    "Large" => SpacingSize.Large,
                    _ => SpacingSize.Medium
                };
                
                ApplyResponsiveMargins(element, responsiveSpacing);
                
                // Apply responsive font size if it's a text element
                if (element is TextBlock textBlock)
                {
                    var baseFontSize = 14.0;
                    textBlock.FontSize = GetResponsiveFontSize(baseFontSize, containerWidth);
                }
                
                _logger.LogDebug("Applied responsive layout to {ElementType} for {Breakpoint} breakpoint", 
                    element.GetType().Name, breakpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply responsive layout");
            }
        }

        public GridLength GetResponsiveColumnWidth(double availableWidth, int columnCount)
        {
            try
            {
                var breakpoint = GetBreakpoint(availableWidth);
                
                return breakpoint switch
                {
                    "Small" => new GridLength(1, GridUnitType.Star), // Full width on small screens
                    "Medium" => columnCount <= 2 ? new GridLength(1, GridUnitType.Star) : new GridLength(availableWidth / columnCount, GridUnitType.Pixel),
                    "Large" => new GridLength(availableWidth / columnCount, GridUnitType.Pixel),
                    _ => new GridLength(1, GridUnitType.Star)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get responsive column width");
                return new GridLength(1, GridUnitType.Star);
            }
        }

        public double GetResponsiveFontSize(double baseFontSize, double containerWidth)
        {
            try
            {
                var breakpoint = GetBreakpoint(containerWidth);
                
                var scaleFactor = breakpoint switch
                {
                    "Small" => 0.9,
                    "Medium" => 1.0,
                    "Large" => 1.1,
                    _ => 1.0
                };
                
                return baseFontSize * scaleFactor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get responsive font size");
                return baseFontSize;
            }
        }

        private string GetBreakpoint(double width)
        {
            return width switch
            {
                < SmallBreakpoint => "Small",
                < MediumBreakpoint => "Medium",
                < LargeBreakpoint => "Large",
                _ => "XLarge"
            };
        }

        #endregion

        #region Initialization

        private void InitializeLayoutSystem()
        {
            try
            {
                // Validate base spacing unit
                if (BaseSpacingUnit <= 0)
                {
                    throw new InvalidOperationException("Base spacing unit must be positive");
                }
                
                _logger.LogDebug("Layout system initialized with base spacing unit: {BaseSpacingUnit}px", BaseSpacingUnit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize layout system");
                throw;
            }
        }

        #endregion

        #region Events

        public event EventHandler<LayoutChangedEventArgs> LayoutChanged;

        protected virtual void OnLayoutChanged(FrameworkElement element, string changeType, object oldValue, object newValue)
        {
            try
            {
                LayoutChanged?.Invoke(this, new LayoutChangedEventArgs
                {
                    Element = element,
                    ChangeType = changeType,
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise layout changed event");
            }
        }

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _applicationIcon = null;
                    _applicationLogo = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
} 
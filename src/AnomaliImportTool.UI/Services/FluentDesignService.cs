using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AnomaliImportTool.UI.Services
{
    /// <summary>
    /// Service for managing Fluent Design elements with subtle shadows and depth effects
    /// Provides modern UI styling consistent with Microsoft Fluent Design System
    /// </summary>
    public interface IFluentDesignService : INotifyPropertyChanged
    {
        // Shadow Effects
        DropShadowEffect ElevationLow { get; }
        DropShadowEffect ElevationMedium { get; }
        DropShadowEffect ElevationHigh { get; }
        DropShadowEffect ElevationVeryHigh { get; }
        
        // Blur Effects
        BlurEffect BlurSubtle { get; }
        BlurEffect BlurMedium { get; }
        BlurEffect BlurStrong { get; }
        
        // Acrylic Materials
        Brush AcrylicBackgroundBrush { get; }
        Brush AcrylicSurfaceBrush { get; }
        Brush AcrylicOverlayBrush { get; }
        
        // Corner Radius Values
        CornerRadius CornerRadiusSmall { get; }
        CornerRadius CornerRadiusMedium { get; }
        CornerRadius CornerRadiusLarge { get; }
        CornerRadius CornerRadiusXLarge { get; }
        
        // Animation Durations
        Duration AnimationFast { get; }
        Duration AnimationMedium { get; }
        Duration AnimationSlow { get; }
        
        // Easing Functions
        IEasingFunction EaseInOut { get; }
        IEasingFunction EaseOut { get; }
        IEasingFunction EaseIn { get; }
        IEasingFunction EaseBackOut { get; }
        
        // Design Tokens
        double SpacingXSmall { get; }
        double SpacingSmall { get; }
        double SpacingMedium { get; }
        double SpacingLarge { get; }
        double SpacingXLarge { get; }
        double SpacingXXLarge { get; }
        
        // Methods
        DropShadowEffect CreateCustomShadow(double blurRadius, double shadowDepth, double opacity, Color color);
        Brush CreateAcrylicBrush(Color baseColor, double opacity, double noiseOpacity = 0.02);
        CornerRadius CreateCustomCornerRadius(double radius);
        Style CreateFluentButtonStyle();
        Style CreateFluentCardStyle();
        Style CreateFluentTextBoxStyle();
        
        // Theme Support
        bool IsLightTheme { get; set; }
        void ApplyFluentTheme(bool isLightTheme);
        
        // Events
        event EventHandler<FluentDesignChangedEventArgs> FluentDesignChanged;
    }

    public class FluentDesignChangedEventArgs : EventArgs
    {
        public bool IsLightTheme { get; set; }
        public Dictionary<string, object> UpdatedElements { get; set; }
    }

    public class FluentDesignService : ReactiveObject, IFluentDesignService
    {
        private readonly ILogger<FluentDesignService> _logger;
        private bool _isLightTheme = true;
        
        // Cached effects and brushes
        private DropShadowEffect _elevationLow;
        private DropShadowEffect _elevationMedium;
        private DropShadowEffect _elevationHigh;
        private DropShadowEffect _elevationVeryHigh;
        
        private BlurEffect _blurSubtle;
        private BlurEffect _blurMedium;
        private BlurEffect _blurStrong;
        
        private Brush _acrylicBackgroundBrush;
        private Brush _acrylicSurfaceBrush;
        private Brush _acrylicOverlayBrush;

        public FluentDesignService(ILogger<FluentDesignService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeFluentDesignElements();
            ApplyFluentTheme(_isLightTheme);
            
            _logger.LogInformation("Fluent Design Service initialized with light theme: {IsLightTheme}", _isLightTheme);
        }

        #region Shadow Effects - Fluent Design Elevation

        public DropShadowEffect ElevationLow => _elevationLow?.Clone() ?? CreateElevationLow();
        public DropShadowEffect ElevationMedium => _elevationMedium?.Clone() ?? CreateElevationMedium();
        public DropShadowEffect ElevationHigh => _elevationHigh?.Clone() ?? CreateElevationHigh();
        public DropShadowEffect ElevationVeryHigh => _elevationVeryHigh?.Clone() ?? CreateElevationVeryHigh();

        private DropShadowEffect CreateElevationLow()
        {
            return new DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 1,
                Opacity = _isLightTheme ? 0.14 : 0.28,
                Color = Colors.Black,
                Direction = 270
            };
        }

        private DropShadowEffect CreateElevationMedium()
        {
            return new DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 2,
                Opacity = _isLightTheme ? 0.20 : 0.35,
                Color = Colors.Black,
                Direction = 270
            };
        }

        private DropShadowEffect CreateElevationHigh()
        {
            return new DropShadowEffect
            {
                BlurRadius = 24,
                ShadowDepth = 4,
                Opacity = _isLightTheme ? 0.24 : 0.42,
                Color = Colors.Black,
                Direction = 270
            };
        }

        private DropShadowEffect CreateElevationVeryHigh()
        {
            return new DropShadowEffect
            {
                BlurRadius = 32,
                ShadowDepth = 8,
                Opacity = _isLightTheme ? 0.28 : 0.50,
                Color = Colors.Black,
                Direction = 270
            };
        }

        #endregion

        #region Blur Effects

        public BlurEffect BlurSubtle => _blurSubtle?.Clone() ?? CreateBlurSubtle();
        public BlurEffect BlurMedium => _blurMedium?.Clone() ?? CreateBlurMedium();
        public BlurEffect BlurStrong => _blurStrong?.Clone() ?? CreateBlurStrong();

        private BlurEffect CreateBlurSubtle()
        {
            return new BlurEffect { Radius = 2.0 };
        }

        private BlurEffect CreateBlurMedium()
        {
            return new BlurEffect { Radius = 8.0 };
        }

        private BlurEffect CreateBlurStrong()
        {
            return new BlurEffect { Radius = 16.0 };
        }

        #endregion

        #region Acrylic Materials

        public Brush AcrylicBackgroundBrush => _acrylicBackgroundBrush;
        public Brush AcrylicSurfaceBrush => _acrylicSurfaceBrush;
        public Brush AcrylicOverlayBrush => _acrylicOverlayBrush;

        private void CreateAcrylicBrushes()
        {
            try
            {
                if (_isLightTheme)
                {
                    _acrylicBackgroundBrush = CreateAcrylicBrush(Color.FromRgb(243, 243, 243), 0.9);
                    _acrylicSurfaceBrush = CreateAcrylicBrush(Color.FromRgb(252, 252, 252), 0.85);
                    _acrylicOverlayBrush = CreateAcrylicBrush(Color.FromRgb(255, 255, 255), 0.8);
                }
                else
                {
                    _acrylicBackgroundBrush = CreateAcrylicBrush(Color.FromRgb(44, 44, 44), 0.9);
                    _acrylicSurfaceBrush = CreateAcrylicBrush(Color.FromRgb(54, 54, 54), 0.85);
                    _acrylicOverlayBrush = CreateAcrylicBrush(Color.FromRgb(64, 64, 64), 0.8);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create acrylic brushes");
                CreateFallbackBrushes();
            }
        }

        private void CreateFallbackBrushes()
        {
            _acrylicBackgroundBrush = new SolidColorBrush(_isLightTheme ? Colors.White : Colors.Black);
            _acrylicSurfaceBrush = new SolidColorBrush(_isLightTheme ? Colors.White : Colors.Black);
            _acrylicOverlayBrush = new SolidColorBrush(_isLightTheme ? Colors.White : Colors.Black);
        }

        #endregion

        #region Corner Radius Values

        public CornerRadius CornerRadiusSmall => new CornerRadius(2);
        public CornerRadius CornerRadiusMedium => new CornerRadius(4);
        public CornerRadius CornerRadiusLarge => new CornerRadius(8);
        public CornerRadius CornerRadiusXLarge => new CornerRadius(12);

        #endregion

        #region Animation Durations and Easing

        public Duration AnimationFast => new Duration(TimeSpan.FromMilliseconds(150));
        public Duration AnimationMedium => new Duration(TimeSpan.FromMilliseconds(300));
        public Duration AnimationSlow => new Duration(TimeSpan.FromMilliseconds(500));

        public IEasingFunction EaseInOut => new CubicEase { EasingMode = EasingMode.EaseInOut };
        public IEasingFunction EaseOut => new CubicEase { EasingMode = EasingMode.EaseOut };
        public IEasingFunction EaseIn => new CubicEase { EasingMode = EasingMode.EaseIn };
        public IEasingFunction EaseBackOut => new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };

        #endregion

        #region Design Tokens - Fluent Spacing System

        public double SpacingXSmall => 4;
        public double SpacingSmall => 8;
        public double SpacingMedium => 16;
        public double SpacingLarge => 24;
        public double SpacingXLarge => 32;
        public double SpacingXXLarge => 48;

        #endregion

        #region Theme Management

        public bool IsLightTheme
        {
            get => _isLightTheme;
            set
            {
                if (_isLightTheme != value)
                {
                    _isLightTheme = value;
                    ApplyFluentTheme(value);
                    this.RaisePropertyChanged();
                    
                    _logger.LogInformation("Fluent Design theme changed to: {Theme}", value ? "Light" : "Dark");
                }
            }
        }

        public void ApplyFluentTheme(bool isLightTheme)
        {
            try
            {
                _isLightTheme = isLightTheme;
                
                // Recreate shadow effects with theme-appropriate opacity
                _elevationLow = CreateElevationLow();
                _elevationMedium = CreateElevationMedium();
                _elevationHigh = CreateElevationHigh();
                _elevationVeryHigh = CreateElevationVeryHigh();
                
                // Recreate blur effects
                _blurSubtle = CreateBlurSubtle();
                _blurMedium = CreateBlurMedium();
                _blurStrong = CreateBlurStrong();
                
                // Recreate acrylic brushes
                CreateAcrylicBrushes();
                
                // Notify property changes
                NotifyFluentElementsChanged();
                
                // Fire theme changed event
                FluentDesignChanged?.Invoke(this, new FluentDesignChangedEventArgs
                {
                    IsLightTheme = isLightTheme,
                    UpdatedElements = GetAllFluentElements()
                });
                
                _logger.LogInformation("Applied Fluent Design theme: {Theme}", isLightTheme ? "Light" : "Dark");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply Fluent Design theme");
            }
        }

        #endregion

        #region Custom Creation Methods

        public DropShadowEffect CreateCustomShadow(double blurRadius, double shadowDepth, double opacity, Color color)
        {
            try
            {
                return new DropShadowEffect
                {
                    BlurRadius = Math.Max(0, blurRadius),
                    ShadowDepth = Math.Max(0, shadowDepth),
                    Opacity = Math.Max(0, Math.Min(1, opacity)),
                    Color = color,
                    Direction = 270
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create custom shadow effect");
                return ElevationLow;
            }
        }

        public Brush CreateAcrylicBrush(Color baseColor, double opacity, double noiseOpacity = 0.02)
        {
            try
            {
                // Create a layered brush that simulates acrylic material
                var gradientBrush = new LinearGradientBrush();
                gradientBrush.StartPoint = new Point(0, 0);
                gradientBrush.EndPoint = new Point(1, 1);
                
                // Base color with opacity
                var baseColorWithOpacity = Color.FromArgb(
                    (byte)(opacity * 255),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B
                );
                
                // Slightly lighter color for gradient effect
                var lighterColor = Color.FromArgb(
                    (byte)(opacity * 255 * 0.8),
                    (byte)Math.Min(255, baseColor.R + 10),
                    (byte)Math.Min(255, baseColor.G + 10),
                    (byte)Math.Min(255, baseColor.B + 10)
                );
                
                gradientBrush.GradientStops.Add(new GradientStop(baseColorWithOpacity, 0.0));
                gradientBrush.GradientStops.Add(new GradientStop(lighterColor, 1.0));
                
                return gradientBrush;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create acrylic brush");
                return new SolidColorBrush(baseColor) { Opacity = opacity };
            }
        }

        public CornerRadius CreateCustomCornerRadius(double radius)
        {
            return new CornerRadius(Math.Max(0, radius));
        }

        #endregion

        #region Fluent Styles Creation

        public Style CreateFluentButtonStyle()
        {
            try
            {
                var style = new Style(typeof(System.Windows.Controls.Button));
                
                // Base setters
                style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(SpacingSmall)));
                style.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, 
                    new Thickness(SpacingMedium, SpacingSmall)));
                style.Setters.Add(new Setter(System.Windows.Controls.Control.BorderThicknessProperty, new Thickness(1)));
                style.Setters.Add(new Setter(System.Windows.Controls.Border.CornerRadiusProperty, CornerRadiusMedium));
                style.Setters.Add(new Setter(UIElement.EffectProperty, ElevationLow));
                
                // Background and foreground
                if (_isLightTheme)
                {
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty, 
                        new SolidColorBrush(Colors.White)));
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty, 
                        new SolidColorBrush(Colors.Black)));
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, 
                        new SolidColorBrush(Color.FromRgb(200, 200, 200))));
                }
                else
                {
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty, 
                        new SolidColorBrush(Color.FromRgb(60, 60, 60))));
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty, 
                        new SolidColorBrush(Colors.White)));
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, 
                        new SolidColorBrush(Color.FromRgb(80, 80, 80))));
                }
                
                return style;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Fluent button style");
                return new Style(typeof(System.Windows.Controls.Button));
            }
        }

        public Style CreateFluentCardStyle()
        {
            try
            {
                var style = new Style(typeof(System.Windows.Controls.Border));
                
                // Card properties
                style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(SpacingMedium)));
                style.Setters.Add(new Setter(System.Windows.Controls.Border.PaddingProperty, new Thickness(SpacingLarge)));
                style.Setters.Add(new Setter(System.Windows.Controls.Border.CornerRadiusProperty, CornerRadiusLarge));
                style.Setters.Add(new Setter(UIElement.EffectProperty, ElevationMedium));
                style.Setters.Add(new Setter(System.Windows.Controls.Border.BackgroundProperty, AcrylicSurfaceBrush));
                
                if (_isLightTheme)
                {
                    style.Setters.Add(new Setter(System.Windows.Controls.Border.BorderBrushProperty, 
                        new SolidColorBrush(Color.FromRgb(230, 230, 230))));
                }
                else
                {
                    style.Setters.Add(new Setter(System.Windows.Controls.Border.BorderBrushProperty, 
                        new SolidColorBrush(Color.FromRgb(70, 70, 70))));
                }
                
                style.Setters.Add(new Setter(System.Windows.Controls.Border.BorderThicknessProperty, new Thickness(1)));
                
                return style;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Fluent card style");
                return new Style(typeof(System.Windows.Controls.Border));
            }
        }

        public Style CreateFluentTextBoxStyle()
        {
            try
            {
                var style = new Style(typeof(System.Windows.Controls.TextBox));
                
                // TextBox properties
                style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(SpacingSmall)));
                style.Setters.Add(new Setter(System.Windows.Controls.Control.PaddingProperty, 
                    new Thickness(SpacingMedium, SpacingSmall)));
                style.Setters.Add(new Setter(System.Windows.Controls.Control.BorderThicknessProperty, new Thickness(1)));
                style.Setters.Add(new Setter(System.Windows.Controls.Border.CornerRadiusProperty, CornerRadiusSmall));
                style.Setters.Add(new Setter(System.Windows.Controls.Control.FontSizeProperty, 14.0));
                
                if (_isLightTheme)
                {
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty, 
                        new SolidColorBrush(Colors.White)));
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty, 
                        new SolidColorBrush(Colors.Black)));
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, 
                        new SolidColorBrush(Color.FromRgb(200, 200, 200))));
                }
                else
                {
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.BackgroundProperty, 
                        new SolidColorBrush(Color.FromRgb(45, 45, 45))));
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.ForegroundProperty, 
                        new SolidColorBrush(Colors.White)));
                    style.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, 
                        new SolidColorBrush(Color.FromRgb(80, 80, 80))));
                }
                
                return style;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Fluent TextBox style");
                return new Style(typeof(System.Windows.Controls.TextBox));
            }
        }

        #endregion

        #region Initialization and Helper Methods

        private void InitializeFluentDesignElements()
        {
            try
            {
                // Initialize shadow effects
                _elevationLow = CreateElevationLow();
                _elevationMedium = CreateElevationMedium();
                _elevationHigh = CreateElevationHigh();
                _elevationVeryHigh = CreateElevationVeryHigh();
                
                // Initialize blur effects
                _blurSubtle = CreateBlurSubtle();
                _blurMedium = CreateBlurMedium();
                _blurStrong = CreateBlurStrong();
                
                // Initialize acrylic brushes
                CreateAcrylicBrushes();
                
                _logger.LogDebug("Fluent Design elements initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Fluent Design elements");
                throw;
            }
        }

        private Dictionary<string, object> GetAllFluentElements()
        {
            return new Dictionary<string, object>
            {
                ["ElevationLow"] = ElevationLow,
                ["ElevationMedium"] = ElevationMedium,
                ["ElevationHigh"] = ElevationHigh,
                ["ElevationVeryHigh"] = ElevationVeryHigh,
                ["BlurSubtle"] = BlurSubtle,
                ["BlurMedium"] = BlurMedium,
                ["BlurStrong"] = BlurStrong,
                ["AcrylicBackgroundBrush"] = AcrylicBackgroundBrush,
                ["AcrylicSurfaceBrush"] = AcrylicSurfaceBrush,
                ["AcrylicOverlayBrush"] = AcrylicOverlayBrush,
                ["CornerRadiusSmall"] = CornerRadiusSmall,
                ["CornerRadiusMedium"] = CornerRadiusMedium,
                ["CornerRadiusLarge"] = CornerRadiusLarge,
                ["CornerRadiusXLarge"] = CornerRadiusXLarge
            };
        }

        private void NotifyFluentElementsChanged()
        {
            try
            {
                // Notify all Fluent Design properties
                this.RaisePropertyChanged(nameof(ElevationLow));
                this.RaisePropertyChanged(nameof(ElevationMedium));
                this.RaisePropertyChanged(nameof(ElevationHigh));
                this.RaisePropertyChanged(nameof(ElevationVeryHigh));
                
                this.RaisePropertyChanged(nameof(BlurSubtle));
                this.RaisePropertyChanged(nameof(BlurMedium));
                this.RaisePropertyChanged(nameof(BlurStrong));
                
                this.RaisePropertyChanged(nameof(AcrylicBackgroundBrush));
                this.RaisePropertyChanged(nameof(AcrylicSurfaceBrush));
                this.RaisePropertyChanged(nameof(AcrylicOverlayBrush));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify Fluent Design element property changes");
            }
        }

        #endregion

        #region Events

        public event EventHandler<FluentDesignChangedEventArgs> FluentDesignChanged;

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _elevationLow = null;
                    _elevationMedium = null;
                    _elevationHigh = null;
                    _elevationVeryHigh = null;
                    _blurSubtle = null;
                    _blurMedium = null;
                    _blurStrong = null;
                    _acrylicBackgroundBrush = null;
                    _acrylicSurfaceBrush = null;
                    _acrylicOverlayBrush = null;
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
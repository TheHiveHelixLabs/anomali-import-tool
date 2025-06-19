using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AnomaliImportTool.UI.Services
{
    /// <summary>
    /// Service for managing professional typography system with Segoe UI font hierarchy
    /// Provides text styles, font scaling, and accessibility features
    /// </summary>
    public interface IProfessionalTypographyService : INotifyPropertyChanged
    {
        // Font Families
        FontFamily PrimaryFontFamily { get; }
        FontFamily SecondaryFontFamily { get; }
        FontFamily MonospaceFontFamily { get; }
        FontFamily IconFontFamily { get; }
        
        // Typography Styles
        TypographyStyle DisplayLarge { get; }
        TypographyStyle DisplayMedium { get; }
        TypographyStyle DisplaySmall { get; }
        TypographyStyle HeadlineLarge { get; }
        TypographyStyle HeadlineMedium { get; }
        TypographyStyle HeadlineSmall { get; }
        TypographyStyle TitleLarge { get; }
        TypographyStyle TitleMedium { get; }
        TypographyStyle TitleSmall { get; }
        TypographyStyle BodyLarge { get; }
        TypographyStyle BodyMedium { get; }
        TypographyStyle BodySmall { get; }
        TypographyStyle LabelLarge { get; }
        TypographyStyle LabelMedium { get; }
        TypographyStyle LabelSmall { get; }
        TypographyStyle CaptionLarge { get; }
        TypographyStyle CaptionMedium { get; }
        TypographyStyle CaptionSmall { get; }
        
        // Scaling and Accessibility
        double FontScaleFactor { get; set; }
        bool IsHighReadabilityMode { get; set; }
        double LineHeightMultiplier { get; set; }
        double LetterSpacingMultiplier { get; set; }
        
        // Methods
        TypographyStyle GetTypographyStyle(TypographyLevel level, TypographySize size);
        TypographyStyle CreateCustomStyle(string fontFamily, double fontSize, FontWeight fontWeight, double lineHeight = 1.2);
        void ApplyAccessibilitySettings(bool highReadability, double fontScale);
        Dictionary<string, TypographyStyle> GetAllStyles();
        
        // Events
        event EventHandler<TypographyChangedEventArgs> TypographyChanged;
    }

    public enum TypographyLevel
    {
        Display,
        Headline,
        Title,
        Body,
        Label,
        Caption
    }

    public enum TypographySize
    {
        Small,
        Medium,
        Large
    }

    public class TypographyStyle
    {
        public FontFamily FontFamily { get; set; }
        public double FontSize { get; set; }
        public FontWeight FontWeight { get; set; }
        public double LineHeight { get; set; }
        public double LetterSpacing { get; set; }
        public FontStyle FontStyle { get; set; }
        public TextDecorationCollection TextDecorations { get; set; }
        public double MarginTop { get; set; }
        public double MarginBottom { get; set; }
        
        public TypographyStyle()
        {
            FontStyle = FontStyles.Normal;
            TextDecorations = null;
            MarginTop = 0;
            MarginBottom = 0;
        }
        
        public TypographyStyle Clone()
        {
            return new TypographyStyle
            {
                FontFamily = FontFamily,
                FontSize = FontSize,
                FontWeight = FontWeight,
                LineHeight = LineHeight,
                LetterSpacing = LetterSpacing,
                FontStyle = FontStyle,
                TextDecorations = TextDecorations,
                MarginTop = MarginTop,
                MarginBottom = MarginBottom
            };
        }
    }

    public class TypographyChangedEventArgs : EventArgs
    {
        public double PreviousScaleFactor { get; set; }
        public double NewScaleFactor { get; set; }
        public bool PreviousHighReadability { get; set; }
        public bool NewHighReadability { get; set; }
        public Dictionary<string, TypographyStyle> UpdatedStyles { get; set; }
    }

    public class ProfessionalTypographyService : ReactiveObject, IProfessionalTypographyService
    {
        private readonly ILogger<ProfessionalTypographyService> _logger;
        private double _fontScaleFactor = 1.0;
        private bool _isHighReadabilityMode = false;
        private double _lineHeightMultiplier = 1.0;
        private double _letterSpacingMultiplier = 1.0;
        
        private Dictionary<string, TypographyStyle> _baseStyles;
        private Dictionary<string, TypographyStyle> _currentStyles;

        // Font Family Constants
        private static readonly FontFamily SegoeUI = new FontFamily("Segoe UI");
        private static readonly FontFamily SegoeUIVariable = new FontFamily("Segoe UI Variable");
        private static readonly FontFamily ConsoleFont = new FontFamily("Consolas, Courier New");
        private static readonly FontFamily IconFont = new FontFamily("Segoe MDL2 Assets");

        public ProfessionalTypographyService(ILogger<ProfessionalTypographyService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeTypographySystem();
            ApplyCurrentSettings();
            
            _logger.LogInformation("Professional Typography Service initialized with Segoe UI font hierarchy");
        }

        #region Font Families

        public FontFamily PrimaryFontFamily => SegoeUI;
        public FontFamily SecondaryFontFamily => SegoeUIVariable;
        public FontFamily MonospaceFontFamily => ConsoleFont;
        public FontFamily IconFontFamily => IconFont;

        #endregion

        #region Typography Styles - Professional Hierarchy

        public TypographyStyle DisplayLarge => GetCurrentStyle("DisplayLarge");
        public TypographyStyle DisplayMedium => GetCurrentStyle("DisplayMedium");
        public TypographyStyle DisplaySmall => GetCurrentStyle("DisplaySmall");
        
        public TypographyStyle HeadlineLarge => GetCurrentStyle("HeadlineLarge");
        public TypographyStyle HeadlineMedium => GetCurrentStyle("HeadlineMedium");
        public TypographyStyle HeadlineSmall => GetCurrentStyle("HeadlineSmall");
        
        public TypographyStyle TitleLarge => GetCurrentStyle("TitleLarge");
        public TypographyStyle TitleMedium => GetCurrentStyle("TitleMedium");
        public TypographyStyle TitleSmall => GetCurrentStyle("TitleSmall");
        
        public TypographyStyle BodyLarge => GetCurrentStyle("BodyLarge");
        public TypographyStyle BodyMedium => GetCurrentStyle("BodyMedium");
        public TypographyStyle BodySmall => GetCurrentStyle("BodySmall");
        
        public TypographyStyle LabelLarge => GetCurrentStyle("LabelLarge");
        public TypographyStyle LabelMedium => GetCurrentStyle("LabelMedium");
        public TypographyStyle LabelSmall => GetCurrentStyle("LabelSmall");
        
        public TypographyStyle CaptionLarge => GetCurrentStyle("CaptionLarge");
        public TypographyStyle CaptionMedium => GetCurrentStyle("CaptionMedium");
        public TypographyStyle CaptionSmall => GetCurrentStyle("CaptionSmall");

        #endregion

        #region Scaling and Accessibility Properties

        public double FontScaleFactor
        {
            get => _fontScaleFactor;
            set
            {
                if (Math.Abs(_fontScaleFactor - value) > 0.01)
                {
                    var previousValue = _fontScaleFactor;
                    _fontScaleFactor = Math.Max(0.5, Math.Min(3.0, value)); // Clamp between 0.5x and 3.0x
                    ApplyCurrentSettings();
                    
                    this.RaisePropertyChanged();
                    NotifyTypographyChanged(previousValue, _fontScaleFactor);
                    
                    _logger.LogInformation("Font scale factor changed from {Previous} to {New}", previousValue, _fontScaleFactor);
                }
            }
        }

        public bool IsHighReadabilityMode
        {
            get => _isHighReadabilityMode;
            set
            {
                if (_isHighReadabilityMode != value)
                {
                    var previousValue = _isHighReadabilityMode;
                    _isHighReadabilityMode = value;
                    ApplyCurrentSettings();
                    
                    this.RaisePropertyChanged();
                    NotifyTypographyChanged(_fontScaleFactor, _fontScaleFactor, previousValue, value);
                    
                    _logger.LogInformation("High readability mode changed from {Previous} to {New}", previousValue, value);
                }
            }
        }

        public double LineHeightMultiplier
        {
            get => _lineHeightMultiplier;
            set
            {
                if (Math.Abs(_lineHeightMultiplier - value) > 0.01)
                {
                    _lineHeightMultiplier = Math.Max(0.8, Math.Min(2.0, value)); // Clamp between 0.8x and 2.0x
                    ApplyCurrentSettings();
                    this.RaisePropertyChanged();
                    
                    _logger.LogDebug("Line height multiplier changed to {Value}", _lineHeightMultiplier);
                }
            }
        }

        public double LetterSpacingMultiplier
        {
            get => _letterSpacingMultiplier;
            set
            {
                if (Math.Abs(_letterSpacingMultiplier - value) > 0.01)
                {
                    _letterSpacingMultiplier = Math.Max(0.5, Math.Min(2.0, value)); // Clamp between 0.5x and 2.0x
                    ApplyCurrentSettings();
                    this.RaisePropertyChanged();
                    
                    _logger.LogDebug("Letter spacing multiplier changed to {Value}", _letterSpacingMultiplier);
                }
            }
        }

        #endregion

        #region Typography System Initialization

        private void InitializeTypographySystem()
        {
            try
            {
                _baseStyles = CreateBaseTypographyStyles();
                _currentStyles = new Dictionary<string, TypographyStyle>();
                
                _logger.LogDebug("Typography system initialized with {StyleCount} base styles", _baseStyles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize typography system");
                throw;
            }
        }

        private Dictionary<string, TypographyStyle> CreateBaseTypographyStyles()
        {
            return new Dictionary<string, TypographyStyle>
            {
                // Display Styles - For large prominent text
                ["DisplayLarge"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 64,
                    FontWeight = FontWeights.Light,
                    LineHeight = 72,
                    LetterSpacing = -0.5,
                    MarginBottom = 24
                },
                ["DisplayMedium"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 48,
                    FontWeight = FontWeights.Light,
                    LineHeight = 56,
                    LetterSpacing = -0.25,
                    MarginBottom = 20
                },
                ["DisplaySmall"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 36,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 44,
                    LetterSpacing = 0,
                    MarginBottom = 16
                },
                
                // Headline Styles - For section headers
                ["HeadlineLarge"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 32,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 40,
                    LetterSpacing = 0,
                    MarginBottom = 16
                },
                ["HeadlineMedium"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 28,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 36,
                    LetterSpacing = 0,
                    MarginBottom = 14
                },
                ["HeadlineSmall"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 24,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 32,
                    LetterSpacing = 0,
                    MarginBottom = 12
                },
                
                // Title Styles - For subsection headers
                ["TitleLarge"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 22,
                    FontWeight = FontWeights.SemiBold,
                    LineHeight = 28,
                    LetterSpacing = 0,
                    MarginBottom = 10
                },
                ["TitleMedium"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    LineHeight = 24,
                    LetterSpacing = 0.1,
                    MarginBottom = 8
                },
                ["TitleSmall"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    LineHeight = 20,
                    LetterSpacing = 0.1,
                    MarginBottom = 6
                },
                
                // Body Styles - For main content
                ["BodyLarge"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 16,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 24,
                    LetterSpacing = 0.1,
                    MarginBottom = 12
                },
                ["BodyMedium"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 14,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 20,
                    LetterSpacing = 0.15,
                    MarginBottom = 10
                },
                ["BodySmall"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 12,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 16,
                    LetterSpacing = 0.2,
                    MarginBottom = 8
                },
                
                // Label Styles - For UI labels and buttons
                ["LabelLarge"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 14,
                    FontWeight = FontWeights.Medium,
                    LineHeight = 20,
                    LetterSpacing = 0.1,
                    MarginBottom = 4
                },
                ["LabelMedium"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    LineHeight = 16,
                    LetterSpacing = 0.15,
                    MarginBottom = 4
                },
                ["LabelSmall"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    LineHeight = 14,
                    LetterSpacing = 0.2,
                    MarginBottom = 2
                },
                
                // Caption Styles - For annotations and metadata
                ["CaptionLarge"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 12,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 16,
                    LetterSpacing = 0.2,
                    MarginBottom = 4
                },
                ["CaptionMedium"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 11,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 14,
                    LetterSpacing = 0.25,
                    MarginBottom = 2
                },
                ["CaptionSmall"] = new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 10,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 12,
                    LetterSpacing = 0.3,
                    MarginBottom = 2
                }
            };
        }

        #endregion

        #region Style Application and Management

        private void ApplyCurrentSettings()
        {
            try
            {
                _currentStyles.Clear();
                
                foreach (var (styleName, baseStyle) in _baseStyles)
                {
                    var appliedStyle = ApplySettingsToStyle(baseStyle);
                    _currentStyles[styleName] = appliedStyle;
                }
                
                NotifyAllStylesChanged();
                
                _logger.LogDebug("Applied current settings to {StyleCount} typography styles", _currentStyles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply current typography settings");
            }
        }

        private TypographyStyle ApplySettingsToStyle(TypographyStyle baseStyle)
        {
            var appliedStyle = baseStyle.Clone();
            
            // Apply font scaling
            appliedStyle.FontSize *= _fontScaleFactor;
            appliedStyle.LineHeight *= _fontScaleFactor;
            appliedStyle.MarginTop *= _fontScaleFactor;
            appliedStyle.MarginBottom *= _fontScaleFactor;
            
            // Apply line height multiplier
            appliedStyle.LineHeight *= _lineHeightMultiplier;
            
            // Apply letter spacing multiplier
            appliedStyle.LetterSpacing *= _letterSpacingMultiplier;
            
            // Apply high readability adjustments
            if (_isHighReadabilityMode)
            {
                ApplyHighReadabilitySettings(appliedStyle);
            }
            
            return appliedStyle;
        }

        private void ApplyHighReadabilitySettings(TypographyStyle style)
        {
            // Increase line height for better readability
            style.LineHeight *= 1.2;
            
            // Increase letter spacing slightly
            style.LetterSpacing += 0.05;
            
            // Increase font weight for better contrast
            if (style.FontWeight == FontWeights.Light)
                style.FontWeight = FontWeights.Normal;
            else if (style.FontWeight == FontWeights.Normal)
                style.FontWeight = FontWeights.Medium;
            
            // Increase margins for better spacing
            style.MarginBottom *= 1.5;
        }

        #endregion

        #region Public Methods

        public TypographyStyle GetTypographyStyle(TypographyLevel level, TypographySize size)
        {
            try
            {
                var styleName = $"{level}{size}";
                return GetCurrentStyle(styleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get typography style: {Level} {Size}", level, size);
                return GetCurrentStyle("BodyMedium"); // Safe fallback
            }
        }

        public TypographyStyle CreateCustomStyle(string fontFamily, double fontSize, FontWeight fontWeight, double lineHeight = 1.2)
        {
            try
            {
                var customStyle = new TypographyStyle
                {
                    FontFamily = new FontFamily(fontFamily),
                    FontSize = fontSize * _fontScaleFactor,
                    FontWeight = fontWeight,
                    LineHeight = fontSize * lineHeight * _fontScaleFactor * _lineHeightMultiplier,
                    LetterSpacing = 0.1 * _letterSpacingMultiplier
                };
                
                if (_isHighReadabilityMode)
                {
                    ApplyHighReadabilitySettings(customStyle);
                }
                
                return customStyle;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create custom typography style");
                return GetCurrentStyle("BodyMedium"); // Safe fallback
            }
        }

        public void ApplyAccessibilitySettings(bool highReadability, double fontScale)
        {
            try
            {
                var previousReadability = _isHighReadabilityMode;
                var previousScale = _fontScaleFactor;
                
                _isHighReadabilityMode = highReadability;
                _fontScaleFactor = Math.Max(0.5, Math.Min(3.0, fontScale));
                
                ApplyCurrentSettings();
                
                this.RaisePropertyChanged(nameof(IsHighReadabilityMode));
                this.RaisePropertyChanged(nameof(FontScaleFactor));
                
                NotifyTypographyChanged(previousScale, _fontScaleFactor, previousReadability, _isHighReadabilityMode);
                
                _logger.LogInformation("Applied accessibility settings - High Readability: {HighReadability}, Font Scale: {FontScale}", 
                    highReadability, fontScale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply accessibility settings");
            }
        }

        public Dictionary<string, TypographyStyle> GetAllStyles()
        {
            return _currentStyles?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone()) ?? new Dictionary<string, TypographyStyle>();
        }

        #endregion

        #region Helper Methods

        private TypographyStyle GetCurrentStyle(string styleName)
        {
            try
            {
                if (_currentStyles?.TryGetValue(styleName, out var style) == true)
                {
                    return style.Clone();
                }
                
                _logger.LogWarning("Typography style not found: {StyleName}", styleName);
                
                // Return a safe default style
                return new TypographyStyle
                {
                    FontFamily = PrimaryFontFamily,
                    FontSize = 14 * _fontScaleFactor,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 20 * _fontScaleFactor * _lineHeightMultiplier,
                    LetterSpacing = 0.1 * _letterSpacingMultiplier
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current typography style: {StyleName}", styleName);
                
                // Return absolute fallback
                return new TypographyStyle
                {
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 14,
                    FontWeight = FontWeights.Normal,
                    LineHeight = 20,
                    LetterSpacing = 0.1
                };
            }
        }

        #endregion

        #region Property Change Notifications

        private void NotifyAllStylesChanged()
        {
            try
            {
                // Notify all typography style properties
                this.RaisePropertyChanged(nameof(DisplayLarge));
                this.RaisePropertyChanged(nameof(DisplayMedium));
                this.RaisePropertyChanged(nameof(DisplaySmall));
                
                this.RaisePropertyChanged(nameof(HeadlineLarge));
                this.RaisePropertyChanged(nameof(HeadlineMedium));
                this.RaisePropertyChanged(nameof(HeadlineSmall));
                
                this.RaisePropertyChanged(nameof(TitleLarge));
                this.RaisePropertyChanged(nameof(TitleMedium));
                this.RaisePropertyChanged(nameof(TitleSmall));
                
                this.RaisePropertyChanged(nameof(BodyLarge));
                this.RaisePropertyChanged(nameof(BodyMedium));
                this.RaisePropertyChanged(nameof(BodySmall));
                
                this.RaisePropertyChanged(nameof(LabelLarge));
                this.RaisePropertyChanged(nameof(LabelMedium));
                this.RaisePropertyChanged(nameof(LabelSmall));
                
                this.RaisePropertyChanged(nameof(CaptionLarge));
                this.RaisePropertyChanged(nameof(CaptionMedium));
                this.RaisePropertyChanged(nameof(CaptionSmall));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify typography style property changes");
            }
        }

        private void NotifyTypographyChanged(double previousScale, double newScale, 
            bool previousReadability = false, bool newReadability = false)
        {
            try
            {
                TypographyChanged?.Invoke(this, new TypographyChangedEventArgs
                {
                    PreviousScaleFactor = previousScale,
                    NewScaleFactor = newScale,
                    PreviousHighReadability = previousReadability,
                    NewHighReadability = newReadability,
                    UpdatedStyles = GetAllStyles()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify typography changed event");
            }
        }

        #endregion

        #region Events

        public event EventHandler<TypographyChangedEventArgs> TypographyChanged;

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _baseStyles?.Clear();
                    _baseStyles = null;
                    _currentStyles?.Clear();
                    _currentStyles = null;
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
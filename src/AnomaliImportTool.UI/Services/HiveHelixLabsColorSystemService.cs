using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AnomaliImportTool.UI.Services
{
    /// <summary>
    /// Service for managing Hive Helix Labs corporate color system with WCAG AA compliance
    /// Provides brand colors, accessibility validation, and theme management
    /// </summary>
    public interface IHiveHelixLabsColorSystemService : INotifyPropertyChanged
    {
        // Brand Colors
        Color PrimaryBrand { get; }
        Color SecondaryBrand { get; }
        Color AccentBrand { get; }
        Color NeutralDark { get; }
        Color NeutralLight { get; }
        
        // Theme Colors
        Color BackgroundPrimary { get; }
        Color BackgroundSecondary { get; }
        Color SurfacePrimary { get; }
        Color SurfaceSecondary { get; }
        
        // Text Colors
        Color TextPrimary { get; }
        Color TextSecondary { get; }
        Color TextDisabled { get; }
        Color TextOnPrimary { get; }
        Color TextOnSecondary { get; }
        
        // Status Colors
        Color SuccessColor { get; }
        Color WarningColor { get; }
        Color ErrorColor { get; }
        Color InfoColor { get; }
        
        // Interactive Colors
        Color ButtonPrimary { get; }
        Color ButtonSecondary { get; }
        Color ButtonHover { get; }
        Color ButtonPressed { get; }
        Color ButtonDisabled { get; }
        
        // Current Theme
        HiveHelixLabsTheme CurrentTheme { get; set; }
        bool IsDarkTheme { get; }
        
        // Methods
        bool ValidateWcagCompliance(Color foreground, Color background);
        double CalculateContrastRatio(Color foreground, Color background);
        Color GetAccessibleTextColor(Color background);
        Dictionary<string, Color> GetThemeColors();
        void ApplyTheme(HiveHelixLabsTheme theme);
        Color GetColorByName(string colorName);
        
        // Events
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;
    }

    public enum HiveHelixLabsTheme
    {
        Light,
        Dark,
        HighContrastLight,
        HighContrastDark,
        Corporate
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public HiveHelixLabsTheme PreviousTheme { get; set; }
        public HiveHelixLabsTheme NewTheme { get; set; }
        public Dictionary<string, Color> NewColors { get; set; }
    }

    public class HiveHelixLabsColorSystemService : ReactiveObject, IHiveHelixLabsColorSystemService
    {
        private readonly ILogger<HiveHelixLabsColorSystemService> _logger;
        private HiveHelixLabsTheme _currentTheme = HiveHelixLabsTheme.Light;
        private Dictionary<string, Color> _currentColors;

        // WCAG AA Compliance Constants
        private const double WcagAaNormalRatio = 4.5;
        private const double WcagAaLargeRatio = 3.0;
        private const double WcagAaaRatio = 7.0;

        public HiveHelixLabsColorSystemService(ILogger<HiveHelixLabsColorSystemService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeColorSystem();
            ApplyTheme(_currentTheme);
            
            _logger.LogInformation("Hive Helix Labs Color System Service initialized with theme: {Theme}", _currentTheme);
        }

        #region Brand Colors - Hive Helix Labs Corporate Identity

        public Color PrimaryBrand => GetCurrentColor("PrimaryBrand");
        public Color SecondaryBrand => GetCurrentColor("SecondaryBrand");
        public Color AccentBrand => GetCurrentColor("AccentBrand");
        public Color NeutralDark => GetCurrentColor("NeutralDark");
        public Color NeutralLight => GetCurrentColor("NeutralLight");

        #endregion

        #region Theme Colors

        public Color BackgroundPrimary => GetCurrentColor("BackgroundPrimary");
        public Color BackgroundSecondary => GetCurrentColor("BackgroundSecondary");
        public Color SurfacePrimary => GetCurrentColor("SurfacePrimary");
        public Color SurfaceSecondary => GetCurrentColor("SurfaceSecondary");

        #endregion

        #region Text Colors

        public Color TextPrimary => GetCurrentColor("TextPrimary");
        public Color TextSecondary => GetCurrentColor("TextSecondary");
        public Color TextDisabled => GetCurrentColor("TextDisabled");
        public Color TextOnPrimary => GetCurrentColor("TextOnPrimary");
        public Color TextOnSecondary => GetCurrentColor("TextOnSecondary");

        #endregion

        #region Status Colors

        public Color SuccessColor => GetCurrentColor("SuccessColor");
        public Color WarningColor => GetCurrentColor("WarningColor");
        public Color ErrorColor => GetCurrentColor("ErrorColor");
        public Color InfoColor => GetCurrentColor("InfoColor");

        #endregion

        #region Interactive Colors

        public Color ButtonPrimary => GetCurrentColor("ButtonPrimary");
        public Color ButtonSecondary => GetCurrentColor("ButtonSecondary");
        public Color ButtonHover => GetCurrentColor("ButtonHover");
        public Color ButtonPressed => GetCurrentColor("ButtonPressed");
        public Color ButtonDisabled => GetCurrentColor("ButtonDisabled");

        #endregion

        #region Theme Management

        public HiveHelixLabsTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    var previousTheme = _currentTheme;
                    _currentTheme = value;
                    ApplyTheme(value);
                    
                    this.RaisePropertyChanged();
                    this.RaisePropertyChanged(nameof(IsDarkTheme));
                    
                    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                    {
                        PreviousTheme = previousTheme,
                        NewTheme = value,
                        NewColors = GetThemeColors()
                    });
                    
                    _logger.LogInformation("Theme changed from {PreviousTheme} to {NewTheme}", previousTheme, value);
                }
            }
        }

        public bool IsDarkTheme => _currentTheme == HiveHelixLabsTheme.Dark || 
                                   _currentTheme == HiveHelixLabsTheme.HighContrastDark;

        #endregion

        #region Color System Initialization

        private void InitializeColorSystem()
        {
            try
            {
                // Initialize with light theme colors first
                _currentColors = new Dictionary<string, Color>();
                
                _logger.LogDebug("Color system initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize color system");
                throw;
            }
        }

        #endregion

        #region Theme Application

        public void ApplyTheme(HiveHelixLabsTheme theme)
        {
            try
            {
                var previousColors = _currentColors?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                _currentColors = theme switch
                {
                    HiveHelixLabsTheme.Light => GetLightThemeColors(),
                    HiveHelixLabsTheme.Dark => GetDarkThemeColors(),
                    HiveHelixLabsTheme.HighContrastLight => GetHighContrastLightColors(),
                    HiveHelixLabsTheme.HighContrastDark => GetHighContrastDarkColors(),
                    HiveHelixLabsTheme.Corporate => GetCorporateThemeColors(),
                    _ => GetLightThemeColors()
                };

                // Validate WCAG compliance for critical color combinations
                ValidateThemeCompliance();
                
                // Notify all color properties changed
                NotifyAllColorsChanged();
                
                _logger.LogInformation("Applied theme: {Theme} with {ColorCount} colors", theme, _currentColors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme: {Theme}", theme);
                throw;
            }
        }

        private Dictionary<string, Color> GetLightThemeColors()
        {
            return new Dictionary<string, Color>
            {
                // Brand Colors - Hive Helix Labs Identity
                ["PrimaryBrand"] = Color.FromRgb(0x00, 0x7A, 0xCC), // Professional Blue
                ["SecondaryBrand"] = Color.FromRgb(0x6B, 0x73, 0x80), // Neutral Gray
                ["AccentBrand"] = Color.FromRgb(0xFF, 0x6B, 0x35), // Energetic Orange
                ["NeutralDark"] = Color.FromRgb(0x32, 0x33, 0x36), // Almost Black
                ["NeutralLight"] = Color.FromRgb(0xF8, 0xF9, 0xFA), // Almost White
                
                // Background Colors
                ["BackgroundPrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["BackgroundSecondary"] = Color.FromRgb(0xF8, 0xF9, 0xFA), // Light Gray
                ["SurfacePrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // White Surface
                ["SurfaceSecondary"] = Color.FromRgb(0xF1, 0xF3, 0xF4), // Light Surface
                
                // Text Colors
                ["TextPrimary"] = Color.FromRgb(0x32, 0x33, 0x36), // Dark Text
                ["TextSecondary"] = Color.FromRgb(0x5F, 0x63, 0x68), // Medium Text
                ["TextDisabled"] = Color.FromRgb(0x9A, 0xA0, 0xA6), // Light Text
                ["TextOnPrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // White on Brand
                ["TextOnSecondary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // White on Secondary
                
                // Status Colors
                ["SuccessColor"] = Color.FromRgb(0x34, 0xA8, 0x53), // Green
                ["WarningColor"] = Color.FromRgb(0xFB, 0xBC, 0x04), // Yellow
                ["ErrorColor"] = Color.FromRgb(0xEA, 0x43, 0x35), // Red
                ["InfoColor"] = Color.FromRgb(0x42, 0x85, 0xF4), // Blue
                
                // Interactive Colors
                ["ButtonPrimary"] = Color.FromRgb(0x00, 0x7A, 0xCC), // Brand Blue
                ["ButtonSecondary"] = Color.FromRgb(0x6B, 0x73, 0x80), // Neutral
                ["ButtonHover"] = Color.FromRgb(0x00, 0x5A, 0x9C), // Darker Blue
                ["ButtonPressed"] = Color.FromRgb(0x00, 0x4A, 0x7C), // Even Darker
                ["ButtonDisabled"] = Color.FromRgb(0xE8, 0xEA, 0xED) // Light Gray
            };
        }

        private Dictionary<string, Color> GetDarkThemeColors()
        {
            return new Dictionary<string, Color>
            {
                // Brand Colors - Adapted for Dark Theme
                ["PrimaryBrand"] = Color.FromRgb(0x4F, 0xA5, 0xD5), // Lighter Blue
                ["SecondaryBrand"] = Color.FromRgb(0x9A, 0xA0, 0xA6), // Light Gray
                ["AccentBrand"] = Color.FromRgb(0xFF, 0x8A, 0x65), // Lighter Orange
                ["NeutralDark"] = Color.FromRgb(0xF8, 0xF9, 0xFA), // Light for Dark Theme
                ["NeutralLight"] = Color.FromRgb(0x32, 0x33, 0x36), // Dark for Dark Theme
                
                // Background Colors
                ["BackgroundPrimary"] = Color.FromRgb(0x20, 0x21, 0x24), // Dark Background
                ["BackgroundSecondary"] = Color.FromRgb(0x28, 0x29, 0x2C), // Slightly Lighter
                ["SurfacePrimary"] = Color.FromRgb(0x32, 0x33, 0x36), // Dark Surface
                ["SurfaceSecondary"] = Color.FromRgb(0x3C, 0x3D, 0x40), // Medium Surface
                
                // Text Colors
                ["TextPrimary"] = Color.FromRgb(0xF8, 0xF9, 0xFA), // Light Text
                ["TextSecondary"] = Color.FromRgb(0xE8, 0xEA, 0xED), // Medium Light
                ["TextDisabled"] = Color.FromRgb(0x9A, 0xA0, 0xA6), // Gray Text
                ["TextOnPrimary"] = Color.FromRgb(0x20, 0x21, 0x24), // Dark on Brand
                ["TextOnSecondary"] = Color.FromRgb(0xF8, 0xF9, 0xFA), // Light on Secondary
                
                // Status Colors - Adjusted for Dark Theme
                ["SuccessColor"] = Color.FromRgb(0x5B, 0xB8, 0x6F), // Lighter Green
                ["WarningColor"] = Color.FromRgb(0xFF, 0xD6, 0x3B), // Lighter Yellow
                ["ErrorColor"] = Color.FromRgb(0xF2, 0x8B, 0x82), // Lighter Red
                ["InfoColor"] = Color.FromRgb(0x66, 0x9D, 0xF6), // Lighter Blue
                
                // Interactive Colors
                ["ButtonPrimary"] = Color.FromRgb(0x4F, 0xA5, 0xD5), // Brand Blue
                ["ButtonSecondary"] = Color.FromRgb(0x5F, 0x63, 0x68), // Dark Neutral
                ["ButtonHover"] = Color.FromRgb(0x6F, 0xB5, 0xE5), // Lighter Blue
                ["ButtonPressed"] = Color.FromRgb(0x3F, 0x95, 0xC5), // Darker Blue
                ["ButtonDisabled"] = Color.FromRgb(0x5F, 0x63, 0x68) // Dark Gray
            };
        }

        private Dictionary<string, Color> GetHighContrastLightColors()
        {
            return new Dictionary<string, Color>
            {
                // High Contrast Light Theme - Maximum Accessibility
                ["PrimaryBrand"] = Color.FromRgb(0x00, 0x00, 0xFF), // Pure Blue
                ["SecondaryBrand"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                ["AccentBrand"] = Color.FromRgb(0xFF, 0x00, 0x00), // Pure Red
                ["NeutralDark"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                ["NeutralLight"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                
                // Background Colors
                ["BackgroundPrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["BackgroundSecondary"] = Color.FromRgb(0xF0, 0xF0, 0xF0), // Light Gray
                ["SurfacePrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["SurfaceSecondary"] = Color.FromRgb(0xE0, 0xE0, 0xE0), // Light Gray
                
                // Text Colors - High Contrast
                ["TextPrimary"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                ["TextSecondary"] = Color.FromRgb(0x40, 0x40, 0x40), // Dark Gray
                ["TextDisabled"] = Color.FromRgb(0x80, 0x80, 0x80), // Medium Gray
                ["TextOnPrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["TextOnSecondary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                
                // Status Colors - High Contrast
                ["SuccessColor"] = Color.FromRgb(0x00, 0x80, 0x00), // Dark Green
                ["WarningColor"] = Color.FromRgb(0xFF, 0x80, 0x00), // Orange
                ["ErrorColor"] = Color.FromRgb(0xFF, 0x00, 0x00), // Pure Red
                ["InfoColor"] = Color.FromRgb(0x00, 0x00, 0xFF), // Pure Blue
                
                // Interactive Colors
                ["ButtonPrimary"] = Color.FromRgb(0x00, 0x00, 0xFF), // Pure Blue
                ["ButtonSecondary"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                ["ButtonHover"] = Color.FromRgb(0x00, 0x00, 0xCC), // Dark Blue
                ["ButtonPressed"] = Color.FromRgb(0x00, 0x00, 0x99), // Darker Blue
                ["ButtonDisabled"] = Color.FromRgb(0xC0, 0xC0, 0xC0) // Light Gray
            };
        }

        private Dictionary<string, Color> GetHighContrastDarkColors()
        {
            return new Dictionary<string, Color>
            {
                // High Contrast Dark Theme - Maximum Accessibility
                ["PrimaryBrand"] = Color.FromRgb(0x00, 0xFF, 0xFF), // Cyan
                ["SecondaryBrand"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["AccentBrand"] = Color.FromRgb(0xFF, 0xFF, 0x00), // Yellow
                ["NeutralDark"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["NeutralLight"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                
                // Background Colors
                ["BackgroundPrimary"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                ["BackgroundSecondary"] = Color.FromRgb(0x20, 0x20, 0x20), // Dark Gray
                ["SurfacePrimary"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                ["SurfaceSecondary"] = Color.FromRgb(0x40, 0x40, 0x40), // Medium Gray
                
                // Text Colors - High Contrast
                ["TextPrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["TextSecondary"] = Color.FromRgb(0xC0, 0xC0, 0xC0), // Light Gray
                ["TextDisabled"] = Color.FromRgb(0x80, 0x80, 0x80), // Medium Gray
                ["TextOnPrimary"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                ["TextOnSecondary"] = Color.FromRgb(0x00, 0x00, 0x00), // Pure Black
                
                // Status Colors - High Contrast
                ["SuccessColor"] = Color.FromRgb(0x00, 0xFF, 0x00), // Bright Green
                ["WarningColor"] = Color.FromRgb(0xFF, 0xFF, 0x00), // Bright Yellow
                ["ErrorColor"] = Color.FromRgb(0xFF, 0x00, 0x00), // Bright Red
                ["InfoColor"] = Color.FromRgb(0x00, 0xFF, 0xFF), // Bright Cyan
                
                // Interactive Colors
                ["ButtonPrimary"] = Color.FromRgb(0x00, 0xFF, 0xFF), // Cyan
                ["ButtonSecondary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["ButtonHover"] = Color.FromRgb(0x33, 0xFF, 0xFF), // Light Cyan
                ["ButtonPressed"] = Color.FromRgb(0x00, 0xCC, 0xCC), // Dark Cyan
                ["ButtonDisabled"] = Color.FromRgb(0x40, 0x40, 0x40) // Dark Gray
            };
        }

        private Dictionary<string, Color> GetCorporateThemeColors()
        {
            return new Dictionary<string, Color>
            {
                // Corporate Theme - Professional Hive Helix Labs
                ["PrimaryBrand"] = Color.FromRgb(0x00, 0x7A, 0xCC), // Corporate Blue
                ["SecondaryBrand"] = Color.FromRgb(0x4A, 0x4A, 0x4A), // Corporate Gray
                ["AccentBrand"] = Color.FromRgb(0xE6, 0x7E, 0x22), // Corporate Orange
                ["NeutralDark"] = Color.FromRgb(0x2D, 0x2D, 0x30), // Corporate Dark
                ["NeutralLight"] = Color.FromRgb(0xF7, 0xF7, 0xF7), // Corporate Light
                
                // Background Colors
                ["BackgroundPrimary"] = Color.FromRgb(0xFD, 0xFD, 0xFD), // Off White
                ["BackgroundSecondary"] = Color.FromRgb(0xF7, 0xF7, 0xF7), // Light Gray
                ["SurfacePrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // Pure White
                ["SurfaceSecondary"] = Color.FromRgb(0xF0, 0xF2, 0xF5), // Corporate Surface
                
                // Text Colors
                ["TextPrimary"] = Color.FromRgb(0x2D, 0x2D, 0x30), // Corporate Dark
                ["TextSecondary"] = Color.FromRgb(0x6E, 0x6E, 0x73), // Corporate Medium
                ["TextDisabled"] = Color.FromRgb(0xAE, 0xAE, 0xB2), // Corporate Light
                ["TextOnPrimary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // White on Brand
                ["TextOnSecondary"] = Color.FromRgb(0xFF, 0xFF, 0xFF), // White on Secondary
                
                // Status Colors - Corporate Approved
                ["SuccessColor"] = Color.FromRgb(0x30, 0xD1, 0x58), // Corporate Green
                ["WarningColor"] = Color.FromRgb(0xFF, 0xCC, 0x02), // Corporate Yellow
                ["ErrorColor"] = Color.FromRgb(0xFF, 0x38, 0x24), // Corporate Red
                ["InfoColor"] = Color.FromRgb(0x00, 0x7A, 0xCC), // Corporate Blue
                
                // Interactive Colors
                ["ButtonPrimary"] = Color.FromRgb(0x00, 0x7A, 0xCC), // Corporate Blue
                ["ButtonSecondary"] = Color.FromRgb(0x4A, 0x4A, 0x4A), // Corporate Gray
                ["ButtonHover"] = Color.FromRgb(0x00, 0x5A, 0x9C), // Darker Corporate Blue
                ["ButtonPressed"] = Color.FromRgb(0x00, 0x4A, 0x7C), // Even Darker
                ["ButtonDisabled"] = Color.FromRgb(0xE5, 0xE5, 0xE7) // Corporate Light Gray
            };
        }

        #endregion

        #region WCAG Compliance Validation

        public bool ValidateWcagCompliance(Color foreground, Color background)
        {
            try
            {
                var contrastRatio = CalculateContrastRatio(foreground, background);
                var isCompliant = contrastRatio >= WcagAaNormalRatio;
                
                _logger.LogDebug("WCAG validation - Contrast ratio: {Ratio:F2}, Compliant: {IsCompliant}", 
                    contrastRatio, isCompliant);
                
                return isCompliant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate WCAG compliance");
                return false;
            }
        }

        public double CalculateContrastRatio(Color foreground, Color background)
        {
            try
            {
                var foregroundLuminance = GetRelativeLuminance(foreground);
                var backgroundLuminance = GetRelativeLuminance(background);
                
                var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
                var darker = Math.Min(foregroundLuminance, backgroundLuminance);
                
                return (lighter + 0.05) / (darker + 0.05);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to calculate contrast ratio");
                return 1.0; // Minimum contrast ratio
            }
        }

        private double GetRelativeLuminance(Color color)
        {
            var r = GetLinearRgbComponent(color.R / 255.0);
            var g = GetLinearRgbComponent(color.G / 255.0);
            var b = GetLinearRgbComponent(color.B / 255.0);
            
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }

        private double GetLinearRgbComponent(double component)
        {
            return component <= 0.03928 
                ? component / 12.92 
                : Math.Pow((component + 0.055) / 1.055, 2.4);
        }

        public Color GetAccessibleTextColor(Color background)
        {
            try
            {
                var whiteContrast = CalculateContrastRatio(Colors.White, background);
                var blackContrast = CalculateContrastRatio(Colors.Black, background);
                
                var textColor = whiteContrast > blackContrast ? Colors.White : Colors.Black;
                
                _logger.LogDebug("Accessible text color selected: {Color} (White: {WhiteContrast:F2}, Black: {BlackContrast:F2})", 
                    textColor, whiteContrast, blackContrast);
                
                return textColor;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to determine accessible text color");
                return Colors.Black; // Safe default
            }
        }

        private void ValidateThemeCompliance()
        {
            try
            {
                var criticalCombinations = new[]
                {
                    ("TextPrimary", "BackgroundPrimary"),
                    ("TextSecondary", "BackgroundSecondary"),
                    ("TextOnPrimary", "ButtonPrimary"),
                    ("TextOnSecondary", "ButtonSecondary")
                };

                foreach (var (foregroundName, backgroundName) in criticalCombinations)
                {
                    if (_currentColors.TryGetValue(foregroundName, out var foreground) &&
                        _currentColors.TryGetValue(backgroundName, out var background))
                    {
                        var isCompliant = ValidateWcagCompliance(foreground, background);
                        if (!isCompliant)
                        {
                            _logger.LogWarning("WCAG compliance issue: {Foreground} on {Background} has insufficient contrast", 
                                foregroundName, backgroundName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate theme compliance");
            }
        }

        #endregion

        #region Color Access Methods

        public Dictionary<string, Color> GetThemeColors()
        {
            return _currentColors?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, Color>();
        }

        public Color GetColorByName(string colorName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(colorName))
                {
                    _logger.LogWarning("Color name is null or empty");
                    return Colors.Transparent;
                }

                if (_currentColors.TryGetValue(colorName, out var color))
                {
                    return color;
                }

                _logger.LogWarning("Color not found: {ColorName}", colorName);
                return Colors.Transparent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get color by name: {ColorName}", colorName);
                return Colors.Transparent;
            }
        }

        private Color GetCurrentColor(string colorName)
        {
            return GetColorByName(colorName);
        }

        #endregion

        #region Property Change Notifications

        private void NotifyAllColorsChanged()
        {
            try
            {
                // Notify all color properties
                this.RaisePropertyChanged(nameof(PrimaryBrand));
                this.RaisePropertyChanged(nameof(SecondaryBrand));
                this.RaisePropertyChanged(nameof(AccentBrand));
                this.RaisePropertyChanged(nameof(NeutralDark));
                this.RaisePropertyChanged(nameof(NeutralLight));
                
                this.RaisePropertyChanged(nameof(BackgroundPrimary));
                this.RaisePropertyChanged(nameof(BackgroundSecondary));
                this.RaisePropertyChanged(nameof(SurfacePrimary));
                this.RaisePropertyChanged(nameof(SurfaceSecondary));
                
                this.RaisePropertyChanged(nameof(TextPrimary));
                this.RaisePropertyChanged(nameof(TextSecondary));
                this.RaisePropertyChanged(nameof(TextDisabled));
                this.RaisePropertyChanged(nameof(TextOnPrimary));
                this.RaisePropertyChanged(nameof(TextOnSecondary));
                
                this.RaisePropertyChanged(nameof(SuccessColor));
                this.RaisePropertyChanged(nameof(WarningColor));
                this.RaisePropertyChanged(nameof(ErrorColor));
                this.RaisePropertyChanged(nameof(InfoColor));
                
                this.RaisePropertyChanged(nameof(ButtonPrimary));
                this.RaisePropertyChanged(nameof(ButtonSecondary));
                this.RaisePropertyChanged(nameof(ButtonHover));
                this.RaisePropertyChanged(nameof(ButtonPressed));
                this.RaisePropertyChanged(nameof(ButtonDisabled));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify color property changes");
            }
        }

        #endregion

        #region Events

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _currentColors?.Clear();
                    _currentColors = null;
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
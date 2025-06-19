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
    /// Service for managing dark/light theme switching with corporate branding
    /// Provides seamless theme transitions and persistent theme preferences
    /// </summary>
    public interface IThemeSwitchingService : INotifyPropertyChanged
    {
        // Current Theme State
        ApplicationTheme CurrentTheme { get; set; }
        bool IsDarkTheme { get; }
        bool IsLightTheme { get; }
        bool IsHighContrastTheme { get; }
        bool IsSystemTheme { get; }
        
        // Theme Management
        void ApplyTheme(ApplicationTheme theme);
        void ToggleTheme();
        void ApplySystemTheme();
        void SaveThemePreference();
        void LoadThemePreference();
        
        // Theme Resources
        ResourceDictionary GetCurrentThemeResources();
        Color GetThemeColor(string colorKey);
        Brush GetThemeBrush(string brushKey);
        Style GetThemeStyle(string styleKey);
        
        // System Integration
        bool IsSystemDarkMode { get; }
        void RegisterForSystemThemeChanges();
        void UnregisterFromSystemThemeChanges();
        
        // Events
        event EventHandler<ThemeChangedEventArgs> ThemeChanged;
        event EventHandler<SystemThemeChangedEventArgs> SystemThemeChanged;
    }

    public enum ApplicationTheme
    {
        Light,
        Dark,
        HighContrastLight,
        HighContrastDark,
        System
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ApplicationTheme PreviousTheme { get; set; }
        public ApplicationTheme NewTheme { get; set; }
        public ResourceDictionary NewResources { get; set; }
        public bool IsSystemTriggered { get; set; }
    }

    public class SystemThemeChangedEventArgs : EventArgs
    {
        public bool IsSystemDarkMode { get; set; }
        public ApplicationTheme RecommendedTheme { get; set; }
    }

    public class ThemeSwitchingService : ReactiveObject, IThemeSwitchingService
    {
        private readonly ILogger<ThemeSwitchingService> _logger;
        private ApplicationTheme _currentTheme = ApplicationTheme.Light;
        private ResourceDictionary _currentThemeResources;
        private bool _isSystemThemeRegistered = false;
        
        // Theme resource dictionaries
        private readonly Dictionary<ApplicationTheme, ResourceDictionary> _themeResources;
        
        // System theme detection
        private readonly System.Timers.Timer _systemThemeCheckTimer;
        private bool _lastKnownSystemDarkMode = false;

        public ThemeSwitchingService(ILogger<ThemeSwitchingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _themeResources = new Dictionary<ApplicationTheme, ResourceDictionary>();
            _systemThemeCheckTimer = new System.Timers.Timer(5000); // Check every 5 seconds
            _systemThemeCheckTimer.Elapsed += OnSystemThemeCheckTimer;
            
            InitializeThemeResources();
            LoadThemePreference();
            ApplyTheme(_currentTheme);
            
            _logger.LogInformation("Theme Switching Service initialized with theme: {Theme}", _currentTheme);
        }

        #region Theme State Properties

        public ApplicationTheme CurrentTheme
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
                    this.RaisePropertyChanged(nameof(IsLightTheme));
                    this.RaisePropertyChanged(nameof(IsHighContrastTheme));
                    this.RaisePropertyChanged(nameof(IsSystemTheme));
                    
                    SaveThemePreference();
                    
                    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                    {
                        PreviousTheme = previousTheme,
                        NewTheme = value,
                        NewResources = GetCurrentThemeResources(),
                        IsSystemTriggered = false
                    });
                    
                    _logger.LogInformation("Theme changed from {PreviousTheme} to {NewTheme}", previousTheme, value);
                }
            }
        }

        public bool IsDarkTheme => _currentTheme == ApplicationTheme.Dark || 
                                   _currentTheme == ApplicationTheme.HighContrastDark ||
                                   (_currentTheme == ApplicationTheme.System && IsSystemDarkMode);

        public bool IsLightTheme => _currentTheme == ApplicationTheme.Light || 
                                    _currentTheme == ApplicationTheme.HighContrastLight ||
                                    (_currentTheme == ApplicationTheme.System && !IsSystemDarkMode);

        public bool IsHighContrastTheme => _currentTheme == ApplicationTheme.HighContrastLight || 
                                           _currentTheme == ApplicationTheme.HighContrastDark;

        public bool IsSystemTheme => _currentTheme == ApplicationTheme.System;

        #endregion

        #region System Theme Detection

        public bool IsSystemDarkMode
        {
            get
            {
                try
                {
                    // Check Windows registry for system theme
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        if (key?.GetValue("AppsUseLightTheme") is int value)
                        {
                            return value == 0; // 0 = Dark theme, 1 = Light theme
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to detect system theme, defaulting to light");
                }
                
                return false; // Default to light theme
            }
        }

        public void RegisterForSystemThemeChanges()
        {
            try
            {
                if (!_isSystemThemeRegistered)
                {
                    _lastKnownSystemDarkMode = IsSystemDarkMode;
                    _systemThemeCheckTimer.Start();
                    _isSystemThemeRegistered = true;
                    
                    _logger.LogInformation("Registered for system theme changes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register for system theme changes");
            }
        }

        public void UnregisterFromSystemThemeChanges()
        {
            try
            {
                if (_isSystemThemeRegistered)
                {
                    _systemThemeCheckTimer.Stop();
                    _isSystemThemeRegistered = false;
                    
                    _logger.LogInformation("Unregistered from system theme changes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister from system theme changes");
            }
        }

        private void OnSystemThemeCheckTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                var currentSystemDarkMode = IsSystemDarkMode;
                if (currentSystemDarkMode != _lastKnownSystemDarkMode)
                {
                    _lastKnownSystemDarkMode = currentSystemDarkMode;
                    
                    // Notify about system theme change
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SystemThemeChanged?.Invoke(this, new SystemThemeChangedEventArgs
                        {
                            IsSystemDarkMode = currentSystemDarkMode,
                            RecommendedTheme = currentSystemDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light
                        });
                        
                        // If current theme is System, apply the change
                        if (_currentTheme == ApplicationTheme.System)
                        {
                            ApplySystemTheme();
                        }
                    });
                    
                    _logger.LogInformation("System theme changed to: {Theme}", currentSystemDarkMode ? "Dark" : "Light");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking system theme");
            }
        }

        #endregion

        #region Theme Resource Management

        private void InitializeThemeResources()
        {
            try
            {
                // Load theme resources from XAML files
                _themeResources[ApplicationTheme.Light] = LoadThemeResourceDictionary("Light");
                _themeResources[ApplicationTheme.Dark] = LoadThemeResourceDictionary("Dark");
                _themeResources[ApplicationTheme.HighContrastLight] = LoadThemeResourceDictionary("HighContrastLight");
                _themeResources[ApplicationTheme.HighContrastDark] = LoadThemeResourceDictionary("HighContrastDark");
                
                _logger.LogDebug("Theme resources initialized for {ThemeCount} themes", _themeResources.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize theme resources");
                CreateFallbackThemeResources();
            }
        }

        private ResourceDictionary LoadThemeResourceDictionary(string themeName)
        {
            try
            {
                // Use the main HiveHelixLabsTheme.xaml as base and create theme-specific variations
                var themeDict = new ResourceDictionary();
                
                // Load base theme
                var baseTheme = new ResourceDictionary
                {
                    Source = new Uri("/AnomaliImportTool.UI;component/Themes/HiveHelixLabsTheme.xaml", UriKind.Relative)
                };
                
                // Merge base theme
                themeDict.MergedDictionaries.Add(baseTheme);
                
                // Apply theme-specific overrides
                ApplyThemeSpecificOverrides(themeDict, themeName);
                
                return themeDict;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load theme resource dictionary for {ThemeName}", themeName);
                return new ResourceDictionary();
            }
        }

        private void ApplyThemeSpecificOverrides(ResourceDictionary themeDict, string themeName)
        {
            try
            {
                switch (themeName)
                {
                    case "Dark":
                        ApplyDarkThemeOverrides(themeDict);
                        break;
                    case "HighContrastLight":
                        ApplyHighContrastLightOverrides(themeDict);
                        break;
                    case "HighContrastDark":
                        ApplyHighContrastDarkOverrides(themeDict);
                        break;
                    case "Light":
                    default:
                        // Light theme is the default, no overrides needed
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme-specific overrides for {ThemeName}", themeName);
            }
        }

        private void ApplyDarkThemeOverrides(ResourceDictionary themeDict)
        {
            // Override with dark theme colors
            themeDict["BackgroundPrimary"] = new SolidColorBrush((Color)themeDict["DarkBackgroundPrimaryColor"]);
            themeDict["BackgroundSecondary"] = new SolidColorBrush((Color)themeDict["DarkBackgroundSecondaryColor"]);
            themeDict["SurfacePrimary"] = new SolidColorBrush((Color)themeDict["DarkSurfacePrimaryColor"]);
            themeDict["SurfaceSecondary"] = new SolidColorBrush((Color)themeDict["DarkSurfaceSecondaryColor"]);
            themeDict["TextPrimary"] = new SolidColorBrush((Color)themeDict["DarkTextPrimaryColor"]);
            themeDict["TextSecondary"] = new SolidColorBrush((Color)themeDict["DarkTextSecondaryColor"]);
            themeDict["TextDisabled"] = new SolidColorBrush((Color)themeDict["DarkTextDisabledColor"]);
            
            // Adjust shadow effects for dark theme
            var elevationLow = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 8,
                ShadowDepth = 1,
                Opacity = 0.28, // Higher opacity for dark theme
                Color = Colors.Black,
                Direction = 270
            };
            themeDict["ElevationLow"] = elevationLow;
        }

        private void ApplyHighContrastLightOverrides(ResourceDictionary themeDict)
        {
            // High contrast light theme overrides
            themeDict["BackgroundPrimary"] = new SolidColorBrush(Colors.White);
            themeDict["TextPrimary"] = new SolidColorBrush(Colors.Black);
            themeDict["HiveHelixPrimaryBrush"] = new SolidColorBrush(Colors.Blue);
            themeDict["SuccessBrush"] = new SolidColorBrush(Colors.DarkGreen);
            themeDict["ErrorBrush"] = new SolidColorBrush(Colors.Red);
            themeDict["WarningBrush"] = new SolidColorBrush(Colors.Orange);
        }

        private void ApplyHighContrastDarkOverrides(ResourceDictionary themeDict)
        {
            // High contrast dark theme overrides
            themeDict["BackgroundPrimary"] = new SolidColorBrush(Colors.Black);
            themeDict["TextPrimary"] = new SolidColorBrush(Colors.White);
            themeDict["HiveHelixPrimaryBrush"] = new SolidColorBrush(Colors.Cyan);
            themeDict["SuccessBrush"] = new SolidColorBrush(Colors.Lime);
            themeDict["ErrorBrush"] = new SolidColorBrush(Colors.Red);
            themeDict["WarningBrush"] = new SolidColorBrush(Colors.Yellow);
        }

        private void CreateFallbackThemeResources()
        {
            try
            {
                // Create minimal fallback resources
                var fallbackDict = new ResourceDictionary();
                
                // Basic colors
                fallbackDict["HiveHelixPrimaryColor"] = Color.FromRgb(0x00, 0x7A, 0xCC);
                fallbackDict["HiveHelixPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC));
                fallbackDict["BackgroundPrimary"] = new SolidColorBrush(Colors.White);
                fallbackDict["TextPrimary"] = new SolidColorBrush(Colors.Black);
                
                _themeResources[ApplicationTheme.Light] = fallbackDict;
                _themeResources[ApplicationTheme.Dark] = fallbackDict;
                _themeResources[ApplicationTheme.HighContrastLight] = fallbackDict;
                _themeResources[ApplicationTheme.HighContrastDark] = fallbackDict;
                
                _logger.LogWarning("Created fallback theme resources");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create fallback theme resources");
            }
        }

        #endregion

        #region Theme Application

        public void ApplyTheme(ApplicationTheme theme)
        {
            try
            {
                var effectiveTheme = theme;
                
                // Resolve system theme to actual theme
                if (theme == ApplicationTheme.System)
                {
                    effectiveTheme = IsSystemDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light;
                }
                
                // Get theme resources
                if (_themeResources.TryGetValue(effectiveTheme, out var themeResources))
                {
                    _currentThemeResources = themeResources;
                    
                    // Apply to application resources
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Clear existing theme resources
                        var existingThemeDict = Application.Current.Resources.MergedDictionaries
                            .FirstOrDefault(d => d.Source?.ToString().Contains("HiveHelixLabsTheme") == true);
                        
                        if (existingThemeDict != null)
                        {
                            Application.Current.Resources.MergedDictionaries.Remove(existingThemeDict);
                        }
                        
                        // Add new theme resources
                        Application.Current.Resources.MergedDictionaries.Add(themeResources);
                    });
                    
                    _logger.LogInformation("Applied theme: {Theme} (effective: {EffectiveTheme})", theme, effectiveTheme);
                }
                else
                {
                    _logger.LogWarning("Theme resources not found for theme: {Theme}", effectiveTheme);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply theme: {Theme}", theme);
            }
        }

        public void ToggleTheme()
        {
            try
            {
                var newTheme = _currentTheme switch
                {
                    ApplicationTheme.Light => ApplicationTheme.Dark,
                    ApplicationTheme.Dark => ApplicationTheme.Light,
                    ApplicationTheme.HighContrastLight => ApplicationTheme.HighContrastDark,
                    ApplicationTheme.HighContrastDark => ApplicationTheme.HighContrastLight,
                    ApplicationTheme.System => IsSystemDarkMode ? ApplicationTheme.Light : ApplicationTheme.Dark,
                    _ => ApplicationTheme.Light
                };
                
                CurrentTheme = newTheme;
                
                _logger.LogInformation("Toggled theme to: {Theme}", newTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle theme");
            }
        }

        public void ApplySystemTheme()
        {
            try
            {
                if (_currentTheme == ApplicationTheme.System)
                {
                    ApplyTheme(ApplicationTheme.System);
                    
                    // Notify that theme was applied due to system change
                    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs
                    {
                        PreviousTheme = _currentTheme,
                        NewTheme = _currentTheme,
                        NewResources = GetCurrentThemeResources(),
                        IsSystemTriggered = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply system theme");
            }
        }

        #endregion

        #region Theme Resource Access

        public ResourceDictionary GetCurrentThemeResources()
        {
            return _currentThemeResources ?? new ResourceDictionary();
        }

        public Color GetThemeColor(string colorKey)
        {
            try
            {
                if (_currentThemeResources?.Contains(colorKey) == true)
                {
                    if (_currentThemeResources[colorKey] is Color color)
                    {
                        return color;
                    }
                    else if (_currentThemeResources[colorKey] is SolidColorBrush brush)
                    {
                        return brush.Color;
                    }
                }
                
                _logger.LogWarning("Theme color not found: {ColorKey}", colorKey);
                return Colors.Transparent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get theme color: {ColorKey}", colorKey);
                return Colors.Transparent;
            }
        }

        public Brush GetThemeBrush(string brushKey)
        {
            try
            {
                if (_currentThemeResources?.Contains(brushKey) == true)
                {
                    if (_currentThemeResources[brushKey] is Brush brush)
                    {
                        return brush;
                    }
                }
                
                _logger.LogWarning("Theme brush not found: {BrushKey}", brushKey);
                return new SolidColorBrush(Colors.Transparent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get theme brush: {BrushKey}", brushKey);
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public Style GetThemeStyle(string styleKey)
        {
            try
            {
                if (_currentThemeResources?.Contains(styleKey) == true)
                {
                    if (_currentThemeResources[styleKey] is Style style)
                    {
                        return style;
                    }
                }
                
                _logger.LogWarning("Theme style not found: {StyleKey}", styleKey);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get theme style: {StyleKey}", styleKey);
                return null;
            }
        }

        #endregion

        #region Theme Persistence

        public void SaveThemePreference()
        {
            try
            {
                var settings = Properties.Settings.Default;
                settings.PreferredTheme = _currentTheme.ToString();
                settings.Save();
                
                _logger.LogDebug("Saved theme preference: {Theme}", _currentTheme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save theme preference");
            }
        }

        public void LoadThemePreference()
        {
            try
            {
                var settings = Properties.Settings.Default;
                if (!string.IsNullOrEmpty(settings.PreferredTheme))
                {
                    if (Enum.TryParse<ApplicationTheme>(settings.PreferredTheme, out var savedTheme))
                    {
                        _currentTheme = savedTheme;
                        _logger.LogDebug("Loaded theme preference: {Theme}", _currentTheme);
                        return;
                    }
                }
                
                // Default to system theme if no preference saved
                _currentTheme = ApplicationTheme.System;
                _logger.LogDebug("No theme preference found, defaulting to System theme");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load theme preference, using default");
                _currentTheme = ApplicationTheme.Light;
            }
        }

        #endregion

        #region Events

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;
        public event EventHandler<SystemThemeChangedEventArgs> SystemThemeChanged;

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    UnregisterFromSystemThemeChanges();
                    _systemThemeCheckTimer?.Dispose();
                    _themeResources?.Clear();
                    _currentThemeResources = null;
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
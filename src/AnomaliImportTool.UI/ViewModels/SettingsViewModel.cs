using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Infrastructure.Services;
using System.Windows;
using Serilog;

namespace AnomaliImportTool.WPF.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ConfigurationService _configurationService;
        private readonly ISecurityService _securityService;
        private readonly IAnomaliApiClient _apiClient;
        private readonly ILogger _logger;

        public readonly WindowViewModel WindowViewModel;
        public readonly NavbarViewModel NavbarViewModel;

        [ObservableProperty]
        private string _baseUrl = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _apiKey = string.Empty;

        [ObservableProperty]
        private int _timeoutSeconds = 30;

        [ObservableProperty]
        private int _maxRetryAttempts = 3;

        [ObservableProperty]
        private bool _useProxy = false;

        [ObservableProperty]
        private string _proxyUrl = string.Empty;

        [ObservableProperty]
        private bool _ignoreSslErrors = false;

        [ObservableProperty]
        private bool _isTestingConnection = false;

        [ObservableProperty]
        private bool _isSaving = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isStatusSuccess = false;

        [ObservableProperty]
        private bool _hasUnsavedChanges = false;

        public SettingsViewModel()
        {
            WindowViewModel = App.Services!.GetRequiredService<WindowViewModel>();
            NavbarViewModel = App.Services!.GetRequiredService<NavbarViewModel>();
            _configurationService = App.Services!.GetRequiredService<ConfigurationService>();
            _securityService = App.Services!.GetRequiredService<ISecurityService>();
            _apiClient = App.Services!.GetRequiredService<IAnomaliApiClient>();
            _logger = App.Services!.GetRequiredService<ILogger>();

            // Load current configuration
            LoadCurrentConfiguration();

            // Set up property change tracking
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Track changes to configuration properties
            var configProperties = new[] { 
                nameof(BaseUrl), nameof(Username), nameof(ApiKey), 
                nameof(TimeoutSeconds), nameof(MaxRetryAttempts), 
                nameof(UseProxy), nameof(ProxyUrl), nameof(IgnoreSslErrors) 
            };

            if (configProperties.Contains(e.PropertyName))
            {
                HasUnsavedChanges = true;
                ClearStatus();
            }
        }

        private void LoadCurrentConfiguration()
        {
            try
            {
                var config = _configurationService.Current?.ApiConfiguration;
                if (config != null)
                {
                    BaseUrl = config.BaseUrl;
                    Username = config.Username;
                    // Load encrypted API key securely
                    ApiKey = _securityService.GetApiKey() ?? string.Empty;
                    TimeoutSeconds = config.TimeoutSeconds;
                    MaxRetryAttempts = config.MaxRetryAttempts;
                    UseProxy = config.UseProxy;
                    ProxyUrl = config.ProxyUrl;
                    IgnoreSslErrors = config.IgnoreSslErrors;
                }
                
                HasUnsavedChanges = false;
                _logger.Information("API configuration loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load API configuration");
                SetStatus("Failed to load configuration", false);
            }
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if (IsTestingConnection) return;

            try
            {
                IsTestingConnection = true;
                SetStatus("Testing connection...", true);

                // Validate configuration first
                var config = CreateApiConfiguration();
                if (!config.IsValid())
                {
                    SetStatus("Invalid configuration. Please check all required fields.", false);
                    return;
                }

                // Test the connection
                var isConnected = await _apiClient.TestConnectionAsync();
                
                if (isConnected)
                {
                    SetStatus("✅ Connection successful!", true);
                }
                else
                {
                    SetStatus("❌ Connection failed. Please check your settings.", false);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Connection test failed");
                SetStatus($"❌ Connection failed: {ex.Message}", false);
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        [RelayCommand]
        private async Task SaveConfigurationAsync()
        {
            if (IsSaving) return;

            try
            {
                IsSaving = true;
                SetStatus("Saving configuration...", true);

                // Validate configuration
                var config = CreateApiConfiguration();
                if (!config.IsValid())
                {
                    SetStatus("Invalid configuration. Please check all required fields.", false);
                    return;
                }

                // Save API key securely
                await _securityService.SaveApiKeyAsync(ApiKey);

                // Update configuration (without sensitive data)
                var configToSave = new ApiConfiguration
                {
                    BaseUrl = config.BaseUrl,
                    Username = config.Username,
                    ApiKey = string.Empty, // Don't store in plain text
                    TimeoutSeconds = config.TimeoutSeconds,
                    MaxRetryAttempts = config.MaxRetryAttempts,
                    UseProxy = config.UseProxy,
                    ProxyUrl = config.ProxyUrl,
                    IgnoreSslErrors = config.IgnoreSslErrors
                };

                await _configurationService.UpdateSectionAsync("api", configToSave);

                HasUnsavedChanges = false;
                SetStatus("✅ Configuration saved successfully!", true);
                _logger.Information("API configuration saved successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save API configuration");
                SetStatus($"❌ Failed to save: {ex.Message}", false);
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void ResetToDefaults()
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all API settings to defaults? This will clear all current values.",
                "Reset to Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                BaseUrl = string.Empty;
                Username = string.Empty;
                ApiKey = string.Empty;
                TimeoutSeconds = 30;
                MaxRetryAttempts = 3;
                UseProxy = false;
                ProxyUrl = string.Empty;
                IgnoreSslErrors = false;
                
                SetStatus("Settings reset to defaults", true);
            }
        }

        [RelayCommand]
        private void ClearApiKey()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear the stored API key?",
                "Clear API Key",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ApiKey = string.Empty;
                try
                {
                    _securityService.ClearApiKey();
                    SetStatus("API key cleared", true);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to clear API key");
                    SetStatus("Failed to clear API key", false);
                }
            }
        }

        private ApiConfiguration CreateApiConfiguration()
        {
            return new ApiConfiguration
            {
                BaseUrl = BaseUrl?.Trim() ?? string.Empty,
                Username = Username?.Trim() ?? string.Empty,
                ApiKey = ApiKey?.Trim() ?? string.Empty,
                TimeoutSeconds = TimeoutSeconds,
                MaxRetryAttempts = MaxRetryAttempts,
                UseProxy = UseProxy,
                ProxyUrl = ProxyUrl?.Trim() ?? string.Empty,
                IgnoreSslErrors = IgnoreSslErrors
            };
        }

        private void SetStatus(string message, bool isSuccess)
        {
            StatusMessage = message;
            IsStatusSuccess = isSuccess;
        }

        private void ClearStatus()
        {
            StatusMessage = string.Empty;
        }

        // Validation properties for UI binding
        public bool IsConfigurationValid => CreateApiConfiguration().IsValid();
        
        public string BaseUrlValidationMessage => 
            string.IsNullOrWhiteSpace(BaseUrl) ? "Base URL is required" : 
            !Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ? "Invalid URL format" :
            uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp ? "URL must use HTTP or HTTPS" :
            string.Empty;

        public string UsernameValidationMessage => 
            string.IsNullOrWhiteSpace(Username) ? "Username is required" : string.Empty;

        public string ApiKeyValidationMessage => 
            string.IsNullOrWhiteSpace(ApiKey) ? "API Key is required" : string.Empty;

        public string TimeoutValidationMessage => 
            TimeoutSeconds <= 0 ? "Timeout must be greater than 0" : string.Empty;

        public string RetryValidationMessage => 
            MaxRetryAttempts < 0 ? "Retry attempts cannot be negative" : string.Empty;

        public string ProxyUrlValidationMessage => 
            UseProxy && string.IsNullOrWhiteSpace(ProxyUrl) ? "Proxy URL is required when proxy is enabled" :
            UseProxy && !Uri.TryCreate(ProxyUrl, UriKind.Absolute, out _) ? "Invalid proxy URL format" :
            string.Empty;
    }
}
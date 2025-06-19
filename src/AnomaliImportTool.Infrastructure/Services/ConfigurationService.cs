using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Models;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace AnomaliImportTool.Infrastructure.Services
{
    /// <summary>
    /// Service for managing application configuration with JSON persistence
    /// </summary>
    public class ConfigurationService
    {
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private AppConfiguration _currentConfig;
        private readonly object _configLock = new object();

        public ConfigurationService(ILogger logger = null)
        {
            _logger = logger ?? Log.Logger;
            
            // Use portable configuration location relative to app directory
            _configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            _configFilePath = Path.Combine(_configDirectory, "appsettings.json");
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
            
            InitializeConfiguration();
        }

        /// <summary>
        /// Gets the current configuration
        /// </summary>
        public AppConfiguration Current
        {
            get
            {
                lock (_configLock)
                {
                    return _currentConfig;
                }
            }
        }

        /// <summary>
        /// Event raised when configuration changes
        /// </summary>
        public event EventHandler<ConfigurationChangedEventArgs> ConfigurationChanged;

        /// <summary>
        /// Initializes configuration, loading from file or creating defaults
        /// </summary>
        private void InitializeConfiguration()
        {
            try
            {
                Directory.CreateDirectory(_configDirectory);
                
                if (File.Exists(_configFilePath))
                {
                    LoadConfiguration();
                }
                else
                {
                    _currentConfig = CreateDefaultConfiguration();
                    SaveConfiguration();
                    _logger.Information("Created default configuration at {ConfigPath}", _configFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to initialize configuration, using defaults");
                _currentConfig = CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Loads configuration from JSON file
        /// </summary>
        public void LoadConfiguration()
        {
            try
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
                
                // Validate loaded configuration
                var validationResult = ValidateConfiguration(config);
                if (!validationResult.IsValid)
                {
                    _logger.Warning("Configuration validation failed: {Errors}", 
                        string.Join(", ", validationResult.Errors));
                    
                    // Merge with defaults to fix missing values
                    config = MergeWithDefaults(config);
                }
                
                lock (_configLock)
                {
                    _currentConfig = config;
                }
                
                _logger.Information("Configuration loaded from {ConfigPath}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load configuration from {ConfigPath}", _configFilePath);
                throw;
            }
        }

        /// <summary>
        /// Saves current configuration to JSON file
        /// </summary>
        public void SaveConfiguration()
        {
            try
            {
                lock (_configLock)
                {
                    var json = JsonSerializer.Serialize(_currentConfig, _jsonOptions);
                    File.WriteAllText(_configFilePath, json);
                }
                
                _logger.Information("Configuration saved to {ConfigPath}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to save configuration to {ConfigPath}", _configFilePath);
                throw;
            }
        }

        /// <summary>
        /// Updates configuration with new values
        /// </summary>
        public async Task UpdateConfigurationAsync(AppConfiguration newConfig)
        {
            if (newConfig == null)
                throw new ArgumentNullException(nameof(newConfig));
            
            // Validate new configuration
            var validationResult = ValidateConfiguration(newConfig);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(
                    $"Configuration validation failed: {string.Join(", ", validationResult.Errors)}");
            }
            
            var oldConfig = _currentConfig;
            
            lock (_configLock)
            {
                _currentConfig = newConfig;
            }
            
            // Save to file
            SaveConfiguration();
            
            // Raise configuration changed event
            ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
            {
                OldConfiguration = oldConfig,
                NewConfiguration = newConfig
            });
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Updates a specific section of the configuration
        /// </summary>
        public async Task UpdateSectionAsync<T>(string sectionName, T sectionValue) where T : class
        {
            var config = CloneConfiguration(_currentConfig);
            
            switch (sectionName.ToLowerInvariant())
            {
                case "api":
                    if (sectionValue is ApiConfiguration apiConfig)
                        config.ApiConfiguration = apiConfig;
                    break;
                    
                case "application":
                    if (sectionValue is ApplicationSettings appSettings)
                        config.ApplicationSettings = appSettings;
                    break;
                    
                case "processing":
                    if (sectionValue is ProcessingOptions processingOptions)
                        config.ProcessingOptions = processingOptions;
                    break;
                    
                default:
                    throw new ArgumentException($"Unknown configuration section: {sectionName}");
            }
            
            await UpdateConfigurationAsync(config);
        }

        /// <summary>
        /// Validates configuration for required fields and consistency
        /// </summary>
        public ConfigurationValidationResult ValidateConfiguration(AppConfiguration config)
        {
            var result = new ConfigurationValidationResult { IsValid = true };
            
            if (config == null)
            {
                result.IsValid = false;
                result.Errors.Add("Configuration cannot be null");
                return result;
            }
            
            // Validate API configuration
            if (config.ApiConfiguration == null)
            {
                result.IsValid = false;
                result.Errors.Add("API configuration is required");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(config.ApiConfiguration.BaseUrl))
                    result.Errors.Add("API base URL is required");
                
                if (config.ApiConfiguration.TimeoutSeconds < 1 || config.ApiConfiguration.TimeoutSeconds > 300)
                    result.Errors.Add("API timeout must be between 1 and 300 seconds");
                
                if (config.ApiConfiguration.MaxRetries < 0 || config.ApiConfiguration.MaxRetries > 10)
                    result.Errors.Add("API max retries must be between 0 and 10");
            }
            
            // Validate application settings
            if (config.ApplicationSettings == null)
            {
                result.IsValid = false;
                result.Errors.Add("Application settings are required");
            }
            else
            {
                if (config.ApplicationSettings.MaxRecentFiles < 0 || config.ApplicationSettings.MaxRecentFiles > 50)
                    result.Errors.Add("Max recent files must be between 0 and 50");
            }
            
            // Validate processing options
            if (config.ProcessingOptions == null)
            {
                result.IsValid = false;
                result.Errors.Add("Processing options are required");
            }
            else
            {
                if (config.ProcessingOptions.MaxFileSizeMB < 1 || config.ProcessingOptions.MaxFileSizeMB > 1000)
                    result.Errors.Add("Max file size must be between 1 and 1000 MB");
                
                if (config.ProcessingOptions.MaxConcurrentFiles < 1 || config.ProcessingOptions.MaxConcurrentFiles > 20)
                    result.Errors.Add("Max concurrent files must be between 1 and 20");
            }
            
            result.IsValid = !result.Errors.Any();
            return result;
        }

        /// <summary>
        /// Creates default configuration
        /// </summary>
        private AppConfiguration CreateDefaultConfiguration()
        {
            return new AppConfiguration
            {
                Version = "1.0.0",
                LastModified = DateTime.UtcNow,
                
                ApiConfiguration = new ApiConfiguration
                {
                    BaseUrl = "https://api.threatstream.com",
                    TimeoutSeconds = 30,
                    MaxRetries = 3,
                    RetryDelaySeconds = 2,
                    ValidateCertificate = true
                },
                
                ApplicationSettings = new ApplicationSettings
                {
                    Theme = ApplicationTheme.System,
                    Language = "en-US",
                    RememberWindowPosition = true,
                    ShowSplashScreen = true,
                    MaxRecentFiles = 10,
                    AutoSaveInterval = 300,
                    EnableTelemetry = false,
                    LogLevel = AnomaliImportTool.Core.Models.LogLevel.Information
                },
                
                ProcessingOptions = new ProcessingOptions
                {
                    EnableOcr = true,
                    OcrLanguages = new List<string> { "eng" },
                    ExtractMetadata = true,
                    MaxFileSizeMB = 100,
                    SupportedFileTypes = new List<string> { ".pdf", ".docx", ".xlsx" },
                    EnableParallelProcessing = true,
                    MaxConcurrentFiles = 4,
                    PreserveOriginalFiles = true
                }
            };
        }

        /// <summary>
        /// Merges configuration with defaults for missing values
        /// </summary>
        private AppConfiguration MergeWithDefaults(AppConfiguration config)
        {
            var defaults = CreateDefaultConfiguration();
            
            config.ApiConfiguration ??= defaults.ApiConfiguration;
            config.ApplicationSettings ??= defaults.ApplicationSettings;
            config.ProcessingOptions ??= defaults.ProcessingOptions;
            
            // Merge nested properties
            if (config.ProcessingOptions.SupportedFileTypes == null || !config.ProcessingOptions.SupportedFileTypes.Any())
            {
                config.ProcessingOptions.SupportedFileTypes = defaults.ProcessingOptions.SupportedFileTypes;
            }
            
            if (config.ProcessingOptions.OcrLanguages == null || !config.ProcessingOptions.OcrLanguages.Any())
            {
                config.ProcessingOptions.OcrLanguages = defaults.ProcessingOptions.OcrLanguages;
            }
            
            return config;
        }

        /// <summary>
        /// Creates a deep clone of the configuration
        /// </summary>
        private AppConfiguration CloneConfiguration(AppConfiguration config)
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            return JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
        }

        /// <summary>
        /// Exports configuration to a file
        /// </summary>
        public async Task ExportConfigurationAsync(string filePath)
        {
            try
            {
                var json = JsonSerializer.Serialize(_currentConfig, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);
                
                _logger.Information("Configuration exported to {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to export configuration to {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Imports configuration from a file
        /// </summary>
        public async Task ImportConfigurationAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var config = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
                
                await UpdateConfigurationAsync(config);
                
                _logger.Information("Configuration imported from {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to import configuration from {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Gets the configuration file path
        /// </summary>
        public string GetConfigurationPath() => _configFilePath;

        /// <summary>
        /// Backs up current configuration
        /// </summary>
        public void BackupConfiguration()
        {
            try
            {
                var backupPath = Path.Combine(_configDirectory, 
                    $"appsettings.backup.{DateTime.Now:yyyyMMdd_HHmmss}.json");
                
                File.Copy(_configFilePath, backupPath, true);
                
                _logger.Information("Configuration backed up to {BackupPath}", backupPath);
                
                // Clean up old backups (keep last 10)
                var backups = Directory.GetFiles(_configDirectory, "appsettings.backup.*.json")
                    .OrderByDescending(f => f)
                    .Skip(10)
                    .ToList();
                
                foreach (var oldBackup in backups)
                {
                    try
                    {
                        File.Delete(oldBackup);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to backup configuration");
            }
        }
    }

    /// <summary>
    /// Application configuration root
    /// </summary>
    public class AppConfiguration
    {
        public string Version { get; set; }
        public DateTime LastModified { get; set; }
        public ApiConfiguration ApiConfiguration { get; set; }
        public ApplicationSettings ApplicationSettings { get; set; }
        public ProcessingOptions ProcessingOptions { get; set; }
    }

    /// <summary>
    /// Configuration validation result
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Configuration changed event args
    /// </summary>
    public class ConfigurationChangedEventArgs : EventArgs
    {
        public AppConfiguration OldConfiguration { get; set; }
        public AppConfiguration NewConfiguration { get; set; }
    }
} 
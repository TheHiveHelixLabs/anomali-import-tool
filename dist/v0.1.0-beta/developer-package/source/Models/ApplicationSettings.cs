using System;
using System.Collections.Generic;

namespace AnomaliImportTool.Core.Models
{
    /// <summary>
    /// Application-wide settings and user preferences.
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Gets or sets the application theme.
        /// </summary>
        public ApplicationTheme Theme { get; set; } = ApplicationTheme.Light;

        /// <summary>
        /// Gets or sets the default directory for file selection.
        /// </summary>
        public string DefaultFileDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default directory for export operations.
        /// </summary>
        public string DefaultExportDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to remember the last window size and position.
        /// </summary>
        public bool RememberWindowSettings { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to remember the window position.
        /// Alternative property name for compatibility.
        /// </summary>
        public bool RememberWindowPosition { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to show the splash screen on startup.
        /// </summary>
        public bool ShowSplashScreen { get; set; } = true;

        /// <summary>
        /// Gets or sets the auto-save interval in minutes.
        /// </summary>
        public int AutoSaveInterval { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to enable telemetry collection.
        /// </summary>
        public bool EnableTelemetry { get; set; } = false;

        /// <summary>
        /// Gets or sets the logging level.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the last window width.
        /// </summary>
        public double LastWindowWidth { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the last window height.
        /// </summary>
        public double LastWindowHeight { get; set; } = 768;

        /// <summary>
        /// Gets or sets the last window left position.
        /// </summary>
        public double LastWindowLeft { get; set; } = 100;

        /// <summary>
        /// Gets or sets the last window top position.
        /// </summary>
        public double LastWindowTop { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether the window was maximized.
        /// </summary>
        public bool WasWindowMaximized { get; set; }

        /// <summary>
        /// Gets or sets whether to show confirmation dialogs.
        /// </summary>
        public bool ShowConfirmationDialogs { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to automatically check for updates.
        /// </summary>
        public bool AutoCheckForUpdates { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of files to process in parallel.
        /// </summary>
        public int MaxParallelFiles { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether to enable detailed logging.
        /// </summary>
        public bool EnableDetailedLogging { get; set; }

        /// <summary>
        /// Gets or sets the maximum log file size in MB.
        /// </summary>
        public int MaxLogFileSizeMB { get; set; } = 10;

        /// <summary>
        /// Gets or sets the number of log files to retain.
        /// </summary>
        public int LogFileRetentionCount { get; set; } = 5;

        /// <summary>
        /// Gets or sets the default language/culture.
        /// </summary>
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets whether to show file processing tooltips.
        /// </summary>
        public bool ShowProcessingTooltips { get; set; } = true;

        /// <summary>
        /// Gets or sets the recent file paths.
        /// </summary>
        public List<string> RecentFiles { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the maximum number of recent files to remember.
        /// </summary>
        public int MaxRecentFiles { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to automatically save processing results.
        /// </summary>
        public bool AutoSaveResults { get; set; } = true;

        /// <summary>
        /// Gets or sets the default naming template for threat bulletins.
        /// </summary>
        public string DefaultNamingTemplate { get; set; } = "{filename} - {date:yyyy-MM-dd}";

        /// <summary>
        /// Gets or sets whether to group files by similarity.
        /// </summary>
        public bool EnableFileGrouping { get; set; } = true;

        /// <summary>
        /// Gets or sets the similarity threshold for file grouping (0.0 to 1.0).
        /// </summary>
        public double FileGroupingSimilarityThreshold { get; set; } = 0.8;

        /// <summary>
        /// Validates the settings.
        /// </summary>
        /// <returns>True if settings are valid; otherwise, false.</returns>
        public bool IsValid()
        {
            if (LastWindowWidth <= 0 || LastWindowHeight <= 0)
                return false;

            if (MaxParallelFiles < 1 || MaxParallelFiles > 10)
                return false;

            if (MaxLogFileSizeMB < 1 || MaxLogFileSizeMB > 1000)
                return false;

            if (LogFileRetentionCount < 1 || LogFileRetentionCount > 100)
                return false;

            if (MaxRecentFiles < 0 || MaxRecentFiles > 50)
                return false;

            if (FileGroupingSimilarityThreshold < 0.0 || FileGroupingSimilarityThreshold > 1.0)
                return false;

            return true;
        }

        /// <summary>
        /// Adds a file to the recent files list.
        /// </summary>
        /// <param name="filePath">The file path to add.</param>
        public void AddRecentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            // Remove if already exists
            RecentFiles.RemoveAll(f => string.Equals(f, filePath, StringComparison.OrdinalIgnoreCase));

            // Add to beginning
            RecentFiles.Insert(0, filePath);

            // Trim to max size
            while (RecentFiles.Count > MaxRecentFiles)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
        }

        /// <summary>
        /// Clears the recent files list.
        /// </summary>
        public void ClearRecentFiles()
        {
            RecentFiles.Clear();
        }
    }

    /// <summary>
    /// Application theme options.
    /// </summary>
    public enum ApplicationTheme
    {
        /// <summary>
        /// Light theme.
        /// </summary>
        Light,

        /// <summary>
        /// Dark theme.
        /// </summary>
        Dark,

        /// <summary>
        /// Follow system theme.
        /// </summary>
        System
    }

    /// <summary>
    /// Logging level options.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Verbose logging - everything.
        /// </summary>
        Verbose,

        /// <summary>
        /// Debug logging - detailed information.
        /// </summary>
        Debug,

        /// <summary>
        /// Information logging - general information.
        /// </summary>
        Information,

        /// <summary>
        /// Warning logging - potentially harmful situations.
        /// </summary>
        Warning,

        /// <summary>
        /// Error logging - error events.
        /// </summary>
        Error,

        /// <summary>
        /// Fatal logging - very severe error events.
        /// </summary>
        Fatal
    }
} 
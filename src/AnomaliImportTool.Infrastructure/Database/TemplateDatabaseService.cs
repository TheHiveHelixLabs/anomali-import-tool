using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;

namespace AnomaliImportTool.Infrastructure.Database;

/// <summary>
/// Service for managing the SQLite template database
/// Handles initialization, schema setup, connection management, and database health
/// </summary>
public class TemplateDatabaseService
{
    private readonly ILogger<TemplateDatabaseService> _logger;
    private readonly string _databasePath;
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the TemplateDatabaseService
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="databasePath">Path to the SQLite database file (optional, defaults to app data)</param>
    public TemplateDatabaseService(ILogger<TemplateDatabaseService> logger, string? databasePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Set default database path if not provided
        _databasePath = databasePath ?? GetDefaultDatabasePath();
        _connectionString = $"Data Source={_databasePath}";
        
        _logger.LogInformation("Template database service initialized with path: {DatabasePath}", _databasePath);
    }

    /// <summary>
    /// Initializes the template database if it doesn't exist
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Initializing template database...");

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created database directory: {Directory}", directory);
            }

            // Check if database exists and is valid
            var isNewDatabase = !File.Exists(_databasePath);
            var needsSchemaUpdate = false;

            if (!isNewDatabase)
            {
                needsSchemaUpdate = await CheckSchemaVersionAsync();
            }

            if (isNewDatabase || needsSchemaUpdate)
            {
                await CreateOrUpdateSchemaAsync();
                _logger.LogInformation("Database schema {Action} successfully", 
                    isNewDatabase ? "created" : "updated");
            }
            else
            {
                _logger.LogInformation("Database schema is up to date");
            }

            // Perform health check
            await PerformHealthCheckAsync();
            
            _logger.LogInformation("Template database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize template database");
            throw;
        }
    }

    /// <summary>
    /// Creates a new database connection
    /// </summary>
    /// <returns>A new SQLite connection</returns>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        return connection;
    }

    /// <summary>
    /// Creates and opens a new database connection
    /// </summary>
    /// <returns>An open SQLite connection</returns>
    public async Task<SqliteConnection> CreateAndOpenConnectionAsync()
    {
        var connection = CreateConnection();
        await connection.OpenAsync();
        
        // Enable foreign key constraints
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync();
        
        return connection;
    }

    /// <summary>
    /// Performs a health check on the database
    /// </summary>
    public async Task<DatabaseHealthStatus> PerformHealthCheckAsync()
    {
        var healthStatus = new DatabaseHealthStatus();
        
        try
        {
            using var connection = await CreateAndOpenConnectionAsync();
            
            // Check basic connectivity
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            var result = await command.ExecuteScalarAsync();
            healthStatus.IsConnected = result?.ToString() == "1";

            if (healthStatus.IsConnected)
            {
                // Check schema version
                healthStatus.SchemaVersion = await GetCurrentSchemaVersionAsync(connection);
                
                // Check table existence
                healthStatus.RequiredTablesExist = await CheckRequiredTablesExistAsync(connection);
                
                // Check database size
                var fileInfo = new FileInfo(_databasePath);
                healthStatus.DatabaseSizeBytes = fileInfo.Exists ? fileInfo.Length : 0;
                
                // Count templates
                healthStatus.TemplateCount = await GetTemplateCountAsync(connection);
                
                // Check database integrity
                healthStatus.IntegrityCheckPassed = await CheckDatabaseIntegrityAsync(connection);
                
                healthStatus.IsHealthy = healthStatus.RequiredTablesExist && 
                                       healthStatus.IntegrityCheckPassed &&
                                       !string.IsNullOrEmpty(healthStatus.SchemaVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            healthStatus.LastError = ex.Message;
            healthStatus.IsHealthy = false;
        }

        _logger.LogInformation("Database health check completed. Status: {IsHealthy}, Version: {Version}", 
            healthStatus.IsHealthy, healthStatus.SchemaVersion);
        
        return healthStatus;
    }

    /// <summary>
    /// Backs up the database to the specified path
    /// </summary>
    /// <param name="backupPath">Path for the backup file</param>
    public async Task BackupDatabaseAsync(string backupPath)
    {
        try
        {
            _logger.LogInformation("Starting database backup to: {BackupPath}", backupPath);
            
            // Ensure backup directory exists
            var backupDirectory = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(backupDirectory) && !Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
            }

            // Use SQLite backup API for consistent backup
            using var sourceConnection = await CreateAndOpenConnectionAsync();
            using var backupConnection = new SqliteConnection($"Data Source={backupPath}");
            await backupConnection.OpenAsync();

            sourceConnection.BackupDatabase(backupConnection);
            
            _logger.LogInformation("Database backup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database backup failed");
            throw;
        }
    }

    /// <summary>
    /// Restores the database from a backup file
    /// </summary>
    /// <param name="backupPath">Path to the backup file</param>
    public async Task RestoreDatabaseAsync(string backupPath)
    {
        try
        {
            _logger.LogInformation("Starting database restore from: {BackupPath}", backupPath);
            
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException($"Backup file not found: {backupPath}");
            }

            // Create backup of current database before restore
            var currentBackupPath = $"{_databasePath}.pre-restore.{DateTime.UtcNow:yyyyMMdd-HHmmss}.bak";
            if (File.Exists(_databasePath))
            {
                await BackupDatabaseAsync(currentBackupPath);
                _logger.LogInformation("Current database backed up to: {CurrentBackupPath}", currentBackupPath);
            }

            // Restore from backup
            using var backupConnection = new SqliteConnection($"Data Source={backupPath}");
            await backupConnection.OpenAsync();
            
            using var targetConnection = new SqliteConnection(_connectionString);
            await targetConnection.OpenAsync();

            backupConnection.BackupDatabase(targetConnection);
            
            _logger.LogInformation("Database restore completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database restore failed");
            throw;
        }
    }

    /// <summary>
    /// Optimizes the database by running VACUUM and ANALYZE
    /// </summary>
    public async Task OptimizeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Starting database optimization...");
            
            using var connection = await CreateAndOpenConnectionAsync();
            
            // Run VACUUM to reclaim space and defragment
            using var vacuumCommand = connection.CreateCommand();
            vacuumCommand.CommandText = "VACUUM;";
            await vacuumCommand.ExecuteNonQueryAsync();
            
            // Run ANALYZE to update query planner statistics
            using var analyzeCommand = connection.CreateCommand();
            analyzeCommand.CommandText = "ANALYZE;";
            await analyzeCommand.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Database optimization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database optimization failed");
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    /// Gets the default database path in the application data directory
    /// </summary>
    private static string GetDefaultDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "AnomaliImportTool");
        
        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }
        
        return Path.Combine(appFolder, "TemplateDatabase.db");
    }

    /// <summary>
    /// Creates or updates the database schema
    /// </summary>
    private async Task CreateOrUpdateSchemaAsync()
    {
        var schemaScript = await LoadSchemaScriptAsync();
        
        using var connection = await CreateAndOpenConnectionAsync();
        
        // Execute schema script in parts to handle potential issues
        var scriptParts = schemaScript.Split(new[] { "-- ============================================================================" }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in scriptParts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = part;
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to execute schema part, continuing...");
                // Continue with other parts
            }
        }
    }

    /// <summary>
    /// Loads the schema script from embedded resource
    /// </summary>
    private async Task<string> LoadSchemaScriptAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "AnomaliImportTool.Infrastructure.Database.TemplateDatabase.sql";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            // Fallback: try to load from file system
            var schemaPath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? "", "Database", "TemplateDatabase.sql");
            if (File.Exists(schemaPath))
            {
                return await File.ReadAllTextAsync(schemaPath);
            }
            
            throw new InvalidOperationException($"Schema script not found: {resourceName}");
        }
        
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Checks if the schema needs to be updated
    /// </summary>
    private async Task<bool> CheckSchemaVersionAsync()
    {
        try
        {
            using var connection = await CreateAndOpenConnectionAsync();
            var currentVersion = await GetCurrentSchemaVersionAsync(connection);
            
            // Compare with expected version
            const string expectedVersion = "1.0.0";
            return currentVersion != expectedVersion;
        }
        catch
        {
            // If we can't check version, assume update is needed
            return true;
        }
    }

    /// <summary>
    /// Gets the current schema version from the database
    /// </summary>
    private async Task<string?> GetCurrentSchemaVersionAsync(SqliteConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT version FROM schema_version ORDER BY applied_at DESC LIMIT 1;";
            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if all required tables exist
    /// </summary>
    private async Task<bool> CheckRequiredTablesExistAsync(SqliteConnection connection)
    {
        var requiredTables = new[]
        {
            "import_templates", "template_versions", "template_fields", "extraction_zones",
            "template_categories", "template_usage_history", "template_performance_metrics",
            "template_sharing", "document_template_matches", "schema_version"
        };

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name IN (" +
                                 string.Join(",", requiredTables.Select(t => $"'{t}'")) + ");";
            
            using var reader = await command.ExecuteReaderAsync();
            var existingTables = new List<string>();
            
            while (await reader.ReadAsync())
            {
                existingTables.Add(reader.GetString(0));
            }
            
            return requiredTables.All(table => existingTables.Contains(table));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the total number of templates in the database
    /// </summary>
    private async Task<int> GetTemplateCountAsync(SqliteConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM import_templates;";
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks database integrity
    /// </summary>
    private async Task<bool> CheckDatabaseIntegrityAsync(SqliteConnection connection)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() == "ok";
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

/// <summary>
/// Represents the health status of the template database
/// </summary>
public class DatabaseHealthStatus
{
    /// <summary>
    /// Whether the database connection is working
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Whether the database is overall healthy
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Current schema version
    /// </summary>
    public string? SchemaVersion { get; set; }

    /// <summary>
    /// Whether all required tables exist
    /// </summary>
    public bool RequiredTablesExist { get; set; }

    /// <summary>
    /// Whether database integrity check passed
    /// </summary>
    public bool IntegrityCheckPassed { get; set; }

    /// <summary>
    /// Database file size in bytes
    /// </summary>
    public long DatabaseSizeBytes { get; set; }

    /// <summary>
    /// Total number of templates in the database
    /// </summary>
    public int TemplateCount { get; set; }

    /// <summary>
    /// Last error message if health check failed
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// When the health check was performed
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
} 
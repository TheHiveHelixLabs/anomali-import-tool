using System.Reflection;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing;

/// <summary>
/// Plugin manager for document processing extensions (STUB IMPLEMENTATION).
/// This is a temporary stub to resolve compilation issues.
/// </summary>
public sealed class DocumentProcessingPluginManager : IDisposable
{
    private readonly ILogger<DocumentProcessingPluginManager> _logger;
    private bool _disposed;
    
    public DocumentProcessingPluginManager(ILogger<DocumentProcessingPluginManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// Gets all loaded plugins (stub - returns empty list)
    /// </summary>
    public IReadOnlyList<object> LoadedPlugins => new List<object>().AsReadOnly();
    
    /// <summary>
    /// Loads plugins from the specified directory (stub implementation)
    /// </summary>
    /// <param name="pluginDirectory">Directory containing plugin assemblies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of plugins loaded (always 0 in stub)</returns>
    public async Task<int> LoadPluginsFromDirectoryAsync(string pluginDirectory, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Plugin loading is not implemented in this stub version");
        await Task.CompletedTask;
        return 0;
    }
    
    /// <summary>
    /// Loads a plugin from the specified assembly file (stub implementation)
    /// </summary>
    /// <param name="assemblyPath">Path to the plugin assembly</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Always false in stub implementation</returns>
    public async Task<bool> LoadPluginFromFileAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Plugin loading is not implemented in this stub version");
        await Task.CompletedTask;
        return false;
    }
    
    /// <summary>
    /// Unloads a plugin by name (stub implementation)
    /// </summary>
    /// <param name="pluginName">Name of the plugin to unload</param>
    /// <returns>Always false in stub implementation</returns>
    public async Task<bool> UnloadPluginAsync(string pluginName)
    {
        _logger.LogWarning("Plugin unloading is not implemented in this stub version");
        await Task.CompletedTask;
        return false;
    }
    
    /// <summary>
    /// Gets plugin information for all loaded plugins (stub - returns empty list)
    /// </summary>
    /// <returns>Empty collection in stub implementation</returns>
    public IReadOnlyList<PluginInfo> GetPluginInfo()
    {
        return new List<PluginInfo>().AsReadOnly();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}

/// <summary>
/// Plugin information record (stub implementation)
/// </summary>
public record PluginInfo(
    string Name,
    string Version,
    string Description,
    int StrategyCount,
    IReadOnlyList<string> SupportedExtensions
); 
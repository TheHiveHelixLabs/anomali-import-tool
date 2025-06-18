using System.Reflection;
using AnomaliImportTool.Core.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing;

/// <summary>
/// Plugin manager for document processing extensions.
/// Implements the Open/Closed Principle by allowing plugins to be loaded without modifying existing code.
/// </summary>
public sealed class DocumentProcessingPluginManager : IDisposable
{
    private readonly IDocumentProcessingStrategyFactory _strategyFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentProcessingPluginManager> _logger;
    private readonly List<IDocumentProcessingPlugin> _loadedPlugins;
    private readonly Dictionary<string, Assembly> _loadedAssemblies;
    private bool _disposed;
    
    public DocumentProcessingPluginManager(
        IDocumentProcessingStrategyFactory strategyFactory,
        IServiceProvider serviceProvider,
        ILogger<DocumentProcessingPluginManager> logger)
    {
        _strategyFactory = strategyFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _loadedPlugins = new List<IDocumentProcessingPlugin>();
        _loadedAssemblies = new Dictionary<string, Assembly>();
    }
    
    /// <summary>
    /// Gets all loaded plugins
    /// </summary>
    public IReadOnlyList<IDocumentProcessingPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();
    
    /// <summary>
    /// Loads plugins from the specified directory
    /// </summary>
    /// <param name="pluginDirectory">Directory containing plugin assemblies</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of plugins loaded</returns>
    public async Task<int> LoadPluginsFromDirectoryAsync(string pluginDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            _logger.LogWarning("Plugin directory does not exist: {PluginDirectory}", pluginDirectory);
            return 0;
        }
        
        var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
        var loadedCount = 0;
        
        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                if (await LoadPluginFromFileAsync(pluginFile, cancellationToken))
                {
                    loadedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from file: {PluginFile}", pluginFile);
            }
        }
        
        _logger.LogInformation("Loaded {LoadedCount} plugins from directory: {PluginDirectory}", 
            loadedCount, pluginDirectory);
        
        return loadedCount;
    }
    
    /// <summary>
    /// Loads a plugin from the specified assembly file
    /// </summary>
    /// <param name="assemblyPath">Path to the plugin assembly</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the plugin was loaded successfully</returns>
    public async Task<bool> LoadPluginFromFileAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Attempting to load plugin from: {AssemblyPath}", assemblyPath);
            
            // Check if assembly is already loaded
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            if (_loadedAssemblies.ContainsKey(assemblyName))
            {
                _logger.LogWarning("Assembly {AssemblyName} is already loaded", assemblyName);
                return false;
            }
            
            // Load the assembly
            var assembly = Assembly.LoadFrom(assemblyPath);
            _loadedAssemblies[assemblyName] = assembly;
            
            // Find plugin types
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IDocumentProcessingPlugin).IsAssignableFrom(t) && 
                           !t.IsInterface && 
                           !t.IsAbstract)
                .ToList();
            
            if (pluginTypes.Count == 0)
            {
                _logger.LogWarning("No plugin types found in assembly: {AssemblyPath}", assemblyPath);
                return false;
            }
            
            // Create and initialize plugins
            foreach (var pluginType in pluginTypes)
            {
                var plugin = await CreateAndInitializePluginAsync(pluginType, cancellationToken);
                if (plugin != null)
                {
                    _loadedPlugins.Add(plugin);
                    RegisterPluginStrategies(plugin);
                    
                    _logger.LogInformation("Successfully loaded plugin: {PluginName} v{PluginVersion} from {AssemblyPath}", 
                        plugin.Name, plugin.Version, assemblyPath);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from assembly: {AssemblyPath}", assemblyPath);
            return false;
        }
    }
    
    /// <summary>
    /// Unloads a plugin by name
    /// </summary>
    /// <param name="pluginName">Name of the plugin to unload</param>
    /// <returns>True if the plugin was found and unloaded</returns>
    public async Task<bool> UnloadPluginAsync(string pluginName)
    {
        var plugin = _loadedPlugins.FirstOrDefault(p => p.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        if (plugin == null)
        {
            _logger.LogWarning("Plugin not found for unloading: {PluginName}", pluginName);
            return false;
        }
        
        try
        {
            // Unregister strategies
            var strategies = plugin.GetStrategies();
            foreach (var strategy in strategies)
            {
                _strategyFactory.UnregisterStrategy(strategy.GetType());
            }
            
            // Dispose plugin
            await plugin.DisposeAsync();
            
            // Remove from loaded plugins
            _loadedPlugins.Remove(plugin);
            
            _logger.LogInformation("Successfully unloaded plugin: {PluginName}", pluginName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading plugin: {PluginName}", pluginName);
            return false;
        }
    }
    
    /// <summary>
    /// Gets plugin information for all loaded plugins
    /// </summary>
    /// <returns>Collection of plugin information</returns>
    public IReadOnlyList<PluginInfo> GetPluginInfo()
    {
        return _loadedPlugins.Select(p => new PluginInfo(
            Name: p.Name,
            Version: p.Version,
            Description: p.Description,
            StrategyCount: p.GetStrategies().Count,
            SupportedExtensions: p.GetStrategies()
                .SelectMany(s => s.SupportedExtensions)
                .Distinct()
                .ToList()
                .AsReadOnly()
        )).ToList().AsReadOnly();
    }
    
    private async Task<IDocumentProcessingPlugin?> CreateAndInitializePluginAsync(Type pluginType, CancellationToken cancellationToken)
    {
        try
        {
            // Create plugin instance
            var plugin = Activator.CreateInstance(pluginType) as IDocumentProcessingPlugin;
            if (plugin == null)
            {
                _logger.LogError("Failed to create instance of plugin type: {PluginType}", pluginType.Name);
                return null;
            }
            
            // Initialize plugin
            await plugin.InitializeAsync(_serviceProvider);
            
            _logger.LogDebug("Created and initialized plugin: {PluginName} ({PluginType})", 
                plugin.Name, pluginType.Name);
            
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create and initialize plugin of type: {PluginType}", pluginType.Name);
            return null;
        }
    }
    
    private void RegisterPluginStrategies(IDocumentProcessingPlugin plugin)
    {
        var strategies = plugin.GetStrategies();
        foreach (var strategy in strategies)
        {
            _strategyFactory.RegisterStrategy(strategy);
            _logger.LogDebug("Registered strategy {StrategyType} from plugin {PluginName}", 
                strategy.GetType().Name, plugin.Name);
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        // Dispose all loaded plugins
        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                plugin.DisposeAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing plugin: {PluginName}", plugin.Name);
            }
        }
        
        _loadedPlugins.Clear();
        _loadedAssemblies.Clear();
        _disposed = true;
        
        _logger.LogInformation("DocumentProcessingPluginManager disposed");
    }
}

/// <summary>
/// Information about a loaded plugin
/// </summary>
public record PluginInfo(
    string Name,
    string Version,
    string Description,
    int StrategyCount,
    IReadOnlyList<string> SupportedExtensions
); 
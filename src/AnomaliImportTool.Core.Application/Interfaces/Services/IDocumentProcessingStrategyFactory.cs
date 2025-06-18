using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

namespace AnomaliImportTool.Core.Application.Interfaces.Services;

/// <summary>
/// Factory interface for creating document processing strategies.
/// Follows the Open/Closed Principle by allowing new strategies to be registered without modifying existing code.
/// </summary>
public interface IDocumentProcessingStrategyFactory
{
    /// <summary>
    /// Gets the appropriate strategy for processing the specified file
    /// </summary>
    /// <param name="filePath">The file path to get a strategy for</param>
    /// <returns>The appropriate strategy, or null if no strategy can handle the file</returns>
    IDocumentProcessingStrategy? GetStrategy(FilePath filePath);
    
    /// <summary>
    /// Gets all available strategies ordered by priority
    /// </summary>
    /// <returns>Collection of all registered strategies</returns>
    IReadOnlyList<IDocumentProcessingStrategy> GetAllStrategies();
    
    /// <summary>
    /// Gets all supported file extensions from all registered strategies
    /// </summary>
    /// <returns>Collection of supported file extensions</returns>
    IReadOnlyList<string> GetSupportedExtensions();
    
    /// <summary>
    /// Registers a new strategy with the factory
    /// </summary>
    /// <param name="strategy">The strategy to register</param>
    void RegisterStrategy(IDocumentProcessingStrategy strategy);
    
    /// <summary>
    /// Unregisters a strategy from the factory
    /// </summary>
    /// <param name="strategyType">The type of strategy to unregister</param>
    /// <returns>True if the strategy was found and removed</returns>
    bool UnregisterStrategy(Type strategyType);
}

/// <summary>
/// Plugin interface for document processing extensions.
/// Allows third-party plugins to extend document processing capabilities.
/// </summary>
public interface IDocumentProcessingPlugin
{
    /// <summary>
    /// Gets the plugin name
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the plugin version
    /// </summary>
    string Version { get; }
    
    /// <summary>
    /// Gets the plugin description
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Gets the strategies provided by this plugin
    /// </summary>
    /// <returns>Collection of strategies provided by this plugin</returns>
    IReadOnlyList<IDocumentProcessingStrategy> GetStrategies();
    
    /// <summary>
    /// Initializes the plugin
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    Task InitializeAsync(IServiceProvider serviceProvider);
    
    /// <summary>
    /// Disposes the plugin and cleans up resources
    /// </summary>
    Task DisposeAsync();
} 
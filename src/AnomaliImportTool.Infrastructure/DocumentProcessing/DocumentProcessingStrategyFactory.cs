using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing;

/// <summary>
/// Factory implementation for document processing strategies (STUB IMPLEMENTATION).
/// This is a temporary stub to resolve compilation issues.
/// </summary>
public sealed class DocumentProcessingStrategyFactory
{
    private readonly ILogger<DocumentProcessingStrategyFactory> _logger;
    
    public DocumentProcessingStrategyFactory(ILogger<DocumentProcessingStrategyFactory> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("Initialized DocumentProcessingStrategyFactory stub implementation");
    }
    
    /// <summary>
    /// Gets a strategy for processing the specified file (stub - returns null)
    /// </summary>
    /// <param name="filePath">File path</param>
    /// <returns>Always null in stub implementation</returns>
    public object? GetStrategy(string filePath)
    {
        _logger.LogWarning("Strategy retrieval is not implemented in this stub version");
        return null;
    }
    
    /// <summary>
    /// Gets all registered strategies (stub - returns empty list)
    /// </summary>
    /// <returns>Empty list in stub implementation</returns>
    public IReadOnlyList<object> GetAllStrategies()
    {
        return new List<object>().AsReadOnly();
    }
    
    /// <summary>
    /// Gets supported file extensions (stub - returns empty list)
    /// </summary>
    /// <returns>Empty list in stub implementation</returns>
    public IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string>().AsReadOnly();
    }
    
    /// <summary>
    /// Registers a strategy (stub - does nothing)
    /// </summary>
    /// <param name="strategy">Strategy to register</param>
    public void RegisterStrategy(object strategy)
    {
        _logger.LogWarning("Strategy registration is not implemented in this stub version");
    }
    
    /// <summary>
    /// Unregisters a strategy (stub - always returns false)
    /// </summary>
    /// <param name="strategyType">Strategy type to unregister</param>
    /// <returns>Always false in stub implementation</returns>
    public bool UnregisterStrategy(Type strategyType)
    {
        _logger.LogWarning("Strategy unregistration is not implemented in this stub version");
        return false;
    }
} 
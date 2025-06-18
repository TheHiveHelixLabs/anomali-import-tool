using System.Collections.Concurrent;
using AnomaliImportTool.Core.Application.Interfaces.Services;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing;

/// <summary>
/// Factory implementation for document processing strategies.
/// Implements the Open/Closed Principle by allowing new strategies to be registered without modifying existing code.
/// </summary>
public sealed class DocumentProcessingStrategyFactory : IDocumentProcessingStrategyFactory
{
    private readonly ConcurrentDictionary<Type, IDocumentProcessingStrategy> _strategies;
    private readonly ILogger<DocumentProcessingStrategyFactory> _logger;
    
    public DocumentProcessingStrategyFactory(
        IEnumerable<IDocumentProcessingStrategy> strategies,
        ILogger<DocumentProcessingStrategyFactory> logger)
    {
        _strategies = new ConcurrentDictionary<Type, IDocumentProcessingStrategy>();
        _logger = logger;
        
        // Register all provided strategies
        foreach (var strategy in strategies)
        {
            RegisterStrategy(strategy);
        }
        
        _logger.LogInformation("Initialized DocumentProcessingStrategyFactory with {StrategyCount} strategies", 
            _strategies.Count);
    }
    
    /// <inheritdoc />
    public IDocumentProcessingStrategy? GetStrategy(FilePath filePath)
    {
        var extension = Path.GetExtension(filePath.Value).ToLowerInvariant();
        
        // Find strategies that can process this file, ordered by priority (descending)
        var candidates = _strategies.Values
            .Where(s => s.CanProcess(filePath))
            .OrderByDescending(s => s.Priority)
            .ToList();
        
        if (candidates.Count == 0)
        {
            _logger.LogWarning("No strategy found for file: {FilePath} with extension: {Extension}", 
                filePath.Value, extension);
            return null;
        }
        
        var selectedStrategy = candidates.First();
        _logger.LogDebug("Selected strategy {StrategyType} for file: {FilePath}", 
            selectedStrategy.GetType().Name, filePath.Value);
        
        return selectedStrategy;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<IDocumentProcessingStrategy> GetAllStrategies()
    {
        return _strategies.Values
            .OrderByDescending(s => s.Priority)
            .ToList()
            .AsReadOnly();
    }
    
    /// <inheritdoc />
    public IReadOnlyList<string> GetSupportedExtensions()
    {
        return _strategies.Values
            .SelectMany(s => s.SupportedExtensions)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(ext => ext)
            .ToList()
            .AsReadOnly();
    }
    
    /// <inheritdoc />
    public void RegisterStrategy(IDocumentProcessingStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        
        var strategyType = strategy.GetType();
        
        if (_strategies.TryAdd(strategyType, strategy))
        {
            _logger.LogInformation("Registered strategy {StrategyType} with extensions: {Extensions}", 
                strategyType.Name, string.Join(", ", strategy.SupportedExtensions));
        }
        else
        {
            _logger.LogWarning("Strategy {StrategyType} is already registered", strategyType.Name);
        }
    }
    
    /// <inheritdoc />
    public bool UnregisterStrategy(Type strategyType)
    {
        ArgumentNullException.ThrowIfNull(strategyType);
        
        if (_strategies.TryRemove(strategyType, out var removedStrategy))
        {
            _logger.LogInformation("Unregistered strategy {StrategyType}", strategyType.Name);
            
            // Dispose if the strategy implements IDisposable
            if (removedStrategy is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            return true;
        }
        
        _logger.LogWarning("Strategy {StrategyType} was not found for unregistration", strategyType.Name);
        return false;
    }
} 
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.Services;

/// <summary>
/// Sample infrastructure service (STUB IMPLEMENTATION).
/// This is a temporary stub to resolve compilation issues.
/// </summary>
public class SampleInfrastructureService
{
    private readonly ILogger<SampleInfrastructureService> _logger;

    public SampleInfrastructureService(ILogger<SampleInfrastructureService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sample method for infrastructure service (stub implementation)
    /// </summary>
    public async Task<string> ProcessSampleDataAsync(string data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing sample data in stub implementation");
        await Task.Delay(100, cancellationToken);
        return $"Processed: {data}";
    }

    /// <summary>
    /// Sample validation method (stub implementation)
    /// </summary>
    public bool ValidateData(string data)
    {
        _logger.LogDebug("Validating data in stub implementation");
        return !string.IsNullOrWhiteSpace(data);
    }
} 
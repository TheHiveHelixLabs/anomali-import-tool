using Microsoft.Extensions.DependencyInjection;
using AnomaliImportTool.Core.Application.DependencyInjection;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

namespace AnomaliImportTool.Infrastructure.Services;

/// <summary>
/// Sample infrastructure service demonstrating automatic registration
/// </summary>
[ServiceRegistration(typeof(ISampleInfrastructureService), ServiceLifetime.Scoped)]
public class SampleInfrastructureService : ISampleInfrastructureService
{
    public string GetServiceInfo()
    {
        return "Sample Infrastructure Service - Automatically Registered";
    }

    public Task<bool> ValidateServiceAsync()
    {
        return Task.FromResult(true);
    }
}

/// <summary>
/// Interface for sample infrastructure service
/// </summary>
public interface ISampleInfrastructureService
{
    string GetServiceInfo();
    Task<bool> ValidateServiceAsync();
} 
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using AnomaliImportTool.Core.Application.DependencyInjection;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

namespace AnomaliImportTool.DocumentProcessing.DependencyInjection;

/// <summary>
/// Document processing service registration module
/// </summary>
public static class DocumentProcessingServiceRegistration
{
    /// <summary>
    /// Register all document processing services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDocumentProcessingServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register services using attribute-based registration
        services.AddServicesFromAssembly(assembly);

        // Register document processing services
        services.AddFileProcessingServices(assembly);
        services.AddDocumentParsingServices(assembly);
        services.AddContentExtractionServices(assembly);
        services.AddDocumentValidationServices(assembly);

        return services;
    }

    /// <summary>
    /// Register file processing services
    /// </summary>
    private static IServiceCollection AddFileProcessingServices(this IServiceCollection services, Assembly assembly)
    {
        // Register file processing implementations
        services.AddImplementationsOf<IFileProcessingService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IFileReaderService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IFileWriterService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IFileValidationService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register document parsing services
    /// </summary>
    private static IServiceCollection AddDocumentParsingServices(this IServiceCollection services, Assembly assembly)
    {
        // Register document parsers by file type
        services.AddImplementationsOf<IPdfParserService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IWordParserService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IExcelParserService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<ITextParserService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IXmlParserService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IJsonParserService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register content extraction services
    /// </summary>
    private static IServiceCollection AddContentExtractionServices(this IServiceCollection services, Assembly assembly)
    {
        // Register content extraction services
        services.AddImplementationsOf<IContentExtractionService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IMetadataExtractionService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IThreatIntelligenceExtractionService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IIocExtractionService>(ServiceLifetime.Scoped, assembly);

        return services;
    }

    /// <summary>
    /// Register document validation services
    /// </summary>
    private static IServiceCollection AddDocumentValidationServices(this IServiceCollection services, Assembly assembly)
    {
        // Register validation services
        services.AddImplementationsOf<IDocumentValidationService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IContentValidationService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<ISchemaValidationService>(ServiceLifetime.Scoped, assembly);
        services.AddImplementationsOf<IIntegrityValidationService>(ServiceLifetime.Scoped, assembly);

        return services;
    }
}

// Document processing service marker interfaces
public interface IFileReaderService { }
public interface IFileWriterService { }
public interface IFileValidationService { }
public interface IPdfParserService { }
public interface IWordParserService { }
public interface IExcelParserService { }
public interface ITextParserService { }
public interface IXmlParserService { }
public interface IJsonParserService { }
public interface IContentExtractionService { }
public interface IMetadataExtractionService { }
public interface IThreatIntelligenceExtractionService { }
public interface IIocExtractionService { }
public interface IDocumentValidationService { }
public interface IContentValidationService { }
public interface ISchemaValidationService { }
public interface IIntegrityValidationService { } 
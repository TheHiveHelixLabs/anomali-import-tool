using System;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Services;
using AnomaliImportTool.Infrastructure.ApiClient;
using AnomaliImportTool.Infrastructure.Database;
using AnomaliImportTool.Infrastructure.DocumentProcessing;
using AnomaliImportTool.Infrastructure.FileProcessing;
using AnomaliImportTool.Infrastructure.Security;
using AnomaliImportTool.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AnomaliImportTool.Infrastructure.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering infrastructure services
    /// </summary>
    public static class InfrastructureServiceRegistration
    {
        /// <summary>
        /// Registers all infrastructure services with the dependency injection container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">Optional configuration instance</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration = null)
        {
            // Register logging service
            services.AddSingleton<LoggingService>(provider =>
            {
                return configuration != null 
                    ? new LoggingService(configuration) 
                    : new LoggingService();
            });
            
            // Register Serilog ILogger
            services.AddSingleton<ILogger>(provider => 
                provider.GetRequiredService<LoggingService>().Logger);
            
            // Register configuration service
            services.AddSingleton<ConfigurationService>(provider =>
            {
                var logger = provider.GetService<ILogger>();
                return new ConfigurationService(logger);
            });
            
            // Register individual document processors
            services.AddTransient<IDocumentProcessor, PdfDocumentProcessor>();
            services.AddTransient<IDocumentProcessor, WordDocumentProcessor>();
            services.AddTransient<IDocumentProcessor, ExcelDocumentProcessor>();
            services.AddTransient<OcrProcessor>();
            
            // Register document processing service (orchestrator)
            services.AddTransient<DocumentProcessingService>();
            
            // Register integration services
            services.AddTransient<DocumentUploadService>();
            
            // Register API client
            services.AddScoped<IAnomaliApiClient, AnomaliApiClient>();
            
            // Register security service
            services.AddSingleton<ISecurityService, WindowsSecurityService>();
            
            // Register file grouping and naming services
            services.AddTransient<FileGroupingService>();
            services.AddTransient<NamingTemplateService>();
            services.AddTransient<MetadataExtractionService>();
            
            // Register template services
            services.AddSingleton<TemplateDatabaseService>();
            services.AddTransient<TemplateSerializationService>();
            services.AddScoped<IImportTemplateService, ImportTemplateService>();
            services.AddScoped<ITemplateMatchingService, TemplateMatchingService>();
            
            // Register file processing services
            services.AddTransient<FileSecurityValidator>();
            services.AddTransient<PdfContentExtractor>();
            
            // Register HTTP clients
            services.AddHttpClient("AnomaliApi", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
                client.DefaultRequestHeaders.Add("User-Agent", "AnomaliImportTool/1.0");
            });

            services.AddHttpClient("Default", client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2);
            });
            
            return services;
        }
        
        /// <summary>
        /// Configures infrastructure services with specific options
        /// </summary>
        public static IServiceCollection ConfigureInfrastructureOptions(this IServiceCollection services, Action<InfrastructureOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services;
        }
    }
    
    /// <summary>
    /// Infrastructure configuration options
    /// </summary>
    public class InfrastructureOptions
    {
        public string LogDirectory { get; set; } = string.Empty;
        public string ConfigDirectory { get; set; } = string.Empty;
        public bool EnableDetailedLogging { get; set; }
        public int MaxConcurrentOperations { get; set; } = 4;
    }
} 
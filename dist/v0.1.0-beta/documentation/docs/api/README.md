# 🔧 API Documentation

Welcome to the comprehensive API documentation for the Anomali Import Tool. This section covers all programmatic interfaces, integration points, and extensibility options.

## 📚 API Documentation Structure

### 🚀 Core APIs
- [**Document Processing API**](document-processing.md) - Process PDF, Word, and Excel documents
- [**Anomali Integration API**](anomali-integration.md) - ThreatStream API integration
- [**File Management API**](file-management.md) - File operations and batch processing
- [**Security API**](security.md) - Credential management and encryption

### 🔌 Integration APIs
- [**Command Line Interface**](cli-reference.md) - Complete CLI documentation
- [**Plugin API**](plugin-api.md) - Extend functionality with custom plugins
- [**Webhook API**](webhooks.md) - Event-driven integrations
- [**REST API**](rest-api.md) - HTTP-based programmatic access

### 🛠️ Developer APIs
- [**Configuration API**](configuration.md) - Application settings and preferences
- [**Logging API**](logging.md) - Structured logging and monitoring
- [**Events API**](events.md) - Application events and notifications
- [**Storage API**](storage.md) - Data persistence and caching

### 📱 Platform APIs
- [**Windows Integration**](platforms/windows.md) - Windows-specific features
- [**macOS Integration**](platforms/macos.md) - macOS-specific features  
- [**Linux Integration**](platforms/linux.md) - Linux-specific features
- [**Web API**](platforms/web.md) - Browser-based functionality

## 🔑 Authentication & Authorization

### API Key Management
```csharp
// Configure API credentials
var config = new ApiConfiguration
{
    ServerUrl = "https://your-anomali-instance.com",
    Username = "your-username",
    ApiKey = "your-api-key",
    ApiVersion = ApiVersion.V2
};

var client = new AnomaliApiClient(config);
```

### Security Headers
```http
Authorization: Bearer your-api-key
Content-Type: application/json
User-Agent: AnomaliImportTool/1.0.0
X-API-Version: v2
```

## 📖 Quick Start Examples

### Document Processing
```csharp
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Infrastructure.DocumentProcessing;

// Initialize document processor
var processor = new DocumentProcessingService(logger, configuration);

// Process a single document
var document = await processor.ProcessDocumentAsync("path/to/document.pdf");

// Process multiple documents
var documents = await processor.ProcessBatchAsync(filePaths);
```

### Anomali Integration
```csharp
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Infrastructure.Services;

// Create threat bulletin
var bulletin = new ThreatBulletin
{
    Name = "Security Exception - User123",
    Body = extractedContent,
    TlpDesignation = TlpDesignation.White,
    Status = BulletinStatus.Published
};

// Upload to ThreatStream
var client = new AnomaliApiClient(configuration);
var result = await client.CreateThreatBulletinAsync(bulletin);
```

### File Management
```csharp
using AnomaliImportTool.Core.Services;

// Group related files
var groupingService = new FileGroupingService(logger);
var groups = await groupingService.GroupFilesByNameAsync(filePaths);

// Apply naming templates
var templateService = new NamingTemplateService();
var names = templateService.ApplyTemplate(template, metadata);
```

## 🔄 Error Handling

### Standard Error Response
```json
{
  "error": {
    "code": "INVALID_DOCUMENT",
    "message": "Document format not supported",
    "details": {
      "filename": "document.xyz",
      "supportedFormats": ["pdf", "docx", "xlsx"]
    },
    "timestamp": "2025-01-20T10:30:00Z",
    "correlationId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### Exception Types
```csharp
// Document processing exceptions
try
{
    var document = await processor.ProcessDocumentAsync(path);
}
catch (UnsupportedDocumentFormatException ex)
{
    // Handle unsupported format
}
catch (DocumentProcessingException ex)
{
    // Handle processing errors
}
catch (FileAccessException ex)
{
    // Handle file access issues
}

// API integration exceptions
try
{
    var result = await client.CreateThreatBulletinAsync(bulletin);
}
catch (AnomaliApiException ex)
{
    // Handle API errors
}
catch (AuthenticationException ex)
{
    // Handle auth failures
}
catch (NetworkException ex)
{
    // Handle network issues
}
```

## 📊 Data Models

### Core Models
```csharp
// Document model
public class Document
{
    public string Id { get; set; }
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public DocumentType Type { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ExtractedContent { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public DocumentStatus Status { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
}

// Threat bulletin model
public class ThreatBulletin
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Body { get; set; }
    public TlpDesignation TlpDesignation { get; set; }
    public BulletinStatus Status { get; set; }
    public VisibilityType Visibility { get; set; }
    public List<string> Tags { get; set; }
    public List<FileAttachment> Attachments { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}
```

### Configuration Models
```csharp
// API configuration
public class ApiConfiguration
{
    public string ServerUrl { get; set; }
    public string Username { get; set; }
    public string ApiKey { get; set; }
    public ApiVersion ApiVersion { get; set; }
    public TimeSpan Timeout { get; set; }
    public ProxyConfiguration Proxy { get; set; }
    public bool ValidateCertificates { get; set; }
}

// Processing options
public class ProcessingOptions
{
    public bool EnableOcr { get; set; }
    public bool ExtractMetadata { get; set; }
    public bool GroupSimilarFiles { get; set; }
    public double SimilarityThreshold { get; set; }
    public NamingTemplate NamingTemplate { get; set; }
    public int MaxBatchSize { get; set; }
    public TimeSpan ProcessingTimeout { get; set; }
}
```

## 🔗 Integration Patterns

### Dependency Injection
```csharp
// Register services
services.AddScoped<IDocumentProcessor, DocumentProcessingService>();
services.AddScoped<IAnomaliApiClient, AnomaliApiClient>();
services.AddScoped<IFileGroupingService, FileGroupingService>();
services.AddScoped<ISecurityService, WindowsSecurityService>();

// Configure options
services.Configure<ApiConfiguration>(configuration.GetSection("Anomali"));
services.Configure<ProcessingOptions>(configuration.GetSection("Processing"));
```

### Event-Driven Architecture
```csharp
// Subscribe to events
eventBus.Subscribe<DocumentProcessedEvent>(async @event =>
{
    logger.LogInformation("Document processed: {@Document}", @event.Document);
    await notificationService.NotifyAsync(@event);
});

// Publish events
await eventBus.PublishAsync(new DocumentProcessedEvent
{
    Document = document,
    ProcessingDuration = stopwatch.Elapsed,
    Timestamp = DateTime.UtcNow
});
```

### Plugin Architecture
```csharp
// Implement custom plugin
public class CustomDocumentProcessor : IDocumentProcessingPlugin
{
    public string Name => "Custom PDF Processor";
    public Version Version => new Version(1, 0, 0);
    public string[] SupportedFormats => new[] { ".pdf" };

    public async Task<Document> ProcessAsync(string filePath, ProcessingOptions options)
    {
        // Custom processing logic
    }
}

// Register plugin
pluginManager.RegisterPlugin<CustomDocumentProcessor>();
```

## 📈 Performance & Monitoring

### Performance Metrics
```csharp
// Track processing metrics
using var activity = activitySource.StartActivity("ProcessDocument");
activity?.SetTag("document.type", document.Type.ToString());
activity?.SetTag("document.size", document.SizeBytes);

var stopwatch = Stopwatch.StartNew();
var result = await processor.ProcessDocumentAsync(path);
stopwatch.Stop();

activity?.SetTag("processing.duration", stopwatch.ElapsedMilliseconds);
```

### Health Checks
```csharp
// Register health checks
services.AddHealthChecks()
    .AddCheck<AnomaliApiHealthCheck>("anomali-api")
    .AddCheck<DocumentProcessingHealthCheck>("document-processing")
    .AddCheck<StorageHealthCheck>("storage");
```

### Logging Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "AnomaliImportTool": "Debug",
      "Microsoft": "Warning"
    },
    "Console": {
      "IncludeScopes": true,
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss "
    },
    "File": {
      "Path": "logs/anomali-import-tool-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 7
    }
  }
}
```

## 🛡️ Security Considerations

### Secure Configuration
```csharp
// Encrypt sensitive configuration
var encryptedConfig = securityService.EncryptConfiguration(apiConfig);
await configurationService.SaveAsync(encryptedConfig);

// Decrypt on load
var decryptedConfig = securityService.DecryptConfiguration(encryptedConfig);
```

### Input Validation
```csharp
// Validate file inputs
public class DocumentValidator : IValidator<Document>
{
    public ValidationResult Validate(Document document)
    {
        var result = new ValidationResult();
        
        if (document.SizeBytes > MaxFileSize)
            result.Errors.Add("File size exceeds maximum limit");
            
        if (!SupportedFormats.Contains(Path.GetExtension(document.FilePath)))
            result.Errors.Add("Unsupported file format");
            
        return result;
    }
}
```

### Audit Logging
```csharp
// Log security events
auditLogger.LogSecurityEvent(new SecurityEvent
{
    EventType = SecurityEventType.DocumentProcessed,
    UserId = context.User.Identity.Name,
    ResourceId = document.Id,
    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
    Timestamp = DateTime.UtcNow,
    Details = new { document.FileName, document.SizeBytes }
});
```

## 📋 API Reference Quick Links

### Core APIs
- [Document Processing](document-processing.md) - PDF, Word, Excel processing
- [Anomali Integration](anomali-integration.md) - ThreatStream API
- [File Management](file-management.md) - Batch operations
- [Security](security.md) - Encryption and credentials

### Platform Integration
- [CLI Reference](cli-reference.md) - Command line interface
- [Plugin Development](plugin-api.md) - Custom extensions
- [REST API](rest-api.md) - HTTP endpoints
- [WebSocket API](websocket-api.md) - Real-time events

### Developer Tools
- [SDK Documentation](sdk.md) - .NET SDK reference
- [Code Examples](examples.md) - Sample implementations
- [Testing Guide](testing.md) - API testing strategies
- [Troubleshooting](troubleshooting.md) - Common issues

## 📞 API Support

For API-related questions and support:

- **API Documentation Issues**: [docs@hivehelixlabs.com](mailto:docs@hivehelixlabs.com)
- **Integration Support**: [integration@hivehelixlabs.com](mailto:integration@hivehelixlabs.com)
- **Developer Community**: [GitHub Discussions](https://github.com/HiveHelixLabs/AnomaliImportTool/discussions)
- **Bug Reports**: [GitHub Issues](https://github.com/HiveHelixLabs/AnomaliImportTool/issues)

---

*API documentation is automatically generated from code annotations and kept in sync with each release. Last updated: January 2025* 
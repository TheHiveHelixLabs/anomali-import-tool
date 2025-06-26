# Service Lifetime Configuration Guide

## Overview

This document provides comprehensive guidance for configuring service lifetimes using Microsoft.Extensions.DependencyInjection in the Anomali Import Tool. Proper service lifetime management is crucial for application performance, memory usage, and thread safety.

## Service Lifetimes

### 1. Singleton (`ServiceLifetime.Singleton`)

**Characteristics:**
- Single instance for the entire application lifetime
- Thread-safe implementation required
- Expensive to create, shared across all requests
- Memory efficient for stateless services

**Use Cases:**
- Configuration objects
- Logging services
- Caching services
- HTTP client factories
- Metrics collectors
- Event aggregators

**Example:**
```csharp
// Configuration - immutable, shared across application
services.AddSingleton<IApplicationConfiguration, ApplicationConfiguration>();

// Logging - thread-safe, expensive to create
services.AddSingleton<ILoggerFactory, LoggerFactory>();

// Caching - shared state, thread-safe
services.AddSingleton<ICacheService, CacheService>();
```

**Anti-patterns:**
❌ Singleton depending on Scoped or Transient services
❌ Mutable state without thread safety
❌ Database contexts as Singleton

### 2. Scoped (`ServiceLifetime.Scoped`)

**Characteristics:**
- One instance per scope (request/operation)
- Maintains state within the scope
- Automatically disposed at scope end
- Perfect for per-operation contexts

**Use Cases:**
- Database contexts
- Unit of work patterns
- Request-specific services
- Business logic services with state
- Security contexts

**Example:**
```csharp
// Data access - per-operation context
services.AddScoped<IDocumentRepository, DocumentRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business logic - maintains state within operation
services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

// Security - per-operation context
services.AddScoped<IAuthenticationService, AuthenticationService>();
```

**Anti-patterns:**
❌ Memory leaks from not disposing IDisposable services
❌ Sharing mutable state across scopes
❌ Thread-unsafe operations in concurrent scenarios

### 3. Transient (`ServiceLifetime.Transient`)

**Characteristics:**
- New instance every time requested
- Stateless and lightweight
- No shared state between instances
- Cheap to create and dispose

**Use Cases:**
- Validation services
- Mapping services
- Utility services
- Command/query handlers
- Cryptographic services

**Example:**
```csharp
// Validation - stateless, lightweight
services.AddTransient<IDocumentValidator, DocumentValidator>();

// Mapping - stateless transformations
services.AddTransient<IDocumentMapper, DocumentMapper>();

// Utilities - stateless helpers
services.AddTransient<IFileHashCalculator, FileHashCalculator>();

// Security - stateless, security-sensitive
services.AddTransient<IEncryptionService, EncryptionService>();
```

**Anti-patterns:**
❌ Expensive-to-create services as Transient
❌ Services that maintain state
❌ Database connections as Transient

## Service Lifetime Patterns

### Factory Pattern

Use factories for complex object creation scenarios:

```csharp
// Factory as Singleton (expensive to create, thread-safe)
services.AddSingleton<IDocumentProcessorFactory, DocumentProcessorFactory>();

// Products as Transient (lightweight, stateless)
services.AddTransient<IPdfDocumentProcessor, PdfDocumentProcessor>();
services.AddTransient<IWordDocumentProcessor, WordDocumentProcessor>();
```

### Decorator Pattern

Use decorators for cross-cutting concerns:

```csharp
// Base service
services.AddScoped<IDocumentService, DocumentService>();

// Decorator for logging
services.AddDecorator<IDocumentService, LoggingDocumentServiceDecorator>();

// Decorator for caching
services.AddDecorator<IDocumentService, CachingDocumentServiceDecorator>();
```

### Conditional Registration

Register different implementations based on environment:

```csharp
// Development services
services.AddConditionalService<IFileWatcherService, DevelopmentFileWatcherService>(
    condition: () => Environment.IsDevelopment(),
    lifetime: ServiceLifetime.Singleton);

// Production services
services.AddConditionalService<IFileWatcherService, ProductionFileWatcherService>(
    condition: () => Environment.IsProduction(),
    lifetime: ServiceLifetime.Singleton);
```

## Best Practices

### 1. Choose Appropriate Lifetimes

| Service Type | Recommended Lifetime | Rationale |
|--------------|---------------------|-----------|
| Configuration | Singleton | Immutable, shared |
| Logging | Singleton | Thread-safe, expensive |
| Database Context | Scoped | Per-operation state |
| Repository | Scoped | Depends on DbContext |
| Validators | Transient | Stateless, lightweight |
| Mappers | Transient | Stateless transformations |
| HTTP Clients | Singleton | Expensive, thread-safe |
| Caching | Singleton | Shared state |

### 2. Dependency Lifetime Rules

- **Singleton** services should only depend on other **Singleton** services
- **Scoped** services can depend on **Singleton** and **Scoped** services
- **Transient** services can depend on any lifetime

### 3. Memory Management

```csharp
// ✅ Good: Implement IDisposable for resources
public class DocumentProcessingService : IDocumentProcessingService, IDisposable
{
    private readonly IFileStream _fileStream;
    
    public void Dispose()
    {
        _fileStream?.Dispose();
    }
}

// ✅ Good: Register as Scoped for automatic disposal
services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
```

### 4. Thread Safety

```csharp
// ✅ Good: Thread-safe Singleton
public class CacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    
    public T Get<T>(string key) => (T)_cache.GetValueOrDefault(key);
    public void Set<T>(string key, T value) => _cache.TryAdd(key, value);
}

// ❌ Bad: Non-thread-safe Singleton
public class BadCacheService : ICacheService
{
    private readonly Dictionary<string, object> _cache = new(); // Not thread-safe!
}
```

## Validation and Diagnostics

### Automatic Validation

The system includes automatic validation for common anti-patterns:

```csharp
public static ServiceLifetimeValidationResult ValidateServiceLifetimes(this IServiceCollection services)
{
    var result = new ServiceLifetimeValidationResult();
    
    // Check for singleton anti-patterns
    ValidateSingletonAntiPatterns(services, result);
    
    // Check for scoped anti-patterns
    ValidateScopedAntiPatterns(services, result);
    
    // Check for transient anti-patterns
    ValidateTransientAntiPatterns(services, result);
    
    // Check for circular dependencies
    ValidateDependencyChains(services, result);
    
    return result;
}
```

### Common Validation Errors

1. **Singleton depending on Scoped/Transient**
   ```
   WARNING: Singleton service ICacheService depends on non-singleton services: IDocumentRepository
   ```

2. **Expensive Transient services**
   ```
   WARNING: Transient service IApiClientFactory appears to be expensive to create - consider Scoped or Singleton lifetime
   ```

3. **Circular dependencies**
   ```
   ERROR: Circular dependency detected: IServiceA -> IServiceB -> IServiceA
   ```

## Configuration Examples

### Complete Service Registration

```csharp
public static IServiceCollection ConfigureServiceLifetimes(this IServiceCollection services)
{
    // Singleton services (application-wide, thread-safe, expensive to create)
    services.AddSingleton<IApplicationConfiguration, ApplicationConfiguration>();
    services.AddSingleton<ILoggerFactory, LoggerFactory>();
    services.AddSingleton<ICacheService, CacheService>();
    services.AddSingleton<IHttpClientFactory, HttpClientFactory>();
    
    // Scoped services (per request/operation, maintains state within scope)
    services.AddScoped<IDocumentRepository, DocumentRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();
    services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
    services.AddScoped<IAuthenticationService, AuthenticationService>();
    
    // Transient services (stateless, lightweight, created on demand)
    services.AddTransient<IDocumentValidator, DocumentValidator>();
    services.AddTransient<IDocumentMapper, DocumentMapper>();
    services.AddTransient<IFileHashCalculator, FileHashCalculator>();
    services.AddTransient<IEncryptionService, EncryptionService>();
    
    // Factory services for complex object creation
    services.AddSingleton<IDocumentProcessorFactory, DocumentProcessorFactory>();
    services.AddTransient<IPdfDocumentProcessor, PdfDocumentProcessor>();
    services.AddTransient<IWordDocumentProcessor, WordDocumentProcessor>();
    
    return services;
}
```

### Service Provider Building

```csharp
public static IServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection();
    
    // Configure all services with proper lifetimes
    services.ConfigureServiceLifetimes();
    
    // Validate configuration
    var validationResult = services.ValidateServiceLifetimes();
    if (!validationResult.IsValid)
    {
        throw new InvalidOperationException($"Service lifetime validation failed: {validationResult.GetSummary()}");
    }
    
    return services.BuildServiceProvider();
}
```

## Performance Considerations

### Memory Usage

| Lifetime | Memory Impact | Notes |
|----------|---------------|-------|
| Singleton | Low | Single instance, long-lived |
| Scoped | Medium | Instance per scope, medium-lived |
| Transient | High | New instance per request, short-lived |

### Creation Cost

| Service Type | Recommended Lifetime | Reason |
|--------------|---------------------|---------|
| HTTP Clients | Singleton | Expensive socket management |
| Database Contexts | Scoped | Connection pooling |
| Validators | Transient | Cheap to create |
| Loggers | Singleton | Expensive initialization |

## Testing Considerations

### Unit Testing

```csharp
[Test]
public void DocumentService_Should_Process_Document()
{
    // Arrange
    var services = new ServiceCollection();
    services.ConfigureServiceLifetimes();
    var provider = services.BuildServiceProvider();
    
    // Act
    var documentService = provider.GetRequiredService<IDocumentService>();
    var result = documentService.ProcessDocument(document);
    
    // Assert
    Assert.That(result.IsSuccess, Is.True);
}
```

### Integration Testing

```csharp
[Test]
public void ServiceLifetimes_Should_Be_Valid()
{
    // Arrange
    var services = new ServiceCollection();
    services.ConfigureServiceLifetimes();
    
    // Act
    var validationResult = services.ValidateServiceLifetimes();
    
    // Assert
    Assert.That(validationResult.IsValid, Is.True);
    Assert.That(validationResult.Errors, Is.Empty);
}
```

## Related Documentation

- [Clean Architecture Dependencies](CleanArchitectureDependencies.md)
- [Assembly Scanning Configuration](AssemblyScanningConfiguration.md)
- [Architecture Fitness Test Results](ArchitectureFitnessTestResults.md)

## Summary

Proper service lifetime configuration is essential for:

1. **Performance**: Avoiding unnecessary object creation
2. **Memory Management**: Preventing memory leaks
3. **Thread Safety**: Ensuring concurrent access safety
4. **Maintainability**: Clear service responsibilities
5. **Testability**: Easy mocking and testing

The Anomali Import Tool implements comprehensive service lifetime management with automatic validation, ensuring optimal performance and reliability while maintaining clean architecture principles. 
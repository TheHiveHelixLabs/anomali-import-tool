# Assembly Scanning Configuration - Automatic Service Registration

## Overview

The Anomali Import Tool implements a sophisticated assembly scanning system that automatically discovers and registers services using attributes and conventions. This eliminates the need for manual service registration and ensures consistent dependency injection across all layers of the Clean Architecture.

## Architecture Components

### 1. Core Registration Infrastructure

#### ServiceRegistrationAttribute
Located in: `AnomaliImportTool.Core.Application.DependencyInjection`

```csharp
[ServiceRegistration(typeof(IMyService), ServiceLifetime.Scoped)]
public class MyService : IMyService
{
    // Implementation
}
```

**Features:**
- Explicit interface specification
- Configurable service lifetime
- Automatic interface detection fallback
- Convention-based registration support

#### ServiceCollectionExtensions
Provides multiple registration strategies:

- **Attribute-based**: `AddServicesFromAssembly()`
- **Interface-based**: `AddImplementationsOf<T>()`
- **Convention-based**: `AddServicesByConvention()`
- **Decorator pattern**: `AddDecorator<TService, TDecorator>()`

### 2. Layer-Specific Registration Modules

#### Application Layer (`ApplicationServiceRegistration`)
```csharp
services.AddApplicationServices()
```

**Registers:**
- Command/Query handlers (`ICommandHandler`, `IQueryHandler`)
- Use case handlers (`IUseCaseHandler`)
- Validators (convention-based)
- Mappers and profiles

#### Infrastructure Layer (`InfrastructureServiceRegistration`)
```csharp
services.AddInfrastructureServices()
```

**Registers:**
- Repository implementations
- Infrastructure services
- HTTP clients with configuration
- External service integrations

#### Security Layer (`SecurityServiceRegistration`)
```csharp
services.AddSecurityServices()
```

**Registers:**
- Cryptography services (Singleton lifetime)
- Authentication services (Scoped lifetime)
- Authorization services (Scoped lifetime)
- Security audit services (Scoped lifetime)

#### Document Processing Layer (`DocumentProcessingServiceRegistration`)
```csharp
services.AddDocumentProcessingServices()
```

**Registers:**
- File processing services
- Document parsers by type (PDF, Word, Excel, etc.)
- Content extraction services
- Document validation services

#### API Layer (`ApiServiceRegistration`)
```csharp
services.AddApiServices()
```

**Registers:**
- Anomali ThreatStream API services
- HTTP client services with named configurations
- API authentication services
- Response handling services

#### Git Layer (`GitServiceRegistration`)
```csharp
services.AddGitServices()
```

**Registers:**
- Git repository services
- Git operation services (pull, push, fetch, merge)
- Git branch services
- Git commit services

### 3. Master Composition Root

#### ServiceCompositionRoot
Located in: `AnomaliImportTool.WPF.DependencyInjection`

**Orchestrates all service registration:**
```csharp
public static IServiceCollection ConfigureServices(this IServiceCollection services)
{
    // Register core services
    services.AddApplicationServices();
    services.AddInfrastructureServices();

    // Register specialized infrastructure services
    services.AddSecurityServices();
    services.AddDocumentProcessingServices();
    services.AddApiServices();
    services.AddGitServices();

    // Register presentation layer services
    services.AddPresentationServices();

    // Register cross-cutting concerns
    services.AddLogging();
    services.AddConfiguration();

    return services;
}
```

## Registration Strategies

### 1. Attribute-Based Registration

**Best for:** Explicit control over service registration

```csharp
[ServiceRegistration(typeof(IDocumentService), ServiceLifetime.Scoped)]
public class DocumentService : IDocumentService
{
    // Automatically registered as IDocumentService with Scoped lifetime
}
```

### 2. Convention-Based Registration

**Best for:** Services following naming conventions

```csharp
// IUserService -> UserService (automatic detection)
public class UserService : IUserService
{
    // Automatically registered by convention
}
```

### 3. Interface-Based Registration

**Best for:** Registering all implementations of a specific interface

```csharp
// Registers all classes implementing IRepository<T>
services.AddImplementationsOf<IRepository<T>>(ServiceLifetime.Scoped, assemblies);
```

### 4. Decorator Pattern Support

**Best for:** Cross-cutting concerns (logging, caching, validation)

```csharp
services.AddDecorator<IUserService, CachedUserService>();
```

## Service Lifetimes

### Singleton
- **Usage:** Stateless services, expensive-to-create services
- **Examples:** Cryptography services, configuration services
- **Thread Safety:** Must be thread-safe

### Scoped
- **Usage:** Per-request services, services with state
- **Examples:** Repositories, business services, API clients
- **Lifecycle:** Created once per scope (request/operation)

### Transient
- **Usage:** Lightweight, stateless services
- **Examples:** View models, validators, mappers
- **Lifecycle:** Created every time requested

## HTTP Client Configuration

### Named HTTP Clients
```csharp
services.AddHttpClient("AnomaliThreatStream", client =>
{
    client.BaseAddress = new Uri("https://api.threatstream.com/");
    client.Timeout = TimeSpan.FromMinutes(10);
    client.DefaultRequestHeaders.Add("User-Agent", "AnomaliImportTool/1.0");
});
```

**Benefits:**
- Centralized configuration
- Connection pooling
- Resilience policies support
- Testability through mocking

## Validation and Diagnostics

### Service Validation
```csharp
var validationResult = ServiceCompositionRoot.ValidateServices(serviceProvider);
if (!validationResult.IsValid)
{
    // Handle validation errors
    foreach (var error in validationResult.Errors)
    {
        logger.LogError(error);
    }
}
```

**Validates:**
- Critical service resolution
- Circular dependencies
- Missing implementations
- Configuration issues

## Best Practices

### 1. Service Design
- **Single Responsibility:** Each service has one clear purpose
- **Interface Segregation:** Small, focused interfaces
- **Dependency Inversion:** Depend on abstractions, not concretions

### 2. Registration Patterns
- Use attributes for explicit control
- Use conventions for consistent patterns
- Use interface-based for bulk registration
- Use decorators for cross-cutting concerns

### 3. Lifetime Management
- Default to Scoped for business services
- Use Singleton for expensive stateless services
- Use Transient for lightweight services
- Avoid Singleton for services with dependencies

### 4. Testing Considerations
- Register test doubles in test projects
- Use service validation in integration tests
- Mock external dependencies
- Test service resolution in startup tests

## Integration with Clean Architecture

### Dependency Flow
```
Presentation Layer (WPF)
    ↓ (depends on)
Infrastructure Layer
    ↓ (depends on)
Application Layer
    ↓ (depends on)
Domain Layer (no dependencies)
```

### Assembly Scanning Flow
1. **Domain Layer:** No services (pure domain logic)
2. **Application Layer:** Interfaces and use cases
3. **Infrastructure Layer:** Implementations and external services
4. **Presentation Layer:** UI services and composition root

## Error Handling

### Common Issues
- **Missing Interface:** Service class doesn't implement expected interface
- **Circular Dependencies:** Services depend on each other directly
- **Lifetime Mismatches:** Longer-lived service depends on shorter-lived service
- **Assembly Loading:** Assembly not found during scanning

### Debugging Tips
- Enable detailed logging during startup
- Use service validation to catch issues early
- Check assembly references and loading
- Verify attribute placement and spelling

## Performance Considerations

### Assembly Scanning Performance
- **Startup Cost:** One-time cost during application startup
- **Runtime Performance:** No impact on runtime performance
- **Memory Usage:** Minimal overhead for attribute metadata

### Optimization Strategies
- Limit assembly scanning to relevant assemblies
- Use specific interface registration over broad scanning
- Cache service provider after configuration
- Use lazy initialization for expensive services

## Future Enhancements

### Planned Features
- **Configuration-based registration:** Register services via configuration files
- **Conditional registration:** Register services based on environment or feature flags
- **Health checks integration:** Automatic health check registration for services
- **Metrics integration:** Automatic performance metrics for service calls

### Extensibility Points
- Custom registration attributes
- Plugin-based service discovery
- Dynamic service registration
- Service proxy generation

## Example Usage

### Basic Service Registration
```csharp
[ServiceRegistration(ServiceLifetime.Scoped)]
public class ThreatAnalysisService : IThreatAnalysisService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IAnomaliApiService _anomaliApiService;

    public ThreatAnalysisService(
        IDocumentRepository documentRepository,
        IAnomaliApiService anomaliApiService)
    {
        _documentRepository = documentRepository;
        _anomaliApiService = anomaliApiService;
    }

    public async Task<ThreatAnalysis> AnalyzeDocumentAsync(Document document)
    {
        // Implementation
    }
}
```

### Service Composition
```csharp
// In Program.cs or App.xaml.cs
var serviceProvider = ServiceCompositionRoot.BuildServiceProvider();

// Service resolution
var threatAnalysisService = serviceProvider.GetRequiredService<IThreatAnalysisService>();
```

This assembly scanning system provides a robust, maintainable, and testable foundation for dependency injection in the Anomali Import Tool, ensuring Clean Architecture principles are maintained while reducing boilerplate code. 
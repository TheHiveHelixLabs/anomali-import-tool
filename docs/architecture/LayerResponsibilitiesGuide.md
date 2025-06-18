# Layer Responsibilities Guide

## Overview

This guide provides practical guidance for developers on what belongs in each layer of the Clean Architecture implementation. It includes examples of correct implementations and common anti-patterns to avoid.

## Quick Reference

| Layer | What Goes Here | What Doesn't Go Here |
|-------|----------------|---------------------|
| **Domain** | Business rules, entities, value objects, domain events | I/O operations, external dependencies, framework code |
| **Application** | Use cases, interfaces, orchestration logic | Business rules, infrastructure details, UI logic |
| **Infrastructure** | Database access, external APIs, file I/O, cross-cutting concerns | Business logic, UI logic |
| **Presentation** | UI logic, user interactions, view models | Business logic, data access |

---

## Domain Layer (Core.Domain)

### ✅ What Belongs Here

#### 1. Domain Entities
Rich objects that encapsulate business logic and maintain invariants.

```csharp
public class ThreatBulletin : BaseEntity
{
    public string Title { get; private set; }
    public string Content { get; private set; }
    public ThreatLevel Level { get; private set; }
    public TlpDesignation TlpDesignation { get; private set; }
    public BulletinStatus Status { get; private set; }
    
    // Business logic methods
    public void UpdateContent(string newContent)
    {
        Guard.Against.NullOrWhiteSpace(newContent, nameof(newContent));
        
        if (Status == BulletinStatus.Published)
            throw new DomainException("Cannot update published bulletin content");
            
        Content = newContent;
        AddDomainEvent(new ThreatBulletinUpdatedEvent(Id, newContent));
    }
    
    public void Publish()
    {
        if (Status != BulletinStatus.Draft)
            throw new DomainException("Only draft bulletins can be published");
            
        Status = BulletinStatus.Published;
        AddDomainEvent(new ThreatBulletinPublishedEvent(Id, Title));
    }
}
```

#### 2. Value Objects
Immutable objects that represent concepts without identity.

```csharp
public readonly record struct FileMetadata
{
    public string FileName { get; }
    public long Size { get; }
    public string MimeType { get; }
    public ContentHash Hash { get; }
    public DateTime CreatedAt { get; }
    
    public FileMetadata(string fileName, long size, string mimeType, ContentHash hash)
    {
        Guard.Against.NullOrWhiteSpace(fileName, nameof(fileName));
        Guard.Against.NegativeOrZero(size, nameof(size));
        Guard.Against.NullOrWhiteSpace(mimeType, nameof(mimeType));
        
        FileName = fileName;
        Size = size;
        MimeType = mimeType;
        Hash = hash;
        CreatedAt = DateTime.UtcNow;
    }
    
    public bool IsValidImageFile() => 
        MimeType.StartsWith("image/") && Size <= 10_000_000; // 10MB max
}
```

#### 3. Domain Primitives
Strongly-typed wrappers around primitive types.

```csharp
public readonly record struct EmailAddress
{
    private readonly string _value;
    
    public EmailAddress(string value)
    {
        Guard.Against.NullOrWhiteSpace(value, nameof(value));
        Guard.Against.InvalidEmail(value, nameof(value));
        _value = value;
    }
    
    public static implicit operator string(EmailAddress email) => email._value;
    public static explicit operator EmailAddress(string value) => new(value);
    
    public string Domain => _value.Split('@')[1];
    public string LocalPart => _value.Split('@')[0];
    
    public override string ToString() => _value;
}
```

#### 4. Domain Events
Events that represent something important that happened in the domain.

```csharp
public class ThreatBulletinImportedEvent : BaseDomainEvent
{
    public Guid BulletinId { get; }
    public string Source { get; }
    public ThreatLevel Level { get; }
    public int DocumentCount { get; }
    
    public ThreatBulletinImportedEvent(
        Guid bulletinId, 
        string source, 
        ThreatLevel level, 
        int documentCount)
    {
        BulletinId = bulletinId;
        Source = source;
        Level = level;
        DocumentCount = documentCount;
    }
}
```

#### 5. Business Rules and Validation
Logic that represents core business constraints.

```csharp
public static class ThreatBulletinRules
{
    public static bool CanBeUpdated(BulletinStatus status) => 
        status == BulletinStatus.Draft;
    
    public static bool RequiresApproval(ThreatLevel level) => 
        level >= ThreatLevel.High;
    
    public static TimeSpan GetRetentionPeriod(TlpDesignation tlp) => tlp switch
    {
        TlpDesignation.Red => TimeSpan.FromDays(30),
        TlpDesignation.Amber => TimeSpan.FromDays(90),
        TlpDesignation.Green => TimeSpan.FromDays(365),
        TlpDesignation.White => TimeSpan.FromDays(1825), // 5 years
        _ => throw new ArgumentOutOfRangeException(nameof(tlp))
    };
}
```

### ❌ What Doesn't Belong Here

#### Anti-Pattern 1: Infrastructure Dependencies
```csharp
// ❌ DON'T DO THIS - Database dependency in domain
public class ThreatBulletin : BaseEntity
{
    public void Save()
    {
        using var connection = new SqlConnection(connectionString);
        // Database logic in domain entity
    }
}

// ✅ DO THIS INSTEAD - Keep domain pure
public class ThreatBulletin : BaseEntity
{
    public void UpdateContent(string newContent)
    {
        // Pure business logic only
        Guard.Against.NullOrWhiteSpace(newContent, nameof(newContent));
        Content = newContent;
        AddDomainEvent(new ThreatBulletinUpdatedEvent(Id, newContent));
    }
}
```

#### Anti-Pattern 2: Framework Dependencies
```csharp
// ❌ DON'T DO THIS - Framework dependency in domain
public class Document : BaseEntity
{
    [JsonProperty("title")]
    public string Title { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Content { get; set; }
}

// ✅ DO THIS INSTEAD - Framework-agnostic domain
public class Document : BaseEntity
{
    public string Title { get; private set; }
    public string Content { get; private set; }
    
    public void UpdateContent(string newContent)
    {
        Guard.Against.NullOrWhiteSpace(newContent, nameof(newContent));
        Guard.Against.LengthOutOfRange(newContent, nameof(newContent), 1, 500);
        Content = newContent;
    }
}
```

---

## Application Layer (Core.Application)

### ✅ What Belongs Here

#### 1. Use Case Implementations
Orchestrate domain objects to fulfill application requirements.

```csharp
public class ImportThreatBulletinHandler : IRequestHandler<ImportThreatBulletinCommand, ImportResult>
{
    private readonly IThreatBulletinRepository _repository;
    private readonly IAnomaliApiService _apiService;
    private readonly INotificationService _notificationService;
    
    public async Task<ImportResult> Handle(
        ImportThreatBulletinCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Fetch data from external API
        var bulletinData = await _apiService.GetBulletinAsync(
            request.BulletinId, 
            cancellationToken);
        
        // 2. Create domain entity
        var bulletin = ThreatBulletin.Create(
            bulletinData.Title,
            bulletinData.Content,
            bulletinData.Level,
            bulletinData.TlpDesignation);
        
        // 3. Apply business rules
        if (ThreatBulletinRules.RequiresApproval(bulletin.Level))
        {
            bulletin.MarkForApproval();
        }
        
        // 4. Persist entity
        await _repository.AddAsync(bulletin, cancellationToken);
        
        // 5. Send notifications
        await _notificationService.NotifyBulletinImportedAsync(
            bulletin.Id, 
            cancellationToken);
        
        return new ImportResult(bulletin.Id, bulletin.Status);
    }
}
```

#### 2. Interface Definitions
Define contracts for infrastructure services.

```csharp
public interface IDocumentProcessingService
{
    Task<ProcessingResult> ProcessDocumentAsync(
        FilePath filePath, 
        CancellationToken cancellationToken = default);
    
    Task<ExtractionResult> ExtractTextAsync(
        FilePath filePath, 
        CancellationToken cancellationToken = default);
    
    Task<ValidationResult> ValidateDocumentAsync(
        FilePath filePath, 
        CancellationToken cancellationToken = default);
}

public interface IThreatBulletinRepository
{
    Task<ThreatBulletin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ThreatBulletin>> GetByStatusAsync(BulletinStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<ThreatBulletin>> GetByThreatLevelAsync(ThreatLevel level, CancellationToken cancellationToken = default);
    Task AddAsync(ThreatBulletin bulletin, CancellationToken cancellationToken = default);
    Task UpdateAsync(ThreatBulletin bulletin, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

#### 3. Application Services
Coordinate multiple use cases or provide application-level functionality.

```csharp
[ServiceRegistration(typeof(IThreatIntelligenceService), ServiceLifetime.Scoped)]
public class ThreatIntelligenceService : IThreatIntelligenceService
{
    private readonly IThreatBulletinRepository _bulletinRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IAnomaliApiService _apiService;
    
    public async Task<ThreatIntelligenceReport> GenerateReportAsync(
        DateRange dateRange, 
        CancellationToken cancellationToken = default)
    {
        // Orchestrate multiple repositories and services
        var bulletins = await _bulletinRepository.GetByDateRangeAsync(dateRange, cancellationToken);
        var documents = await _documentRepository.GetByDateRangeAsync(dateRange, cancellationToken);
        
        var threatLevelDistribution = bulletins
            .GroupBy(b => b.Level)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var topThreats = bulletins
            .Where(b => b.Level >= ThreatLevel.High)
            .OrderByDescending(b => b.Level)
            .Take(10)
            .ToList();
        
        return new ThreatIntelligenceReport(
            dateRange,
            bulletins.Count(),
            documents.Count(),
            threatLevelDistribution,
            topThreats);
    }
}
```

#### 4. Data Transfer Objects (DTOs)
Objects for transferring data between layers.

```csharp
public record ImportThreatBulletinCommand(
    string BulletinId,
    bool IncludeDocuments = true,
    bool NotifySubscribers = true) : IRequest<ImportResult>;

public record ImportResult(
    Guid BulletinId,
    BulletinStatus Status,
    int DocumentsImported = 0,
    string? ErrorMessage = null);

public record ThreatBulletinDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public ThreatLevel Level { get; init; }
    public BulletinStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PublishedAt { get; init; }
}
```

### ❌ What Doesn't Belong Here

#### Anti-Pattern 1: Business Logic in Application Services
```csharp
// ❌ DON'T DO THIS - Business logic in application layer
public class ThreatBulletinService
{
    public async Task UpdateBulletinAsync(Guid id, string newContent)
    {
        var bulletin = await _repository.GetByIdAsync(id);
        
        // Business logic should be in domain, not here
        if (string.IsNullOrWhiteSpace(newContent))
            throw new ValidationException("Content cannot be empty");
            
        if (bulletin.Status == BulletinStatus.Published)
            throw new BusinessException("Cannot update published bulletin");
            
        bulletin.Content = newContent; // Direct property access
        await _repository.UpdateAsync(bulletin);
    }
}

// ✅ DO THIS INSTEAD - Delegate to domain
public class UpdateThreatBulletinHandler : IRequestHandler<UpdateThreatBulletinCommand>
{
    public async Task Handle(UpdateThreatBulletinCommand request, CancellationToken cancellationToken)
    {
        var bulletin = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        // Let domain handle business logic
        bulletin.UpdateContent(request.NewContent);
        
        await _repository.UpdateAsync(bulletin, cancellationToken);
    }
}
```

---

## Infrastructure Layer

### ✅ What Belongs Here

#### 1. Repository Implementations
Concrete implementations of repository interfaces.

```csharp
[ServiceRegistration(typeof(IThreatBulletinRepository), ServiceLifetime.Scoped)]
public class ThreatBulletinRepository : IThreatBulletinRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ThreatBulletinRepository> _logger;
    
    public async Task<ThreatBulletin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.ThreatBulletins
                .Include(b => b.Documents)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving threat bulletin {BulletinId}", id);
            throw;
        }
    }
    
    public async Task AddAsync(ThreatBulletin bulletin, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.ThreatBulletins.Add(bulletin);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Added threat bulletin {BulletinId}", bulletin.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding threat bulletin {BulletinId}", bulletin.Id);
            throw;
        }
    }
}
```

#### 2. External Service Clients
Implementations for external API integrations.

```csharp
[ServiceRegistration(typeof(IAnomaliApiService), ServiceLifetime.Scoped)]
public class AnomaliApiService : IAnomaliApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnomaliApiService> _logger;
    private readonly AnomaliApiOptions _options;
    
    public async Task<ApiResponse<ThreatBulletinData>> GetBulletinAsync(
        string bulletinId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v1/bulletins/{bulletinId}", 
                cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API request failed with status {StatusCode}", response.StatusCode);
                return ApiResponse<ThreatBulletinData>.Failure($"API returned {response.StatusCode}");
            }
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<ThreatBulletinData>(content);
            
            return ApiResponse<ThreatBulletinData>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching bulletin {BulletinId}", bulletinId);
            return ApiResponse<ThreatBulletinData>.Failure(ex.Message);
        }
    }
}
```

#### 3. Cross-Cutting Concerns
Infrastructure services for logging, caching, etc.

```csharp
[ServiceRegistration(typeof(INotificationService), ServiceLifetime.Scoped)]
public class EmailNotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailNotificationService> _logger;
    
    public async Task NotifyBulletinImportedAsync(Guid bulletinId, CancellationToken cancellationToken = default)
    {
        try
        {
            var subscribers = await GetSubscribersAsync(cancellationToken);
            
            var tasks = subscribers.Select(subscriber => 
                _emailSender.SendEmailAsync(
                    subscriber.Email,
                    "New Threat Bulletin Imported",
                    $"A new threat bulletin (ID: {bulletinId}) has been imported.",
                    cancellationToken));
            
            await Task.WhenAll(tasks);
            
            _logger.LogInformation("Notified {Count} subscribers about bulletin {BulletinId}", 
                subscribers.Count, bulletinId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notifications for bulletin {BulletinId}", bulletinId);
            throw;
        }
    }
}
```

### ❌ What Doesn't Belong Here

#### Anti-Pattern 1: Business Logic in Infrastructure
```csharp
// ❌ DON'T DO THIS - Business logic in infrastructure
public class ThreatBulletinRepository : IThreatBulletinRepository
{
    public async Task AddAsync(ThreatBulletin bulletin, CancellationToken cancellationToken = default)
    {
        // Business logic doesn't belong here
        if (bulletin.Level >= ThreatLevel.High && !bulletin.IsApproved)
        {
            throw new BusinessException("High-level threats require approval");
        }
        
        _context.ThreatBulletins.Add(bulletin);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// ✅ DO THIS INSTEAD - Keep infrastructure focused on I/O
public class ThreatBulletinRepository : IThreatBulletinRepository
{
    public async Task AddAsync(ThreatBulletin bulletin, CancellationToken cancellationToken = default)
    {
        // Pure data access logic
        _context.ThreatBulletins.Add(bulletin);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

---

## Presentation Layer (WPF)

### ✅ What Belongs Here

#### 1. View Models
Handle UI state and user interactions.

```csharp
public class ThreatBulletinListViewModel : ViewModelBase
{
    private readonly IThreatBulletinService _bulletinService;
    private readonly INotificationService _notificationService;
    
    public ObservableCollection<ThreatBulletinDto> Bulletins { get; } = new();
    public ICommand ImportBulletinCommand { get; }
    public ICommand RefreshCommand { get; }
    
    public ThreatBulletinListViewModel(
        IThreatBulletinService bulletinService,
        INotificationService notificationService)
    {
        _bulletinService = bulletinService;
        _notificationService = notificationService;
        
        ImportBulletinCommand = new AsyncRelayCommand<string>(ImportBulletinAsync);
        RefreshCommand = new AsyncRelayCommand(LoadBulletinsAsync);
    }
    
    private async Task ImportBulletinAsync(string? bulletinId)
    {
        if (string.IsNullOrWhiteSpace(bulletinId)) return;
        
        try
        {
            IsLoading = true;
            
            var command = new ImportThreatBulletinCommand(bulletinId);
            var result = await _bulletinService.ImportBulletinAsync(command);
            
            if (result.IsSuccess)
            {
                await LoadBulletinsAsync();
                await _notificationService.ShowSuccessAsync("Bulletin imported successfully");
            }
            else
            {
                await _notificationService.ShowErrorAsync($"Import failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Unexpected error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

#### 2. User Controls and Views
UI components and layouts.

```xml
<UserControl x:Class="AnomaliImportTool.WPF.Views.ThreatBulletinListView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="10">
            <Button Content="Import Bulletin" 
                    Command="{Binding ImportBulletinCommand}"
                    CommandParameter="{Binding SelectedBulletinId}"/>
            <Button Content="Refresh" 
                    Command="{Binding RefreshCommand}"
                    Margin="10,0,0,0"/>
        </StackPanel>
        
        <!-- Data Grid -->
        <DataGrid Grid.Row="1" 
                  ItemsSource="{Binding Bulletins}"
                  SelectedItem="{Binding SelectedBulletin}"
                  AutoGenerateColumns="False"
                  IsReadOnly="True">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Title" Binding="{Binding Title}"/>
                <DataGridTextColumn Header="Level" Binding="{Binding Level}"/>
                <DataGridTextColumn Header="Status" Binding="{Binding Status}"/>
                <DataGridTextColumn Header="Created" Binding="{Binding CreatedAt, StringFormat=yyyy-MM-dd}"/>
            </DataGrid.Columns>
        </DataGrid>
    </Grid>
</UserControl>
```

#### 3. UI-Specific Services
Services that handle UI concerns.

```csharp
[ServiceRegistration(typeof(IDialogService), ServiceLifetime.Singleton)]
public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }
    
    public async Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
    
    public async Task<string?> ShowInputDialogAsync(string title, string prompt)
    {
        var dialog = new InputDialog(title, prompt);
        return dialog.ShowDialog() == true ? dialog.InputText : null;
    }
}
```

### ❌ What Doesn't Belong Here

#### Anti-Pattern 1: Business Logic in View Models
```csharp
// ❌ DON'T DO THIS - Business logic in presentation
public class ThreatBulletinViewModel : ViewModelBase
{
    private async Task SaveBulletinAsync()
    {
        // Business logic doesn't belong here
        if (string.IsNullOrWhiteSpace(Title))
        {
            await ShowErrorAsync("Title is required");
            return;
        }
        
        if (Level >= ThreatLevel.High && !IsApproved)
        {
            await ShowErrorAsync("High-level threats require approval");
            return;
        }
        
        // Direct database access doesn't belong here
        using var context = new ApplicationDbContext();
        context.ThreatBulletins.Add(new ThreatBulletin { Title = Title, Level = Level });
        await context.SaveChangesAsync();
    }
}

// ✅ DO THIS INSTEAD - Delegate to application layer
public class ThreatBulletinViewModel : ViewModelBase
{
    private async Task SaveBulletinAsync()
    {
        try
        {
            var command = new CreateThreatBulletinCommand(Title, Content, Level);
            var result = await _bulletinService.CreateBulletinAsync(command);
            
            if (result.IsSuccess)
            {
                await _navigationService.NavigateToAsync<ThreatBulletinListViewModel>();
            }
            else
            {
                await _dialogService.ShowErrorAsync("Error", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", ex.Message);
        }
    }
}
```

---

## Validation Checklist

Before committing code, ask these questions:

### Domain Layer
- [ ] Does this class contain only business logic?
- [ ] Are there any infrastructure dependencies?
- [ ] Are value objects immutable?
- [ ] Do entities maintain their invariants?

### Application Layer
- [ ] Does this service orchestrate rather than implement business logic?
- [ ] Are all external dependencies abstracted behind interfaces?
- [ ] Are use cases focused on a single responsibility?

### Infrastructure Layer
- [ ] Does this class implement an interface from the Application layer?
- [ ] Is business logic delegated to the domain?
- [ ] Are exceptions properly handled and logged?

### Presentation Layer
- [ ] Is business logic delegated to the Application layer?
- [ ] Are view models focused on UI concerns?
- [ ] Is data binding used appropriately?

By following these guidelines, you'll maintain a clean, maintainable architecture that supports the long-term success of the project. 
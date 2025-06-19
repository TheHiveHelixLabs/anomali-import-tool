using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.Uno.ViewModels;

/// <summary>
/// Main ViewModel for the Anomali Import Tool Uno application
/// Demonstrates cross-platform functionality with Core models
/// </summary>
public class MainViewModel : BaseViewModel
{
    private readonly ObservableCollection<Document> _documents;
    private readonly ObservableCollection<ThreatBulletin> _threatBulletins;
    private string _selectedFilePath = string.Empty;
    private bool _isProcessing = false;
    
    /// <summary>
    /// Initializes a new instance of MainViewModel
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public MainViewModel(ILogger<MainViewModel> logger) : base(logger)
    {
        Title = "Anomali Threat Bulletin Import Tool - Cross Platform";
        
        // Initialize collections
        _documents = new ObservableCollection<Document>();
        _threatBulletins = new ObservableCollection<ThreatBulletin>();
        
        // Setup commands
        SetupCommands();
        
        // Load sample data
        LoadSampleDataAsync();
    }
    
    #region Properties
    
    /// <summary>
    /// Collection of documents for processing
    /// </summary>
    public ObservableCollection<Document> Documents => _documents;
    
    /// <summary>
    /// Collection of processed threat bulletins
    /// </summary>
    public ObservableCollection<ThreatBulletin> ThreatBulletins => _threatBulletins;
    
    /// <summary>
    /// Currently selected file path
    /// </summary>
    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set => this.RaiseAndSetIfChanged(ref _selectedFilePath, value);
    }
    
    /// <summary>
    /// Whether document processing is active
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        set => this.RaiseAndSetIfChanged(ref _isProcessing, value);
    }
    
    /// <summary>
    /// Cross-platform status information
    /// </summary>
    public string PlatformInfo => GetPlatformInfo();
    
    #endregion
    
    #region Commands
    
    /// <summary>
    /// Command to select a file for processing
    /// </summary>
    public ReactiveCommand<Unit, Unit> SelectFileCommand { get; private set; } = null!;
    
    /// <summary>
    /// Command to process the selected document
    /// </summary>
    public ReactiveCommand<Unit, Unit> ProcessDocumentCommand { get; private set; } = null!;
    
    /// <summary>
    /// Command to clear all data
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearDataCommand { get; private set; } = null!;
    
    /// <summary>
    /// Command to add sample data for demonstration
    /// </summary>
    public ReactiveCommand<Unit, Unit> AddSampleDataCommand { get; private set; } = null!;
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Setup reactive commands
    /// </summary>
    private void SetupCommands()
    {
        // Select file command
        SelectFileCommand = CreateCommand(
            SelectFileAsync,
            WhenNotBusy,
            "Selecting file...");
        
        // Process document command
        var canProcess = this.WhenAnyValue(
            x => x.SelectedFilePath,
            x => x.IsProcessing,
            (path, processing) => !string.IsNullOrEmpty(path) && !processing);
        
        ProcessDocumentCommand = CreateCommand(
            ProcessDocumentAsync,
            canProcess,
            "Processing document...");
        
        // Clear data command
        ClearDataCommand = CreateCommand(
            ClearDataAsync,
            WhenNotBusy,
            "Clearing data...");
        
        // Add sample data command
        AddSampleDataCommand = CreateCommand(
            AddSampleDataAsync,
            WhenNotBusy,
            "Adding sample data...");
    }
    
    /// <summary>
    /// Simulate file selection (cross-platform placeholder)
    /// </summary>
    private async Task SelectFileAsync()
    {
        await Task.Delay(500); // Simulate file dialog
        
        // For demonstration, set a sample file path
        SelectedFilePath = "/path/to/threat-bulletin.pdf";
        SetStatus("File selected successfully");
        
        Logger.LogInformation("File selected: {FilePath}", SelectedFilePath);
    }
    
    /// <summary>
    /// Process the selected document
    /// </summary>
    private async Task ProcessDocumentAsync()
    {
        try
        {
            IsProcessing = true;
            
            // Simulate document processing using Core models
            await Task.Delay(2000); // Simulate processing time
            
            // Create a sample document from Core models
            var document = new Document
            {
                FileName = System.IO.Path.GetFileName(SelectedFilePath),
                FilePath = SelectedFilePath,
                FileSize = 1024 * 512, // 512 KB
                ProcessingStartTime = DateTime.UtcNow.AddSeconds(-2),
                ProcessingEndTime = DateTime.UtcNow,
                Status = DocumentStatus.Completed,
                IsScanned = true,
                PageCount = 15,
                TlpDesignation = TlpDesignation.White,
                Creator = "Uno Platform Cross-Platform Engine"
            };
            
            Documents.Add(document);
            
            // Create a sample threat bulletin
            var bulletin = new ThreatBulletin
            {
                Id = Guid.NewGuid().ToString(),
                Name = "APT29 Campaign Analysis",
                Body = "Analysis of recent APT29 activities targeting government entities",
                Indicators = new() { "malware.exe", "192.168.1.100", "evil-domain.com" },
                Status = BulletinStatus.Published,
                Tlp = TlpDesignation.White,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            ThreatBulletins.Add(bulletin);
            
            SetStatus($"Document processed successfully! Platform: {PlatformInfo}");
            
            Logger.LogInformation("Document processed successfully: {FileName}", document.FileName);
        }
        catch (Exception ex)
        {
            SetError($"Failed to process document: {ex.Message}");
            Logger.LogError(ex, "Error processing document");
        }
        finally
        {
            IsProcessing = false;
        }
    }
    
    /// <summary>
    /// Clear all data
    /// </summary>
    private async Task ClearDataAsync()
    {
        await Task.Delay(100);
        
        Documents.Clear();
        ThreatBulletins.Clear();
        SelectedFilePath = string.Empty;
        
        SetStatus("Data cleared");
        Logger.LogInformation("All data cleared");
    }
    
    /// <summary>
    /// Add sample data for demonstration
    /// </summary>
    private async Task AddSampleDataAsync()
    {
        await Task.Delay(500);
        
        for (int i = 1; i <= 3; i++)
        {
            var document = new Document
            {
                FileName = $"sample-document-{i}.pdf",
                FilePath = $"/samples/sample-document-{i}.pdf",
                FileSize = 1024 * (100 + i * 50),
                ProcessingStartTime = DateTime.UtcNow.AddMinutes(-i),
                ProcessingEndTime = DateTime.UtcNow.AddMinutes(-i + 1),
                Status = DocumentStatus.Completed,
                IsScanned = true,
                PageCount = 10 + i * 2,
                TlpDesignation = TlpDesignation.Green,
                Creator = $"Uno Platform Engine #{i}"
            };
            
            Documents.Add(document);
        }
        
        var bulletins = new[]
        {
            new ThreatBulletin
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Ransomware Campaign Alert",
                Body = "New ransomware variant targeting healthcare organizations",
                Indicators = new() { "ransom.exe", "198.51.100.42" },
                Status = BulletinStatus.Published,
                Tlp = TlpDesignation.Amber,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                UpdatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new ThreatBulletin
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Phishing Campaign Update",
                Body = "Ongoing phishing campaign targeting financial institutions",
                Indicators = new() { "phish.html", "203.0.113.10", "fake-bank.com" },
                Status = BulletinStatus.Draft,
                Tlp = TlpDesignation.Red,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-3)
            }
        };
        
        foreach (var bulletin in bulletins)
        {
            ThreatBulletins.Add(bulletin);
        }
        
        SetStatus("Sample data added successfully");
        Logger.LogInformation("Sample data added: {DocumentCount} documents, {BulletinCount} bulletins", 
            Documents.Count, ThreatBulletins.Count);
    }
    
    /// <summary>
    /// Load initial sample data
    /// </summary>
    private async void LoadSampleDataAsync()
    {
        await Task.Delay(1000); // Simulate initial load
        await AddSampleDataAsync();
        SetStatus("Welcome to Anomali Import Tool - Cross Platform Edition!");
    }
    
    /// <summary>
    /// Get platform information for display
    /// </summary>
    private string GetPlatformInfo()
    {
        var platform = Environment.OSVersion.Platform.ToString();
        var runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        return $"{platform} | {runtime}";
    }
    
    #endregion
    
    #region BaseViewModel Overrides
    
    /// <summary>
    /// Refresh data - reload sample data
    /// </summary>
    protected override async Task RefreshAsync()
    {
        await ClearDataAsync();
        await AddSampleDataAsync();
        SetStatus("Data refreshed");
    }
    
    #endregion
} 
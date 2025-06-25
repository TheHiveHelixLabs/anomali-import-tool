using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AnomaliImportTool.Core.Models;
using System.Threading.Tasks;
using System.Windows.Input;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.UI.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.UI.DependencyInjection;
using System.IO;
using System.Linq;

namespace AnomaliImportTool.WPF.ViewModels;

/// <summary>
/// ViewModel for Template Selection - provides automatic template suggestion via TemplateMatchingService
/// and manual override by user selection.
/// </summary>
public class TemplateSelectionViewModel : INotifyPropertyChanged
{
    #region Fields / Services

    private readonly IImportTemplateService _templateService;
    private readonly ITemplateMatchingService _matchingService;
    private readonly ILogger<TemplateSelectionViewModel> _logger;

    private string _searchText = string.Empty;
    private ImportTemplate? _selectedTemplate;
    private bool _isLoading;
    private string _statusMessage = string.Empty;
    private string? _documentPath;

    #endregion

    #region Constructor

    // Primary constructor for DI
    public TemplateSelectionViewModel(IImportTemplateService templateService,
                                       ITemplateMatchingService matchingService,
                                       ILogger<TemplateSelectionViewModel> logger)
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _matchingService = matchingService ?? throw new ArgumentNullException(nameof(matchingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Templates = new ObservableCollection<ImportTemplate>();
        SearchCommand = new RelayCommand(async _ => await SearchAsync());
        RefreshCommand = new RelayCommand(async _ => await LoadTemplatesAsync());
        CreateCommand = new RelayCommand(_ => OnCreateTemplate());
        EditCommand = new RelayCommand(_ => OnEditTemplate(), _ => SelectedTemplate != null);
    }

    // Parameterless constructor for XAML designer / fallback; resolves services via service provider.
    public TemplateSelectionViewModel() : this(
        ServiceCompositionRoot.BuildServiceProvider().GetRequiredService<IImportTemplateService>(),
        ServiceCompositionRoot.BuildServiceProvider().GetRequiredService<ITemplateMatchingService>(),
        ServiceCompositionRoot.BuildServiceProvider().GetRequiredService<ILogger<TemplateSelectionViewModel>>())
    {
    }

    #endregion

    #region Properties

    public ObservableCollection<ImportTemplate> Templates { get; }

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); }
    }

    public ImportTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set { _selectedTemplate = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Path of the document used for automatic template suggestion.
    /// If set, SuggestTemplatesAsync will be triggered.
    /// </summary>
    public string? DocumentPath
    {
        get => _documentPath;
        set { _documentPath = value; OnPropertyChanged(); _ = SuggestTemplatesAsync(); }
    }

    #endregion

    #region Commands

    public ICommand SearchCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand EditCommand { get; }

    #endregion

    #region Public API

    /// <summary>
    /// Load templates initially (e.g., on view loaded).
    /// </summary>
    public async Task InitializeAsync()
    {
        await LoadTemplatesAsync();
        if (!string.IsNullOrEmpty(DocumentPath))
        {
            await SuggestTemplatesAsync();
        }
    }

    #endregion

    #region Private Helpers

    private async Task LoadTemplatesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading templates...";
            Templates.Clear();
            var templates = await _templateService.GetAllTemplatesAsync();
            foreach (var t in templates)
                Templates.Add(t);
            StatusMessage = $"Loaded {Templates.Count} templates.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading templates");
            StatusMessage = $"Error loading templates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadTemplatesAsync();
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "Searching...";
            Templates.Clear();
            var criteria = new TemplateSearchCriteria { SearchTerm = SearchText };
            var results = await _templateService.SearchTemplatesAsync(criteria);
            foreach (var t in results)
                Templates.Add(t);
            StatusMessage = $"Found {Templates.Count} templates.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching templates");
            StatusMessage = $"Error searching templates: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SuggestTemplatesAsync()
    {
        if (string.IsNullOrEmpty(DocumentPath) || !File.Exists(DocumentPath))
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Suggesting templates...";

            var bestMatch = await _matchingService.FindBestMatchAsync(DocumentPath!);
            if (bestMatch != null)
            {
                // Ensure template list contains best match
                if (!Templates.Any(t => t.Id == bestMatch.Template.Id))
                {
                    Templates.Insert(0, bestMatch.Template);
                }
                SelectedTemplate = Templates.FirstOrDefault(t => t.Id == bestMatch.Template.Id);
                StatusMessage = $"Suggested template: {bestMatch.Template.Name} (Confidence {bestMatch.ConfidenceScore:F2})";
            }
            else
            {
                StatusMessage = "No suitable template suggestion found.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting template");
            StatusMessage = $"Error suggesting template: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnCreateTemplate()
    {
        // TODO: Navigate to template creation view or open dialog
        StatusMessage = "Create template functionality not implemented yet.";
    }

    private void OnEditTemplate()
    {
        if (SelectedTemplate == null)
            return;
        // TODO: Navigate to template edit view / dialog
        StatusMessage = $"Edit template '{SelectedTemplate.Name}' not implemented yet.";
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #endregion
} 
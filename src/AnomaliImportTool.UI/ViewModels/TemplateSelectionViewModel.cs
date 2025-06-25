using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.WPF.ViewModels;

/// <summary>
/// Stub ViewModel for Template Selection. Smart matching and full logic will be implemented in Task 4.2.
/// </summary>
public class TemplateSelectionViewModel : INotifyPropertyChanged
{
    private string _searchText = string.Empty;
    private ImportTemplate? _selectedTemplate;

    public ObservableCollection<ImportTemplate> Templates { get; } = new();

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

    // Placeholder commands (will be replaced with ReactiveCommand in 4.2)
    public System.Windows.Input.ICommand? SearchCommand { get; } = null;
    public System.Windows.Input.ICommand? RefreshCommand { get; } = null;
    public System.Windows.Input.ICommand? CreateCommand { get; } = null;
    public System.Windows.Input.ICommand? EditCommand { get; } = null;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
} 
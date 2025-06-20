using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.WPF.Commands;

namespace AnomaliImportTool.WPF.ViewModels;

public class TemplateCreationViewModel : INotifyPropertyChanged
{
    private BitmapImage? _previewImage;
    private TemplateField? _selectedField;

    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        set { _previewImage = value; OnPropertyChanged(); }
    }

    public ObservableCollection<TemplateField> Fields { get; } = new();

    public ObservableCollection<ExtractionZone> Zones { get; } = new();

    public ObservableCollection<FieldExtractionPreview> PreviewResults { get; } = new();

    public TemplateField? SelectedField
    {
        get => _selectedField;
        set { _selectedField = value; OnPropertyChanged(); }
    }

    public ICommand AddFieldCommand { get; }
    public ICommand RemoveFieldCommand { get; }
    public ICommand SaveTemplateCommand { get; }

    public TemplateCreationViewModel()
    {
        AddFieldCommand = new RelayCommand(_ => AddField());
        RemoveFieldCommand = new RelayCommand(_ => RemoveField(), _ => SelectedField != null);
        SaveTemplateCommand = new RelayCommand(_ => SaveTemplate());
        // Populate PreviewResults with placeholder values
        PreviewResults.Add(new FieldExtractionPreview { FieldName = "SampleField", Value = "Value", Confidence = 0.85 });
    }

    private void AddField()
    {
        var newField = new TemplateField { Name = $"Field{Fields.Count + 1}", FieldType = TemplateFieldType.Text };
        Fields.Add(newField);
    }

    private void RemoveField()
    {
        if (SelectedField != null)
        {
            Fields.Remove(SelectedField);
            SelectedField = null;
        }
    }

    private void SaveTemplate()
    {
        // Placeholder for save logic (will connect to service later)
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
} 
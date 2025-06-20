using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.WPF.Commands;
using System.Linq;
using Microsoft.Win32;

namespace AnomaliImportTool.WPF.ViewModels;

public class TemplateCreationViewModel : INotifyPropertyChanged
{
    private BitmapImage? _previewImage;
    private TemplateField? _selectedField;
    private string _templateName = string.Empty;

    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        set { _previewImage = value; OnPropertyChanged(); }
    }

    public ObservableCollection<TemplateField> Fields { get; } = new();

    public ObservableCollection<ExtractionZone> Zones { get; } = new();

    public ObservableCollection<FieldExtractionPreview> PreviewResults { get; } = new();

    public ObservableCollection<string> ValidationErrors { get; } = new();

    public TemplateField? SelectedField
    {
        get => _selectedField;
        set { _selectedField = value; OnPropertyChanged(); }
    }

    public string TemplateName
    {
        get => _templateName;
        set { _templateName = value; OnPropertyChanged(); }
    }

    public ICommand AddFieldCommand { get; }
    public ICommand RemoveFieldCommand { get; }
    public ICommand SaveTemplateCommand { get; }
    public ICommand ValidateTemplateCommand { get; }
    public ICommand TestTemplateCommand { get; }

    public TemplateCreationViewModel()
    {
        AddFieldCommand = new RelayCommand(_ => AddField());
        RemoveFieldCommand = new RelayCommand(_ => RemoveField(), _ => SelectedField != null);
        SaveTemplateCommand = new RelayCommand(_ => SaveTemplate());
        ValidateTemplateCommand = new RelayCommand(_ => ValidateTemplate());
        TestTemplateCommand = new RelayCommand(_ => TestTemplate());
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
        // For now, just validate and transform; integration with service later
        if (!ValidateTemplate())
            return;

        // Build ImportTemplate object
        var importTemplate = new ImportTemplate
        {
            Name = TemplateName,
            Fields = Fields.ToList(),
            SupportedFormats = new() { "pdf", "docx", "xlsx" }, // default placeholder
        };

        // TODO: Call template service to save
    }

    private bool ValidateTemplate()
    {
        ValidationErrors.Clear();
        var template = new ImportTemplate
        {
            Name = TemplateName,
            Fields = Fields.ToList(),
            SupportedFormats = new() { "pdf" }
        };

        var result = template.ValidateTemplate();
        if (!result.IsValid)
        {
            foreach (var error in result.Errors)
                ValidationErrors.Add(error);
        }
        return result.IsValid;
    }

    private void TestTemplate()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "Documents|*.pdf;*.docx;*.xlsx|All Files|*.*",
            Title = "Select sample document"
        };
        if (openDialog.ShowDialog() == true)
        {
            // Placeholder: simulate test result
            PreviewResults.Clear();
            var rnd = new Random();
            foreach (var field in Fields)
            {
                PreviewResults.Add(new FieldExtractionPreview
                {
                    FieldName = field.Name,
                    Value = "SampleValue",
                    Confidence = Math.Round(rnd.NextDouble(), 2)
                });
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
} 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnomaliImportTool.Tests.UI.Infrastructure;
using AnomaliImportTool.UI.ViewModels;
using AnomaliImportTool.Core.Models;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinUIEx;
using System.Threading;

namespace AnomaliImportTool.Tests.UI.Tests;

[TestClass]
public class TemplateCreationWorkflowTests : TestBase
{
    private TemplateCreationViewModel? _viewModel;
    private TemplateCreationView? _view;
    private const int UI_DELAY = 500; // milliseconds

    [TestInitialize]
    public async Task TestInitialize()
    {
        await base.InitializeAsync();
        
        // Navigate to template creation view
        await NavigateToTemplateCreationAsync();
        
        // Get the view and view model
        _view = GetCurrentView<TemplateCreationView>();
        _viewModel = _view?.DataContext as TemplateCreationViewModel;
        
        _view.Should().NotBeNull("Template creation view should be loaded");
        _viewModel.Should().NotBeNull("Template creation view model should be available");
    }

    [TestCleanup]
    public async Task TestCleanup()
    {
        await base.CleanupAsync();
    }

    [TestMethod]
    public async Task TemplateCreation_BasicWorkflow_ShouldCreateTemplateSuccessfully()
    {
        // Arrange - Set up basic template information
        var templateName = "Test Security Template";
        var templateDescription = "Automated test template for security reports";
        var templateCategory = "Security";

        // Act - Fill in basic template information
        await SetTemplateBasicInfoAsync(templateName, templateDescription, templateCategory);
        
        // Add supported formats
        await AddSupportedFormatAsync("pdf");
        await AddSupportedFormatAsync("docx");
        
        // Add a simple text field
        await AddTextFieldAsync("ThreatLevel", "Threat Level", isRequired: true);
        
        // Add field extraction rule
        await AddExtractionRuleAsync("ThreatLevel", ExtractionRuleType.RegexPattern, @"Threat Level:\s*(\w+)");
        
        // Save the template
        await SaveTemplateAsync();
        
        // Assert - Verify template was created successfully
        _viewModel!.IsTemplateValid.Should().BeTrue("Template should be valid after adding required fields");
        _viewModel.CurrentTemplate.Name.Should().Be(templateName);
        _viewModel.CurrentTemplate.Description.Should().Be(templateDescription);
        _viewModel.CurrentTemplate.Category.Should().Be(templateCategory);
        _viewModel.CurrentTemplate.Fields.Should().HaveCount(1);
        _viewModel.IsSaved.Should().BeTrue("Template should be marked as saved");
    }

    [TestMethod]
    public async Task TemplateCreation_ComplexWorkflow_ShouldCreateComplexTemplateWithMultipleFields()
    {
        // Arrange
        var templateName = "Complex Security Analysis Template";
        var templateDescription = "Comprehensive template for security analysis reports";

        // Act - Create complex template
        await SetTemplateBasicInfoAsync(templateName, templateDescription, "Security");
        
        // Add multiple supported formats
        await AddSupportedFormatAsync("pdf");
        await AddSupportedFormatAsync("docx");
        await AddSupportedFormatAsync("xlsx");
        
        // Add multiple fields with different types
        await AddTextFieldAsync("ReportTitle", "Report Title", isRequired: true);
        await AddDateFieldAsync("AnalysisDate", "Analysis Date", isRequired: true);
        await AddDropdownFieldAsync("ThreatLevel", "Threat Level", 
            new[] { "LOW", "MEDIUM", "HIGH", "CRITICAL" }, defaultValue: "MEDIUM");
        await AddNumberFieldAsync("SeverityScore", "Severity Score (1-10)", 
            minValue: 1, maxValue: 10);
        await AddEmailFieldAsync("AnalystEmail", "Analyst Email", isRequired: true);
        
        // Add extraction rules for each field
        await AddExtractionRuleAsync("ReportTitle", ExtractionRuleType.RegexPattern, @"Title:\s*(.+)");
        await AddExtractionRuleAsync("AnalysisDate", ExtractionRuleType.RegexPattern, @"Date:\s*(\d{4}-\d{2}-\d{2})");
        await AddExtractionRuleAsync("ThreatLevel", ExtractionRuleType.RegexPattern, @"Threat Level:\s*(\w+)");
        await AddExtractionRuleAsync("SeverityScore", ExtractionRuleType.RegexPattern, @"Severity:\s*(\d+)");
        await AddExtractionRuleAsync("AnalystEmail", ExtractionRuleType.RegexPattern, @"Analyst:\s*([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})");
        
        // Add validation rules
        await AddValidationRuleAsync("SeverityScore", ValidationRuleType.Range, minValue: 1, maxValue: 10);
        await AddValidationRuleAsync("AnalystEmail", ValidationRuleType.RegexPattern, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
        
        // Save template
        await SaveTemplateAsync();
        
        // Assert
        _viewModel!.CurrentTemplate.Fields.Should().HaveCount(5);
        _viewModel.CurrentTemplate.SupportedFormats.Should().HaveCount(3);
        _viewModel.IsTemplateValid.Should().BeTrue();
        _viewModel.IsSaved.Should().BeTrue();
        
        // Verify field types are correct
        var titleField = _viewModel.CurrentTemplate.Fields.First(f => f.Name == "ReportTitle");
        titleField.FieldType.Should().Be(FieldType.Text);
        titleField.IsRequired.Should().BeTrue();
        
        var dateField = _viewModel.CurrentTemplate.Fields.First(f => f.Name == "AnalysisDate");
        dateField.FieldType.Should().Be(FieldType.Date);
        
        var dropdownField = _viewModel.CurrentTemplate.Fields.First(f => f.Name == "ThreatLevel");
        dropdownField.FieldType.Should().Be(FieldType.DropdownList);
        dropdownField.DefaultValue.Should().Be("MEDIUM");
    }

    [TestMethod]
    public async Task TemplateCreation_ExtractionZoneWorkflow_ShouldCreateZoneBasedExtractionSuccessfully()
    {
        // Arrange
        var testDocument = CreateTestDocument("Sample document for zone testing");
        
        // Act - Create template with extraction zones
        await SetTemplateBasicInfoAsync("Zone-Based Template", "Template with extraction zones", "Testing");
        await AddSupportedFormatAsync("pdf");
        
        // Add field with extraction zone
        await AddTextFieldAsync("DocumentTitle", "Document Title", isRequired: true);
        
        // Load test document for zone selection
        await LoadDocumentForZoneSelectionAsync(testDocument);
        
        // Select extraction zone for the field
        await SelectExtractionZoneAsync("DocumentTitle", x: 50, y: 100, width: 400, height: 30);
        
        // Add another field with different zone
        await AddTextFieldAsync("DocumentContent", "Document Content", isRequired: false);
        await SelectExtractionZoneAsync("DocumentContent", x: 50, y: 150, width: 500, height: 200);
        
        // Save template
        await SaveTemplateAsync();
        
        // Assert
        var titleField = _viewModel!.CurrentTemplate.Fields.First(f => f.Name == "DocumentTitle");
        titleField.ExtractionZones.Should().HaveCount(1);
        titleField.ExtractionZones[0].X.Should().Be(50);
        titleField.ExtractionZones[0].Y.Should().Be(100);
        titleField.ExtractionZones[0].Width.Should().Be(400);
        titleField.ExtractionZones[0].Height.Should().Be(30);
        
        var contentField = _viewModel.CurrentTemplate.Fields.First(f => f.Name == "DocumentContent");
        contentField.ExtractionZones.Should().HaveCount(1);
    }

    [TestMethod]
    public async Task TemplateCreation_TemplateTestingWorkflow_ShouldTestTemplateAgainstDocument()
    {
        // Arrange
        var testDocument = CreateTestDocumentWithKnownContent();
        
        // Act - Create template and test it
        await SetTemplateBasicInfoAsync("Testing Template", "Template for testing extraction", "Testing");
        await AddSupportedFormatAsync("pdf");
        
        // Add fields with extraction rules that match the test document
        await AddTextFieldAsync("ThreatLevel", "Threat Level", isRequired: true);
        await AddExtractionRuleAsync("ThreatLevel", ExtractionRuleType.RegexPattern, @"Threat Level:\s*(\w+)");
        
        await AddTextFieldAsync("Analyst", "Analyst Name", isRequired: true);
        await AddExtractionRuleAsync("Analyst", ExtractionRuleType.RegexPattern, @"Analyst:\s*([A-Za-z\s]+)");
        
        // Test template against document
        await TestTemplateAsync(testDocument);
        
        // Assert - Verify test results
        _viewModel!.TestResults.Should().NotBeNull("Test results should be available");
        _viewModel.TestResults!.IsSuccessful.Should().BeTrue("Template test should be successful");
        _viewModel.TestResults.ExtractedFields.Should().ContainKey("ThreatLevel");
        _viewModel.TestResults.ExtractedFields.Should().ContainKey("Analyst");
        _viewModel.TestResults.ExtractedFields["ThreatLevel"].Should().Be("HIGH");
        _viewModel.TestResults.ExtractedFields["Analyst"].Should().Be("John Smith");
        _viewModel.TestResults.OverallConfidence.Should().BeGreaterThan(0.7);
    }

    [TestMethod]
    public async Task TemplateCreation_FieldValidation_ShouldPreventInvalidFieldConfigurations()
    {
        // Arrange & Act - Try to create invalid field configurations
        await SetTemplateBasicInfoAsync("Validation Test Template", "Testing validation", "Testing");
        
        // Test 1: Try to add field without name
        await AddTextFieldAsync("", "Description", isRequired: true);
        
        // Assert - Should show validation error
        _viewModel!.HasValidationErrors.Should().BeTrue("Should have validation errors for empty field name");
        _viewModel.ValidationErrors.Should().Contain(e => e.Contains("Field name is required"));
        
        // Test 2: Try to add duplicate field name
        await AddTextFieldAsync("TestField", "First Field", isRequired: true);
        await AddTextFieldAsync("TestField", "Duplicate Field", isRequired: false);
        
        // Assert - Should show duplicate name error
        _viewModel.ValidationErrors.Should().Contain(e => e.Contains("duplicate") || e.Contains("already exists"));
        
        // Test 3: Try to save template without required information
        await ClearTemplateBasicInfoAsync();
        await SaveTemplateAsync();
        
        // Assert - Should prevent saving
        _viewModel.IsSaved.Should().BeFalse("Template should not be saved without required information");
        _viewModel.HasValidationErrors.Should().BeTrue("Should have validation errors for missing required information");
    }

    [TestMethod]
    public async Task TemplateCreation_ExportImportWorkflow_ShouldExportAndImportTemplateSuccessfully()
    {
        // Arrange - Create a complete template
        await SetTemplateBasicInfoAsync("Export Test Template", "Template for export testing", "Testing");
        await AddSupportedFormatAsync("pdf");
        await AddTextFieldAsync("Title", "Document Title", isRequired: true);
        await AddExtractionRuleAsync("Title", ExtractionRuleType.RegexPattern, @"Title:\s*(.+)");
        await SaveTemplateAsync();
        
        var originalTemplate = _viewModel!.CurrentTemplate;
        
        // Act - Export template
        var exportedJson = await ExportTemplateAsync();
        
        // Clear current template
        await CreateNewTemplateAsync();
        
        // Import template from JSON
        await ImportTemplateAsync(exportedJson);
        
        // Assert - Verify imported template matches original
        var importedTemplate = _viewModel.CurrentTemplate;
        importedTemplate.Name.Should().Be(originalTemplate.Name);
        importedTemplate.Description.Should().Be(originalTemplate.Description);
        importedTemplate.Category.Should().Be(originalTemplate.Category);
        importedTemplate.SupportedFormats.Should().BeEquivalentTo(originalTemplate.SupportedFormats);
        importedTemplate.Fields.Should().HaveCount(originalTemplate.Fields.Count);
        
        var importedField = importedTemplate.Fields.First();
        var originalField = originalTemplate.Fields.First();
        importedField.Name.Should().Be(originalField.Name);
        importedField.DisplayName.Should().Be(originalField.DisplayName);
        importedField.FieldType.Should().Be(originalField.FieldType);
        importedField.IsRequired.Should().Be(originalField.IsRequired);
    }

    [TestMethod]
    public async Task TemplateCreation_PreviewMode_ShouldShowLiveExtractionPreview()
    {
        // Arrange
        var testDocument = CreateTestDocumentWithKnownContent();
        
        // Act - Create template with preview
        await SetTemplateBasicInfoAsync("Preview Template", "Template with live preview", "Testing");
        await AddSupportedFormatAsync("pdf");
        await LoadDocumentForPreviewAsync(testDocument);
        
        // Add field and watch preview update
        await AddTextFieldAsync("ThreatLevel", "Threat Level", isRequired: true);
        await AddExtractionRuleAsync("ThreatLevel", ExtractionRuleType.RegexPattern, @"Threat Level:\s*(\w+)");
        
        // Assert - Preview should update automatically
        await WaitForPreviewUpdateAsync();
        _viewModel!.PreviewResults.Should().NotBeNull("Preview results should be available");
        _viewModel.PreviewResults!.ExtractedFields.Should().ContainKey("ThreatLevel");
        _viewModel.PreviewResults.ExtractedFields["ThreatLevel"].Should().Be("HIGH");
        
        // Modify extraction rule and verify preview updates
        await ModifyExtractionRuleAsync("ThreatLevel", @"Level:\s*(\w+)");
        await WaitForPreviewUpdateAsync();
        
        // Preview should update with new rule
        _viewModel.PreviewResults.ExtractedFields["ThreatLevel"].Should().Be("HIGH");
    }

    [TestMethod]
    public async Task TemplateCreation_AccessibilityCompliance_ShouldSupportKeyboardNavigationAndScreenReaders()
    {
        // Arrange - Set up accessibility testing
        await EnableAccessibilityTestingAsync();
        
        // Act & Assert - Test keyboard navigation
        await TestKeyboardNavigationAsync();
        
        // Test tab order
        var tabOrder = await GetTabOrderAsync();
        tabOrder.Should().Contain("TemplateNameTextBox");
        tabOrder.Should().Contain("TemplateDescriptionTextBox");
        tabOrder.Should().Contain("CategoryComboBox");
        tabOrder.Should().Contain("AddFieldButton");
        tabOrder.Should().Contain("SaveTemplateButton");
        
        // Test screen reader support
        await TestScreenReaderSupportAsync();
        
        // Verify all interactive elements have accessible names
        var accessibleElements = await GetAccessibleElementsAsync();
        accessibleElements.Should().AllSatisfy(element => 
        {
            element.AutomationProperties.GetName(element).Should().NotBeNullOrEmpty(
                $"Element {element.GetType().Name} should have accessible name");
        });
        
        // Test high contrast support
        await TestHighContrastSupportAsync();
    }

    [TestMethod]
    public async Task TemplateCreation_ErrorHandling_ShouldHandleErrorsGracefully()
    {
        // Test 1: Handle invalid document loading
        await SetTemplateBasicInfoAsync("Error Test Template", "Testing error handling", "Testing");
        
        var invalidDocumentPath = Path.Combine(Path.GetTempPath(), "nonexistent.pdf");
        await LoadDocumentForZoneSelectionAsync(invalidDocumentPath);
        
        // Should show error message without crashing
        _viewModel!.HasErrors.Should().BeTrue("Should have errors for invalid document");
        _viewModel.ErrorMessages.Should().Contain(m => m.Contains("document") || m.Contains("file"));
        
        // Test 2: Handle invalid extraction rules
        await AddTextFieldAsync("TestField", "Test Field", isRequired: true);
        await AddExtractionRuleAsync("TestField", ExtractionRuleType.RegexPattern, "[invalid regex(");
        
        // Should show validation error for invalid regex
        _viewModel.HasValidationErrors.Should().BeTrue("Should have validation errors for invalid regex");
        
        // Test 3: Handle save failures gracefully
        await SetTemplateSaveErrorSimulationAsync(true);
        await SaveTemplateAsync();
        
        // Should show error message and not mark as saved
        _viewModel.IsSaved.Should().BeFalse("Template should not be marked as saved after error");
        _viewModel.HasErrors.Should().BeTrue("Should show save error message");
    }

    #region Helper Methods

    private async Task NavigateToTemplateCreationAsync()
    {
        // Navigate to template creation view
        var navigationService = GetService<INavigationService>();
        await navigationService.NavigateToAsync<TemplateCreationView>();
        await Task.Delay(UI_DELAY);
    }

    private async Task SetTemplateBasicInfoAsync(string name, string description, string category)
    {
        await RunOnUIThreadAsync(() =>
        {
            _viewModel!.CurrentTemplate.Name = name;
            _viewModel.CurrentTemplate.Description = description;
            _viewModel.CurrentTemplate.Category = category;
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task ClearTemplateBasicInfoAsync()
    {
        await RunOnUIThreadAsync(() =>
        {
            _viewModel!.CurrentTemplate.Name = string.Empty;
            _viewModel.CurrentTemplate.Description = string.Empty;
            _viewModel.CurrentTemplate.Category = string.Empty;
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task AddSupportedFormatAsync(string format)
    {
        await RunOnUIThreadAsync(() =>
        {
            if (!_viewModel!.CurrentTemplate.SupportedFormats.Contains(format))
            {
                _viewModel.CurrentTemplate.SupportedFormats.Add(format);
            }
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task AddTextFieldAsync(string name, string displayName, bool isRequired)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = new TemplateField
            {
                Id = Guid.NewGuid(),
                Name = name,
                DisplayName = displayName,
                FieldType = FieldType.Text,
                IsRequired = isRequired,
                ExtractionRules = new List<ExtractionRule>(),
                ValidationRules = new List<ValidationRule>(),
                ExtractionZones = new List<ExtractionZone>()
            };
            _viewModel!.CurrentTemplate.Fields.Add(field);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task AddDateFieldAsync(string name, string displayName, bool isRequired)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = new TemplateField
            {
                Id = Guid.NewGuid(),
                Name = name,
                DisplayName = displayName,
                FieldType = FieldType.Date,
                IsRequired = isRequired,
                ExtractionRules = new List<ExtractionRule>(),
                ValidationRules = new List<ValidationRule>(),
                ExtractionZones = new List<ExtractionZone>()
            };
            _viewModel!.CurrentTemplate.Fields.Add(field);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task AddDropdownFieldAsync(string name, string displayName, string[] options, string defaultValue)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = new TemplateField
            {
                Id = Guid.NewGuid(),
                Name = name,
                DisplayName = displayName,
                FieldType = FieldType.DropdownList,
                DefaultValue = defaultValue,
                ValidationRules = new List<ValidationRule>(),
                ExtractionRules = new List<ExtractionRule>(),
                ExtractionZones = new List<ExtractionZone>()
            };
            _viewModel!.CurrentTemplate.Fields.Add(field);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task AddNumberFieldAsync(string name, string displayName, int minValue, int maxValue)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = new TemplateField
            {
                Id = Guid.NewGuid(),
                Name = name,
                DisplayName = displayName,
                FieldType = FieldType.Number,
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule
                    {
                        RuleType = ValidationRuleType.Range,
                        MinValue = minValue,
                        MaxValue = maxValue
                    }
                },
                ExtractionRules = new List<ExtractionRule>(),
                ExtractionZones = new List<ExtractionZone>()
            };
            _viewModel!.CurrentTemplate.Fields.Add(field);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task AddEmailFieldAsync(string name, string displayName, bool isRequired)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = new TemplateField
            {
                Id = Guid.NewGuid(),
                Name = name,
                DisplayName = displayName,
                FieldType = FieldType.Email,
                IsRequired = isRequired,
                ValidationRules = new List<ValidationRule>
                {
                    new ValidationRule
                    {
                        RuleType = ValidationRuleType.RegexPattern,
                        Pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"
                    }
                },
                ExtractionRules = new List<ExtractionRule>(),
                ExtractionZones = new List<ExtractionZone>()
            };
            _viewModel!.CurrentTemplate.Fields.Add(field);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task AddExtractionRuleAsync(string fieldName, ExtractionRuleType ruleType, string pattern)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = _viewModel!.CurrentTemplate.Fields.First(f => f.Name == fieldName);
            var rule = new ExtractionRule
            {
                RuleType = ruleType,
                Pattern = pattern,
                CaptureGroup = 1
            };
            field.ExtractionRules.Add(rule);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task AddValidationRuleAsync(string fieldName, ValidationRuleType ruleType, string pattern = null, int? minValue = null, int? maxValue = null)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = _viewModel!.CurrentTemplate.Fields.First(f => f.Name == fieldName);
            var rule = new ValidationRule
            {
                RuleType = ruleType,
                Pattern = pattern,
                MinValue = minValue,
                MaxValue = maxValue
            };
            field.ValidationRules.Add(rule);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task SelectExtractionZoneAsync(string fieldName, int x, int y, int width, int height)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = _viewModel!.CurrentTemplate.Fields.First(f => f.Name == fieldName);
            var zone = new ExtractionZone
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                PageNumber = 1
            };
            field.ExtractionZones.Add(zone);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task SaveTemplateAsync()
    {
        await RunOnUIThreadAsync(async () =>
        {
            if (_viewModel!.SaveTemplateCommand.CanExecute(null))
            {
                _viewModel.SaveTemplateCommand.Execute(null);
            }
        });
        await Task.Delay(UI_DELAY * 2); // Wait longer for save operation
    }

    private async Task LoadDocumentForZoneSelectionAsync(string documentPath)
    {
        await RunOnUIThreadAsync(() =>
        {
            _viewModel!.LoadDocumentForZoneSelection(documentPath);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task LoadDocumentForPreviewAsync(string documentPath)
    {
        await RunOnUIThreadAsync(() =>
        {
            _viewModel!.LoadDocumentForPreview(documentPath);
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task TestTemplateAsync(string testDocumentPath)
    {
        await RunOnUIThreadAsync(async () =>
        {
            if (_viewModel!.TestTemplateCommand.CanExecute(testDocumentPath))
            {
                _viewModel.TestTemplateCommand.Execute(testDocumentPath);
            }
        });
        await Task.Delay(UI_DELAY * 2);
    }

    private async Task<string> ExportTemplateAsync()
    {
        string exportedJson = null;
        await RunOnUIThreadAsync(() =>
        {
            if (_viewModel!.ExportTemplateCommand.CanExecute(null))
            {
                _viewModel.ExportTemplateCommand.Execute(null);
                exportedJson = _viewModel.ExportedTemplateJson;
            }
        });
        await Task.Delay(UI_DELAY);
        return exportedJson;
    }

    private async Task ImportTemplateAsync(string templateJson)
    {
        await RunOnUIThreadAsync(() =>
        {
            if (_viewModel!.ImportTemplateCommand.CanExecute(templateJson))
            {
                _viewModel.ImportTemplateCommand.Execute(templateJson);
            }
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task CreateNewTemplateAsync()
    {
        await RunOnUIThreadAsync(() =>
        {
            if (_viewModel!.NewTemplateCommand.CanExecute(null))
            {
                _viewModel.NewTemplateCommand.Execute(null);
            }
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task WaitForPreviewUpdateAsync()
    {
        await Task.Delay(UI_DELAY * 2); // Wait for preview to update
    }

    private async Task ModifyExtractionRuleAsync(string fieldName, string newPattern)
    {
        await RunOnUIThreadAsync(() =>
        {
            var field = _viewModel!.CurrentTemplate.Fields.First(f => f.Name == fieldName);
            var rule = field.ExtractionRules.First();
            rule.Pattern = newPattern;
        });
        await Task.Delay(UI_DELAY);
    }

    private async Task SetTemplateSaveErrorSimulationAsync(bool simulate)
    {
        await RunOnUIThreadAsync(() =>
        {
            _viewModel!.SimulateSaveError = simulate;
        });
    }

    private string CreateTestDocument(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    private string CreateTestDocumentWithKnownContent()
    {
        var content = @"Security Incident Report
        
        Threat Level: HIGH
        Analyst: John Smith
        Date: 2024-01-15
        
        Description: Advanced persistent threat detected with suspicious network activity.
        Immediate action required for containment.";
        
        return CreateTestDocument(content);
    }

    #endregion
} 
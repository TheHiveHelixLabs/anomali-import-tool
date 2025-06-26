using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AnomaliImportTool.Core.Models;

/// <summary>
/// Represents a field in an import template that defines how to extract specific data from documents
/// Supports various field types including username, ticket number, date, and custom fields
/// </summary>
public class TemplateField
{
    /// <summary>
    /// Unique identifier for the field
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Field name (used as key in extracted data)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the field in UI
    /// </summary>
    [StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Field description for documentation
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of field (determines validation and extraction behavior)
    /// </summary>
    public TemplateFieldType FieldType { get; set; } = TemplateFieldType.Text;

    /// <summary>
    /// Extraction method to use for this field
    /// </summary>
    public ExtractionMethod ExtractionMethod { get; set; } = ExtractionMethod.Text;

    /// <summary>
    /// Whether this field is required for successful template application
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Order in which fields should be processed (lower numbers first)
    /// </summary>
    public int ProcessingOrder { get; set; } = 0;

    /// <summary>
    /// Extraction zones that define where to look for this field's data
    /// </summary>
    public List<ExtractionZone> ExtractionZones { get; set; } = new();

    /// <summary>
    /// Text patterns (regex) to match for extraction
    /// </summary>
    public List<string> TextPatterns { get; set; } = new();

    /// <summary>
    /// Keywords that indicate the presence of this field
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Field-specific validation rules
    /// </summary>
    public FieldValidationRules ValidationRules { get; set; } = new();

    /// <summary>
    /// Data transformation rules to apply after extraction
    /// </summary>
    public DataTransformation Transformation { get; set; } = new();

    /// <summary>
    /// Fallback extraction options if primary method fails
    /// </summary>
    public FallbackOptions Fallback { get; set; } = new();

    /// <summary>
    /// Field-specific metadata and configuration
    /// </summary>
    public Dictionary<string, object> FieldMetadata { get; set; } = new();

    /// <summary>
    /// Default value to use if extraction fails (optional)
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether this field supports multiple values (e.g., multiple ticket numbers)
    /// </summary>
    public bool AllowMultipleValues { get; set; } = false;

    /// <summary>
    /// Separator to use when combining multiple values
    /// </summary>
    public string MultiValueSeparator { get; set; } = ";";

    /// <summary>
    /// Output format for this field's data
    /// </summary>
    public string OutputFormat { get; set; } = string.Empty;

    /// <summary>
    /// Whether this field supports multiple values (alias for backward compatibility)
    /// </summary>
    public bool SupportsMultipleValues 
    { 
        get => AllowMultipleValues; 
        set => AllowMultipleValues = value; 
    }

    /// <summary>
    /// Separator for multiple values (alias for backward compatibility)
    /// </summary>
    public string ValueSeparator 
    { 
        get => MultiValueSeparator; 
        set => MultiValueSeparator = value; 
    }

    /// <summary>
    /// Confidence threshold for field extraction (0.0 to 1.0)
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// Creates a copy of the template field
    /// </summary>
    /// <returns>New TemplateField instance</returns>
    public TemplateField CreateCopy()
    {
        return new TemplateField
        {
            Id = Guid.NewGuid(),
            Name = Name,
            DisplayName = DisplayName,
            Description = Description,
            FieldType = FieldType,
            ExtractionMethod = ExtractionMethod,
            IsRequired = IsRequired,
            ProcessingOrder = ProcessingOrder,
            ExtractionZones = ExtractionZones.Select(z => z.CreateCopy()).ToList(),
            TextPatterns = new List<string>(TextPatterns),
            Keywords = new List<string>(Keywords),
            ValidationRules = ValidationRules.CreateCopy(),
            Transformation = Transformation.CreateCopy(),
            Fallback = Fallback.CreateCopy(),
            FieldMetadata = new Dictionary<string, object>(FieldMetadata),
            DefaultValue = DefaultValue,
            AllowMultipleValues = AllowMultipleValues,
            MultiValueSeparator = MultiValueSeparator,
            OutputFormat = OutputFormat,
            ConfidenceThreshold = ConfidenceThreshold
        };
    }

    /// <summary>
    /// Validates the field configuration
    /// </summary>
    /// <returns>Validation result with any errors</returns>
    public TemplateValidationResult ValidateField()
    {
        var result = new TemplateValidationResult { IsValid = true };

        // Validate required properties
        if (string.IsNullOrWhiteSpace(Name))
        {
            result.IsValid = false;
            result.Errors.Add("Field name is required");
        }

        // Validate field name format (alphanumeric and underscores only)
        if (!string.IsNullOrWhiteSpace(Name) && !Regex.IsMatch(Name, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
        {
            result.IsValid = false;
            result.Errors.Add("Field name must start with a letter and contain only letters, numbers, and underscores");
        }

        // Validate extraction zones or patterns exist
        if (ExtractionZones.Count == 0 && TextPatterns.Count == 0 && Keywords.Count == 0)
        {
            result.IsValid = false;
            result.Errors.Add("Field must have at least one extraction zone, text pattern, or keyword");
        }

        // Validate text patterns are valid regex
        foreach (var pattern in TextPatterns)
        {
            try
            {
                Regex.IsMatch("test", pattern);
            }
            catch (ArgumentException)
            {
                result.IsValid = false;
                result.Errors.Add($"Invalid regex pattern: {pattern}");
            }
        }

        // Validate extraction zones
        foreach (var zone in ExtractionZones)
        {
            var zoneResult = zone.ValidateZone();
            if (!zoneResult.IsValid)
            {
                result.IsValid = false;
                foreach (var error in zoneResult.Errors)
                {
                    result.Errors.Add($"Extraction zone error: {error}");
                }
            }
        }

        // Field type specific validation
        ValidateFieldType(result);

        return result;
    }

    /// <summary>
    /// Creates a field configured for username extraction
    /// </summary>
    /// <param name="name">Field name</param>
    /// <param name="displayName">Display name</param>
    /// <returns>Configured username field</returns>
    public static TemplateField CreateUsernameField(string name = "document_author", string displayName = "Document Author")
    {
        return new TemplateField
        {
            Name = name,
            DisplayName = displayName,
            Description = "Extracts the username/author from the document",
            FieldType = TemplateFieldType.Username,
            ExtractionMethod = ExtractionMethod.Text,
            IsRequired = true,
            TextPatterns = new List<string>
            {
                @"(?i)author:?\s*([a-zA-Z]+\.?[a-zA-Z]+)",
                @"(?i)created\s+by:?\s*([a-zA-Z]+\.?[a-zA-Z]+)",
                @"(?i)submitted\s+by:?\s*([a-zA-Z]+\.?[a-zA-Z]+)",
                @"([a-zA-Z]+\.[a-zA-Z]+)@\w+\.\w+"
            },
            Keywords = new List<string> { "author", "created by", "submitted by", "requestor" },
            ValidationRules = new FieldValidationRules
            {
                MinLength = 2,
                MaxLength = 50,
                RegexPattern = @"^[a-zA-Z]+\.?[a-zA-Z]+$",
                CustomValidations = new List<string>
                {
                    "!string.IsNullOrEmpty(value)",
                    "!value.Contains(' ') || value.Split('.').Length == 2"
                }
            },
            Transformation = new DataTransformation
            {
                ToLowerCase = true,
                TrimWhitespace = true,
                RemoveSpecialCharacters = false
            }
        };
    }

    /// <summary>
    /// Creates a field configured for ticket number extraction
    /// </summary>
    /// <param name="name">Field name</param>
    /// <param name="displayName">Display name</param>
    /// <returns>Configured ticket number field</returns>
    public static TemplateField CreateTicketNumberField(string name = "ticket_number", string displayName = "Ticket Number")
    {
        return new TemplateField
        {
            Name = name,
            DisplayName = displayName,
            Description = "Extracts ticket/case/request numbers from the document",
            FieldType = TemplateFieldType.TicketNumber,
            ExtractionMethod = ExtractionMethod.Text,
            IsRequired = false,
            TextPatterns = new List<string>
            {
                @"(?i)ticket\s*#?:?\s*([A-Z]+-?\d+)",
                @"(?i)case\s*#?:?\s*([A-Z]+-?\d+)",
                @"(?i)request\s*#?:?\s*([A-Z]+-?\d+)",
                @"(?i)incident\s*#?:?\s*([A-Z]+-?\d+)",
                @"\b([A-Z]{2,5}-\d{3,8})\b",
                @"\b(INC\d{7,10})\b",
                @"\b(REQ\d{7,10})\b"
            },
            Keywords = new List<string> { "ticket", "case", "request", "incident", "reference" },
            ValidationRules = new FieldValidationRules
            {
                MinLength = 3,
                MaxLength = 20,
                RegexPattern = @"^[A-Z]+-?\d+$|^[A-Z]{3}\d{7,10}$",
                CustomValidations = new List<string>
                {
                    "value.Any(char.IsDigit)",
                    "value.Any(char.IsLetter)"
                }
            },
            Transformation = new DataTransformation
            {
                ToUpperCase = true,
                TrimWhitespace = true,
                RemoveSpecialCharacters = false
            },
            AllowMultipleValues = true,
            MultiValueSeparator = "; "
        };
    }

    /// <summary>
    /// Creates a field configured for date extraction
    /// </summary>
    /// <param name="name">Field name</param>
    /// <param name="displayName">Display name</param>
    /// <returns>Configured date field</returns>
    public static TemplateField CreateDateField(string name = "document_date", string displayName = "Document Date")
    {
        return new TemplateField
        {
            Name = name,
            DisplayName = displayName,
            Description = "Extracts dates from the document",
            FieldType = TemplateFieldType.Date,
            ExtractionMethod = ExtractionMethod.Text,
            IsRequired = false,
            TextPatterns = new List<string>
            {
                @"\b(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})\b",
                @"\b(\d{4}[/-]\d{1,2}[/-]\d{1,2})\b",
                @"\b((?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*\.?\s+\d{1,2},?\s+\d{4})\b",
                @"\b(\d{1,2}\s+(?:January|February|March|April|May|June|July|August|September|October|November|December)\s+\d{4})\b",
                @"(?i)date:?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})",
                @"(?i)created:?\s*(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})"
            },
            Keywords = new List<string> { "date", "created", "modified", "submitted", "approved" },
            ValidationRules = new FieldValidationRules
            {
                MinLength = 8,
                MaxLength = 20,
                CustomValidations = new List<string>
                {
                    "DateTime.TryParse(value, out _)"
                }
            },
            Transformation = new DataTransformation
            {
                TrimWhitespace = true,
                FormatAsDate = true,
                DateFormat = "yyyy-MM-dd"
            }
        };
    }

    /// <summary>
    /// Creates a custom field with basic configuration
    /// </summary>
    /// <param name="name">Field name</param>
    /// <param name="displayName">Display name</param>
    /// <param name="fieldType">Field type</param>
    /// <returns>Configured custom field</returns>
    public static TemplateField CreateCustomField(string name, string displayName, TemplateFieldType fieldType = TemplateFieldType.Custom)
    {
        return new TemplateField
        {
            Name = name,
            DisplayName = displayName,
            Description = $"Custom field: {displayName}",
            FieldType = fieldType,
            ExtractionMethod = ExtractionMethod.Text,
            IsRequired = false,
            ValidationRules = new FieldValidationRules
            {
                MinLength = 1,
                MaxLength = 500
            },
            Transformation = new DataTransformation
            {
                TrimWhitespace = true
            }
        };
    }

    /// <summary>
    /// Validates field type specific requirements
    /// </summary>
    private void ValidateFieldType(TemplateValidationResult result)
    {
        switch (FieldType)
        {
            case TemplateFieldType.Username:
                if (ValidationRules.RegexPattern == null)
                {
                    result.Warnings.Add("Username field should have a regex pattern for validation");
                }
                break;

            case TemplateFieldType.TicketNumber:
                if (TextPatterns.Count == 0)
                {
                    result.Warnings.Add("Ticket number field should have text patterns for extraction");
                }
                break;

            case TemplateFieldType.Date:
                if (!Transformation.FormatAsDate)
                {
                    result.Warnings.Add("Date field should enable date formatting in transformation");
                }
                break;

            case TemplateFieldType.Email:
                if (ValidationRules.RegexPattern == null)
                {
                    ValidationRules.RegexPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                }
                break;
        }
    }
}

/// <summary>
/// Types of template fields that can be extracted
/// </summary>
public enum TemplateFieldType
{
    /// <summary>
    /// Plain text field
    /// </summary>
    Text,

    /// <summary>
    /// Username/author field
    /// </summary>
    Username,

    /// <summary>
    /// Ticket/case number field
    /// </summary>
    TicketNumber,

    /// <summary>
    /// Date field
    /// </summary>
    Date,

    /// <summary>
    /// Email address field
    /// </summary>
    Email,

    /// <summary>
    /// Numeric field
    /// </summary>
    Number,

    /// <summary>
    /// Boolean/checkbox field
    /// </summary>
    Boolean,

    /// <summary>
    /// Custom field type
    /// </summary>
    Custom,

    /// <summary>
    /// Approval status field
    /// </summary>
    ApprovalStatus,

    /// <summary>
    /// Priority level field
    /// </summary>
    Priority,

    /// <summary>
    /// Category/classification field
    /// </summary>
    Category
}

/// <summary>
/// Methods for extracting field data from documents
/// </summary>
public enum ExtractionMethod
{
    /// <summary>
    /// Extract from document text using patterns/keywords
    /// </summary>
    Text,

    /// <summary>
    /// Extract from specific coordinate zones
    /// </summary>
    Coordinates,

    /// <summary>
    /// Extract using OCR from specific areas
    /// </summary>
    OCR,

    /// <summary>
    /// Extract from document metadata
    /// </summary>
    Metadata,

    /// <summary>
    /// Combine multiple extraction methods
    /// </summary>
    Hybrid
}

/// <summary>
/// Field validation rules
/// </summary>
public class FieldValidationRules
{
    /// <summary>
    /// Minimum allowed length
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Maximum allowed length
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Regular expression pattern for validation
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// Whether empty values are allowed
    /// </summary>
    public bool AllowEmpty { get; set; } = true;

    /// <summary>
    /// Custom validation expressions (C# code)
    /// </summary>
    public List<string> CustomValidations { get; set; } = new();

    /// <summary>
    /// Creates a copy of the validation rules
    /// </summary>
    public FieldValidationRules CreateCopy()
    {
        return new FieldValidationRules
        {
            MinLength = MinLength,
            MaxLength = MaxLength,
            RegexPattern = RegexPattern,
            AllowEmpty = AllowEmpty,
            CustomValidations = new List<string>(CustomValidations)
        };
    }
}

/// <summary>
/// Data transformation rules to apply after extraction
/// </summary>
public class DataTransformation
{
    /// <summary>
    /// Convert to lowercase
    /// </summary>
    public bool ToLowerCase { get; set; } = false;

    /// <summary>
    /// Convert to uppercase
    /// </summary>
    public bool ToUpperCase { get; set; } = false;

    /// <summary>
    /// Trim whitespace
    /// </summary>
    public bool TrimWhitespace { get; set; } = true;

    /// <summary>
    /// Remove special characters
    /// </summary>
    public bool RemoveSpecialCharacters { get; set; } = false;

    /// <summary>
    /// Format as date using specified format
    /// </summary>
    public bool FormatAsDate { get; set; } = false;

    /// <summary>
    /// Date format string (if FormatAsDate is true)
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Custom transformation expressions (C# code)
    /// </summary>
    public List<string> CustomTransformations { get; set; } = new();

    /// <summary>
    /// Creates a copy of the transformation rules
    /// </summary>
    public DataTransformation CreateCopy()
    {
        return new DataTransformation
        {
            ToLowerCase = ToLowerCase,
            ToUpperCase = ToUpperCase,
            TrimWhitespace = TrimWhitespace,
            RemoveSpecialCharacters = RemoveSpecialCharacters,
            FormatAsDate = FormatAsDate,
            DateFormat = DateFormat,
            CustomTransformations = new List<string>(CustomTransformations)
        };
    }
}

/// <summary>
/// Fallback options if primary extraction fails
/// </summary>
public class FallbackOptions
{
    /// <summary>
    /// Enable fallback extraction methods
    /// </summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Alternative extraction methods to try
    /// </summary>
    public List<ExtractionMethod> FallbackMethods { get; set; } = new();

    /// <summary>
    /// Alternative text patterns to try
    /// </summary>
    public List<string> FallbackPatterns { get; set; } = new();

    /// <summary>
    /// Whether to use machine learning for fallback extraction
    /// </summary>
    public bool UseMachineLearning { get; set; } = false;

    /// <summary>
    /// Creates a copy of the fallback options
    /// </summary>
    public FallbackOptions CreateCopy()
    {
        return new FallbackOptions
        {
            EnableFallback = EnableFallback,
            FallbackMethods = new List<ExtractionMethod>(FallbackMethods),
            FallbackPatterns = new List<string>(FallbackPatterns),
            UseMachineLearning = UseMachineLearning
        };
    }
} 
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

namespace AnomaliImportTool.Core.Domain.ValueObjects;

/// <summary>
/// Represents the result of detailed content extraction from a file
/// </summary>
public sealed record DetailedExtractionResult(
    string ExtractedText,
    string FileFormat,
    int PageCount,
    bool IsEncrypted,
    IReadOnlyList<string> Warnings,
    TimeSpan ProcessingTime)
{
    public bool HasWarnings => Warnings.Count > 0;
    public bool IsSuccessful => !string.IsNullOrWhiteSpace(ExtractedText);
}

/// <summary>
/// Represents structured data extracted from a file
/// </summary>
public sealed record StructuredDataResult(
    IReadOnlyDictionary<string, object> Data,
    IReadOnlyList<TableData> Tables,
    IReadOnlyList<FormField> FormFields,
    string Schema,
    bool IsValid)
{
    public bool HasTables => Tables.Count > 0;
    public bool HasFormFields => FormFields.Count > 0;
}

/// <summary>
/// Represents table data extracted from a document
/// </summary>
public sealed record TableData(
    string Name,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    int ColumnCount,
    int RowCount)
{
    public bool HasHeaders => Headers.Count > 0;
    public bool IsEmpty => RowCount == 0;
}

/// <summary>
/// Represents a form field extracted from a document
/// </summary>
public sealed record FormField(
    string Name,
    string Value,
    string Type,
    bool IsRequired,
    bool IsReadOnly)
{
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
}

/// <summary>
/// Represents file validation result
/// </summary>
public sealed record FileValidationResult(
    bool IsValid,
    string FileFormat,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings,
    TimeSpan ValidationTime)
{
    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;
    public bool IsSuccessful => IsValid && !HasErrors;
}

/// <summary>
/// Represents integrity validation result
/// </summary>
public sealed record IntegrityValidationResult(
    bool IsIntegrityValid,
    ContentHash ActualHash,
    ContentHash ExpectedHash,
    bool HashesMatch,
    string ValidationMethod)
{
    public bool IsSuccessful => IsIntegrityValid && HashesMatch;
}

/// <summary>
/// Represents security validation result
/// </summary>
public sealed record SecurityValidationResult(
    bool IsSafe,
    IReadOnlyList<SecurityThreat> Threats,
    string ScanEngine,
    DateTime ScanTimestamp,
    string ScanVersion)
{
    public bool HasThreats => Threats.Count > 0;
    public bool IsSuccessful => IsSafe && !HasThreats;
}

/// <summary>
/// Represents size validation result
/// </summary>
public sealed record SizeValidationResult(
    bool IsSizeValid,
    long ActualSize,
    long MaxAllowedSize,
    string SizeUnit)
{
    public bool ExceedsLimit => ActualSize > MaxAllowedSize;
    public double SizeRatio => MaxAllowedSize > 0 ? (double)ActualSize / MaxAllowedSize : 0;
}

/// <summary>
/// Represents accessibility validation result
/// </summary>
public sealed record AccessibilityValidationResult(
    bool IsAccessible,
    bool FileExists,
    bool HasReadPermission,
    bool HasWritePermission,
    string? ErrorMessage)
{
    public bool IsSuccessful => IsAccessible && FileExists && HasReadPermission;
}

/// <summary>
/// Represents extended metadata extracted from a file
/// </summary>
public sealed record ExtendedMetadata(
    IReadOnlyDictionary<string, object> Properties,
    string DocumentType,
    string? Author,
    string? Title,
    string? Subject,
    DateTime? CreatedDate,
    DateTime? ModifiedDate)
{
    public bool HasAuthor => !string.IsNullOrWhiteSpace(Author);
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    public bool HasProperties => Properties.Count > 0;
}

/// <summary>
/// Represents security-related metadata
/// </summary>
public sealed record SecurityMetadata(
    bool HasDigitalSignature,
    bool IsSignatureValid,
    IReadOnlyList<CertificateInfo> Certificates,
    IReadOnlyList<string> Permissions,
    bool IsEncrypted)
{
    public bool IsSigned => HasDigitalSignature;
    public bool IsSecure => HasDigitalSignature && IsSignatureValid;
}

/// <summary>
/// Represents certificate information
/// </summary>
public sealed record CertificateInfo(
    string Subject,
    string Issuer,
    DateTime ValidFrom,
    DateTime ValidTo,
    string Thumbprint,
    bool IsValid)
{
    public bool IsExpired => DateTime.UtcNow > ValidTo;
    public bool IsNotYetValid => DateTime.UtcNow < ValidFrom;
    public bool IsCurrentlyValid => IsValid && !IsExpired && !IsNotYetValid;
}

/// <summary>
/// Represents file processing options
/// </summary>
public sealed record FileProcessingOptions(
    bool ValidateFormat,
    bool ValidateSecurity,
    bool ExtractContent,
    bool ExtractMetadata,
    bool ExtractStructuredData,
    long MaxFileSizeBytes,
    TimeSpan Timeout,
    IReadOnlyList<string> AllowedExtensions)
{
    public static FileProcessingOptions Default => new(
        ValidateFormat: true,
        ValidateSecurity: true,
        ExtractContent: true,
        ExtractMetadata: true,
        ExtractStructuredData: false,
        MaxFileSizeBytes: 100 * 1024 * 1024, // 100 MB
        Timeout: TimeSpan.FromMinutes(5),
        AllowedExtensions: new[] { ".pdf", ".docx", ".xlsx", ".txt" });
}

/// <summary>
/// Represents complete file processing result
/// </summary>
public sealed record FileProcessingResult(
    FilePath FilePath,
    bool IsSuccessful,
    FileValidationResult? ValidationResult,
    DetailedExtractionResult? ContentResult,
    FileMetadata? Metadata,
    StructuredDataResult? StructuredData,
    IReadOnlyList<ProcessingError> Errors,
    TimeSpan TotalProcessingTime)
{
    public bool HasErrors => Errors.Count > 0;
    public bool HasContent => ContentResult?.IsSuccessful == true;
    public bool HasMetadata => Metadata is not null;
}

/// <summary>
/// Represents file processing progress
/// </summary>
public sealed record FileProcessingProgress(
    FilePath FilePath,
    string CurrentStep,
    int CompletedFiles,
    int TotalFiles,
    double PercentComplete,
    TimeSpan ElapsedTime,
    TimeSpan? EstimatedRemainingTime)
{
    public bool IsComplete => CompletedFiles >= TotalFiles;
    public string ProgressText => $"{CompletedFiles}/{TotalFiles} ({PercentComplete:F1}%)";
}

/// <summary>
/// Represents batch processing result
/// </summary>
public sealed record BatchProcessingResult(
    IReadOnlyList<FileProcessingResult> Results,
    int SuccessfulCount,
    int FailedCount,
    TimeSpan TotalProcessingTime,
    IReadOnlyList<ProcessingError> BatchErrors)
{
    public int TotalFiles => Results.Count;
    public double SuccessRate => TotalFiles > 0 ? (double)SuccessfulCount / TotalFiles : 0;
    public bool HasBatchErrors => BatchErrors.Count > 0;
}

/// <summary>
/// Represents configuration validation result
/// </summary>
public sealed record ConfigurationValidationResult(
    bool IsValid,
    IReadOnlyList<ConfigurationError> Errors,
    IReadOnlyList<ConfigurationWarning> Warnings)
{
    public bool HasErrors => Errors.Count > 0;
    public bool HasWarnings => Warnings.Count > 0;
    public bool IsSuccessful => IsValid && !HasErrors;
}

/// <summary>
/// Represents a validation error
/// </summary>
public sealed record ValidationError(
    string Code,
    string Message,
    string? PropertyName,
    object? AttemptedValue)
{
    public bool HasPropertyName => !string.IsNullOrWhiteSpace(PropertyName);
}

/// <summary>
/// Represents a validation warning
/// </summary>
public sealed record ValidationWarning(
    string Code,
    string Message,
    string? PropertyName,
    object? AttemptedValue)
{
    public bool HasPropertyName => !string.IsNullOrWhiteSpace(PropertyName);
}

/// <summary>
/// Represents a security threat
/// </summary>
public sealed record SecurityThreat(
    string Type,
    string Description,
    string Severity,
    string? Recommendation)
{
    public bool IsHighSeverity => Severity.Equals("High", StringComparison.OrdinalIgnoreCase);
    public bool HasRecommendation => !string.IsNullOrWhiteSpace(Recommendation);
}

/// <summary>
/// Represents a processing error
/// </summary>
public sealed record ProcessingError(
    string Code,
    string Message,
    string? StackTrace,
    Exception? InnerException)
{
    public bool HasStackTrace => !string.IsNullOrWhiteSpace(StackTrace);
    public bool HasInnerException => InnerException is not null;
}

/// <summary>
/// Represents a configuration error
/// </summary>
public sealed record ConfigurationError(
    string Code,
    string Message,
    string ConfigurationPath,
    object? InvalidValue)
{
    public bool HasInvalidValue => InvalidValue is not null;
}

/// <summary>
/// Represents a configuration warning
/// </summary>
public sealed record ConfigurationWarning(
    string Code,
    string Message,
    string ConfigurationPath,
    object? RecommendedValue)
{
    public bool HasRecommendedValue => RecommendedValue is not null;
} 
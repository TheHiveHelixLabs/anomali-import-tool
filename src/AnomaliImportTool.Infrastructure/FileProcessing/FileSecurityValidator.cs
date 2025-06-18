using System;
using AnomaliImportTool.Core.Application.Interfaces.Infrastructure;
using AnomaliImportTool.Core.Domain.ValueObjects;
using AnomaliImportTool.Core.Domain.SharedKernel.Primitives;
using AnomaliImportTool.Core.Domain.SharedKernel.Guards;

namespace AnomaliImportTool.Infrastructure.FileProcessing;

/// <summary>
/// Single responsibility: Validate file security (malware scan, safe content)
/// Focused implementation that handles only security validation concerns
/// </summary>
public class FileSecurityValidator : IFileValidator
{
    private readonly IReadOnlyList<string> _dangerousExtensions = new[]
    {
        ".exe", ".bat", ".cmd", ".com", ".scr", ".pif", ".vbs", ".js", ".jar", ".ps1"
    };

    private readonly IReadOnlyList<string> _safeExtensions = new[]
    {
        ".pdf", ".docx", ".xlsx", ".txt", ".jpg", ".png", ".gif", ".csv"
    };

    /// <summary>
    /// Validate file format and structure
    /// </summary>
    public async Task<FileValidationResult> ValidateFormatAsync(FilePath filePath, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(filePath.Value, nameof(filePath));
        
        var startTime = DateTime.UtcNow;
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        
        try
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var fileName = Path.GetFileName(filePath);
            
            // Check for dangerous file extensions
            if (_dangerousExtensions.Contains(extension))
            {
                errors.Add(new ValidationError(
                    Code: "DANGEROUS_EXTENSION",
                    Message: $"File extension '{extension}' is potentially dangerous",
                    PropertyName: "Extension",
                    AttemptedValue: extension));
            }
            
            // Check for suspicious file names
            if (IsSuspiciousFileName(fileName))
            {
                warnings.Add(new ValidationWarning(
                    Code: "SUSPICIOUS_FILENAME",
                    Message: $"File name '{fileName}' contains suspicious patterns",
                    PropertyName: "FileName",
                    AttemptedValue: fileName));
            }
            
            // Check for double extensions (e.g., document.pdf.exe)
            if (HasDoubleExtension(fileName))
            {
                errors.Add(new ValidationError(
                    Code: "DOUBLE_EXTENSION",
                    Message: $"File '{fileName}' has suspicious double extension",
                    PropertyName: "FileName",
                    AttemptedValue: fileName));
            }
            
            await Task.Delay(50, cancellationToken); // Simulate processing
            
            var validationTime = DateTime.UtcNow - startTime;
            var isValid = errors.Count == 0;
            
            return new FileValidationResult(
                IsValid: isValid,
                FileFormat: GetFileFormat(extension),
                Errors: errors,
                Warnings: warnings,
                ValidationTime: validationTime);
        }
        catch (Exception ex)
        {
            var validationTime = DateTime.UtcNow - startTime;
            errors.Add(new ValidationError(
                Code: "VALIDATION_ERROR",
                Message: $"Validation failed: {ex.Message}",
                PropertyName: null,
                AttemptedValue: filePath));
                
            return new FileValidationResult(
                IsValid: false,
                FileFormat: "Unknown",
                Errors: errors,
                Warnings: warnings,
                ValidationTime: validationTime);
        }
    }

    /// <summary>
    /// Validate file integrity using checksums
    /// </summary>
    public async Task<IntegrityValidationResult> ValidateIntegrityAsync(FilePath filePath, ContentHash expectedHash, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(filePath.Value, nameof(filePath));
        Guard.Against.NullOrWhiteSpace(expectedHash.Value, nameof(expectedHash));
        
        try
        {
            // Calculate actual file hash
            var actualHash = await CalculateFileHashAsync(filePath, expectedHash.Algorithm.ToString(), cancellationToken);
            var hashesMatch = actualHash.Value.Equals(expectedHash.Value, StringComparison.OrdinalIgnoreCase);
            
            return new IntegrityValidationResult(
                IsIntegrityValid: hashesMatch,
                ActualHash: actualHash,
                ExpectedHash: expectedHash,
                HashesMatch: hashesMatch,
                ValidationMethod: expectedHash.Algorithm.ToString());
        }
        catch (Exception)
        {
            var defaultHash = new ContentHash("0000000000000000000000000000000000000000000000000000000000000000", expectedHash.Algorithm);
            return new IntegrityValidationResult(
                IsIntegrityValid: false,
                ActualHash: defaultHash,
                ExpectedHash: expectedHash,
                HashesMatch: false,
                ValidationMethod: expectedHash.Algorithm.ToString());
        }
    }

    /// <summary>
    /// Validate file security (malware scan, safe content)
    /// </summary>
    public async Task<SecurityValidationResult> ValidateSecurityAsync(FilePath filePath, CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(filePath.Value, nameof(filePath));
        
        var threats = new List<SecurityThreat>();
        var scanTimestamp = DateTime.UtcNow;
        
        try
        {
            // Simulate malware scanning
            await Task.Delay(300, cancellationToken);
            
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var fileName = Path.GetFileName(filePath);
            
            // Check for known malicious patterns
            if (ContainsMaliciousPatterns(fileName))
            {
                threats.Add(new SecurityThreat(
                    Type: "Suspicious Pattern",
                    Description: "File name contains patterns commonly used by malware",
                    Severity: "Medium",
                    Recommendation: "Review file content carefully before processing"));
            }
            
            // Check for executable disguised as document
            if (IsExecutableDisguisedAsDocument(fileName, extension))
            {
                threats.Add(new SecurityThreat(
                    Type: "Disguised Executable",
                    Description: "File appears to be an executable disguised as a document",
                    Severity: "High",
                    Recommendation: "Do not execute or open this file"));
            }
            
            // Simulate advanced threat detection
            var advancedThreats = await PerformAdvancedThreatDetectionAsync(filePath, cancellationToken);
            threats.AddRange(advancedThreats);
            
            var isSafe = threats.Count == 0 || threats.All(t => !t.IsHighSeverity);
            
            return new SecurityValidationResult(
                IsSafe: isSafe,
                Threats: threats,
                ScanEngine: "Windows Defender Simulation",
                ScanTimestamp: scanTimestamp,
                ScanVersion: "1.0.0");
        }
        catch (Exception)
        {
            threats.Add(new SecurityThreat(
                Type: "Scan Error",
                Description: "Security scan could not be completed",
                Severity: "High",
                Recommendation: "Manual security review required"));
                
            return new SecurityValidationResult(
                IsSafe: false,
                Threats: threats,
                ScanEngine: "Windows Defender Simulation",
                ScanTimestamp: scanTimestamp,
                ScanVersion: "1.0.0");
        }
    }

    /// <summary>
    /// Validate file size constraints
    /// </summary>
    public async Task<SizeValidationResult> ValidateSizeAsync(FilePath filePath, long maxSizeBytes)
    {
        Guard.Against.NullOrWhiteSpace(filePath.Value, nameof(filePath));
        Guard.Against.NegativeOrZero(maxSizeBytes, nameof(maxSizeBytes));
        
        try
        {
            await Task.Delay(10); // Simulate processing
            
            var fileInfo = new FileInfo(filePath);
            var actualSize = fileInfo.Length;
            var isSizeValid = actualSize <= maxSizeBytes;
            
            return new SizeValidationResult(
                IsSizeValid: isSizeValid,
                ActualSize: actualSize,
                MaxAllowedSize: maxSizeBytes,
                SizeUnit: "bytes");
        }
        catch (Exception)
        {
            return new SizeValidationResult(
                IsSizeValid: false,
                ActualSize: 0,
                MaxAllowedSize: maxSizeBytes,
                SizeUnit: "bytes");
        }
    }

    /// <summary>
    /// Check if file exists and is accessible
    /// </summary>
    public async Task<AccessibilityValidationResult> ValidateAccessibilityAsync(FilePath filePath)
    {
        Guard.Against.NullOrWhiteSpace(filePath.Value, nameof(filePath));
        
        try
        {
            await Task.Delay(10); // Simulate processing
            
            var fileInfo = new FileInfo(filePath);
            var fileExists = fileInfo.Exists;
            var hasReadPermission = CanReadFile(filePath);
            var hasWritePermission = CanWriteFile(filePath);
            
            var isAccessible = fileExists && hasReadPermission;
            
            return new AccessibilityValidationResult(
                IsAccessible: isAccessible,
                FileExists: fileExists,
                HasReadPermission: hasReadPermission,
                HasWritePermission: hasWritePermission,
                ErrorMessage: isAccessible ? null : "File is not accessible for reading");
        }
        catch (Exception ex)
        {
            return new AccessibilityValidationResult(
                IsAccessible: false,
                FileExists: false,
                HasReadPermission: false,
                HasWritePermission: false,
                ErrorMessage: ex.Message);
        }
    }

    #region Private Helper Methods

    private bool IsSuspiciousFileName(string fileName)
    {
        var suspiciousPatterns = new[]
        {
            "invoice", "payment", "urgent", "confidential", "important",
            "temp", "tmp", "download", "update", "install"
        };
        
        return suspiciousPatterns.Any(pattern => 
            fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasDoubleExtension(string fileName)
    {
        var parts = fileName.Split('.');
        return parts.Length > 2 && 
               _safeExtensions.Contains($".{parts[^2]}", StringComparer.OrdinalIgnoreCase) &&
               _dangerousExtensions.Contains($".{parts[^1]}", StringComparer.OrdinalIgnoreCase);
    }

    private string GetFileFormat(string extension)
    {
        return extension switch
        {
            ".pdf" => "Portable Document Format",
            ".docx" => "Microsoft Word Document",
            ".xlsx" => "Microsoft Excel Spreadsheet",
            ".txt" => "Plain Text",
            ".jpg" or ".jpeg" => "JPEG Image",
            ".png" => "PNG Image",
            ".gif" => "GIF Image",
            ".csv" => "Comma-Separated Values",
            _ => "Unknown"
        };
    }

    private async Task<ContentHash> CalculateFileHashAsync(FilePath filePath, string algorithm, CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken); // Simulate hash calculation
        
        // In real implementation, this would calculate actual file hash
        var fileInfo = new FileInfo(filePath);
        var simulatedHash = $"{algorithm}_{fileInfo.Length}_{fileInfo.LastWriteTime:yyyyMMddHHmmss}";
        
        return new ContentHash(simulatedHash, Enum.Parse<HashAlgorithmType>(algorithm));
    }

    private bool ContainsMaliciousPatterns(string fileName)
    {
        var maliciousPatterns = new[]
        {
            "trojan", "virus", "malware", "keylogger", "backdoor",
            "ransomware", "spyware", "adware", "rootkit"
        };
        
        return maliciousPatterns.Any(pattern => 
            fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsExecutableDisguisedAsDocument(string fileName, string extension)
    {
        // Check if file has document-like name but executable extension
        var documentKeywords = new[] { "document", "report", "invoice", "contract", "agreement" };
        var hasDocumentKeyword = documentKeywords.Any(keyword => 
            fileName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            
        return hasDocumentKeyword && _dangerousExtensions.Contains(extension);
    }

    private async Task<IList<SecurityThreat>> PerformAdvancedThreatDetectionAsync(FilePath filePath, CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken); // Simulate advanced scanning
        
        var threats = new List<SecurityThreat>();
        
        // Simulate heuristic analysis
        var fileSize = new FileInfo(filePath).Length;
        if (fileSize > 100 * 1024 * 1024) // Files larger than 100MB
        {
            threats.Add(new SecurityThreat(
                Type: "Large File",
                Description: "File is unusually large for its type",
                Severity: "Low",
                Recommendation: "Verify file content is legitimate"));
        }
        
        return threats;
    }

    private bool CanReadFile(FilePath filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool CanWriteFile(FilePath filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            return !fileInfo.IsReadOnly && !fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly);
        }
        catch
        {
            return false;
        }
    }

    #endregion
} 
using AnomaliImportTool.Core.Domain.SharedKernel.Exceptions;
using AnomaliImportTool.Core.Domain.SharedKernel.Guards;

namespace AnomaliImportTool.Core.Domain.SharedKernel.Primitives;

/// <summary>
/// File path domain primitive with built-in validation and security checks
/// Note: File system operations moved to infrastructure layer
/// </summary>
public readonly record struct FilePath
{
    // Common invalid path characters (simplified for domain validation)
    private static readonly char[] InvalidPathChars = { '<', '>', ':', '"', '|', '?', '*', '\0' };
    private static readonly char[] InvalidFileNameChars = { '\\', '/', '<', '>', ':', '"', '|', '?', '*', '\0' };

    public string Value { get; }
    public string FileName => GetFileNameFromPath(Value);
    public string Extension => GetExtensionFromPath(Value);
    public string DirectoryName => GetDirectoryFromPath(Value);

    public FilePath(string value)
    {
        Guard.Against.NullOrWhiteSpace(value, nameof(value));

        var normalizedPath = NormalizePath(value);
        
        ValidatePath(normalizedPath);
        ValidateSecurityConstraints(normalizedPath);

        Value = normalizedPath;
    }

    private static void ValidatePath(string path)
    {
        if (path.Length > 260) // Windows MAX_PATH limitation
            throw new DomainException("File path exceeds maximum length of 260 characters");

        if (path.IndexOfAny(InvalidPathChars) >= 0)
            throw new DomainException("File path contains invalid characters");

        var fileName = GetFileNameFromPath(path);
        if (!string.IsNullOrEmpty(fileName) && fileName.IndexOfAny(InvalidFileNameChars) >= 0)
            throw new DomainException("File name contains invalid characters");
    }

    private static void ValidateSecurityConstraints(string path)
    {
        // Prevent path traversal attacks
        if (path.Contains(".."))
            throw new DomainException("File path contains path traversal sequences");

        // Prevent access to system directories (basic check)
        var systemPaths = new[] { @"C:\Windows", @"C:\System32", "/etc", "/bin", "/usr/bin" };
        if (systemPaths.Any(sysPath => path.StartsWith(sysPath, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Access to system directories is not allowed");
    }

    // Note: File existence checks moved to infrastructure layer
    // Use infrastructure services to check file existence

    private static string NormalizePath(string path)
    {
        // Simple path normalization without System.IO dependencies
        return path.Replace('\\', '/').Trim();
    }

    private static string GetFileNameFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        
        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
    }

    private static string GetExtensionFromPath(string path)
    {
        var fileName = GetFileNameFromPath(path);
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 ? fileName.Substring(lastDot) : string.Empty;
    }

    private static string GetDirectoryFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        
        var lastSlash = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        return lastSlash > 0 ? path.Substring(0, lastSlash) : string.Empty;
    }

    public static implicit operator string(FilePath filePath) => filePath.Value;
    public static explicit operator FilePath(string value) => new(value);

    public override string ToString() => Value;

    public static bool TryCreate(string value, out FilePath filePath)
    {
        try
        {
            filePath = new FilePath(value);
            return true;
        }
        catch
        {
            filePath = default;
            return false;
        }
    }
} 
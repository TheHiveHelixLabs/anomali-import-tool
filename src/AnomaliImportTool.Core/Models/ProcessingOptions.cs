using System;
using System.Collections.Generic;

namespace AnomaliImportTool.Core.Models
{
    /// <summary>
    /// Options and settings for document processing operations.
    /// </summary>
    public class ProcessingOptions
    {
        /// <summary>
        /// Gets or sets whether to enable OCR for scanned documents.
        /// </summary>
        public bool EnableOcr { get; set; } = true;

        /// <summary>
        /// Gets or sets the OCR language for text recognition.
        /// Default is English (en-US).
        /// </summary>
        public string OcrLanguage { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets the OCR languages for text recognition.
        /// </summary>
        public List<string> OcrLanguages { get; set; } = new List<string> { "en-US" };

        /// <summary>
        /// Gets or sets the minimum confidence level for OCR results (0.0 to 1.0).
        /// Text with confidence below this threshold will be ignored.
        /// </summary>
        public double OcrMinConfidence { get; set; } = 0.6;

        /// <summary>
        /// Gets or sets the maximum file size in MB to process.
        /// Files larger than this will be skipped.
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 100;

        /// <summary>
        /// Gets or sets the maximum number of files to process in parallel.
        /// </summary>
        public int MaxParallelFiles { get; set; } = 4;

        /// <summary>
        /// Gets or sets the maximum number of concurrent files to process.
        /// </summary>
        public int MaxConcurrentFiles { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether to enable parallel processing.
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to preserve original files after processing.
        /// </summary>
        public bool PreserveOriginalFiles { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to extract metadata from documents.
        /// </summary>
        public bool ExtractMetadata { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to extract text content from documents.
        /// </summary>
        public bool ExtractTextContent { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum text content length to extract.
        /// Content longer than this will be truncated.
        /// </summary>
        public int MaxTextContentLength { get; set; } = 1000000; // 1 million characters

        /// <summary>
        /// Gets or sets whether to preserve original formatting in extracted text.
        /// </summary>
        public bool PreserveFormatting { get; set; } = false;

        /// <summary>
        /// Gets or sets the default TLP (Traffic Light Protocol) designation for bulletins.
        /// </summary>
        public TlpDesignation DefaultTlpDesignation { get; set; } = TlpDesignation.Amber;

        /// <summary>
        /// Gets or sets whether to automatically detect TLP from document content.
        /// </summary>
        public bool AutoDetectTlp { get; set; } = true;

        /// <summary>
        /// Gets or sets the patterns to search for when extracting metadata.
        /// Key is the field name, value is the regex pattern.
        /// </summary>
        public Dictionary<string, string> MetadataExtractionPatterns { get; set; } = new Dictionary<string, string>
        {
            { "ticket", @"(?i)ticket\s*#?\s*:?\s*([A-Z0-9-]+)" },
            { "cve", @"CVE-\d{4}-\d{4,}" },
            { "date", @"\b\d{1,2}[-/]\d{1,2}[-/]\d{2,4}\b" },
            { "email", @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b" },
            { "ip", @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b" },
            { "domain", @"(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}" }
        };

        /// <summary>
        /// Gets or sets the file extensions to process.
        /// Empty list means all supported extensions.
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new List<string>
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx"
        };

        /// <summary>
        /// Gets or sets the supported file types for processing.
        /// </summary>
        public List<string> SupportedFileTypes { get; set; } = new List<string>
        {
            "pdf", "doc", "docx", "xls", "xlsx"
        };

        /// <summary>
        /// Gets or sets whether to skip password-protected files.
        /// </summary>
        public bool SkipPasswordProtected { get; set; } = false;

        /// <summary>
        /// Gets or sets the password to try for protected files.
        /// Only used if SkipPasswordProtected is false.
        /// </summary>
        public string DefaultPassword { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to create a backup of processed files.
        /// </summary>
        public bool CreateBackup { get; set; } = false;

        /// <summary>
        /// Gets or sets the timeout in seconds for processing a single file.
        /// </summary>
        public int ProcessingTimeoutSeconds { get; set; } = 300; // 5 minutes

        /// <summary>
        /// Gets or sets whether to continue processing if a file fails.
        /// </summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to validate file content before processing.
        /// </summary>
        public bool ValidateFileContent { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to scan files for malware signatures.
        /// </summary>
        public bool EnableMalwareScan { get; set; } = true;

        /// <summary>
        /// Validates the processing options.
        /// </summary>
        /// <returns>True if options are valid; otherwise, false.</returns>
        public bool IsValid()
        {
            if (OcrMinConfidence < 0.0 || OcrMinConfidence > 1.0)
                return false;

            if (MaxFileSizeMB < 1 || MaxFileSizeMB > 1000)
                return false;

            if (MaxTextContentLength < 1000 || MaxTextContentLength > 10000000)
                return false;

            if (ProcessingTimeoutSeconds < 10 || ProcessingTimeoutSeconds > 3600)
                return false;

            if (string.IsNullOrWhiteSpace(OcrLanguage))
                return false;

            if (MaxParallelFiles < 1 || MaxParallelFiles > 10)
                return false;

            if (MaxConcurrentFiles < 1 || MaxConcurrentFiles > 10)
                return false;

            return true;
        }

        /// <summary>
        /// Gets whether the specified file extension is allowed for processing.
        /// </summary>
        /// <param name="extension">The file extension to check.</param>
        /// <returns>True if the extension is allowed; otherwise, false.</returns>
        public bool IsExtensionAllowed(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            if (AllowedExtensions == null || AllowedExtensions.Count == 0)
                return true; // All extensions allowed if list is empty

            return AllowedExtensions.Contains(extension.ToLowerInvariant());
        }

        /// <summary>
        /// Creates a copy of the processing options suitable for logging.
        /// Sensitive information like passwords is redacted.
        /// </summary>
        /// <returns>A sanitized copy of the processing options.</returns>
        public ProcessingOptions GetSanitizedCopy()
        {
            var copy = (ProcessingOptions)this.MemberwiseClone();
            
            if (!string.IsNullOrEmpty(copy.DefaultPassword))
            {
                copy.DefaultPassword = "***REDACTED***";
            }

            // Deep copy the dictionaries to avoid reference sharing
            copy.MetadataExtractionPatterns = new Dictionary<string, string>(this.MetadataExtractionPatterns);
            copy.AllowedExtensions = new List<string>(this.AllowedExtensions);
            copy.OcrLanguages = new List<string>(this.OcrLanguages);
            copy.SupportedFileTypes = new List<string>(this.SupportedFileTypes);

            return copy;
        }
    }
} 
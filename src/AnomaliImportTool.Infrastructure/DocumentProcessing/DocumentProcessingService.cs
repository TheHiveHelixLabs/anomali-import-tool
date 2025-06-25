using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing
{
    /// <summary>
    /// Service that coordinates document processing using appropriate processors.
    /// </summary>
    public class DocumentProcessingService : IDocumentProcessor
    {
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly Dictionary<string, IDocumentProcessor> _processors;
        private readonly ProcessingOptions _defaultOptions;
        private readonly TemplateExtractionEngine? _templateExtractionEngine;

        public DocumentProcessingService(
            ILogger<DocumentProcessingService> logger,
            IEnumerable<IDocumentProcessor> processors,
            TemplateExtractionEngine? templateExtractionEngine = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processors = new Dictionary<string, IDocumentProcessor>(StringComparer.OrdinalIgnoreCase);
            _defaultOptions = new ProcessingOptions();
            _templateExtractionEngine = templateExtractionEngine;

            // Register processors by their supported extensions
            foreach (var processor in processors ?? Enumerable.Empty<IDocumentProcessor>())
            {
                foreach (var extension in processor.SupportedExtensions)
                {
                    _processors[extension] = processor;
                    _logger.LogDebug("Registered {ProcessorType} for extension {Extension}", 
                        processor.GetType().Name, extension);
                }
            }

            _logger.LogInformation("DocumentProcessingService initialized with {ProcessorCount} processors", 
                _processors.Count);
        }

        /// <summary>
        /// Gets the supported file extensions for this processor
        /// </summary>
        public string[] SupportedExtensions => GetSupportedExtensions();

        /// <summary>
        /// Processes a document and extracts its content
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed document with extracted content</returns>
        public async Task<Document> ProcessDocumentAsync(string filePath, CancellationToken cancellationToken = default)
        {
            return await ProcessDocumentInternalAsync(filePath, _defaultOptions, cancellationToken);
        }

        /// <summary>
        /// Processes a document with specific options
        /// </summary>
        /// <param name="filePath">Path to the file to process</param>
        /// <param name="options">Processing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed document with extracted content</returns>
        public async Task<Document> ProcessAsync(string filePath, ProcessingOptions options = null, CancellationToken cancellationToken = default)
        {
            return await ProcessDocumentInternalAsync(filePath, options, cancellationToken);
        }

        /// <summary>
        /// Checks if the processor can handle the given file type
        /// </summary>
        /// <param name="fileExtension">File extension to check</param>
        /// <returns>True if the processor can handle the file</returns>
        public bool CanProcess(string fileExtension)
        {
            return _processors.ContainsKey(fileExtension?.TrimStart('.'));
        }

        /// <summary>
        /// Validates a file without fully processing it
        /// </summary>
        /// <param name="filePath">Path to the file to validate</param>
        /// <returns>True if the file is valid for processing</returns>
        public async Task<bool> ValidateAsync(string filePath)
        {
            return await ValidateDocumentAsync(filePath);
        }

        /// <summary>
        /// Processes a single document file internally.
        /// </summary>
        /// <param name="filePath">The path to the document file.</param>
        /// <param name="options">Processing options. If null, default options will be used.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The processed document.</returns>
        public async Task<Document> ProcessDocumentInternalAsync(
            string filePath, 
            ProcessingOptions options = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            options ??= _defaultOptions;

            // Get appropriate processor
            var processor = GetProcessorForFile(filePath);
            if (processor == null)
            {
                throw new NotSupportedException($"No processor available for file type: {Path.GetExtension(filePath)}");
            }

            _logger.LogInformation("Processing document: {FilePath} with {ProcessorType}", 
                filePath, processor.GetType().Name);

            try
            {
                return await processor.ProcessAsync(filePath, options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Processes multiple documents in parallel.
        /// </summary>
        /// <param name="filePaths">The paths to the document files.</param>
        /// <param name="options">Processing options. If null, default options will be used.</param>
        /// <param name="progress">Progress reporter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The batch processing result.</returns>
        public async Task<BatchProcessingResult> ProcessBatchAsync(
            IEnumerable<string> filePaths,
            ProcessingOptions options = null,
            IProgress<BatchProcessingProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (filePaths == null)
            {
                throw new ArgumentNullException(nameof(filePaths));
            }

            var fileList = filePaths.ToList();
            if (fileList.Count == 0)
            {
                return new BatchProcessingResult
                {
                    TotalFiles = 0,
                    SuccessfulFiles = 0,
                    FailedFiles = 0,
                    ProcessedDocuments = new List<Document>(),
                    Errors = new Dictionary<string, string>()
                };
            }

            options ??= _defaultOptions;

            _logger.LogInformation("Starting batch processing of {FileCount} files", fileList.Count);

            var result = new BatchProcessingResult
            {
                TotalFiles = fileList.Count,
                StartTime = DateTime.UtcNow
            };

            var processedDocuments = new ConcurrentBag<Document>();
            var errors = new ConcurrentDictionary<string, string>();

            // Create semaphore to limit parallelism
            using (var semaphore = new SemaphoreSlim(options.MaxParallelFiles))
            {
                var tasks = fileList.Select(async (filePath, index) =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var progressInfo = new BatchProcessingProgress
                        {
                            TotalFiles = fileList.Count,
                            ProcessedFiles = index,
                            CurrentFile = filePath,
                            PercentComplete = (index * 100.0) / fileList.Count
                        };
                        
                        progress?.Report(progressInfo);

                        try
                        {
                            var document = await ProcessDocumentInternalAsync(filePath, options, cancellationToken);
                            processedDocuments.Add(document);
                            Interlocked.Increment(ref result.SuccessfulFiles);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process file: {FilePath}", filePath);
                            errors.TryAdd(filePath, ex.Message);
                            Interlocked.Increment(ref result.FailedFiles);

                            if (!options.ContinueOnError)
                            {
                                throw;
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToArray();

                await Task.WhenAll(tasks);
            }

            result.EndTime = DateTime.UtcNow;
            result.ProcessedDocuments = processedDocuments.ToList();
            result.Errors = errors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            _logger.LogInformation("Batch processing completed. Success: {SuccessCount}, Failed: {FailedCount}, Duration: {Duration}",
                result.SuccessfulFiles, result.FailedFiles, result.Duration);

            return result;
        }

        /// <summary>
        /// Validates a document file without processing it.
        /// </summary>
        /// <param name="filePath">The path to the document file.</param>
        /// <returns>True if the file is valid; otherwise, false.</returns>
        public async Task<bool> ValidateDocumentAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var processor = GetProcessorForFile(filePath);
            if (processor == null)
                return false;

            try
            {
                return await processor.ValidateAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating document: {FilePath}", filePath);
                return false;
            }
        }

        /// <summary>
        /// Gets the appropriate processor for a file based on its extension.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The document processor, or null if none available.</returns>
        public IDocumentProcessor GetProcessorForFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                return null;

            _processors.TryGetValue(extension, out var processor);
            return processor;
        }

        /// <summary>
        /// Gets all supported file extensions.
        /// </summary>
        /// <returns>Array of supported extensions.</returns>
        public string[] GetSupportedExtensions()
        {
            return _processors.Keys.ToArray();
        }

        /// <summary>
        /// Checks if a file type is supported.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <returns>True if the file type is supported; otherwise, false.</returns>
        public bool IsFileTypeSupported(string filePath)
        {
            return GetProcessorForFile(filePath) != null;
        }

        /// <summary>
        /// Processes a document and applies a template-guided extraction, returning the document with extracted fields populated.
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <param name="template">Template to apply</param>
        /// <param name="options">Processing options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task<Document> ProcessWithTemplateAsync(string filePath, ImportTemplate template, ProcessingOptions options = null, CancellationToken cancellationToken = default)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            // First process the document to extract text/content
            var document = await ProcessDocumentInternalAsync(filePath, options, cancellationToken);

            // Run template extraction
            var engine = _templateExtractionEngine ?? new TemplateExtractionEngine(NullLogger<TemplateExtractionEngine>.Instance);
            var extractionResult = await engine.ExtractFieldsAsync(document, template);

            // Merge field results into document
            foreach (var kvp in extractionResult.FieldResults)
            {
                if (kvp.Value.IsSuccessful && !string.IsNullOrEmpty(kvp.Value.ExtractedValue))
                {
                    document.ExtractedFields[kvp.Key] = kvp.Value.ExtractedValue!;
                }
            }

            // Add extraction metadata
            document.CustomProperties["TemplateId"] = template.Id;
            document.CustomProperties["TemplateConfidence"] = extractionResult.OverallConfidence;
            document.CustomProperties["TemplateAppliedAt"] = extractionResult.ExtractionEndTime;

            return document;
        }
    }

    /// <summary>
    /// Represents the result of batch document processing.
    /// </summary>
    public class BatchProcessingResult
    {
        /// <summary>
        /// Gets or sets the total number of files to process.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of successfully processed files.
        /// </summary>
        public int SuccessfulFiles;

        /// <summary>
        /// Gets or sets the number of failed files.
        /// </summary>
        public int FailedFiles;

        /// <summary>
        /// Gets or sets the list of processed documents.
        /// </summary>
        public List<Document> ProcessedDocuments { get; set; } = new List<Document>();

        /// <summary>
        /// Gets or sets the error messages for failed files.
        /// Key is the file path, value is the error message.
        /// </summary>
        public Dictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the start time of batch processing.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time of batch processing.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets the duration of batch processing.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Gets the success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalFiles > 0 ? (SuccessfulFiles * 100.0) / TotalFiles : 0;
    }

    /// <summary>
    /// Represents progress information for batch processing.
    /// </summary>
    public class BatchProcessingProgress
    {
        /// <summary>
        /// Gets or sets the total number of files to process.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Gets or sets the number of files processed so far.
        /// </summary>
        public int ProcessedFiles { get; set; }

        /// <summary>
        /// Gets or sets the current file being processed.
        /// </summary>
        public string CurrentFile { get; set; }

        /// <summary>
        /// Gets or sets the percentage complete.
        /// </summary>
        public double PercentComplete { get; set; }
    }
} 
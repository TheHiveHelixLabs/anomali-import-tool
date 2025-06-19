using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.DocumentProcessing;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.Infrastructure.Services
{
    /// <summary>
    /// Service that integrates document processing with Anomali API uploads
    /// to provide end-to-end document import workflow.
    /// </summary>
    public class DocumentUploadService
    {
        private readonly ILogger<DocumentUploadService> _logger;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly IAnomaliApiClient _apiClient;

        public DocumentUploadService(
            ILogger<DocumentUploadService> logger,
            IDocumentProcessor documentProcessor,
            IAnomaliApiClient apiClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Processes a document and uploads it to Anomali as a threat bulletin.
        /// </summary>
        /// <param name="filePath">Path to the document file</param>
        /// <param name="options">Processing options</param>
        /// <param name="uploadOptions">Upload options for creating the threat bulletin</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Upload result containing the processed document and created bulletin</returns>
        public async Task<DocumentUploadResult> ProcessAndUploadAsync(
            string filePath,
            ProcessingOptions options = null,
            UploadOptions uploadOptions = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", filePath);
            }

            uploadOptions ??= new UploadOptions();
            
            var result = new DocumentUploadResult
            {
                FilePath = filePath,
                StartTime = DateTime.UtcNow,
                Status = UploadStatus.Processing
            };

            try
            {
                _logger.LogInformation("Starting end-to-end processing for file: {FilePath}", filePath);

                // Step 1: Process the document
                _logger.LogDebug("Step 1: Processing document {FilePath}", filePath);
                result.ProcessedDocument = await _documentProcessor.ProcessAsync(filePath, options, cancellationToken);

                // Step 2: Create threat bulletin from processed document
                _logger.LogDebug("Step 2: Creating threat bulletin for {FilePath}", filePath);
                var bulletin = CreateThreatBulletinFromDocument(result.ProcessedDocument, uploadOptions);
                
                result.Status = UploadStatus.Uploading;

                // Step 3: Upload bulletin to Anomali
                _logger.LogDebug("Step 3: Uploading threat bulletin for {FilePath}", filePath);
                result.CreatedBulletin = await _apiClient.CreateThreatBulletinAsync(bulletin, cancellationToken);

                // Step 4: Upload original file as attachment (if requested)
                if (uploadOptions.AttachOriginalFile && result.CreatedBulletin?.Id != null)
                {
                    _logger.LogDebug("Step 4: Uploading original file as attachment for {FilePath}", filePath);
                    var attachmentSuccess = await _apiClient.UploadAttachmentAsync(
                        result.CreatedBulletin.Id, 
                        filePath, 
                        Path.GetFileName(filePath), 
                        cancellationToken);

                    if (!attachmentSuccess)
                    {
                        _logger.LogWarning("Failed to upload attachment for bulletin {BulletinId}, but bulletin was created successfully", 
                            result.CreatedBulletin.Id);
                    }
                }

                result.Status = UploadStatus.Completed;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation("Successfully completed end-to-end processing for {FilePath}. Bulletin ID: {BulletinId}", 
                    filePath, result.CreatedBulletin?.Id);

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Processing cancelled for file: {FilePath}", filePath);
                result.Status = UploadStatus.Cancelled;
                result.EndTime = DateTime.UtcNow;
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in end-to-end processing for file: {FilePath}", filePath);
                result.Status = UploadStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                throw;
            }
        }

        /// <summary>
        /// Processes multiple documents and uploads them as threat bulletins.
        /// </summary>
        /// <param name="filePaths">Paths to the document files</param>
        /// <param name="options">Processing options</param>
        /// <param name="uploadOptions">Upload options</param>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Batch upload result</returns>
        public async Task<BatchUploadResult> ProcessAndUploadBatchAsync(
            IEnumerable<string> filePaths,
            ProcessingOptions options = null,
            UploadOptions uploadOptions = null,
            IProgress<BatchUploadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (filePaths == null)
            {
                throw new ArgumentNullException(nameof(filePaths));
            }

            var fileList = filePaths.ToList();
            if (fileList.Count == 0)
            {
                return new BatchUploadResult
                {
                    TotalFiles = 0,
                    SuccessfulUploads = 0,
                    FailedUploads = 0,
                    Results = new List<DocumentUploadResult>()
                };
            }

            uploadOptions ??= new UploadOptions();
            options ??= new ProcessingOptions();

            _logger.LogInformation("Starting batch upload of {FileCount} files", fileList.Count);

            var batchResult = new BatchUploadResult
            {
                TotalFiles = fileList.Count,
                StartTime = DateTime.UtcNow
            };

            var results = new List<DocumentUploadResult>();
            var semaphore = new SemaphoreSlim(options.MaxParallelFiles);

            var tasks = fileList.Select(async (filePath, index) =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    // Report progress
                    var progressInfo = new BatchUploadProgress
                    {
                        TotalFiles = fileList.Count,
                        ProcessedFiles = index,
                        CurrentFile = filePath,
                        PercentComplete = (index * 100.0) / fileList.Count
                    };
                    progress?.Report(progressInfo);

                    try
                    {
                        var result = await ProcessAndUploadAsync(filePath, options, uploadOptions, cancellationToken);
                        lock (results)
                        {
                            results.Add(result);
                            batchResult.SuccessfulUploads++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process and upload file: {FilePath}", filePath);
                        
                        var failedResult = new DocumentUploadResult
                        {
                            FilePath = filePath,
                            Status = UploadStatus.Failed,
                            ErrorMessage = ex.Message,
                            StartTime = DateTime.UtcNow,
                            EndTime = DateTime.UtcNow
                        };

                        lock (results)
                        {
                            results.Add(failedResult);
                            batchResult.FailedUploads++;
                        }

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

            batchResult.EndTime = DateTime.UtcNow;
            batchResult.Results = results;

            _logger.LogInformation("Batch upload completed. Success: {SuccessCount}, Failed: {FailedCount}, Duration: {Duration}",
                batchResult.SuccessfulUploads, batchResult.FailedUploads, batchResult.Duration);

            return batchResult;
        }

        /// <summary>
        /// Tests the end-to-end integration by verifying document processing capabilities
        /// and API connectivity.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Integration test result</returns>
        public async Task<IntegrationTestResult> TestIntegrationAsync(CancellationToken cancellationToken = default)
        {
            var result = new IntegrationTestResult
            {
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Testing integration components");

                // Test 1: Check supported file types
                result.SupportedExtensions = _documentProcessor.SupportedExtensions;
                result.HasDocumentProcessors = result.SupportedExtensions.Length > 0;

                // Test 2: Test API connectivity
                result.ApiConnectionSuccessful = await _apiClient.TestConnectionAsync(cancellationToken);

                // Test 3: Overall integration status
                result.IntegrationReady = result.HasDocumentProcessors && result.ApiConnectionSuccessful;

                result.EndTime = DateTime.UtcNow;
                result.Success = true;

                _logger.LogInformation("Integration test completed. Ready: {Ready}, Processors: {ProcessorCount}, API: {ApiStatus}",
                    result.IntegrationReady, result.SupportedExtensions.Length, result.ApiConnectionSuccessful);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during integration testing");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                return result;
            }
        }

        private ThreatBulletin CreateThreatBulletinFromDocument(Document document, UploadOptions options)
        {
            var bulletin = new ThreatBulletin
            {
                Name = options.BulletinName ?? $"Threat Intel - {document.FileName}",
                Body = document.ExtractedText ?? string.Empty,
                Summary = options.BulletinDescription ?? $"Threat intelligence extracted from {document.FileName}",
                Tlp = document.TlpDesignation,
                Source = options.Source ?? "Document Import Tool",
                Confidence = options.Confidence ?? 70,
                Tags = options.Tags?.ToDictionary(tag => tag, tag => tag) ?? new Dictionary<string, string> { { "document-import", "true" }, { "file-type", document.FileType?.ToLower() ?? "unknown" } },
                CreatedAt = DateTime.UtcNow
            };

            // Add metadata as additional tags
            if (!string.IsNullOrEmpty(document.Author))
            {
                bulletin.Tags.Add("author", document.Author);
            }

            if (document.CreationDate.HasValue)
            {
                bulletin.Tags.Add("created", document.CreationDate.Value.ToString("yyyy-MM-dd"));
            }

            if (!string.IsNullOrEmpty(document.Title))
            {
                bulletin.Tags.Add("title", document.Title);
            }

            if (!string.IsNullOrEmpty(document.Subject))
            {
                bulletin.Tags.Add("subject", document.Subject);
            }

            return bulletin;
        }
    }

    /// <summary>
    /// Options for uploading documents to Anomali.
    /// </summary>
    public class UploadOptions
    {
        /// <summary>
        /// Name for the threat bulletin. If null, a default name will be generated.
        /// </summary>
        public string BulletinName { get; set; }

        /// <summary>
        /// Description for the threat bulletin. If null, a default description will be generated.
        /// </summary>
        public string BulletinDescription { get; set; }

        /// <summary>
        /// Source attribution for the threat bulletin.
        /// </summary>
        public string Source { get; set; } = "Document Import Tool";

        /// <summary>
        /// Confidence level for the threat intelligence (0-100).
        /// </summary>
        public int? Confidence { get; set; } = 70;

        /// <summary>
        /// Tags to apply to the threat bulletin.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Whether to attach the original file to the threat bulletin.
        /// </summary>
        public bool AttachOriginalFile { get; set; } = true;
    }

    /// <summary>
    /// Result of processing and uploading a single document.
    /// </summary>
    public class DocumentUploadResult
    {
        /// <summary>
        /// Path to the original file.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// The processed document information.
        /// </summary>
        public Document ProcessedDocument { get; set; }

        /// <summary>
        /// The created threat bulletin.
        /// </summary>
        public ThreatBulletin CreatedBulletin { get; set; }

        /// <summary>
        /// Current status of the upload process.
        /// </summary>
        public UploadStatus Status { get; set; }

        /// <summary>
        /// Error message if the process failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Start time of the process.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the process.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the process.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Whether the process was successful.
        /// </summary>
        public bool IsSuccessful => Status == UploadStatus.Completed;
    }

    /// <summary>
    /// Status of the upload process.
    /// </summary>
    public enum UploadStatus
    {
        /// <summary>
        /// Upload has not started.
        /// </summary>
        NotStarted,

        /// <summary>
        /// Document is being processed.
        /// </summary>
        Processing,

        /// <summary>
        /// Document is being uploaded to Anomali.
        /// </summary>
        Uploading,

        /// <summary>
        /// Upload completed successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// Upload failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Upload was cancelled.
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// Result of processing and uploading multiple documents.
    /// </summary>
    public class BatchUploadResult
    {
        /// <summary>
        /// Total number of files in the batch.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Number of successfully uploaded files.
        /// </summary>
        public int SuccessfulUploads { get; set; }

        /// <summary>
        /// Number of failed uploads.
        /// </summary>
        public int FailedUploads { get; set; }

        /// <summary>
        /// Individual upload results.
        /// </summary>
        public List<DocumentUploadResult> Results { get; set; } = new List<DocumentUploadResult>();

        /// <summary>
        /// Start time of the batch process.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the batch process.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the batch process.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Success rate as a percentage.
        /// </summary>
        public double SuccessRate => TotalFiles > 0 ? (SuccessfulUploads * 100.0) / TotalFiles : 0;
    }

    /// <summary>
    /// Progress information for batch uploads.
    /// </summary>
    public class BatchUploadProgress
    {
        /// <summary>
        /// Total number of files to process.
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// Number of files processed so far.
        /// </summary>
        public int ProcessedFiles { get; set; }

        /// <summary>
        /// Currently processing file.
        /// </summary>
        public string CurrentFile { get; set; } = string.Empty;

        /// <summary>
        /// Percentage complete (0-100).
        /// </summary>
        public double PercentComplete { get; set; }
    }

    /// <summary>
    /// Result of integration testing.
    /// </summary>
    public class IntegrationTestResult
    {
        /// <summary>
        /// Whether the test was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether document processors are available.
        /// </summary>
        public bool HasDocumentProcessors { get; set; }

        /// <summary>
        /// Whether API connection is working.
        /// </summary>
        public bool ApiConnectionSuccessful { get; set; }

        /// <summary>
        /// Whether the integration is ready for use.
        /// </summary>
        public bool IntegrationReady { get; set; }

        /// <summary>
        /// Supported file extensions.
        /// </summary>
        public string[] SupportedExtensions { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Error message if the test failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Start time of the test.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the test.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the test.
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;
    }
} 
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tesseract;

namespace AnomaliImportTool.Infrastructure.DocumentProcessing
{
    /// <summary>
    /// Provides OCR (Optical Character Recognition) functionality for scanned documents.
    /// </summary>
    public class OcrProcessor : IDisposable
    {
        private readonly ILogger<OcrProcessor> _logger;
        private readonly string _tessDataPath;
        private TesseractEngine _engine;
        private bool _disposed;

        public OcrProcessor(ILogger<OcrProcessor> logger, string tessDataPath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Use provided path or default to a relative path
            _tessDataPath = tessDataPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            
            InitializeEngine();
        }

        /// <summary>
        /// Performs OCR on an image and returns the extracted text.
        /// </summary>
        /// <param name="imageBytes">The image data as byte array.</param>
        /// <param name="language">The language code for OCR (default: eng).</param>
        /// <param name="minConfidence">Minimum confidence level (0-100) for accepting OCR results.</param>
        /// <returns>The extracted text from the image.</returns>
        public async Task<OcrResult> ProcessImageAsync(byte[] imageBytes, string language = "eng", double minConfidence = 60.0)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new ArgumentException("Image bytes cannot be null or empty.", nameof(imageBytes));
            }

            return await Task.Run(() =>
            {
                try
                {
                    using (var img = Pix.LoadFromMemory(imageBytes))
                    {
                        using (var page = _engine.Process(img))
                        {
                            var text = page.GetText();
                            var confidence = page.GetMeanConfidence();
                            
                            _logger.LogDebug("OCR completed with confidence: {Confidence}%", confidence * 100);

                            // Check if confidence meets minimum threshold
                            if (confidence * 100 < minConfidence)
                            {
                                _logger.LogWarning("OCR confidence {Confidence}% is below minimum threshold {MinConfidence}%", 
                                    confidence * 100, minConfidence);
                            }

                            return new OcrResult
                            {
                                Text = text,
                                Confidence = confidence,
                                Language = language,
                                Success = true,
                                ProcessingTime = TimeSpan.Zero // Would need to track actual time
                            };
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error performing OCR on image");
                    return new OcrResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        Language = language
                    };
                }
            });
        }

        /// <summary>
        /// Performs OCR on an image file.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <param name="language">The language code for OCR (default: eng).</param>
        /// <param name="minConfidence">Minimum confidence level (0-100) for accepting OCR results.</param>
        /// <returns>The extracted text from the image.</returns>
        public async Task<OcrResult> ProcessImageFileAsync(string imagePath, string language = "eng", double minConfidence = 60.0)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                throw new ArgumentException("Image path cannot be null or empty.", nameof(imagePath));
            }

            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("Image file not found.", imagePath);
            }

            var imageBytes = await File.ReadAllBytesAsync(imagePath);
            return await ProcessImageAsync(imageBytes, language, minConfidence);
        }

        /// <summary>
        /// Checks if OCR is available and properly configured.
        /// </summary>
        /// <returns>True if OCR is available; otherwise, false.</returns>
        public bool IsOcrAvailable()
        {
            try
            {
                return _engine != null && Directory.Exists(_tessDataPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the available OCR languages.
        /// </summary>
        /// <returns>Array of available language codes.</returns>
        public string[] GetAvailableLanguages()
        {
            try
            {
                if (!Directory.Exists(_tessDataPath))
                {
                    return Array.Empty<string>();
                }

                var languages = new System.Collections.Generic.List<string>();
                var files = Directory.GetFiles(_tessDataPath, "*.traineddata");
                
                foreach (var file in files)
                {
                    var langCode = Path.GetFileNameWithoutExtension(file);
                    languages.Add(langCode);
                }

                return languages.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available OCR languages");
                return Array.Empty<string>();
            }
        }

        private void InitializeEngine()
        {
            try
            {
                // Ensure tessdata directory exists
                if (!Directory.Exists(_tessDataPath))
                {
                    _logger.LogWarning("Tessdata directory not found at {Path}. OCR will not be available.", _tessDataPath);
                    return;
                }

                // Initialize Tesseract engine with English language
                _engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                
                // Set default page segmentation mode
                _engine.DefaultPageSegMode = PageSegMode.Auto;
                
                _logger.LogInformation("OCR engine initialized successfully with tessdata path: {Path}", _tessDataPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OCR engine");
                _engine = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _engine?.Dispose();
                }

                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Represents the result of an OCR operation.
    /// </summary>
    public class OcrResult
    {
        /// <summary>
        /// Gets or sets whether the OCR operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the extracted text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the confidence level (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Gets or sets the language used for OCR.
        /// </summary>
        public string Language { get; set; } = "eng";

        /// <summary>
        /// Gets or sets the error message if OCR failed.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the processing time.
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }
    }
} 
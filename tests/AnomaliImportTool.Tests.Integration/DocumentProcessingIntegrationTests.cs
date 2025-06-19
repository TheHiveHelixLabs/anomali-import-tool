using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.DocumentProcessing;
using AnomaliImportTool.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using AnomaliImportTool.Infrastructure.ApiClient;
using AnomaliImportTool.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using System.Text.Json;
using System.Net;
using Moq.Contrib.HttpClient;
using Moq.Protected;

namespace AnomaliImportTool.Tests.Integration
{
    public class DocumentProcessingIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly string _testDirectory;
        private readonly IDocumentProcessor _documentProcessor;
        private readonly Mock<ILogger<PdfDocumentProcessor>> _pdfLoggerMock;
        private readonly Mock<ILogger<OcrProcessor>> _ocrLoggerMock;
        private readonly Mock<ILogger<AnomaliApiClient>> _apiLoggerMock;
        private readonly Mock<ILogger<WindowsSecurityService>> _securityLoggerMock;
        private readonly string _testPdfPath;

        public DocumentProcessingIntegrationTests()
        {
            // Setup test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"DocProcessingTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            // Setup dependency injection
            var services = new ServiceCollection();
            
            // Add logging
            var loggingService = new LoggingService();
            services.AddSingleton(loggingService);
            services.AddSingleton(loggingService.Logger);
            
            // Add document processing services
            services.AddTransient<PdfDocumentProcessor>();
            services.AddTransient<WordDocumentProcessor>();
            services.AddTransient<ExcelDocumentProcessor>();
            services.AddTransient<OcrProcessor>();
            services.AddTransient<DocumentProcessingStrategyFactory>();
            services.AddTransient<DocumentProcessingPluginManager>();
            services.AddTransient<IDocumentProcessor, DocumentProcessingService>();
            
            _serviceProvider = services.BuildServiceProvider();
            _documentProcessor = _serviceProvider.GetRequiredService<IDocumentProcessor>();

            _pdfLoggerMock = new Mock<ILogger<PdfDocumentProcessor>>();
            _ocrLoggerMock = new Mock<ILogger<OcrProcessor>>();
            _apiLoggerMock = new Mock<ILogger<AnomaliApiClient>>();
            _securityLoggerMock = new Mock<ILogger<WindowsSecurityService>>();
            
            _testPdfPath = Path.Combine(_testDirectory, "test.pdf");
            CreateTestPdf(_testPdfPath);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task ProcessDocument_ExtractsTextFromTextFile()
        {
            // Arrange
            var content = @"APT29 Campaign Analysis
Date: 2024-03-15
Author: Security Team

This report contains information about recent APT29 activities.
IOCs:
- 192.168.1.100
- malicious.domain.com
- CVE-2024-1234";

            var testFile = Path.Combine(_testDirectory, "test_report.txt");
            await File.WriteAllTextAsync(testFile, content);

            // Act
            var result = await _documentProcessor.ProcessDocumentAsync(testFile);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Content);
            Assert.Contains("APT29", result.Content);
            Assert.Contains("192.168.1.100", result.Content);
            Assert.Contains("CVE-2024-1234", result.Content);
        }

        [Fact]
        public async Task ProcessDocument_ExtractsMetadataFromContent()
        {
            // Arrange
            var content = @"Threat Intelligence Report
Author: John Doe
Date: 2024-03-15
Incident: INC-12345

Campaign: CozyBear
Email: analyst@company.com";

            var testFile = Path.Combine(_testDirectory, "metadata_test.txt");
            await File.WriteAllTextAsync(testFile, content);

            // Act
            var result = await _documentProcessor.ProcessDocumentAsync(testFile);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Metadata);
            Assert.Contains("John Doe", result.Metadata["Author"]);
            Assert.Contains("analyst@company.com", result.Content);
        }

        [Fact]
        public async Task ProcessDocument_HandlesMultipleFilesInParallel()
        {
            // Arrange
            var documents = new List<string>();
            var tasks = new List<Task<Document>>();

            for (int i = 0; i < 5; i++)
            {
                var content = $"Test document {i}\nContent line 1\nContent line 2";
                var testFile = Path.Combine(_testDirectory, $"parallel_test_{i}.txt");
                await File.WriteAllTextAsync(testFile, content);
                documents.Add(testFile);
            }

            // Act
            foreach (var filePath in documents)
            {
                tasks.Add(_documentProcessor.ProcessDocumentAsync(filePath));
            }
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(5, results.Length);
            Assert.All(results, r => Assert.NotNull(r));
            Assert.All(results, r => Assert.NotNull(r.Content));
        }

        [Fact]
        public async Task ProcessDocument_HandlesLargeFile()
        {
            // Arrange
            var sb = new StringBuilder();
            sb.AppendLine("Large Document Test");
            
            // Create a ~1MB file
            for (int i = 0; i < 10000; i++)
            {
                sb.AppendLine($"Line {i}: This is test content for the large file processing test. " +
                             "It contains various types of data including IPs like 10.0.0.{i % 255} " +
                             "and domains like test{i}.example.com");
            }

            var testFile = Path.Combine(_testDirectory, "large_file.txt");
            await File.WriteAllTextAsync(testFile, sb.ToString());

            // Act
            var result = await _documentProcessor.ProcessDocumentAsync(testFile);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Content);
            Assert.Contains("Large Document Test", result.Content);
            Assert.True(result.Content.Length > 100000); // Should be quite large
        }

        [Fact]
        public async Task ProcessDocument_HandlesUnsupportedFileType()
        {
            // Arrange
            var testFile = Path.Combine(_testDirectory, "test.xyz");
            await File.WriteAllTextAsync(testFile, "This is an unsupported file type");

            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => 
                _documentProcessor.ProcessDocumentAsync(testFile));
        }

        [Fact]
        public async Task ProcessDocument_HandlesNonExistentFile()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDirectory, "does_not_exist.txt");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _documentProcessor.ProcessDocumentAsync(nonExistentFile));
        }

        [Fact]
        public async Task ProcessDocument_ExtractsSecurityIndicators()
        {
            // Arrange
            var content = @"Security Alert Report
Date: 2024-03-15

Indicators of Compromise:
- IP Address: 192.168.1.100
- Domain: malicious.example.com
- Hash: a1b2c3d4e5f6g7h8i9j0
- CVE: CVE-2024-1234
- Email: threat@attacker.com
- URL: http://malicious.site/payload";

            var testFile = Path.Combine(_testDirectory, "security_indicators.txt");
            await File.WriteAllTextAsync(testFile, content);

            // Act
            var result = await _documentProcessor.ProcessDocumentAsync(testFile);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Content);
            Assert.Contains("192.168.1.100", result.Content);
            Assert.Contains("malicious.example.com", result.Content);
            Assert.Contains("CVE-2024-1234", result.Content);
            Assert.Contains("threat@attacker.com", result.Content);
        }

        [Fact]
        public async Task ProcessDocument_PreservesFormatting()
        {
            // Arrange
            var content = @"EXECUTIVE SUMMARY
==================

This is a security report with structured formatting.

SECTION 1: Overview
-------------------
• Point 1
• Point 2
• Point 3

SECTION 2: Details
------------------
1. First item
2. Second item
3. Third item

    Indented content here
    More indented content

CONCLUSION
==========
End of report.";

            var testFile = Path.Combine(_testDirectory, "formatted_document.txt");
            await File.WriteAllTextAsync(testFile, content);

            // Act
            var result = await _documentProcessor.ProcessDocumentAsync(testFile);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Content);
            Assert.Contains("EXECUTIVE SUMMARY", result.Content);
            Assert.Contains("==================", result.Content);
            Assert.Contains("SECTION 1: Overview", result.Content);
            Assert.Contains("CONCLUSION", result.Content);
        }

        [Fact]
        public async Task EndToEndWorkflow_ProcessPdfAndCreateBulletin_Success()
        {
            // Arrange
            var processor = new PdfDocumentProcessor(_pdfLoggerMock.Object, _ocrLoggerMock.Object);
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.anomali.com")
            };
            
            var config = new ApiConfiguration
            {
                BaseUrl = "https://test.anomali.com/api/v2",
                Username = "testuser",
                ApiKey = "testkey",
                TimeoutSeconds = 30
            };
            
            var apiClient = new AnomaliApiClient(_apiLoggerMock.Object, httpClient, config);
            
            var processingOptions = new ProcessingOptions
            {
                ExtractTextContent = true,
                ExtractMetadata = true,
                EnableOcr = false // Disable OCR for test performance
            };

            // Act - Process Document
            var document = await processor.ProcessAsync(_testPdfPath, processingOptions);

            // Assert - Document Processing
            Assert.NotNull(document);
            Assert.Equal(DocumentStatus.Completed, document.Status);
            Assert.NotEmpty(document.FileName);
            Assert.NotEmpty(document.FileType);
            Assert.True(document.FileSizeBytes > 0);

            // Act - Create Threat Bulletin
            var bulletin = new ThreatBulletin
            {
                Name = $"Test Bulletin - {document.FileName}",
                Body = document.ExtractedText ?? "Test content",
                Status = BulletinStatus.Published,
                Tlp = TlpDesignation.Amber
            };

            var createdBulletin = await apiClient.CreateThreatBulletinAsync(bulletin);

            // Assert - Bulletin Creation
            Assert.NotNull(createdBulletin);
            Assert.NotEmpty(createdBulletin.Id);
            Assert.Equal(bulletin.Name, createdBulletin.Name);
        }

        [Fact]
        public async Task SecurityService_ConfigurationPersistence_Success()
        {
            // Arrange
            var credentialPath = Path.Combine(_testDirectory, "test_creds.dat");
            var securityService = new WindowsSecurityService(_securityLoggerMock.Object, credentialPath);
            
            var originalConfig = new ApiConfiguration
            {
                BaseUrl = "https://test.anomali.com/api/v2",
                Username = "integrationtest",
                ApiKey = "integration-api-key-12345",
                TimeoutSeconds = 45,
                MaxRetryAttempts = 3,
                UseProxy = false
            };

            // Act - Save Configuration
            await securityService.SaveApiConfigurationAsync(originalConfig);

            // Assert - File Created
            Assert.True(File.Exists(credentialPath));

            // Act - Load Configuration
            var loadedConfig = await securityService.LoadApiConfigurationAsync();

            // Assert - Configuration Integrity
            Assert.NotNull(loadedConfig);
            Assert.Equal(originalConfig.BaseUrl, loadedConfig.BaseUrl);
            Assert.Equal(originalConfig.Username, loadedConfig.Username);
            Assert.Equal(originalConfig.ApiKey, loadedConfig.ApiKey);
            Assert.Equal(originalConfig.TimeoutSeconds, loadedConfig.TimeoutSeconds);
            Assert.Equal(originalConfig.MaxRetryAttempts, loadedConfig.MaxRetryAttempts);
            Assert.Equal(originalConfig.UseProxy, loadedConfig.UseProxy);

            // Act - Validate Credentials
            var isValid = await securityService.ValidateStoredCredentialsAsync();

            // Assert - Validation Success
            Assert.True(isValid);
        }

        [Fact]
        public async Task ApiClient_ConnectionTest_WithValidConfiguration_Success()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.anomali.com")
            };
            
            var config = new ApiConfiguration
            {
                BaseUrl = "https://test.anomali.com/api/v2",
                Username = "testuser",
                ApiKey = "testkey"
            };
            
            var apiClient = new AnomaliApiClient(_apiLoggerMock.Object, httpClient, config);

            // Act
            var connectionResult = await apiClient.TestConnectionAsync();

            // Assert
            Assert.True(connectionResult);
        }

        [Fact]
        public async Task ApiClient_CreateBulletin_WithValidData_Success()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.anomali.com")
            };
            
            var config = new ApiConfiguration
            {
                BaseUrl = "https://test.anomali.com/api/v2",
                Username = "testuser",
                ApiKey = "testkey"
            };
            
            var apiClient = new AnomaliApiClient(_apiLoggerMock.Object, httpClient, config);
            
            var bulletin = new ThreatBulletin
            {
                Name = "Integration Test Bulletin",
                Body = "This is a test bulletin created during integration testing.",
                Status = BulletinStatus.Published,
                Tlp = TlpDesignation.Green,
                Source = "Integration Test",
                Confidence = 75
            };

            // Act
            var result = await apiClient.CreateThreatBulletinAsync(bulletin);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test-bulletin-id-123", result.Id);
            Assert.Equal(bulletin.Name, result.Name);
            Assert.Equal(bulletin.Body, result.Body);
        }

        [Fact]
        public async Task DocumentProcessor_MultipleFormats_ProcessingSuccess()
        {
            // Arrange
            var processor = new PdfDocumentProcessor(_pdfLoggerMock.Object, _ocrLoggerMock.Object);
            var options = new ProcessingOptions
            {
                ExtractTextContent = true,
                ExtractMetadata = true,
                MaxFileSizeMB = 50
            };

            // Act & Assert - PDF Processing
            var pdfResult = await processor.ProcessAsync(_testPdfPath, options);
            Assert.Equal(DocumentStatus.Completed, pdfResult.Status);
            Assert.Equal("PDF", pdfResult.FileType);
            Assert.True(pdfResult.ProcessingDuration.TotalMilliseconds > 0);
        }

        [Fact]
        public async Task EncryptionDecryption_DataIntegrity_Success()
        {
            // Arrange
            var securityService = new WindowsSecurityService(_securityLoggerMock.Object);
            var sensitiveData = "This is highly sensitive API key data: sk_live_1234567890abcdef";

            // Act
            var encrypted = await securityService.EncryptAsync(sensitiveData);
            var decrypted = await securityService.DecryptAsync(encrypted);

            // Assert
            Assert.Equal(sensitiveData, decrypted);
            Assert.NotEqual(sensitiveData, encrypted);
            Assert.NotEmpty(encrypted);
        }

        [Fact]
        public async Task ApiClient_AttachmentUpload_Success()
        {
            // Arrange
            var mockHttpMessageHandler = CreateMockHttpMessageHandler();
            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://test.anomali.com")
            };
            
            var config = new ApiConfiguration
            {
                BaseUrl = "https://test.anomali.com/api/v2",
                Username = "testuser",
                ApiKey = "testkey"
            };
            
            var apiClient = new AnomaliApiClient(_apiLoggerMock.Object, httpClient, config);

            // Act
            var uploadResult = await apiClient.UploadAttachmentAsync("test-bulletin-123", _testPdfPath, "test-attachment.pdf");

            // Assert
            Assert.True(uploadResult);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ApiConfiguration_Validation_InvalidInputs_Fails(string invalidBaseUrl)
        {
            // Arrange
            var config = new ApiConfiguration
            {
                BaseUrl = invalidBaseUrl,
                Username = "testuser",
                ApiKey = "testkey"
            };

            // Act & Assert
            Assert.False(config.IsValid());
        }

        [Fact]
        public async Task ProcessingOptions_Validation_Success()
        {
            // Arrange
            var validOptions = new ProcessingOptions
            {
                MaxFileSizeMB = 50,
                OcrMinConfidence = 0.8,
                ProcessingTimeoutSeconds = 120
            };

            var invalidOptions = new ProcessingOptions
            {
                MaxFileSizeMB = -1, // Invalid
                OcrMinConfidence = 1.5, // Invalid
                ProcessingTimeoutSeconds = 0 // Invalid
            };

            // Act & Assert
            Assert.True(validOptions.IsValid());
            Assert.False(invalidOptions.IsValid());
        }

        private Mock<HttpMessageHandler> CreateMockHttpMessageHandler()
        {
            var mockHandler = new Mock<HttpMessageHandler>();
            
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    
                    if (request.RequestUri.AbsolutePath.Contains("/intelligence/"))
                    {
                        if (request.Method == HttpMethod.Get)
                        {
                            // Mock intelligence list response for connection test
                            response.Content = new StringContent("{\"results\":[]}", Encoding.UTF8, "application/json");
                        }
                        else if (request.Method == HttpMethod.Post)
                        {
                            // Mock bulletin creation response
                            var mockBulletin = new ThreatBulletin
                            {
                                Id = "test-bulletin-id-123",
                                Name = "Integration Test Bulletin",
                                Body = "This is a test bulletin created during integration testing.",
                                Status = BulletinStatus.Published,
                                CreatedAt = DateTime.UtcNow
                            };
                            
                            var json = JsonSerializer.Serialize(mockBulletin, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                            
                            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
                        }
                    }
                    else if (request.RequestUri.AbsolutePath.Contains("/attachments/"))
                    {
                        // Mock attachment upload response
                        response.Content = new StringContent("{\"success\":true}", Encoding.UTF8, "application/json");
                    }
                    
                    return response;
                });

            return mockHandler;
        }

        private void CreateTestPdf(string filePath)
        {
            // Create a minimal valid PDF file for testing
            var pdfContent = new byte[]
            {
                0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 0x0A, // %PDF-1.4
                0x25, 0xC4, 0xE5, 0xF2, 0xE5, 0xEB, 0xA7, 0xF3, 0xA0, 0xD0, 0xC4, 0xC6, 0x0A, // Binary comment
                0x31, 0x20, 0x30, 0x20, 0x6F, 0x62, 0x6A, 0x0A, // 1 0 obj
                0x3C, 0x3C, 0x0A, // <<
                0x2F, 0x54, 0x79, 0x70, 0x65, 0x20, 0x2F, 0x43, 0x61, 0x74, 0x61, 0x6C, 0x6F, 0x67, 0x0A, // /Type /Catalog
                0x2F, 0x50, 0x61, 0x67, 0x65, 0x73, 0x20, 0x32, 0x20, 0x30, 0x20, 0x52, 0x0A, // /Pages 2 0 R
                0x3E, 0x3E, 0x0A, // >>
                0x65, 0x6E, 0x64, 0x6F, 0x62, 0x6A, 0x0A, // endobj
                0x0A, // newline
                // Add more minimal PDF structure...
                0x25, 0x25, 0x45, 0x4F, 0x46 // %%EOF
            };
            
            File.WriteAllBytes(filePath, pdfContent);
        }
    }
} 
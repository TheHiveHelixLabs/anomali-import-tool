using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Models;
using AnomaliImportTool.Infrastructure.ApiClient;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnomaliImportTool.Tests.Unit
{
    public class AnomaliApiClientTests : IDisposable
    {
        private readonly Mock<ILogger<AnomaliApiClient>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly ApiConfiguration _validConfiguration;
        private readonly AnomaliApiClient _apiClient;

        public AnomaliApiClientTests()
        {
            _loggerMock = new Mock<ILogger<AnomaliApiClient>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://test.anomali.com")
            };

            _validConfiguration = new ApiConfiguration
            {
                BaseUrl = "https://test.anomali.com/api/v2",
                Username = "testuser",
                ApiKey = "testkey123",
                TimeoutSeconds = 30,
                MaxRetryAttempts = 3
            };

            _apiClient = new AnomaliApiClient(_loggerMock.Object, _httpClient, _validConfiguration);
        }

        [Fact]
        public void Constructor_WithValidParameters_Succeeds()
        {
            // Assert - Constructor should not throw
            Assert.NotNull(_apiClient);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AnomaliApiClient(null, _httpClient, _validConfiguration));
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AnomaliApiClient(_loggerMock.Object, null, _validConfiguration));
        }

        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AnomaliApiClient(_loggerMock.Object, _httpClient, null));
        }

        [Fact]
        public void Constructor_WithInvalidConfiguration_ThrowsArgumentException()
        {
            // Arrange
            var invalidConfig = new ApiConfiguration
            {
                BaseUrl = "", // Invalid
                Username = "test",
                ApiKey = "key"
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new AnomaliApiClient(_loggerMock.Object, _httpClient, invalidConfig));
        }

        [Fact]
        public async Task TestConnectionAsync_WithSuccessfulResponse_ReturnsTrue()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.OK, "{\"results\":[]}");

            // Act
            var result = await _apiClient.TestConnectionAsync();

            // Assert
            Assert.True(result);
            VerifyHttpRequest(HttpMethod.Get, "/api/v2/intelligence/");
        }

        [Fact]
        public async Task TestConnectionAsync_WithFailedResponse_ReturnsFalse()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");

            // Act
            var result = await _apiClient.TestConnectionAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestConnectionAsync_WithException_ReturnsFalse()
        {
            // Arrange
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _apiClient.TestConnectionAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CreateThreatBulletinAsync_WithValidBulletin_ReturnsCreatedBulletin()
        {
            // Arrange
            var bulletin = new ThreatBulletin
            {
                Name = "Test Bulletin",
                Body = "Test content",
                Status = BulletinStatus.Published,
                Tlp = TlpDesignation.Amber
            };

            var responseBody = JsonSerializer.Serialize(new ThreatBulletin
            {
                Id = "12345",
                Name = bulletin.Name,
                Body = bulletin.Body,
                Status = bulletin.Status,
                Tlp = bulletin.Tlp,
                CreatedAt = DateTime.UtcNow
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            SetupHttpResponse(HttpStatusCode.OK, responseBody);

            // Act
            var result = await _apiClient.CreateThreatBulletinAsync(bulletin);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("12345", result.Id);
            Assert.Equal(bulletin.Name, result.Name);
            Assert.Equal(bulletin.Body, result.Body);
            VerifyHttpRequest(HttpMethod.Post, "/api/v2/intelligence/");
        }

        [Fact]
        public async Task CreateThreatBulletinAsync_WithNullBulletin_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _apiClient.CreateThreatBulletinAsync(null));
        }

        [Fact]
        public async Task CreateThreatBulletinAsync_WithHttpError_ThrowsInvalidOperationException()
        {
            // Arrange
            var bulletin = new ThreatBulletin
            {
                Name = "Test Bulletin",
                Body = "Test content"
            };

            SetupHttpResponse(HttpStatusCode.BadRequest, "Bad Request");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _apiClient.CreateThreatBulletinAsync(bulletin));
        }

        [Fact]
        public async Task UploadAttachmentAsync_WithValidParameters_ReturnsTrue()
        {
            // Arrange
            var testFilePath = CreateTestFile("test.pdf", "PDF content");
            SetupHttpResponse(HttpStatusCode.OK, "{\"success\":true}");

            // Act
            var result = await _apiClient.UploadAttachmentAsync("bulletin123", testFilePath, "test.pdf");

            // Assert
            Assert.True(result);
            VerifyHttpRequest(HttpMethod.Post, "/api/v2/intelligence/bulletin123/attachments/");
        }

        [Fact]
        public async Task UploadAttachmentAsync_WithInvalidBulletinId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _apiClient.UploadAttachmentAsync("", "test.pdf"));
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _apiClient.UploadAttachmentAsync(null, "test.pdf"));
        }

        [Fact]
        public async Task UploadAttachmentAsync_WithInvalidFilePath_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _apiClient.UploadAttachmentAsync("bulletin123", ""));
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _apiClient.UploadAttachmentAsync("bulletin123", null));
        }

        [Fact]
        public async Task UploadAttachmentAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<System.IO.FileNotFoundException>(() => 
                _apiClient.UploadAttachmentAsync("bulletin123", "nonexistent.pdf"));
        }

        [Fact]
        public async Task UploadAttachmentAsync_WithHttpError_ReturnsFalse()
        {
            // Arrange
            var testFilePath = CreateTestFile("test.pdf", "PDF content");
            SetupHttpResponse(HttpStatusCode.BadRequest, "Bad Request");

            // Act
            var result = await _apiClient.UploadAttachmentAsync("bulletin123", testFilePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetThreatBulletinAsync_WithValidId_ReturnsBulletin()
        {
            // Arrange
            var bulletinId = "test-bulletin-123";
            var responseBody = JsonSerializer.Serialize(new ThreatBulletin
            {
                Id = bulletinId,
                Name = "Test Bulletin",
                Body = "Test content",
                Status = BulletinStatus.Published,
                Tlp = TlpDesignation.Green
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            SetupHttpResponse(HttpStatusCode.OK, responseBody);

            // Act
            var result = await _apiClient.GetThreatBulletinAsync(bulletinId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(bulletinId, result.Id);
            Assert.Equal("Test Bulletin", result.Name);
            VerifyHttpRequest(HttpMethod.Get, $"/api/v2/intelligence/{bulletinId}/");
        }

        [Fact]
        public async Task GetThreatBulletinAsync_WithNotFoundResponse_ReturnsNull()
        {
            // Arrange
            SetupHttpResponse(HttpStatusCode.NotFound, "Not Found");

            // Act
            var result = await _apiClient.GetThreatBulletinAsync("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task GetThreatBulletinAsync_WithInvalidId_ThrowsArgumentException(string bulletinId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _apiClient.GetThreatBulletinAsync(bulletinId));
        }

        [Fact]
        public async Task CreateThreatBulletinAsync_WithRetryPolicy_RetriesOnFailure()
        {
            // Arrange
            var bulletin = new ThreatBulletin
            {
                Name = "Test Bulletin",
                Body = "Test content"
            };

            var callCount = 0;
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    callCount++;
                    if (callCount < 3)
                    {
                        // Fail first 2 attempts
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
                    }
                    
                    // Succeed on 3rd attempt
                    var responseBody = JsonSerializer.Serialize(new ThreatBulletin
                    {
                        Id = "12345",
                        Name = bulletin.Name,
                        Body = bulletin.Body
                    }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                    });
                });

            // Act
            var result = await _apiClient.CreateThreatBulletinAsync(bulletin);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("12345", result.Id);
            Assert.Equal(3, callCount); // Should have retried
        }

        [Fact]
        public async Task CancelOperation_DuringApiCall_ThrowsOperationCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var bulletin = new ThreatBulletin { Name = "Test", Body = "Test" };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, token) =>
                {
                    await Task.Delay(1000, token); // Simulate slow operation
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });

            // Act & Assert
            cts.Cancel(); // Cancel immediately
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _apiClient.CreateThreatBulletinAsync(bulletin, cts.Token));
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                });
        }

        private void VerifyHttpRequest(HttpMethod method, string requestUri)
        {
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == method && 
                    req.RequestUri.ToString().Contains(requestUri)),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        private string CreateTestFile(string fileName, string content)
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
            System.IO.File.WriteAllText(tempPath, content);
            return tempPath;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _apiClient?.Dispose();
        }
    }
}
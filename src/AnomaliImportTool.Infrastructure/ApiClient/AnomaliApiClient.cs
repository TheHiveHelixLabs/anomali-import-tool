using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace AnomaliImportTool.Infrastructure.ApiClient
{
    /// <summary>
    /// Client for interacting with the Anomali ThreatStream API.
    /// </summary>
    public class AnomaliApiClient : IAnomaliApiClient, IDisposable
    {
        private readonly ILogger<AnomaliApiClient> _logger;
        private readonly HttpClient _httpClient;
        private readonly ApiConfiguration _configuration;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private bool _disposed;

        public AnomaliApiClient(
            ILogger<AnomaliApiClient> logger,
            HttpClient httpClient,
            ApiConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            if (!_configuration.IsValid())
            {
                throw new ArgumentException("Invalid API configuration provided.", nameof(configuration));
            }

            ConfigureHttpClient();
            _retryPolicy = CreateRetryPolicy();
        }

        /// <summary>
        /// Tests the connection to the Anomali API.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the connection is successful; otherwise, false.</returns>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Testing connection to Anomali API at {BaseUrl}", _configuration.BaseUrl);

                // Make a simple API call to verify connectivity and authentication
                var request = new HttpRequestMessage(HttpMethod.Get, "/api/v2/intelligence/");
                request.Headers.Add("Limit", "1"); // Just get one item to test

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.SendAsync(request, cancellationToken));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully connected to Anomali API");
                    return true;
                }

                _logger.LogWarning("Failed to connect to Anomali API. Status: {StatusCode}, Reason: {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection to Anomali API");
                return false;
            }
        }

        /// <summary>
        /// Creates a new threat bulletin in Anomali.
        /// </summary>
        /// <param name="bulletin">The threat bulletin to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created threat bulletin with server-assigned ID.</returns>
        public async Task<ThreatBulletin> CreateThreatBulletinAsync(
            ThreatBulletin bulletin, 
            CancellationToken cancellationToken = default)
        {
            if (bulletin == null)
            {
                throw new ArgumentNullException(nameof(bulletin));
            }

            try
            {
                _logger.LogInformation("Creating threat bulletin: {Name}", bulletin.Name);

                var json = JsonSerializer.Serialize(bulletin, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.PostAsync("/api/v2/intelligence/", content, cancellationToken));

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var createdBulletin = JsonSerializer.Deserialize<ThreatBulletin>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Successfully created threat bulletin with ID: {Id}", createdBulletin?.Id);
                return createdBulletin;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error creating threat bulletin");
                throw new InvalidOperationException($"Failed to create threat bulletin: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating threat bulletin");
                throw;
            }
        }

        /// <summary>
        /// Uploads an attachment to a threat bulletin.
        /// </summary>
        /// <param name="bulletinId">The ID of the threat bulletin.</param>
        /// <param name="filePath">The path to the file to upload.</param>
        /// <param name="fileName">The name to give the file in Anomali.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the upload is successful; otherwise, false.</returns>
        public async Task<bool> UploadAttachmentAsync(
            string bulletinId, 
            string filePath, 
            string fileName = null, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(bulletinId))
            {
                throw new ArgumentException("Bulletin ID cannot be null or empty.", nameof(bulletinId));
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }

            try
            {
                fileName ??= Path.GetFileName(filePath);
                _logger.LogInformation("Uploading attachment {FileName} to bulletin {BulletinId}", fileName, bulletinId);

                using (var fileStream = File.OpenRead(filePath))
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    content.Add(fileContent, "file", fileName);

                    var response = await _retryPolicy.ExecuteAsync(async () =>
                        await _httpClient.PostAsync($"/api/v2/intelligence/{bulletinId}/attachments/", content, cancellationToken));

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully uploaded attachment to bulletin {BulletinId}", bulletinId);
                        return true;
                    }

                    _logger.LogWarning("Failed to upload attachment. Status: {StatusCode}, Reason: {ReasonPhrase}",
                        response.StatusCode, response.ReasonPhrase);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading attachment to bulletin {BulletinId}", bulletinId);
                throw;
            }
        }

        /// <summary>
        /// Gets a threat bulletin by ID.
        /// </summary>
        /// <param name="bulletinId">The bulletin ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The threat bulletin, or null if not found.</returns>
        public async Task<ThreatBulletin> GetThreatBulletinAsync(
            string bulletinId, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(bulletinId))
            {
                throw new ArgumentException("Bulletin ID cannot be null or empty.", nameof(bulletinId));
            }

            try
            {
                _logger.LogInformation("Getting threat bulletin {BulletinId}", bulletinId);

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _httpClient.GetAsync($"/api/v2/intelligence/{bulletinId}/", cancellationToken));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var bulletin = JsonSerializer.Deserialize<ThreatBulletin>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    _logger.LogInformation("Successfully retrieved threat bulletin {BulletinId}", bulletinId);
                    return bulletin;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Threat bulletin {BulletinId} not found", bulletinId);
                    return null;
                }

                _logger.LogError("Failed to get threat bulletin. Status: {StatusCode}, Reason: {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting threat bulletin {BulletinId}", bulletinId);
                throw;
            }
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_configuration.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

            // Add authentication headers
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"apikey {_configuration.Username}:{_configuration.ApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Add user agent
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AnomaliImportTool/1.0");

            _logger.LogDebug("Configured HTTP client with base URL: {BaseUrl}", _configuration.BaseUrl);
        }

        private IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    _configuration.MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var request = outcome.Result?.RequestMessage;
                        _logger.LogWarning("Retry {RetryCount} after {Delay}ms for {Method} {Uri}",
                            retryCount, timespan.TotalMilliseconds, request?.Method, request?.RequestUri);
                    });
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
                    _httpClient?.Dispose();
                }

                _disposed = true;
            }
        }
    }
} 
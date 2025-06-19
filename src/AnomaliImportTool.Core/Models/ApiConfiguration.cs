using System;

namespace AnomaliImportTool.Core.Models
{
    /// <summary>
    /// Configuration settings for the Anomali API connection.
    /// </summary>
    public class ApiConfiguration
    {
        /// <summary>
        /// Gets or sets the base URL for the Anomali API endpoint.
        /// Example: "https://your-instance.threatstream.com/api/v2"
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username for API authentication.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key for authentication.
        /// This should be stored securely and not in plain text.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timeout for API calls in seconds.
        /// Default is 30 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed API calls.
        /// Default is 3 retries.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the maximum number of retries for failed API calls.
        /// Alternative property name for compatibility.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay in seconds between retry attempts.
        /// Default is 2 seconds.
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 2;

        /// <summary>
        /// Gets or sets whether to validate SSL certificates.
        /// Default is true for security.
        /// </summary>
        public bool ValidateCertificate { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use a proxy for API connections.
        /// </summary>
        public bool UseProxy { get; set; }

        /// <summary>
        /// Gets or sets the proxy URL if UseProxy is true.
        /// </summary>
        public string ProxyUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether to bypass SSL certificate validation.
        /// Warning: This should only be used for testing purposes.
        /// </summary>
        public bool IgnoreSslErrors { get; set; }

        /// <summary>
        /// Validates the configuration settings.
        /// </summary>
        /// <returns>True if the configuration is valid; otherwise, false.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                return false;

            if (string.IsNullOrWhiteSpace(Username))
                return false;

            if (string.IsNullOrWhiteSpace(ApiKey))
                return false;

            if (TimeoutSeconds <= 0)
                return false;

            if (MaxRetryAttempts < 0)
                return false;

            if (MaxRetries < 0)
                return false;

            if (RetryDelaySeconds < 0)
                return false;

            if (UseProxy && string.IsNullOrWhiteSpace(ProxyUrl))
                return false;

            // Validate BaseUrl is a valid URL
            if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || 
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return false;

            return true;
        }

        /// <summary>
        /// Creates a copy of the configuration with the API key cleared for security.
        /// </summary>
        /// <returns>A sanitized copy of the configuration.</returns>
        public ApiConfiguration GetSanitizedCopy()
        {
            return new ApiConfiguration
            {
                BaseUrl = this.BaseUrl,
                Username = this.Username,
                ApiKey = "***REDACTED***",
                TimeoutSeconds = this.TimeoutSeconds,
                MaxRetryAttempts = this.MaxRetryAttempts,
                MaxRetries = this.MaxRetries,
                RetryDelaySeconds = this.RetryDelaySeconds,
                ValidateCertificate = this.ValidateCertificate,
                UseProxy = this.UseProxy,
                ProxyUrl = this.ProxyUrl,
                IgnoreSslErrors = this.IgnoreSslErrors
            };
        }
    }
} 
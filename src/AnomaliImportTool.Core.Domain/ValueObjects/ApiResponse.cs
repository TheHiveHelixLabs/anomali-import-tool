namespace AnomaliImportTool.Core.Domain.ValueObjects;

/// <summary>
/// API response value object
/// </summary>
public sealed record ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public int StatusCode { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Success(T data, int statusCode = 200, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            StatusCode = statusCode,
            CorrelationId = correlationId
        };
    }

    public static ApiResponse<T> Failure(string errorMessage, int statusCode = 400, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            StatusCode = statusCode,
            CorrelationId = correlationId
        };
    }
} 
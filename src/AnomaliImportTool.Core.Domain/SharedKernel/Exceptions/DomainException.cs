namespace AnomaliImportTool.Core.Domain.SharedKernel.Exceptions;

/// <summary>
/// Exception thrown when domain business rules are violated
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message) : base(message)
    {
        ErrorCode = "DOMAIN_ERROR";
    }

    public DomainException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "DOMAIN_ERROR";
    }

    public DomainException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
} 
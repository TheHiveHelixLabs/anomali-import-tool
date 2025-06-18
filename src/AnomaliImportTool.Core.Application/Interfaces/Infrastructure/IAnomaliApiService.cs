using AnomaliImportTool.Core.Domain.Entities;
using AnomaliImportTool.Core.Domain.ValueObjects;

namespace AnomaliImportTool.Core.Application.Interfaces.Infrastructure;

/// <summary>
/// Interface for Anomali ThreatStream API operations following Clean Architecture dependency inversion
/// </summary>
public interface IAnomaliApiService
{
    Task<ApiResponse<ThreatBulletin>> CreateThreatBulletinAsync(ThreatBulletin threatBulletin, CancellationToken cancellationToken = default);
    Task<ApiResponse<ThreatBulletin>> UpdateThreatBulletinAsync(ThreatBulletin threatBulletin, CancellationToken cancellationToken = default);
    Task<ApiResponse<ThreatBulletin>> GetThreatBulletinAsync(string bulletinId, CancellationToken cancellationToken = default);
    Task<ApiResponse<IEnumerable<ThreatBulletin>>> GetThreatBulletinsAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteThreatBulletinAsync(string bulletinId, CancellationToken cancellationToken = default);
    Task<ApiResponse<string>> UploadFileAttachmentAsync(string bulletinId, string filePath, CancellationToken cancellationToken = default);
    Task<ApiResponse<ImportSession>> ImportObservablesAsync(IEnumerable<Observable> observables, CancellationToken cancellationToken = default);
    Task<bool> ValidateConnectionAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<AuthenticationResult>> AuthenticateAsync(string username, string apiKey, CancellationToken cancellationToken = default);
} 
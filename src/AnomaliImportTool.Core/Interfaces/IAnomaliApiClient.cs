using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.Core.Interfaces;

/// <summary>
/// Interface for communicating with Anomali ThreatStream API
/// </summary>
public interface IAnomaliApiClient
{
    /// <summary>
    /// Tests the connection to the Anomali API
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new threat bulletin in Anomali
    /// </summary>
    /// <param name="bulletin">The threat bulletin to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created threat bulletin with server-assigned ID</returns>
    Task<ThreatBulletin> CreateThreatBulletinAsync(ThreatBulletin bulletin, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Uploads a file attachment to a threat bulletin
    /// </summary>
    /// <param name="bulletinId">The ID of the threat bulletin</param>
    /// <param name="filePath">The path to the file to upload</param>
    /// <param name="fileName">Optional name for the file in Anomali</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if upload is successful</returns>
    Task<bool> UploadAttachmentAsync(string bulletinId, string filePath, string fileName = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a threat bulletin by ID
    /// </summary>
    /// <param name="bulletinId">The bulletin ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The threat bulletin, or null if not found</returns>
    Task<ThreatBulletin> GetThreatBulletinAsync(string bulletinId, CancellationToken cancellationToken = default);
} 
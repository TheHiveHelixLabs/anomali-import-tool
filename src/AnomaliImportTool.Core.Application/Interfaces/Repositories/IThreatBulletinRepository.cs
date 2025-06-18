using AnomaliImportTool.Core.Domain.Entities;

namespace AnomaliImportTool.Core.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for threat bulletin operations following Clean Architecture dependency inversion
/// </summary>
public interface IThreatBulletinRepository
{
    Task<IEnumerable<ThreatBulletin>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ThreatBulletin?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ThreatBulletin> AddAsync(ThreatBulletin threatBulletin, CancellationToken cancellationToken = default);
    Task<ThreatBulletin> UpdateAsync(ThreatBulletin threatBulletin, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ThreatBulletin>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<ThreatBulletin>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
} 
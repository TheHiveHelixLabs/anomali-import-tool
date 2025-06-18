using AnomaliImportTool.Core.Domain.Entities;

namespace AnomaliImportTool.Core.Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for document operations following Clean Architecture dependency inversion
/// </summary>
public interface IDocumentRepository
{
    Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetByBatchIdAsync(Guid batchId, CancellationToken cancellationToken = default);
} 
using AnomaliImportTool.Core.Domain.ValueObjects;

namespace AnomaliImportTool.Core.Application.Interfaces.Services;

/// <summary>
/// Interface for notification operations following Clean Architecture dependency inversion
/// </summary>
public interface INotificationService
{
    Task SendNotificationAsync(NotificationMessage message, CancellationToken cancellationToken = default);
    Task SendProgressUpdateAsync(ProgressUpdate update, CancellationToken cancellationToken = default);
    Task SendErrorNotificationAsync(ErrorNotification error, CancellationToken cancellationToken = default);
    Task SendSuccessNotificationAsync(SuccessNotification success, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationMessage>> GetNotificationHistoryAsync(CancellationToken cancellationToken = default);
    Task ClearNotificationsAsync(CancellationToken cancellationToken = default);
    event EventHandler<NotificationEventArgs>? NotificationReceived;
} 
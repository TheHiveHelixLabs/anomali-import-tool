using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.WinUI.Services
{
    /// <summary>
    /// Undo/Redo service interface for critical operations
    /// </summary>
    public interface IUndoRedoService
    {
        /// <summary>
        /// Initialize undo/redo service
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Execute an undoable action
        /// </summary>
        Task<bool> ExecuteAsync(IUndoableAction action);

        /// <summary>
        /// Undo the last action
        /// </summary>
        Task<bool> UndoAsync();

        /// <summary>
        /// Redo the last undone action
        /// </summary>
        Task<bool> RedoAsync();

        /// <summary>
        /// Clear all undo/redo history
        /// </summary>
        void ClearHistory();

        /// <summary>
        /// Create a checkpoint for batch operations
        /// </summary>
        string CreateCheckpoint(string description);

        /// <summary>
        /// Restore to a specific checkpoint
        /// </summary>
        Task<bool> RestoreToCheckpointAsync(string checkpointId);

        /// <summary>
        /// Start a batch operation
        /// </summary>
        void BeginBatch(string description);

        /// <summary>
        /// End a batch operation
        /// </summary>
        void EndBatch();

        /// <summary>
        /// Cancel current batch operation
        /// </summary>
        void CancelBatch();

        /// <summary>
        /// Check if undo is available
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Check if redo is available
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// Get description of next undo action
        /// </summary>
        string NextUndoDescription { get; }

        /// <summary>
        /// Get description of next redo action
        /// </summary>
        string NextRedoDescription { get; }

        /// <summary>
        /// Get current history count
        /// </summary>
        int HistoryCount { get; }

        /// <summary>
        /// Get available checkpoints
        /// </summary>
        IReadOnlyList<UndoCheckpoint> Checkpoints { get; }

        /// <summary>
        /// Observable for undo/redo events
        /// </summary>
        IObservable<UndoRedoEvent> UndoRedoEvents { get; }

        /// <summary>
        /// Set maximum history size
        /// </summary>
        void SetMaxHistorySize(int maxSize);

        /// <summary>
        /// Enable or disable undo/redo functionality
        /// </summary>
        void SetEnabled(bool enabled);
    }

    /// <summary>
    /// Interface for undoable actions
    /// </summary>
    public interface IUndoableAction
    {
        /// <summary>
        /// Action identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Action description for UI display
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Action category
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Action priority
        /// </summary>
        UndoActionPriority Priority { get; }

        /// <summary>
        /// Execute the action
        /// </summary>
        Task<bool> ExecuteAsync();

        /// <summary>
        /// Undo the action
        /// </summary>
        Task<bool> UndoAsync();

        /// <summary>
        /// Check if action can be undone
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Timestamp when action was executed
        /// </summary>
        DateTime Timestamp { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        Dictionary<string, object> Metadata { get; }
    }

    /// <summary>
    /// Action priority levels
    /// </summary>
    public enum UndoActionPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Undo/Redo event data
    /// </summary>
    public class UndoRedoEvent
    {
        public string EventType { get; set; }
        public string ActionId { get; set; }
        public string ActionDescription { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Undo checkpoint information
    /// </summary>
    public class UndoCheckpoint
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public int HistoryIndex { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Batch operation context
    /// </summary>
    public class BatchOperation
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public List<IUndoableAction> Actions { get; set; } = new List<IUndoableAction>();
        public DateTime StartTime { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Base implementation of undoable action
    /// </summary>
    public abstract class UndoableActionBase : IUndoableAction
    {
        public string Id { get; protected set; } = Guid.NewGuid().ToString();
        public string Description { get; protected set; }
        public string Category { get; protected set; } = "General";
        public UndoActionPriority Priority { get; protected set; } = UndoActionPriority.Normal;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        public abstract bool CanUndo { get; }
        public abstract Task<bool> ExecuteAsync();
        public abstract Task<bool> UndoAsync();
    }

    /// <summary>
    /// Batch undoable action that groups multiple actions
    /// </summary>
    public class BatchUndoableAction : UndoableActionBase
    {
        private readonly List<IUndoableAction> _actions;

        public BatchUndoableAction(string description, List<IUndoableAction> actions)
        {
            Description = description;
            Category = "Batch";
            _actions = actions ?? new List<IUndoableAction>();
        }

        public override bool CanUndo => _actions.All(a => a.CanUndo);

        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                foreach (var action in _actions)
                {
                    var success = await action.ExecuteAsync();
                    if (!success)
                    {
                        // Rollback previously executed actions
                        await UndoExecutedActionsAsync();
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                await UndoExecutedActionsAsync();
                throw;
            }
        }

        public override async Task<bool> UndoAsync()
        {
            try
            {
                // Undo in reverse order
                for (int i = _actions.Count - 1; i >= 0; i--)
                {
                    var success = await _actions[i].UndoAsync();
                    if (!success)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task UndoExecutedActionsAsync()
        {
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                try
                {
                    await _actions[i].UndoAsync();
                }
                catch
                {
                    // Continue with other actions
                }
            }
        }
    }

    /// <summary>
    /// Professional undo/redo service with comprehensive functionality
    /// </summary>
    public class UndoRedoService : IUndoRedoService, IDisposable
    {
        private readonly ILogger<UndoRedoService> _logger;
        private readonly IAccessibilityService _accessibilityService;
        private readonly IAudioFeedbackService _audioFeedbackService;

        private readonly List<IUndoableAction> _undoHistory;
        private readonly List<IUndoableAction> _redoHistory;
        private readonly List<UndoCheckpoint> _checkpoints;
        private readonly Subject<UndoRedoEvent> _undoRedoEvents;

        private BatchOperation _currentBatch;
        private bool _isEnabled = true;
        private bool _isInitialized = false;
        private int _maxHistorySize = 100;
        private int _currentHistoryIndex = -1;

        // Undo/Redo constants
        private const int DEFAULT_MAX_HISTORY_SIZE = 100;
        private const int MAX_CHECKPOINTS = 20;

        public UndoRedoService(
            ILogger<UndoRedoService> logger,
            IAccessibilityService accessibilityService,
            IAudioFeedbackService audioFeedbackService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));
            _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));

            _undoHistory = new List<IUndoableAction>();
            _redoHistory = new List<IUndoableAction>();
            _checkpoints = new List<UndoCheckpoint>();
            _undoRedoEvents = new Subject<UndoRedoEvent>();

            _maxHistorySize = DEFAULT_MAX_HISTORY_SIZE;

            _logger.LogInformation("UndoRedoService initialized");
        }

        public IObservable<UndoRedoEvent> UndoRedoEvents => _undoRedoEvents.AsObservable();

        public bool CanUndo => _isEnabled && _undoHistory.Count > 0;
        public bool CanRedo => _isEnabled && _redoHistory.Count > 0;

        public string NextUndoDescription => CanUndo ? _undoHistory.Last().Description : string.Empty;
        public string NextRedoDescription => CanRedo ? _redoHistory.Last().Description : string.Empty;

        public int HistoryCount => _undoHistory.Count + _redoHistory.Count;

        public IReadOnlyList<UndoCheckpoint> Checkpoints => _checkpoints.AsReadOnly();

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing undo/redo service");

                _isInitialized = true;

                _logger.LogInformation("Undo/redo service initialized with max history size: {MaxSize}", _maxHistorySize);

                await _accessibilityService.AnnounceAsync(
                    "Undo/redo system ready", 
                    AccessibilityAnnouncementPriority.Low);

                _undoRedoEvents.OnNext(new UndoRedoEvent
                {
                    EventType = "ServiceInitialized",
                    Data = new Dictionary<string, object> { ["MaxHistorySize"] = _maxHistorySize }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize undo/redo service");
                throw;
            }
        }

        public async Task<bool> ExecuteAsync(IUndoableAction action)
        {
            try
            {
                if (!_isEnabled || !_isInitialized || action == null)
                {
                    return false;
                }

                _logger.LogDebug("Executing undoable action: {ActionId} - {Description}", action.Id, action.Description);

                // Execute the action
                action.Timestamp = DateTime.UtcNow;
                var success = await action.ExecuteAsync();

                if (success)
                {
                    // Add to current batch or history
                    if (_currentBatch != null && _currentBatch.IsActive)
                    {
                        _currentBatch.Actions.Add(action);
                    }
                    else
                    {
                        AddToHistory(action);
                    }

                    // Play success sound
                    await _audioFeedbackService.PlaySuccessSoundAsync();

                    // Fire success event
                    _undoRedoEvents.OnNext(new UndoRedoEvent
                    {
                        EventType = "ActionExecuted",
                        ActionId = action.Id,
                        ActionDescription = action.Description,
                        Success = true
                    });

                    _logger.LogDebug("Action executed successfully: {ActionId}", action.Id);
                }
                else
                {
                    // Play error sound
                    await _audioFeedbackService.PlayErrorSoundAsync();

                    // Fire failure event
                    _undoRedoEvents.OnNext(new UndoRedoEvent
                    {
                        EventType = "ActionExecuted",
                        ActionId = action.Id,
                        ActionDescription = action.Description,
                        Success = false
                    });

                    _logger.LogWarning("Action execution failed: {ActionId}", action.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute action: {ActionId}", action?.Id);
                
                await _audioFeedbackService.PlayErrorSoundAsync();

                _undoRedoEvents.OnNext(new UndoRedoEvent
                {
                    EventType = "ActionExecuted",
                    ActionId = action?.Id,
                    ActionDescription = action?.Description,
                    Success = false,
                    ErrorMessage = ex.Message
                });

                return false;
            }
        }

        public async Task<bool> UndoAsync()
        {
            try
            {
                if (!CanUndo)
                {
                    _logger.LogDebug("Cannot undo - no actions in history or service disabled");
                    return false;
                }

                var action = _undoHistory.Last();
                _logger.LogInformation("Undoing action: {ActionId} - {Description}", action.Id, action.Description);

                var success = await action.UndoAsync();

                if (success)
                {
                    // Move action from undo to redo history
                    _undoHistory.RemoveAt(_undoHistory.Count - 1);
                    _redoHistory.Add(action);

                    // Trim redo history if needed
                    TrimRedoHistory();

                    // Play undo sound
                    await _audioFeedbackService.PlayNavigationSoundAsync();

                    // Announce undo
                    await _accessibilityService.AnnounceAsync(
                        $"Undone: {action.Description}", 
                        AccessibilityAnnouncementPriority.Medium);

                    // Fire undo event
                    _undoRedoEvents.OnNext(new UndoRedoEvent
                    {
                        EventType = "ActionUndone",
                        ActionId = action.Id,
                        ActionDescription = action.Description,
                        Success = true
                    });

                    _logger.LogInformation("Action undone successfully: {ActionId}", action.Id);
                }
                else
                {
                    await _audioFeedbackService.PlayErrorSoundAsync();

                    _undoRedoEvents.OnNext(new UndoRedoEvent
                    {
                        EventType = "ActionUndone",
                        ActionId = action.Id,
                        ActionDescription = action.Description,
                        Success = false
                    });

                    _logger.LogWarning("Failed to undo action: {ActionId}", action.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to undo action");
                await _audioFeedbackService.PlayErrorSoundAsync();
                return false;
            }
        }

        public async Task<bool> RedoAsync()
        {
            try
            {
                if (!CanRedo)
                {
                    _logger.LogDebug("Cannot redo - no actions in redo history or service disabled");
                    return false;
                }

                var action = _redoHistory.Last();
                _logger.LogInformation("Redoing action: {ActionId} - {Description}", action.Id, action.Description);

                var success = await action.ExecuteAsync();

                if (success)
                {
                    // Move action from redo to undo history
                    _redoHistory.RemoveAt(_redoHistory.Count - 1);
                    _undoHistory.Add(action);

                    // Trim undo history if needed
                    TrimUndoHistory();

                    // Play redo sound
                    await _audioFeedbackService.PlayNavigationSoundAsync();

                    // Announce redo
                    await _accessibilityService.AnnounceAsync(
                        $"Redone: {action.Description}", 
                        AccessibilityAnnouncementPriority.Medium);

                    // Fire redo event
                    _undoRedoEvents.OnNext(new UndoRedoEvent
                    {
                        EventType = "ActionRedone",
                        ActionId = action.Id,
                        ActionDescription = action.Description,
                        Success = true
                    });

                    _logger.LogInformation("Action redone successfully: {ActionId}", action.Id);
                }
                else
                {
                    await _audioFeedbackService.PlayErrorSoundAsync();

                    _undoRedoEvents.OnNext(new UndoRedoEvent
                    {
                        EventType = "ActionRedone",
                        ActionId = action.Id,
                        ActionDescription = action.Description,
                        Success = false
                    });

                    _logger.LogWarning("Failed to redo action: {ActionId}", action.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to redo action");
                await _audioFeedbackService.PlayErrorSoundAsync();
                return false;
            }
        }

        public void ClearHistory()
        {
            try
            {
                _logger.LogInformation("Clearing undo/redo history");

                var totalCount = _undoHistory.Count + _redoHistory.Count;

                _undoHistory.Clear();
                _redoHistory.Clear();
                _checkpoints.Clear();
                _currentHistoryIndex = -1;

                _undoRedoEvents.OnNext(new UndoRedoEvent
                {
                    EventType = "HistoryCleared",
                    Data = new Dictionary<string, object> { ["ClearedCount"] = totalCount }
                });

                _logger.LogInformation("Cleared {Count} actions from history", totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear history");
            }
        }

        public string CreateCheckpoint(string description)
        {
            try
            {
                var checkpoint = new UndoCheckpoint
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = description,
                    Timestamp = DateTime.UtcNow,
                    HistoryIndex = _undoHistory.Count
                };

                _checkpoints.Add(checkpoint);

                // Trim checkpoints if needed
                if (_checkpoints.Count > MAX_CHECKPOINTS)
                {
                    _checkpoints.RemoveAt(0);
                }

                _logger.LogInformation("Created checkpoint: {CheckpointId} - {Description}", checkpoint.Id, description);

                _undoRedoEvents.OnNext(new UndoRedoEvent
                {
                    EventType = "CheckpointCreated",
                    ActionId = checkpoint.Id,
                    ActionDescription = description
                });

                return checkpoint.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create checkpoint");
                return null;
            }
        }

        public async Task<bool> RestoreToCheckpointAsync(string checkpointId)
        {
            try
            {
                var checkpoint = _checkpoints.FirstOrDefault(c => c.Id == checkpointId);
                if (checkpoint == null)
                {
                    _logger.LogWarning("Checkpoint not found: {CheckpointId}", checkpointId);
                    return false;
                }

                _logger.LogInformation("Restoring to checkpoint: {CheckpointId} - {Description}", 
                    checkpointId, checkpoint.Description);

                // Undo actions until we reach the checkpoint
                while (_undoHistory.Count > checkpoint.HistoryIndex && CanUndo)
                {
                    var success = await UndoAsync();
                    if (!success)
                    {
                        _logger.LogWarning("Failed to undo action during checkpoint restoration");
                        return false;
                    }
                }

                // Announce restoration
                await _accessibilityService.AnnounceAsync(
                    $"Restored to checkpoint: {checkpoint.Description}", 
                    AccessibilityAnnouncementPriority.High);

                _undoRedoEvents.OnNext(new UndoRedoEvent
                {
                    EventType = "CheckpointRestored",
                    ActionId = checkpointId,
                    ActionDescription = checkpoint.Description,
                    Success = true
                });

                _logger.LogInformation("Successfully restored to checkpoint: {CheckpointId}", checkpointId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore to checkpoint: {CheckpointId}", checkpointId);
                return false;
            }
        }

        public void BeginBatch(string description)
        {
            try
            {
                if (_currentBatch != null && _currentBatch.IsActive)
                {
                    _logger.LogWarning("Cannot begin batch - another batch is already active");
                    return;
                }

                _currentBatch = new BatchOperation
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = description,
                    StartTime = DateTime.UtcNow,
                    IsActive = true
                };

                _logger.LogInformation("Started batch operation: {BatchId} - {Description}", _currentBatch.Id, description);

                _undoRedoEvents.OnNext(new UndoRedoEvent
                {
                    EventType = "BatchStarted",
                    ActionId = _currentBatch.Id,
                    ActionDescription = description
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to begin batch operation");
            }
        }

        public void EndBatch()
        {
            try
            {
                if (_currentBatch == null || !_currentBatch.IsActive)
                {
                    _logger.LogWarning("Cannot end batch - no active batch operation");
                    return;
                }

                _logger.LogInformation("Ending batch operation: {BatchId} - {Description} ({ActionCount} actions)", 
                    _currentBatch.Id, _currentBatch.Description, _currentBatch.Actions.Count);

                if (_currentBatch.Actions.Count > 0)
                {
                    // Create batch action and add to history
                    var batchAction = new BatchUndoableAction(_currentBatch.Description, _currentBatch.Actions);
                    AddToHistory(batchAction);
                }

                _undoRedoEvents.OnNext(new UndoRedoEvent
                {
                    EventType = "BatchEnded",
                    ActionId = _currentBatch.Id,
                    ActionDescription = _currentBatch.Description,
                    Success = true,
                    Data = new Dictionary<string, object> { ["ActionCount"] = _currentBatch.Actions.Count }
                });

                _currentBatch.IsActive = false;
                _currentBatch = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end batch operation");
            }
        }

        public void CancelBatch()
        {
            try
            {
                if (_currentBatch == null || !_currentBatch.IsActive)
                {
                    _logger.LogWarning("Cannot cancel batch - no active batch operation");
                    return;
                }

                _logger.LogInformation("Canceling batch operation: {BatchId} - {Description}", 
                    _currentBatch.Id, _currentBatch.Description);

                _undoRedoEvents.OnNext(new UndoRedoEvent
                {
                    EventType = "BatchCanceled",
                    ActionId = _currentBatch.Id,
                    ActionDescription = _currentBatch.Description
                });

                _currentBatch.IsActive = false;
                _currentBatch = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel batch operation");
            }
        }

        public void SetMaxHistorySize(int maxSize)
        {
            _maxHistorySize = Math.Max(1, maxSize);
            _logger.LogInformation("Max history size set to: {MaxSize}", _maxHistorySize);

            // Trim existing history if needed
            TrimUndoHistory();
            TrimRedoHistory();
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Undo/redo service {Status}", enabled ? "enabled" : "disabled");

            if (!enabled && _currentBatch != null && _currentBatch.IsActive)
            {
                CancelBatch();
            }
        }

        private void AddToHistory(IUndoableAction action)
        {
            try
            {
                // Clear redo history when new action is added
                _redoHistory.Clear();

                // Add to undo history
                _undoHistory.Add(action);

                // Trim history if needed
                TrimUndoHistory();

                _currentHistoryIndex = _undoHistory.Count - 1;

                _logger.LogDebug("Added action to history: {ActionId} - History size: {Size}", 
                    action.Id, _undoHistory.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add action to history");
            }
        }

        private void TrimUndoHistory()
        {
            while (_undoHistory.Count > _maxHistorySize)
            {
                var removedAction = _undoHistory[0];
                _undoHistory.RemoveAt(0);

                _logger.LogDebug("Trimmed action from undo history: {ActionId}", removedAction.Id);
            }
        }

        private void TrimRedoHistory()
        {
            while (_redoHistory.Count > _maxHistorySize)
            {
                var removedAction = _redoHistory[0];
                _redoHistory.RemoveAt(0);

                _logger.LogDebug("Trimmed action from redo history: {ActionId}", removedAction.Id);
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Disposing UndoRedoService");

                // Cancel active batch
                if (_currentBatch != null && _currentBatch.IsActive)
                {
                    CancelBatch();
                }

                // Clear all history
                ClearHistory();

                // Dispose observables
                _undoRedoEvents?.Dispose();

                _logger.LogInformation("UndoRedoService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing UndoRedoService");
            }
        }
    }
} 
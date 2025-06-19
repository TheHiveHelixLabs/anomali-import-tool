using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.UI.ViewModels;

/// <summary>
/// Base ViewModel class providing common ReactiveUI functionality
/// Implements IActivatableViewModel for proper lifecycle management
/// </summary>
public abstract class BaseViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly ILogger _logger;
    
    // Common backing fields
    private bool _isBusy = false;
    private string _statusMessage = string.Empty;
    private bool _hasErrors = false;
    private string _errorMessage = string.Empty;
    private string _title = string.Empty;
    
    /// <summary>
    /// Initializes a new instance of BaseViewModel
    /// </summary>
    /// <param name="logger">Logger instance</param>
    protected BaseViewModel(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize ViewModelActivator
        Activator = new ViewModelActivator();
        
        // Setup common reactive properties
        SetupReactiveProperties();
        
        // Setup activation handling
        SetupActivation();
    }
    
    #region Common Reactive Properties
    
    /// <summary>
    /// Whether the ViewModel is currently performing an operation
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }
    
    /// <summary>
    /// Current status message for user feedback
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }
    
    /// <summary>
    /// Whether there are validation or operation errors
    /// </summary>
    public bool HasErrors
    {
        get => _hasErrors;
        set => this.RaiseAndSetIfChanged(ref _hasErrors, value);
    }
    
    /// <summary>
    /// Current error message if HasErrors is true
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }
    
    /// <summary>
    /// Title for the ViewModel/View
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
    
    /// <summary>
    /// Observable that emits when IsBusy changes to false
    /// Useful for chaining operations
    /// </summary>
    public IObservable<bool> WhenNotBusy => this.WhenAnyValue(x => x.IsBusy, busy => !busy);
    
    /// <summary>
    /// Observable that emits when errors are cleared
    /// </summary>
    public IObservable<bool> WhenErrorsCleared => this.WhenAnyValue(x => x.HasErrors, hasErrors => !hasErrors);
    
    #endregion
    
    #region Common Commands
    
    /// <summary>
    /// Command to clear current error state
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearErrorCommand { get; private set; } = null!;
    
    /// <summary>
    /// Command to refresh/reload data
    /// </summary>
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; private set; } = null!;
    
    #endregion
    
    #region Setup Methods
    
    /// <summary>
    /// Setup common reactive properties and computed values
    /// </summary>
    private void SetupReactiveProperties()
    {
        // Clear error message when HasErrors becomes false
        this.WhenAnyValue(x => x.HasErrors)
            .Where(hasErrors => !hasErrors)
            .Subscribe(_ => ErrorMessage = string.Empty);
        
        // Clear status message when busy state changes
        this.WhenAnyValue(x => x.IsBusy)
            .Where(busy => !busy)
            .Delay(TimeSpan.FromSeconds(2)) // Keep status visible for 2 seconds
            .Subscribe(_ => StatusMessage = string.Empty);
    }
    
    /// <summary>
    /// Setup common commands
    /// </summary>
    private void SetupCommands()
    {
        // Clear error command
        ClearErrorCommand = ReactiveCommand.Create(ClearError);
        
        // Refresh command - can be overridden by derived classes
        RefreshCommand = ReactiveCommand.CreateFromTask(
            RefreshAsync,
            WhenNotBusy,
            RxApp.MainThreadScheduler);
        
        // Setup error handling for refresh command
        RefreshCommand.ThrownExceptions
            .SelectMany(HandleException)
            .Subscribe();
    }
    
    /// <summary>
    /// Setup activation handling for lifecycle management
    /// </summary>
    private void SetupActivation()
    {
        this.WhenActivated(disposables =>
        {
            // Log activation
            _logger.LogDebug("{ViewModelType} activated", GetType().Name);
            
            // Setup disposal
            Disposable.Create(() => _logger.LogDebug("{ViewModelType} deactivated", GetType().Name))
                .DisposeWith(disposables);
            
            // Call derived class activation
            OnActivated(disposables);
        });
    }
    
    #endregion
    
    #region Protected Methods for Derived Classes
    
    /// <summary>
    /// Execute an async operation with automatic busy state management
    /// </summary>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="statusMessage">Optional status message during operation</param>
    /// <returns>Task representing the operation</returns>
    protected async Task ExecuteWithBusyAsync(Func<Task> operation, string? statusMessage = null)
    {
        if (IsBusy)
        {
            _logger.LogWarning("Attempted to execute operation while already busy");
            return;
        }
        
        try
        {
            IsBusy = true;
            ClearError();
            
            if (!string.IsNullOrEmpty(statusMessage))
            {
                StatusMessage = statusMessage;
            }
            
            await operation();
            
            _logger.LogDebug("Operation completed successfully in {ViewModelType}", GetType().Name);
        }
        catch (Exception ex)
        {
            SetError($"Operation failed: {ex.Message}");
            _logger.LogError(ex, "Operation failed in {ViewModelType}", GetType().Name);
            throw; // Re-throw to allow handling by command error handling
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Execute an async operation with result and automatic busy state management
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="statusMessage">Optional status message during operation</param>
    /// <returns>Task with result</returns>
    protected async Task<T> ExecuteWithBusyAsync<T>(Func<Task<T>> operation, string? statusMessage = null)
    {
        if (IsBusy)
        {
            _logger.LogWarning("Attempted to execute operation while already busy");
            throw new InvalidOperationException("Another operation is already in progress");
        }
        
        try
        {
            IsBusy = true;
            ClearError();
            
            if (!string.IsNullOrEmpty(statusMessage))
            {
                StatusMessage = statusMessage;
            }
            
            var result = await operation();
            
            _logger.LogDebug("Operation completed successfully in {ViewModelType}", GetType().Name);
            return result;
        }
        catch (Exception ex)
        {
            SetError($"Operation failed: {ex.Message}");
            _logger.LogError(ex, "Operation failed in {ViewModelType}", GetType().Name);
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    /// <summary>
    /// Set error state with message
    /// </summary>
    /// <param name="message">Error message</param>
    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasErrors = true;
        StatusMessage = string.Empty;
    }
    
    /// <summary>
    /// Clear error state
    /// </summary>
    protected void ClearError()
    {
        HasErrors = false;
        ErrorMessage = string.Empty;
    }
    
    /// <summary>
    /// Set status message for user feedback
    /// </summary>
    /// <param name="message">Status message</param>
    protected void SetStatus(string message)
    {
        StatusMessage = message;
        ClearError();
    }
    
    /// <summary>
    /// Handle exceptions in a consistent way
    /// </summary>
    /// <param name="exception">The exception to handle</param>
    /// <returns>Observable for reactive error handling</returns>
    protected virtual IObservable<Unit> HandleException(Exception exception)
    {
        return Observable.Start(() =>
        {
            SetError(exception.Message);
            _logger.LogError(exception, "Unhandled exception in {ViewModelType}", GetType().Name);
        }, RxApp.MainThreadScheduler);
    }
    
    /// <summary>
    /// Create a ReactiveCommand with automatic error handling
    /// </summary>
    /// <param name="executeAsync">The async method to execute</param>
    /// <param name="canExecute">Observable for can execute condition</param>
    /// <param name="statusMessage">Status message during execution</param>
    /// <returns>ReactiveCommand with error handling</returns>
    protected ReactiveCommand<Unit, Unit> CreateCommand(
        Func<Task> executeAsync,
        IObservable<bool>? canExecute = null,
        string? statusMessage = null)
    {
        canExecute ??= WhenNotBusy;
        
        var command = ReactiveCommand.CreateFromTask(
            () => ExecuteWithBusyAsync(executeAsync, statusMessage),
            canExecute,
            RxApp.MainThreadScheduler);
        
        // Setup error handling
        command.ThrownExceptions
            .SelectMany(HandleException)
            .Subscribe();
        
        return command;
    }
    
    /// <summary>
    /// Create a ReactiveCommand with result and automatic error handling
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="executeAsync">The async method to execute</param>
    /// <param name="canExecute">Observable for can execute condition</param>
    /// <param name="statusMessage">Status message during execution</param>
    /// <returns>ReactiveCommand with error handling</returns>
    protected ReactiveCommand<Unit, T> CreateCommand<T>(
        Func<Task<T>> executeAsync,
        IObservable<bool>? canExecute = null,
        string? statusMessage = null)
    {
        canExecute ??= WhenNotBusy;
        
        var command = ReactiveCommand.CreateFromTask(
            () => ExecuteWithBusyAsync(executeAsync, statusMessage),
            canExecute,
            RxApp.MainThreadScheduler);
        
        // Setup error handling
        command.ThrownExceptions
            .SelectMany(HandleException)
            .Subscribe();
        
        return command;
    }
    
    #endregion
    
    #region Virtual Methods for Derived Classes
    
    /// <summary>
    /// Called when the ViewModel is activated
    /// Override in derived classes for specific activation logic
    /// </summary>
    /// <param name="disposables">CompositeDisposable for managing subscriptions</param>
    protected virtual void OnActivated(CompositeDisposable disposables)
    {
        // Base implementation - derived classes can override
    }
    
    /// <summary>
    /// Refresh operation - override in derived classes
    /// </summary>
    /// <returns>Task representing the refresh operation</returns>
    protected virtual async Task RefreshAsync()
    {
        // Base implementation - derived classes should override
        await Task.Delay(100); // Simulate refresh
        SetStatus("Refreshed");
    }
    
    /// <summary>
    /// Validation logic - override in derived classes
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public virtual bool Validate()
    {
        // Base implementation - derived classes can override
        ClearError();
        return true;
    }
    
    #endregion
    
    #region IActivatableViewModel
    
    /// <summary>
    /// ViewModelActivator for ReactiveUI lifecycle management
    /// </summary>
    public ViewModelActivator Activator { get; }
    
    #endregion
    
    #region Protected Properties
    
    /// <summary>
    /// Logger instance for derived classes
    /// </summary>
    protected ILogger Logger => _logger;
    
    #endregion
} 
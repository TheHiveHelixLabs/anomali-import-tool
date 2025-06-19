using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using Windows.UI;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AnomaliImportTool.UI.Controls;

/// <summary>
/// Professional LiveCard control with corporate branding and hover animations
/// </summary>
public sealed partial class LiveCard : UserControl
{
    private readonly ILogger<LiveCard> _logger;
    private bool _isAnimating = false;
    
    // Dependency Properties
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(LiveCard), new PropertyMetadata(string.Empty));
    
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(LiveCard), new PropertyMetadata(string.Empty));
    
    public static readonly DependencyProperty IconSourceProperty =
        DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(LiveCard), new PropertyMetadata(null));
    
    public static readonly DependencyProperty IsElevatedProperty =
        DependencyProperty.Register(nameof(IsElevated), typeof(bool), typeof(LiveCard), new PropertyMetadata(false, OnIsElevatedChanged));
    
    public static readonly DependencyProperty CardActionProperty =
        DependencyProperty.Register(nameof(CardAction), typeof(Action), typeof(LiveCard), new PropertyMetadata(null));
    
    // Properties
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    
    public ImageSource IconSource
    {
        get => (ImageSource)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }
    
    public bool IsElevated
    {
        get => (bool)GetValue(IsElevatedProperty);
        set => SetValue(IsElevatedProperty, value);
    }
    
    public Action? CardAction
    {
        get => (Action?)GetValue(CardActionProperty);
        set => SetValue(CardActionProperty, value);
    }
    
    public LiveCard()
    {
        this.InitializeComponent();
        _logger = App.Current.Services.GetService<ILogger<LiveCard>>() ?? throw new InvalidOperationException("Logger not available");
        
        // Setup event handlers
        this.PointerEntered += OnPointerEntered;
        this.PointerExited += OnPointerExited;
        this.Tapped += OnCardTapped;
        
        _logger.LogDebug("LiveCard control initialized");
    }
    
    private async void OnPointerEntered(object sender, PointerEventArgs e)
    {
        if (_isAnimating) return;
        
        try
        {
            _isAnimating = true;
            await AnimateElevationAsync(true);
        }
        finally
        {
            _isAnimating = false;
        }
    }
    
    private async void OnPointerExited(object sender, PointerEventArgs e)
    {
        if (_isAnimating) return;
        
        try
        {
            _isAnimating = true;
            await AnimateElevationAsync(false);
        }
        finally
        {
            _isAnimating = false;
        }
    }
    
    private void OnCardTapped(object sender, TappedEventArgs e)
    {
        try
        {
            CardAction?.Invoke();
            _logger.LogDebug("LiveCard action executed for: {Title}", Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing LiveCard action for: {Title}", Title);
        }
    }
    
    private async Task AnimateElevationAsync(bool elevate)
    {
        const double animationDuration = 200; // ms
        const double elevatedScale = 1.05;
        const double normalScale = 1.0;
        const double elevatedOpacity = 0.95;
        const double normalOpacity = 1.0;
        
        var targetScale = elevate ? elevatedScale : normalScale;
        var targetOpacity = elevate ? elevatedOpacity : normalOpacity;
        
        var scaleAnimation = new DoubleAnimation
        {
            To = targetScale,
            Duration = TimeSpan.FromMilliseconds(animationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        var opacityAnimation = new DoubleAnimation
        {
            To = targetOpacity,
            Duration = TimeSpan.FromMilliseconds(animationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        
        var storyboard = new Storyboard();
        storyboard.Children.Add(scaleAnimation);
        storyboard.Children.Add(opacityAnimation);
        
        // Set targets
        Storyboard.SetTarget(scaleAnimation, this.RenderTransform as CompositeTransform ?? new CompositeTransform());
        Storyboard.SetTargetProperty(scaleAnimation, "ScaleX");
        
        Storyboard.SetTarget(opacityAnimation, this);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        
        storyboard.Begin();
        await Task.Delay((int)animationDuration);
    }
    
    private static void OnIsElevatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LiveCard card && e.NewValue is bool isElevated)
        {
            _ = card.AnimateElevationAsync(isElevated);
        }
    }
}

/// <summary>
/// Professional LiveProgressRing with smooth progress indication and corporate styling
/// </summary>
public sealed partial class LiveProgressRing : UserControl
{
    private readonly ILogger<LiveProgressRing> _logger;
    private ProgressRing? _progressRing;
    
    // Dependency Properties
    public static readonly DependencyProperty ProgressValueProperty =
        DependencyProperty.Register(nameof(ProgressValue), typeof(double), typeof(LiveProgressRing), 
            new PropertyMetadata(0.0, OnProgressValueChanged));
    
    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(LiveProgressRing), 
            new PropertyMetadata(false));
    
    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(LiveProgressRing), 
            new PropertyMetadata(string.Empty));
    
    public static readonly DependencyProperty ProgressColorProperty =
        DependencyProperty.Register(nameof(ProgressColor), typeof(Brush), typeof(LiveProgressRing), 
            new PropertyMetadata(new SolidColorBrush(Colors.Blue)));
    
    // Properties
    public double ProgressValue
    {
        get => (double)GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }
    
    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }
    
    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }
    
    public Brush ProgressColor
    {
        get => (Brush)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }
    
    public LiveProgressRing()
    {
        this.InitializeComponent();
        _logger = App.Current.Services.GetService<ILogger<LiveProgressRing>>() ?? throw new InvalidOperationException("Logger not available");
        
        // Initialize progress ring
        SetupProgressRing();
        
        _logger.LogDebug("LiveProgressRing control initialized");
    }
    
    private void SetupProgressRing()
    {
        _progressRing = new ProgressRing
        {
            Width = 48,
            Height = 48,
            IsIndeterminate = IsIndeterminate
        };
        
        // Add to content (would need proper XAML template)
        this.Content = _progressRing;
    }
    
    private static async void OnProgressValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LiveProgressRing ring && e.NewValue is double newValue)
        {
            await ring.AnimateProgressChangeAsync(newValue);
        }
    }
    
    private async Task AnimateProgressChangeAsync(double targetValue)
    {
        if (_progressRing == null) return;
        
        const double animationDuration = 300; // ms
        
        var animation = new DoubleAnimation
        {
            To = targetValue,
            Duration = TimeSpan.FromMilliseconds(animationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        
        Storyboard.SetTarget(animation, _progressRing);
        Storyboard.SetTargetProperty(animation, "Value");
        
        storyboard.Begin();
        await Task.Delay((int)animationDuration);
        
        _logger.LogDebug("Progress animated to: {ProgressValue}%", targetValue);
    }
}

/// <summary>
/// AnimatedDropZone with breathing animation for file uploads
/// </summary>
public sealed partial class AnimatedDropZone : UserControl
{
    private readonly ILogger<AnimatedDropZone> _logger;
    private bool _isBreathing = false;
    private Storyboard? _breathingStoryboard;
    
    // Dependency Properties
    public static readonly DependencyProperty AllowedFileTypesProperty =
        DependencyProperty.Register(nameof(AllowedFileTypes), typeof(string), typeof(AnimatedDropZone), 
            new PropertyMetadata("All Files"));
    
    public static readonly DependencyProperty DropActionProperty =
        DependencyProperty.Register(nameof(DropAction), typeof(Action<string[]>), typeof(AnimatedDropZone), 
            new PropertyMetadata(null));
    
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(AnimatedDropZone), 
            new PropertyMetadata(false, OnIsActiveChanged));
    
    // Properties
    public string AllowedFileTypes
    {
        get => (string)GetValue(AllowedFileTypesProperty);
        set => SetValue(AllowedFileTypesProperty, value);
    }
    
    public Action<string[]>? DropAction
    {
        get => (Action<string[]>?)GetValue(DropActionProperty);
        set => SetValue(DropActionProperty, value);
    }
    
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
    
    public AnimatedDropZone()
    {
        this.InitializeComponent();
        _logger = App.Current.Services.GetService<ILogger<AnimatedDropZone>>() ?? throw new InvalidOperationException("Logger not available");
        
        // Setup drag and drop
        this.AllowDrop = true;
        this.DragEnter += OnDragEnter;
        this.DragLeave += OnDragLeave;
        this.Drop += OnDrop;
        
        // Setup breathing animation
        SetupBreathingAnimation();
        
        _logger.LogDebug("AnimatedDropZone control initialized");
    }
    
    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            StartBreathingAnimation();
            _logger.LogDebug("Drag enter detected, starting breathing animation");
        }
    }
    
    private void OnDragLeave(object sender, DragEventArgs e)
    {
        StopBreathingAnimation();
        _logger.LogDebug("Drag leave detected, stopping breathing animation");
    }
    
    private async void OnDrop(object sender, DragEventArgs e)
    {
        try
        {
            StopBreathingAnimation();
            
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var filePaths = new List<string>();
                
                foreach (var item in items)
                {
                    if (item is Windows.Storage.StorageFile file)
                    {
                        filePaths.Add(file.Path);
                    }
                }
                
                if (filePaths.Count > 0)
                {
                    DropAction?.Invoke(filePaths.ToArray());
                    _logger.LogInformation("Files dropped: {FileCount} files", filePaths.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file drop");
        }
    }
    
    private void SetupBreathingAnimation()
    {
        var scaleAnimation = new DoubleAnimationUsingKeyFrames
        {
            RepeatBehavior = RepeatBehavior.Forever,
            AutoReverse = true
        };
        
        scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = TimeSpan.Zero,
            Value = 1.0
        });
        
        scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = TimeSpan.FromMilliseconds(1500),
            Value = 1.05,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        
        _breathingStoryboard = new Storyboard();
        _breathingStoryboard.Children.Add(scaleAnimation);
        
        Storyboard.SetTarget(scaleAnimation, this.RenderTransform as CompositeTransform ?? new CompositeTransform());
        Storyboard.SetTargetProperty(scaleAnimation, "ScaleX");
    }
    
    private void StartBreathingAnimation()
    {
        if (!_isBreathing && _breathingStoryboard != null)
        {
            _isBreathing = true;
            _breathingStoryboard.Begin();
        }
    }
    
    private void StopBreathingAnimation()
    {
        if (_isBreathing && _breathingStoryboard != null)
        {
            _isBreathing = false;
            _breathingStoryboard.Stop();
            
            // Reset scale
            if (this.RenderTransform is CompositeTransform transform)
            {
                transform.ScaleX = 1.0;
                transform.ScaleY = 1.0;
            }
        }
    }
    
    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnimatedDropZone zone && e.NewValue is bool isActive)
        {
            if (isActive)
            {
                zone.StartBreathingAnimation();
            }
            else
            {
                zone.StopBreathingAnimation();
            }
        }
    }
}

/// <summary>
/// WorkflowBreadcrumb for professional step navigation
/// </summary>
public sealed partial class WorkflowBreadcrumb : UserControl
{
    private readonly ILogger<WorkflowBreadcrumb> _logger;
    
    // Dependency Properties
    public static readonly DependencyProperty StepsProperty =
        DependencyProperty.Register(nameof(Steps), typeof(BreadcrumbStep[]), typeof(WorkflowBreadcrumb), 
            new PropertyMetadata(null, OnStepsChanged));
    
    public static readonly DependencyProperty CurrentStepProperty =
        DependencyProperty.Register(nameof(CurrentStep), typeof(int), typeof(WorkflowBreadcrumb), 
            new PropertyMetadata(0, OnCurrentStepChanged));
    
    public static readonly DependencyProperty StepClickedProperty =
        DependencyProperty.Register(nameof(StepClicked), typeof(Action<int>), typeof(WorkflowBreadcrumb), 
            new PropertyMetadata(null));
    
    // Properties
    public BreadcrumbStep[]? Steps
    {
        get => (BreadcrumbStep[]?)GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }
    
    public int CurrentStep
    {
        get => (int)GetValue(CurrentStepProperty);
        set => SetValue(CurrentStepProperty, value);
    }
    
    public Action<int>? StepClicked
    {
        get => (Action<int>?)GetValue(StepClickedProperty);
        set => SetValue(StepClickedProperty, value);
    }
    
    public WorkflowBreadcrumb()
    {
        this.InitializeComponent();
        _logger = App.Current.Services.GetService<ILogger<WorkflowBreadcrumb>>() ?? throw new InvalidOperationException("Logger not available");
        
        _logger.LogDebug("WorkflowBreadcrumb control initialized");
    }
    
    private static void OnStepsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WorkflowBreadcrumb breadcrumb)
        {
            breadcrumb.RebuildBreadcrumb();
        }
    }
    
    private static void OnCurrentStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WorkflowBreadcrumb breadcrumb)
        {
            breadcrumb.UpdateCurrentStep();
        }
    }
    
    private void RebuildBreadcrumb()
    {
        try
        {
            // In a real implementation, this would rebuild the visual breadcrumb
            _logger.LogDebug("Rebuilding breadcrumb with {StepCount} steps", Steps?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rebuilding breadcrumb");
        }
    }
    
    private void UpdateCurrentStep()
    {
        try
        {
            // In a real implementation, this would update the visual current step indicator
            _logger.LogDebug("Current step updated to: {CurrentStep}", CurrentStep);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating current step");
        }
    }
}

/// <summary>
/// Breadcrumb step definition
/// </summary>
public class BreadcrumbStep
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsCompleted { get; set; }
    public bool IsClickable { get; set; } = true;
    public string Icon { get; set; } = "";
} 
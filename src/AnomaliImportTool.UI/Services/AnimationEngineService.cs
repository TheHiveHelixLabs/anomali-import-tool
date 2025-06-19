using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AnomaliImportTool.UI.Services
{
    /// <summary>
    /// Service for managing 60 FPS animation engine with GPU acceleration
    /// Provides smooth professional animations and performance optimization
    /// </summary>
    public interface IAnimationEngineService : INotifyPropertyChanged
    {
        // Performance Metrics
        bool IsGpuAccelerationEnabled { get; }
        double CurrentFrameRate { get; }
        int ActiveAnimationsCount { get; }
        bool IsHighPerformanceMode { get; set; }
        
        // Animation Creation
        Storyboard CreateStoryboard(string name, bool autoReverse = false, RepeatBehavior repeatBehavior = default);
        DoubleAnimation CreateDoubleAnimation(double from, double to, Duration duration, IEasingFunction easingFunction = null);
        ColorAnimation CreateColorAnimation(Color from, Color to, Duration duration, IEasingFunction easingFunction = null);
        ThicknessAnimation CreateThicknessAnimation(Thickness from, Thickness to, Duration duration, IEasingFunction easingFunction = null);
        
        // Common Animations
        void FadeIn(FrameworkElement element, Duration duration, IEasingFunction easingFunction = null);
        void FadeOut(FrameworkElement element, Duration duration, IEasingFunction easingFunction = null);
        void SlideIn(FrameworkElement element, SlideDirection direction, Duration duration, IEasingFunction easingFunction = null);
        void SlideOut(FrameworkElement element, SlideDirection direction, Duration duration, IEasingFunction easingFunction = null);
        void ScaleIn(FrameworkElement element, Duration duration, IEasingFunction easingFunction = null);
        void ScaleOut(FrameworkElement element, Duration duration, IEasingFunction easingFunction = null);
        
        // Advanced Animations
        void AnimateProperty(DependencyObject target, DependencyProperty property, object from, object to, Duration duration, IEasingFunction easingFunction = null);
        void AnimateTransform(FrameworkElement element, Transform transform, Duration duration, IEasingFunction easingFunction = null);
        void AnimateOpacity(FrameworkElement element, double targetOpacity, Duration duration, IEasingFunction easingFunction = null);
        void AnimateSize(FrameworkElement element, Size targetSize, Duration duration, IEasingFunction easingFunction = null);
        
        // Animation Management
        void StartAnimation(string name, Storyboard storyboard, FrameworkElement target = null);
        void StopAnimation(string name);
        void PauseAnimation(string name);
        void ResumeAnimation(string name);
        void StopAllAnimations();
        
        // Performance Optimization
        void OptimizeForPerformance();
        void EnableGpuAcceleration(FrameworkElement element);
        void DisableGpuAcceleration(FrameworkElement element);
        void SetAnimationQuality(AnimationQuality quality);
        
        // Events
        event EventHandler<AnimationCompletedEventArgs> AnimationCompleted;
        event EventHandler<AnimationStartedEventArgs> AnimationStarted;
        event EventHandler<PerformanceChangedEventArgs> PerformanceChanged;
    }

    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public enum AnimationQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    public class AnimationCompletedEventArgs : EventArgs
    {
        public string AnimationName { get; set; }
        public FrameworkElement Target { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class AnimationStartedEventArgs : EventArgs
    {
        public string AnimationName { get; set; }
        public FrameworkElement Target { get; set; }
        public AnimationType Type { get; set; }
    }

    public class PerformanceChangedEventArgs : EventArgs
    {
        public double FrameRate { get; set; }
        public int ActiveAnimations { get; set; }
        public bool IsGpuAccelerated { get; set; }
    }

    public enum AnimationType
    {
        Fade,
        Slide,
        Scale,
        Transform,
        Property,
        Custom
    }

    public class AnimationEngineService : ReactiveObject, IAnimationEngineService
    {
        private readonly ILogger<AnimationEngineService> _logger;
        private readonly Dictionary<string, Storyboard> _activeAnimations;
        private readonly Dictionary<string, FrameworkElement> _animationTargets;
        private readonly DispatcherTimer _performanceTimer;
        
        private bool _isHighPerformanceMode = true;
        private AnimationQuality _animationQuality = AnimationQuality.High;
        private double _currentFrameRate = 60.0;
        private int _frameCount = 0;
        private DateTime _lastFrameTime = DateTime.Now;

        // GPU Acceleration
        private readonly HashSet<FrameworkElement> _gpuAcceleratedElements;
        
        // Performance Constants
        private const int TargetFrameRate = 60;
        private const double PerformanceUpdateInterval = 1000; // 1 second

        public AnimationEngineService(ILogger<AnimationEngineService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _activeAnimations = new Dictionary<string, Storyboard>();
            _animationTargets = new Dictionary<string, FrameworkElement>();
            _gpuAcceleratedElements = new HashSet<FrameworkElement>();
            
            InitializePerformanceMonitoring();
            OptimizeForPerformance();
            
            _logger.LogInformation("Animation Engine Service initialized with 60 FPS target and GPU acceleration");
        }

        #region Performance Properties

        public bool IsGpuAccelerationEnabled => RenderCapability.Tier >= 2;

        public double CurrentFrameRate
        {
            get => _currentFrameRate;
            private set
            {
                if (Math.Abs(_currentFrameRate - value) > 0.1)
                {
                    _currentFrameRate = value;
                    this.RaisePropertyChanged();
                    
                    PerformanceChanged?.Invoke(this, new PerformanceChangedEventArgs
                    {
                        FrameRate = value,
                        ActiveAnimations = ActiveAnimationsCount,
                        IsGpuAccelerated = IsGpuAccelerationEnabled
                    });
                }
            }
        }

        public int ActiveAnimationsCount => _activeAnimations.Count;

        public bool IsHighPerformanceMode
        {
            get => _isHighPerformanceMode;
            set
            {
                if (_isHighPerformanceMode != value)
                {
                    _isHighPerformanceMode = value;
                    this.RaisePropertyChanged();
                    
                    if (value)
                    {
                        OptimizeForPerformance();
                    }
                    
                    _logger.LogInformation("High performance mode {Status}", value ? "enabled" : "disabled");
                }
            }
        }

        #endregion

        #region Animation Creation

        public Storyboard CreateStoryboard(string name, bool autoReverse = false, RepeatBehavior repeatBehavior = default)
        {
            try
            {
                var storyboard = new Storyboard
                {
                    AutoReverse = autoReverse,
                    RepeatBehavior = repeatBehavior == default ? RepeatBehavior.Forever : repeatBehavior
                };
                
                // Add event handlers
                storyboard.Completed += (s, e) => OnAnimationCompleted(name, null);
                storyboard.CurrentStateInvalidated += (s, e) => OnAnimationStateChanged(name, storyboard);
                
                _logger.LogDebug("Created storyboard: {Name}", name);
                return storyboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create storyboard: {Name}", name);
                return new Storyboard();
            }
        }

        public DoubleAnimation CreateDoubleAnimation(double from, double to, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                var animation = new DoubleAnimation
                {
                    From = from,
                    To = to,
                    Duration = duration,
                    EasingFunction = easingFunction ?? GetDefaultEasingFunction()
                };
                
                // Enable GPU acceleration for smooth animation
                Timeline.SetDesiredFrameRate(animation, TargetFrameRate);
                
                return animation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create double animation");
                return new DoubleAnimation();
            }
        }

        public ColorAnimation CreateColorAnimation(Color from, Color to, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                var animation = new ColorAnimation
                {
                    From = from,
                    To = to,
                    Duration = duration,
                    EasingFunction = easingFunction ?? GetDefaultEasingFunction()
                };
                
                Timeline.SetDesiredFrameRate(animation, TargetFrameRate);
                
                return animation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create color animation");
                return new ColorAnimation();
            }
        }

        public ThicknessAnimation CreateThicknessAnimation(Thickness from, Thickness to, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                var animation = new ThicknessAnimation
                {
                    From = from,
                    To = to,
                    Duration = duration,
                    EasingFunction = easingFunction ?? GetDefaultEasingFunction()
                };
                
                Timeline.SetDesiredFrameRate(animation, TargetFrameRate);
                
                return animation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create thickness animation");
                return new ThicknessAnimation();
            }
        }

        #endregion

        #region Common Animations

        public void FadeIn(FrameworkElement element, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null) return;
                
                var animationName = $"FadeIn_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                var animation = CreateDoubleAnimation(0, 1, duration, easingFunction);
                
                Storyboard.SetTarget(animation, element);
                Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
                
                storyboard.Children.Add(animation);
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started fade in animation for {ElementType}", element.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fade in element");
            }
        }

        public void FadeOut(FrameworkElement element, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null) return;
                
                var animationName = $"FadeOut_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                var animation = CreateDoubleAnimation(element.Opacity, 0, duration, easingFunction);
                
                Storyboard.SetTarget(animation, element);
                Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
                
                storyboard.Children.Add(animation);
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started fade out animation for {ElementType}", element.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fade out element");
            }
        }

        public void SlideIn(FrameworkElement element, SlideDirection direction, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null) return;
                
                var animationName = $"SlideIn_{direction}_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                
                // Ensure the element has a transform
                if (element.RenderTransform == null || element.RenderTransform == Transform.Identity)
                {
                    element.RenderTransform = new TranslateTransform();
                }
                
                var transform = element.RenderTransform as TranslateTransform ?? new TranslateTransform();
                element.RenderTransform = transform;
                
                DoubleAnimation animation;
                PropertyPath propertyPath;
                
                switch (direction)
                {
                    case SlideDirection.Left:
                        animation = CreateDoubleAnimation(-element.ActualWidth, 0, duration, easingFunction);
                        propertyPath = new PropertyPath("RenderTransform.X");
                        break;
                    case SlideDirection.Right:
                        animation = CreateDoubleAnimation(element.ActualWidth, 0, duration, easingFunction);
                        propertyPath = new PropertyPath("RenderTransform.X");
                        break;
                    case SlideDirection.Up:
                        animation = CreateDoubleAnimation(-element.ActualHeight, 0, duration, easingFunction);
                        propertyPath = new PropertyPath("RenderTransform.Y");
                        break;
                    case SlideDirection.Down:
                        animation = CreateDoubleAnimation(element.ActualHeight, 0, duration, easingFunction);
                        propertyPath = new PropertyPath("RenderTransform.Y");
                        break;
                    default:
                        return;
                }
                
                Storyboard.SetTarget(animation, element);
                Storyboard.SetTargetProperty(animation, propertyPath);
                
                storyboard.Children.Add(animation);
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started slide in animation for {ElementType} in {Direction} direction", 
                    element.GetType().Name, direction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to slide in element");
            }
        }

        public void SlideOut(FrameworkElement element, SlideDirection direction, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null) return;
                
                var animationName = $"SlideOut_{direction}_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                
                // Ensure the element has a transform
                if (element.RenderTransform == null || element.RenderTransform == Transform.Identity)
                {
                    element.RenderTransform = new TranslateTransform();
                }
                
                var transform = element.RenderTransform as TranslateTransform ?? new TranslateTransform();
                element.RenderTransform = transform;
                
                DoubleAnimation animation;
                PropertyPath propertyPath;
                
                switch (direction)
                {
                    case SlideDirection.Left:
                        animation = CreateDoubleAnimation(0, -element.ActualWidth, duration, easingFunction);
                        propertyPath = new PropertyPath("RenderTransform.X");
                        break;
                    case SlideDirection.Right:
                        animation = CreateDoubleAnimation(0, element.ActualWidth, duration, easingFunction);
                        propertyPath = new PropertyPath("RenderTransform.X");
                        break;
                    case SlideDirection.Up:
                        animation = CreateDoubleAnimation(0, -element.ActualHeight, duration, easingFunction);
                        propertyPath = new PropertyPath("RenderTransform.Y");
                        break;
                    case SlideDirection.Down:
                        animation = CreateDoubleAnimation(0, element.ActualHeight, duration, easingFunction);
                        propertyPath = new PropertyPath("RenderTransform.Y");
                        break;
                    default:
                        return;
                }
                
                Storyboard.SetTarget(animation, element);
                Storyboard.SetTargetProperty(animation, propertyPath);
                
                storyboard.Children.Add(animation);
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started slide out animation for {ElementType} in {Direction} direction", 
                    element.GetType().Name, direction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to slide out element");
            }
        }

        public void ScaleIn(FrameworkElement element, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null) return;
                
                var animationName = $"ScaleIn_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                
                // Ensure the element has a scale transform
                if (element.RenderTransform == null || element.RenderTransform == Transform.Identity)
                {
                    element.RenderTransform = new ScaleTransform();
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }
                
                var scaleTransform = element.RenderTransform as ScaleTransform ?? new ScaleTransform();
                element.RenderTransform = scaleTransform;
                
                var scaleXAnimation = CreateDoubleAnimation(0, 1, duration, easingFunction);
                var scaleYAnimation = CreateDoubleAnimation(0, 1, duration, easingFunction);
                
                Storyboard.SetTarget(scaleXAnimation, element);
                Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
                
                Storyboard.SetTarget(scaleYAnimation, element);
                Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
                
                storyboard.Children.Add(scaleXAnimation);
                storyboard.Children.Add(scaleYAnimation);
                
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started scale in animation for {ElementType}", element.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scale in element");
            }
        }

        public void ScaleOut(FrameworkElement element, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null) return;
                
                var animationName = $"ScaleOut_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                
                // Ensure the element has a scale transform
                if (element.RenderTransform == null || element.RenderTransform == Transform.Identity)
                {
                    element.RenderTransform = new ScaleTransform();
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }
                
                var scaleTransform = element.RenderTransform as ScaleTransform ?? new ScaleTransform();
                element.RenderTransform = scaleTransform;
                
                var scaleXAnimation = CreateDoubleAnimation(1, 0, duration, easingFunction);
                var scaleYAnimation = CreateDoubleAnimation(1, 0, duration, easingFunction);
                
                Storyboard.SetTarget(scaleXAnimation, element);
                Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
                
                Storyboard.SetTarget(scaleYAnimation, element);
                Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
                
                storyboard.Children.Add(scaleXAnimation);
                storyboard.Children.Add(scaleYAnimation);
                
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started scale out animation for {ElementType}", element.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scale out element");
            }
        }

        #endregion

        #region Advanced Animations

        public void AnimateProperty(DependencyObject target, DependencyProperty property, object from, object to, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (target == null || property == null) return;
                
                var animationName = $"Property_{property.Name}_{target.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                
                AnimationTimeline animation = property.PropertyType.Name switch
                {
                    "Double" => CreateDoubleAnimation((double)from, (double)to, duration, easingFunction),
                    "Color" => CreateColorAnimation((Color)from, (Color)to, duration, easingFunction),
                    "Thickness" => CreateThicknessAnimation((Thickness)from, (Thickness)to, duration, easingFunction),
                    _ => throw new NotSupportedException($"Property type {property.PropertyType.Name} is not supported")
                };
                
                Storyboard.SetTarget(animation, target);
                Storyboard.SetTargetProperty(animation, new PropertyPath(property));
                
                storyboard.Children.Add(animation);
                StartAnimation(animationName, storyboard, target as FrameworkElement);
                
                _logger.LogDebug("Started property animation for {PropertyName} on {TargetType}", 
                    property.Name, target.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate property");
            }
        }

        public void AnimateTransform(FrameworkElement element, Transform transform, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null || transform == null) return;
                
                var animationName = $"Transform_{transform.GetType().Name}_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                
                element.RenderTransform = transform;
                
                // Note: Transform animation requires specific implementation based on transform type
                // This is a simplified version - full implementation would handle all transform types
                
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started transform animation for {ElementType} with {TransformType}", 
                    element.GetType().Name, transform.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate transform");
            }
        }

        public void AnimateOpacity(FrameworkElement element, double targetOpacity, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null) return;
                
                var currentOpacity = element.Opacity;
                var animationName = $"Opacity_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                var animation = CreateDoubleAnimation(currentOpacity, targetOpacity, duration, easingFunction);
                
                Storyboard.SetTarget(animation, element);
                Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));
                
                storyboard.Children.Add(animation);
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started opacity animation for {ElementType} from {From} to {To}", 
                    element.GetType().Name, currentOpacity, targetOpacity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate opacity");
            }
        }

        public void AnimateSize(FrameworkElement element, Size targetSize, Duration duration, IEasingFunction easingFunction = null)
        {
            try
            {
                if (element == null) return;
                
                var animationName = $"Size_{element.GetHashCode()}";
                var storyboard = CreateStoryboard(animationName);
                
                var widthAnimation = CreateDoubleAnimation(element.Width, targetSize.Width, duration, easingFunction);
                var heightAnimation = CreateDoubleAnimation(element.Height, targetSize.Height, duration, easingFunction);
                
                Storyboard.SetTarget(widthAnimation, element);
                Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(FrameworkElement.WidthProperty));
                
                Storyboard.SetTarget(heightAnimation, element);
                Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(FrameworkElement.HeightProperty));
                
                storyboard.Children.Add(widthAnimation);
                storyboard.Children.Add(heightAnimation);
                
                StartAnimation(animationName, storyboard, element);
                
                _logger.LogDebug("Started size animation for {ElementType} to {Width}x{Height}", 
                    element.GetType().Name, targetSize.Width, targetSize.Height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate size");
            }
        }

        #endregion

        #region Animation Management

        public void StartAnimation(string name, Storyboard storyboard, FrameworkElement target = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || storyboard == null) return;
                
                // Stop existing animation with the same name
                StopAnimation(name);
                
                // Store the animation
                _activeAnimations[name] = storyboard;
                if (target != null)
                {
                    _animationTargets[name] = target;
                }
                
                // Enable GPU acceleration for the target
                if (target != null && _isHighPerformanceMode)
                {
                    EnableGpuAcceleration(target);
                }
                
                // Start the animation
                if (target != null)
                {
                    storyboard.Begin(target);
                }
                else
                {
                    storyboard.Begin();
                }
                
                AnimationStarted?.Invoke(this, new AnimationStartedEventArgs
                {
                    AnimationName = name,
                    Target = target,
                    Type = AnimationType.Custom
                });
                
                this.RaisePropertyChanged(nameof(ActiveAnimationsCount));
                
                _logger.LogDebug("Started animation: {Name}", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start animation: {Name}", name);
            }
        }

        public void StopAnimation(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                
                if (_activeAnimations.TryGetValue(name, out var storyboard))
                {
                    storyboard.Stop();
                    _activeAnimations.Remove(name);
                    
                    if (_animationTargets.TryGetValue(name, out var target))
                    {
                        _animationTargets.Remove(name);
                    }
                    
                    this.RaisePropertyChanged(nameof(ActiveAnimationsCount));
                    
                    _logger.LogDebug("Stopped animation: {Name}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop animation: {Name}", name);
            }
        }

        public void PauseAnimation(string name)
        {
            try
            {
                if (_activeAnimations.TryGetValue(name, out var storyboard))
                {
                    storyboard.Pause();
                    _logger.LogDebug("Paused animation: {Name}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pause animation: {Name}", name);
            }
        }

        public void ResumeAnimation(string name)
        {
            try
            {
                if (_activeAnimations.TryGetValue(name, out var storyboard))
                {
                    storyboard.Resume();
                    _logger.LogDebug("Resumed animation: {Name}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resume animation: {Name}", name);
            }
        }

        public void StopAllAnimations()
        {
            try
            {
                var animationNames = _activeAnimations.Keys.ToList();
                foreach (var name in animationNames)
                {
                    StopAnimation(name);
                }
                
                _logger.LogInformation("Stopped all animations ({Count} total)", animationNames.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop all animations");
            }
        }

        #endregion

        #region Performance Optimization

        public void OptimizeForPerformance()
        {
            try
            {
                // Enable hardware acceleration
                RenderOptions.ProcessRenderMode = RenderMode.Default;
                
                // Set animation timeline quality
                Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = TargetFrameRate });
                
                _logger.LogInformation("Optimized animation engine for performance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize for performance");
            }
        }

        public void EnableGpuAcceleration(FrameworkElement element)
        {
            try
            {
                if (element == null || !IsGpuAccelerationEnabled) return;
                
                RenderOptions.SetBitmapScalingMode(element, BitmapScalingMode.HighQuality);
                RenderOptions.SetCachingHint(element, CachingHint.Cache);
                RenderOptions.SetCacheInvalidationThresholdMinimum(element, 0.5);
                RenderOptions.SetCacheInvalidationThresholdMaximum(element, 2.0);
                
                _gpuAcceleratedElements.Add(element);
                
                _logger.LogDebug("Enabled GPU acceleration for {ElementType}", element.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable GPU acceleration");
            }
        }

        public void DisableGpuAcceleration(FrameworkElement element)
        {
            try
            {
                if (element == null) return;
                
                RenderOptions.SetCachingHint(element, CachingHint.Unspecified);
                _gpuAcceleratedElements.Remove(element);
                
                _logger.LogDebug("Disabled GPU acceleration for {ElementType}", element.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable GPU acceleration");
            }
        }

        public void SetAnimationQuality(AnimationQuality quality)
        {
            try
            {
                _animationQuality = quality;
                
                var frameRate = quality switch
                {
                    AnimationQuality.Low => 30,
                    AnimationQuality.Medium => 45,
                    AnimationQuality.High => 60,
                    AnimationQuality.Ultra => 120,
                    _ => 60
                };
                
                Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(Timeline),
                    new FrameworkPropertyMetadata { DefaultValue = frameRate });
                
                _logger.LogInformation("Set animation quality to {Quality} ({FrameRate} FPS)", quality, frameRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set animation quality");
            }
        }

        #endregion

        #region Helper Methods

        private IEasingFunction GetDefaultEasingFunction()
        {
            return new CubicEase { EasingMode = EasingMode.EaseInOut };
        }

        private void InitializePerformanceMonitoring()
        {
            try
            {
                _performanceTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(PerformanceUpdateInterval)
                };
                
                _performanceTimer.Tick += OnPerformanceTimerTick;
                _performanceTimer.Start();
                
                CompositionTarget.Rendering += OnCompositionTargetRendering;
                
                _logger.LogDebug("Performance monitoring initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize performance monitoring");
            }
        }

        private void OnPerformanceTimerTick(object sender, EventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                var elapsed = (now - _lastFrameTime).TotalSeconds;
                
                if (elapsed > 0)
                {
                    CurrentFrameRate = _frameCount / elapsed;
                    _frameCount = 0;
                    _lastFrameTime = now;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in performance timer tick");
            }
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            _frameCount++;
        }

        private void OnAnimationCompleted(string name, FrameworkElement target)
        {
            try
            {
                if (_activeAnimations.ContainsKey(name))
                {
                    var storyboard = _activeAnimations[name];
                    _activeAnimations.Remove(name);
                    
                    if (_animationTargets.ContainsKey(name))
                    {
                        _animationTargets.Remove(name);
                    }
                    
                    this.RaisePropertyChanged(nameof(ActiveAnimationsCount));
                    
                    AnimationCompleted?.Invoke(this, new AnimationCompletedEventArgs
                    {
                        AnimationName = name,
                        Target = target,
                        Duration = storyboard.Duration.HasTimeSpan ? storyboard.Duration.TimeSpan : TimeSpan.Zero
                    });
                    
                    _logger.LogDebug("Animation completed: {Name}", name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling animation completion");
            }
        }

        private void OnAnimationStateChanged(string name, Storyboard storyboard)
        {
            try
            {
                // Handle animation state changes if needed
                _logger.LogTrace("Animation state changed: {Name}, State: {State}", name, storyboard.GetCurrentState());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling animation state change");
            }
        }

        #endregion

        #region Events

        public event EventHandler<AnimationCompletedEventArgs> AnimationCompleted;
        public event EventHandler<AnimationStartedEventArgs> AnimationStarted;
        public event EventHandler<PerformanceChangedEventArgs> PerformanceChanged;

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopAllAnimations();
                    _performanceTimer?.Stop();
                    CompositionTarget.Rendering -= OnCompositionTargetRendering;
                    _activeAnimations?.Clear();
                    _animationTargets?.Clear();
                    _gpuAcceleratedElements?.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
} 
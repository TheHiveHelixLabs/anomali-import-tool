using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AnomaliImportTool.UI.Services
{
    /// <summary>
    /// Service for managing Material Design timing functions and easing curves
    /// Provides professional animation timing standards and smooth transitions
    /// </summary>
    public interface IMaterialDesignTimingService : INotifyPropertyChanged
    {
        // Standard Material Design Durations
        Duration DurationFast { get; }
        Duration DurationMedium { get; }
        Duration DurationSlow { get; }
        Duration DurationEntering { get; }
        Duration DurationExiting { get; }
        Duration DurationComplex { get; }
        
        // Material Design Easing Functions
        IEasingFunction StandardEasing { get; }
        IEasingFunction DecelerateEasing { get; }
        IEasingFunction AccelerateEasing { get; }
        IEasingFunction SharpEasing { get; }
        IEasingFunction LinearEasing { get; }
        
        // Advanced Easing Curves
        IEasingFunction FastOutSlowInEasing { get; }
        IEasingFunction LinearOutSlowInEasing { get; }
        IEasingFunction FastOutLinearInEasing { get; }
        IEasingFunction EaseInOutEasing { get; }
        IEasingFunction BounceEasing { get; }
        IEasingFunction ElasticEasing { get; }
        
        // Timing Creation Methods
        Duration CreateDuration(double milliseconds);
        Duration CreateDuration(TimeSpan timeSpan);
        IEasingFunction CreateCustomEasing(EasingType type, EasingMode mode, double? parameter = null);
        
        // Animation Timing Helpers
        Duration GetRecommendedDuration(AnimationComplexity complexity);
        IEasingFunction GetRecommendedEasing(AnimationType animationType);
        Duration ScaleDuration(Duration baseDuration, double scaleFactor);
        
        // Material Design Animation Patterns
        AnimationTimingPattern GetEnterPattern();
        AnimationTimingPattern GetExitPattern();
        AnimationTimingPattern GetTransformPattern();
        AnimationTimingPattern GetFadePattern();
        AnimationTimingPattern GetSlidePattern();
        
        // Performance Optimization
        bool IsReducedMotionEnabled { get; }
        Duration GetAccessibleDuration(Duration originalDuration);
        IEasingFunction GetAccessibleEasing(IEasingFunction originalEasing);
        
        // Events
        event EventHandler<TimingChangedEventArgs> TimingChanged;
    }

    public enum EasingType
    {
        Cubic,
        Quadratic,
        Quartic,
        Quintic,
        Sine,
        Exponential,
        Circular,
        Back,
        Bounce,
        Elastic,
        Power
    }

    public enum AnimationComplexity
    {
        Simple,
        Standard,
        Complex,
        Elaborate
    }

    public enum AnimationType
    {
        Fade,
        Slide,
        Scale,
        Rotate,
        Transform,
        Color,
        Layout,
        Navigation
    }

    public class AnimationTimingPattern
    {
        public Duration Duration { get; set; }
        public IEasingFunction EasingFunction { get; set; }
        public double DelayMultiplier { get; set; } = 0.0;
        public string Description { get; set; }
    }

    public class TimingChangedEventArgs : EventArgs
    {
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public bool IsAccessibilityTriggered { get; set; }
    }

    public class MaterialDesignTimingService : ReactiveObject, IMaterialDesignTimingService
    {
        private readonly ILogger<MaterialDesignTimingService> _logger;
        private bool _isReducedMotionEnabled = false;
        
        // Material Design Standard Durations (in milliseconds)
        private const double FastDurationMs = 150;
        private const double MediumDurationMs = 300;
        private const double SlowDurationMs = 500;
        private const double EnteringDurationMs = 225;
        private const double ExitingDurationMs = 195;
        private const double ComplexDurationMs = 375;
        
        // Accessibility scaling factors
        private const double ReducedMotionScaleFactor = 0.1; // Very fast for reduced motion
        private const double AccessibilityMinDurationMs = 50; // Minimum duration for accessibility

        public MaterialDesignTimingService(ILogger<MaterialDesignTimingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            InitializeTimingSystem();
            CheckAccessibilitySettings();
            
            _logger.LogInformation("Material Design Timing Service initialized with standard timing curves");
        }

        #region Standard Material Design Durations

        public Duration DurationFast => CreateDuration(FastDurationMs);
        public Duration DurationMedium => CreateDuration(MediumDurationMs);
        public Duration DurationSlow => CreateDuration(SlowDurationMs);
        public Duration DurationEntering => CreateDuration(EnteringDurationMs);
        public Duration DurationExiting => CreateDuration(ExitingDurationMs);
        public Duration DurationComplex => CreateDuration(ComplexDurationMs);

        #endregion

        #region Material Design Easing Functions

        public IEasingFunction StandardEasing => new CubicEase 
        { 
            EasingMode = EasingMode.EaseInOut 
        };

        public IEasingFunction DecelerateEasing => new CubicEase 
        { 
            EasingMode = EasingMode.EaseOut 
        };

        public IEasingFunction AccelerateEasing => new CubicEase 
        { 
            EasingMode = EasingMode.EaseIn 
        };

        public IEasingFunction SharpEasing => new CubicEase 
        { 
            EasingMode = EasingMode.EaseIn 
        };

        public IEasingFunction LinearEasing => null; // No easing function = linear

        #endregion

        #region Advanced Easing Curves

        public IEasingFunction FastOutSlowInEasing => new CubicEase 
        { 
            EasingMode = EasingMode.EaseOut 
        };

        public IEasingFunction LinearOutSlowInEasing => new QuadraticEase 
        { 
            EasingMode = EasingMode.EaseOut 
        };

        public IEasingFunction FastOutLinearInEasing => new CubicEase 
        { 
            EasingMode = EasingMode.EaseIn 
        };

        public IEasingFunction EaseInOutEasing => new CubicEase 
        { 
            EasingMode = EasingMode.EaseInOut 
        };

        public IEasingFunction BounceEasing => new BounceEase 
        { 
            EasingMode = EasingMode.EaseOut,
            Bounces = 2,
            Bounciness = 1.5
        };

        public IEasingFunction ElasticEasing => new ElasticEase 
        { 
            EasingMode = EasingMode.EaseOut,
            Oscillations = 1,
            Springiness = 3
        };

        #endregion

        #region Timing Creation Methods

        public Duration CreateDuration(double milliseconds)
        {
            try
            {
                var adjustedMs = _isReducedMotionEnabled 
                    ? Math.Max(AccessibilityMinDurationMs, milliseconds * ReducedMotionScaleFactor)
                    : milliseconds;
                
                return new Duration(TimeSpan.FromMilliseconds(adjustedMs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create duration from milliseconds: {Milliseconds}", milliseconds);
                return new Duration(TimeSpan.FromMilliseconds(MediumDurationMs));
            }
        }

        public Duration CreateDuration(TimeSpan timeSpan)
        {
            try
            {
                var adjustedTimeSpan = _isReducedMotionEnabled 
                    ? TimeSpan.FromMilliseconds(Math.Max(AccessibilityMinDurationMs, timeSpan.TotalMilliseconds * ReducedMotionScaleFactor))
                    : timeSpan;
                
                return new Duration(adjustedTimeSpan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create duration from TimeSpan: {TimeSpan}", timeSpan);
                return new Duration(TimeSpan.FromMilliseconds(MediumDurationMs));
            }
        }

        public IEasingFunction CreateCustomEasing(EasingType type, EasingMode mode, double? parameter = null)
        {
            try
            {
                IEasingFunction easingFunction = type switch
                {
                    EasingType.Cubic => new CubicEase { EasingMode = mode },
                    EasingType.Quadratic => new QuadraticEase { EasingMode = mode },
                    EasingType.Quartic => new QuarticEase { EasingMode = mode },
                    EasingType.Quintic => new QuinticEase { EasingMode = mode },
                    EasingType.Sine => new SineEase { EasingMode = mode },
                    EasingType.Exponential => new ExponentialEase { EasingMode = mode, Exponent = parameter ?? 2.0 },
                    EasingType.Circular => new CircleEase { EasingMode = mode },
                    EasingType.Back => new BackEase { EasingMode = mode, Amplitude = parameter ?? 1.0 },
                    EasingType.Bounce => new BounceEase 
                    { 
                        EasingMode = mode, 
                        Bounces = (int)(parameter ?? 3), 
                        Bounciness = 2.0 
                    },
                    EasingType.Elastic => new ElasticEase 
                    { 
                        EasingMode = mode, 
                        Oscillations = (int)(parameter ?? 3), 
                        Springiness = 3.0 
                    },
                    EasingType.Power => new PowerEase { EasingMode = mode, Power = parameter ?? 2.0 },
                    _ => new CubicEase { EasingMode = mode }
                };

                _logger.LogDebug("Created custom easing function: {Type} with mode {Mode}", type, mode);
                return easingFunction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create custom easing function");
                return new CubicEase { EasingMode = mode };
            }
        }

        #endregion

        #region Animation Timing Helpers

        public Duration GetRecommendedDuration(AnimationComplexity complexity)
        {
            try
            {
                var milliseconds = complexity switch
                {
                    AnimationComplexity.Simple => FastDurationMs,
                    AnimationComplexity.Standard => MediumDurationMs,
                    AnimationComplexity.Complex => SlowDurationMs,
                    AnimationComplexity.Elaborate => ComplexDurationMs,
                    _ => MediumDurationMs
                };

                return CreateDuration(milliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recommended duration for complexity: {Complexity}", complexity);
                return DurationMedium;
            }
        }

        public IEasingFunction GetRecommendedEasing(AnimationType animationType)
        {
            try
            {
                return animationType switch
                {
                    AnimationType.Fade => DecelerateEasing,
                    AnimationType.Slide => StandardEasing,
                    AnimationType.Scale => FastOutSlowInEasing,
                    AnimationType.Rotate => StandardEasing,
                    AnimationType.Transform => EaseInOutEasing,
                    AnimationType.Color => LinearOutSlowInEasing,
                    AnimationType.Layout => StandardEasing,
                    AnimationType.Navigation => FastOutSlowInEasing,
                    _ => StandardEasing
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recommended easing for animation type: {AnimationType}", animationType);
                return StandardEasing;
            }
        }

        public Duration ScaleDuration(Duration baseDuration, double scaleFactor)
        {
            try
            {
                if (!baseDuration.HasTimeSpan) return baseDuration;
                
                var scaledMs = baseDuration.TimeSpan.TotalMilliseconds * scaleFactor;
                return CreateDuration(scaledMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to scale duration");
                return baseDuration;
            }
        }

        #endregion

        #region Material Design Animation Patterns

        public AnimationTimingPattern GetEnterPattern()
        {
            return new AnimationTimingPattern
            {
                Duration = DurationEntering,
                EasingFunction = DecelerateEasing,
                DelayMultiplier = 0.0,
                Description = "Material Design enter pattern - elements decelerate as they enter"
            };
        }

        public AnimationTimingPattern GetExitPattern()
        {
            return new AnimationTimingPattern
            {
                Duration = DurationExiting,
                EasingFunction = AccelerateEasing,
                DelayMultiplier = 0.0,
                Description = "Material Design exit pattern - elements accelerate as they exit"
            };
        }

        public AnimationTimingPattern GetTransformPattern()
        {
            return new AnimationTimingPattern
            {
                Duration = DurationMedium,
                EasingFunction = StandardEasing,
                DelayMultiplier = 0.0,
                Description = "Material Design transform pattern - smooth transitions for transformations"
            };
        }

        public AnimationTimingPattern GetFadePattern()
        {
            return new AnimationTimingPattern
            {
                Duration = DurationFast,
                EasingFunction = LinearOutSlowInEasing,
                DelayMultiplier = 0.0,
                Description = "Material Design fade pattern - quick linear start with slow finish"
            };
        }

        public AnimationTimingPattern GetSlidePattern()
        {
            return new AnimationTimingPattern
            {
                Duration = DurationMedium,
                EasingFunction = FastOutSlowInEasing,
                DelayMultiplier = 0.1,
                Description = "Material Design slide pattern - fast start with slow finish and slight delay"
            };
        }

        #endregion

        #region Performance and Accessibility

        public bool IsReducedMotionEnabled
        {
            get => _isReducedMotionEnabled;
            private set
            {
                if (_isReducedMotionEnabled != value)
                {
                    var oldValue = _isReducedMotionEnabled;
                    _isReducedMotionEnabled = value;
                    this.RaisePropertyChanged();
                    
                    TimingChanged?.Invoke(this, new TimingChangedEventArgs
                    {
                        PropertyName = nameof(IsReducedMotionEnabled),
                        OldValue = oldValue,
                        NewValue = value,
                        IsAccessibilityTriggered = true
                    });
                    
                    _logger.LogInformation("Reduced motion {Status}", value ? "enabled" : "disabled");
                }
            }
        }

        public Duration GetAccessibleDuration(Duration originalDuration)
        {
            try
            {
                if (!_isReducedMotionEnabled) return originalDuration;
                
                if (!originalDuration.HasTimeSpan) return originalDuration;
                
                var reducedMs = Math.Max(AccessibilityMinDurationMs, 
                    originalDuration.TimeSpan.TotalMilliseconds * ReducedMotionScaleFactor);
                
                return new Duration(TimeSpan.FromMilliseconds(reducedMs));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get accessible duration");
                return originalDuration;
            }
        }

        public IEasingFunction GetAccessibleEasing(IEasingFunction originalEasing)
        {
            try
            {
                if (!_isReducedMotionEnabled) return originalEasing;
                
                // For reduced motion, use linear easing to minimize visual distraction
                return LinearEasing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get accessible easing");
                return originalEasing;
            }
        }

        #endregion

        #region Advanced Timing Functions

        /// <summary>
        /// Creates a custom cubic bezier easing function
        /// </summary>
        public IEasingFunction CreateCubicBezier(double x1, double y1, double x2, double y2)
        {
            try
            {
                // WPF doesn't have native cubic bezier, so we approximate with existing easing functions
                // This is a simplified implementation - full cubic bezier would require custom easing function
                
                if (x1 <= 0.5 && x2 <= 0.5)
                {
                    return new CubicEase { EasingMode = EasingMode.EaseIn };
                }
                else if (x1 >= 0.5 && x2 >= 0.5)
                {
                    return new CubicEase { EasingMode = EasingMode.EaseOut };
                }
                else
                {
                    return new CubicEase { EasingMode = EasingMode.EaseInOut };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create cubic bezier easing");
                return StandardEasing;
            }
        }

        /// <summary>
        /// Creates a stepped easing function for discrete animations
        /// </summary>
        public IEasingFunction CreateSteppedEasing(int steps, bool jumpStart = false)
        {
            try
            {
                // WPF doesn't have native stepped easing, so we use a custom implementation
                // This would typically require a custom easing function class
                
                _logger.LogDebug("Created stepped easing with {Steps} steps", steps);
                return LinearEasing; // Fallback to linear for now
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create stepped easing");
                return LinearEasing;
            }
        }

        /// <summary>
        /// Gets timing for staggered animations
        /// </summary>
        public IEnumerable<AnimationTimingPattern> GetStaggeredTimingPattern(int itemCount, Duration baseDuration, double staggerDelay = 50)
        {
            try
            {
                var patterns = new List<AnimationTimingPattern>();
                
                for (int i = 0; i < itemCount; i++)
                {
                    patterns.Add(new AnimationTimingPattern
                    {
                        Duration = baseDuration,
                        EasingFunction = StandardEasing,
                        DelayMultiplier = i * (staggerDelay / 1000.0), // Convert to seconds
                        Description = $"Staggered animation item {i + 1} of {itemCount}"
                    });
                }
                
                _logger.LogDebug("Created staggered timing pattern for {ItemCount} items", itemCount);
                return patterns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create staggered timing pattern");
                return new List<AnimationTimingPattern>();
            }
        }

        #endregion

        #region Initialization and Configuration

        private void InitializeTimingSystem()
        {
            try
            {
                // Validate timing constants
                if (FastDurationMs <= 0 || MediumDurationMs <= 0 || SlowDurationMs <= 0)
                {
                    throw new InvalidOperationException("Duration constants must be positive");
                }
                
                _logger.LogDebug("Timing system initialized with Material Design standards");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize timing system");
                throw;
            }
        }

        private void CheckAccessibilitySettings()
        {
            try
            {
                // Check system settings for reduced motion preference
                // This is a simplified check - full implementation would use Windows accessibility APIs
                var reducedMotion = SystemParameters.ClientAreaAnimation == false;
                IsReducedMotionEnabled = reducedMotion;
                
                _logger.LogDebug("Accessibility settings checked - Reduced motion: {ReducedMotion}", reducedMotion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check accessibility settings, using defaults");
                IsReducedMotionEnabled = false;
            }
        }

        /// <summary>
        /// Updates timing based on system performance
        /// </summary>
        public void OptimizeForPerformance(double currentFrameRate)
        {
            try
            {
                if (currentFrameRate < 30) // Low performance
                {
                    _logger.LogInformation("Optimizing timing for low performance (FPS: {FrameRate})", currentFrameRate);
                    // Could adjust durations or disable complex animations
                }
                else if (currentFrameRate >= 60) // High performance
                {
                    _logger.LogDebug("High performance detected (FPS: {FrameRate}), using standard timing", currentFrameRate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize timing for performance");
            }
        }

        #endregion

        #region Events

        public event EventHandler<TimingChangedEventArgs> TimingChanged;

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up resources
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
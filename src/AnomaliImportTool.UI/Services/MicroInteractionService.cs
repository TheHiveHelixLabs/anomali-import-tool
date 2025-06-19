using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AnomaliImportTool.UI.Services
{
    /// <summary>
    /// Service for managing micro-interactions and hover effects
    /// Provides delightful user feedback through subtle animations and visual cues
    /// </summary>
    public interface IMicroInteractionService : INotifyPropertyChanged
    {
        // Hover Effects
        void ApplyButtonHoverEffect(Button button, HoverEffectType effectType = HoverEffectType.Lift);
        void ApplyCardHoverEffect(FrameworkElement card, HoverEffectType effectType = HoverEffectType.Lift);
        void ApplyListItemHoverEffect(FrameworkElement item, HoverEffectType effectType = HoverEffectType.Highlight);
        void ApplyIconHoverEffect(FrameworkElement icon, HoverEffectType effectType = HoverEffectType.Scale);
        
        // Click Interactions
        void ApplyClickRippleEffect(FrameworkElement element, Point clickPoint, Color rippleColor);
        void ApplyButtonClickEffect(Button button, ClickEffectType effectType = ClickEffectType.Press);
        void ApplyCheckboxToggleEffect(CheckBox checkbox);
        void ApplyRadioButtonSelectEffect(RadioButton radioButton);
        
        // Focus Interactions
        void ApplyFocusEffect(FrameworkElement element, FocusEffectType effectType = FocusEffectType.Glow);
        void RemoveFocusEffect(FrameworkElement element);
        void ApplyTabNavigationEffect(FrameworkElement element);
        
        // Loading and Progress Interactions
        void ApplyLoadingSpinnerEffect(FrameworkElement element);
        void ApplyProgressBarPulseEffect(ProgressBar progressBar);
        void ApplySkeletonLoadingEffect(FrameworkElement element);
        
        // Transition Effects
        void ApplySlideInEffect(FrameworkElement element, SlideDirection direction);
        void ApplyFadeInEffect(FrameworkElement element);
        void ApplyScaleInEffect(FrameworkElement element);
        void ApplyBounceInEffect(FrameworkElement element);
        
        // Notification Interactions
        void ApplySuccessNotificationEffect(FrameworkElement element);
        void ApplyErrorNotificationEffect(FrameworkElement element);
        void ApplyWarningNotificationEffect(FrameworkElement element);
        void ApplyInfoNotificationEffect(FrameworkElement element);
        
        // Accessibility Support
        bool IsReducedMotionEnabled { get; }
        void EnableAccessibilityMode(bool enabled);
        void SetAnimationScale(double scale);
        
        // Configuration
        void ConfigureHoverDelay(TimeSpan delay);
        void ConfigureAnimationDuration(MicroInteractionType type, Duration duration);
        void EnableDebugMode(bool enabled);
        
        // Events
        event EventHandler<MicroInteractionEventArgs> InteractionStarted;
        event EventHandler<MicroInteractionEventArgs> InteractionCompleted;
    }

    public enum HoverEffectType
    {
        None,
        Lift,
        Scale,
        Highlight,
        Glow,
        Shadow,
        ColorShift,
        Rotate,
        Bounce
    }

    public enum ClickEffectType
    {
        None,
        Press,
        Ripple,
        Scale,
        Bounce,
        Flash,
        Shake
    }

    public enum FocusEffectType
    {
        None,
        Glow,
        Border,
        Scale,
        Highlight,
        Pulse
    }

    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum MicroInteractionType
    {
        Hover,
        Click,
        Focus,
        Loading,
        Transition,
        Notification
    }

    public class MicroInteractionEventArgs : EventArgs
    {
        public FrameworkElement Element { get; set; }
        public MicroInteractionType InteractionType { get; set; }
        public string EffectName { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsAccessibilityOptimized { get; set; }
    }

    public class MicroInteractionService : ReactiveObject, IMicroInteractionService
    {
        private readonly ILogger<MicroInteractionService> _logger;
        private readonly IMaterialDesignTimingService _timingService;
        private readonly Dictionary<FrameworkElement, List<Storyboard>> _activeAnimations;
        private readonly Dictionary<MicroInteractionType, Duration> _animationDurations;
        
        private bool _isReducedMotionEnabled = false;
        private bool _isDebugModeEnabled = false;
        private double _animationScale = 1.0;
        private TimeSpan _hoverDelay = TimeSpan.FromMilliseconds(100);
        
        // Animation constants
        private const double DefaultLiftDistance = 4.0;
        private const double DefaultScaleFactor = 1.05;
        private const double DefaultGlowRadius = 8.0;
        private const double DefaultShadowDepth = 6.0;

        public MicroInteractionService(
            ILogger<MicroInteractionService> logger,
            IMaterialDesignTimingService timingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timingService = timingService ?? throw new ArgumentNullException(nameof(timingService));
            
            _activeAnimations = new Dictionary<FrameworkElement, List<Storyboard>>();
            _animationDurations = new Dictionary<MicroInteractionType, Duration>();
            
            InitializeMicroInteractionSystem();
            CheckAccessibilitySettings();
            
            _logger.LogInformation("Micro-Interaction Service initialized with delightful effects");
        }

        #region Hover Effects

        public void ApplyButtonHoverEffect(Button button, HoverEffectType effectType = HoverEffectType.Lift)
        {
            try
            {
                if (button == null || _isReducedMotionEnabled) return;
                
                RemoveExistingAnimations(button);
                
                var storyboard = new Storyboard();
                
                switch (effectType)
                {
                    case HoverEffectType.Lift:
                        ApplyLiftHoverEffect(button, storyboard);
                        break;
                    case HoverEffectType.Scale:
                        ApplyScaleHoverEffect(button, storyboard);
                        break;
                    case HoverEffectType.Glow:
                        ApplyGlowHoverEffect(button, storyboard);
                        break;
                }
                
                SetupHoverEventHandlers(button, storyboard);
                TrackAnimation(button, storyboard);
                
                OnInteractionStarted(button, MicroInteractionType.Hover, effectType.ToString());
                
                _logger.LogDebug("Applied {EffectType} hover effect to button", effectType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply button hover effect: {EffectType}", effectType);
            }
        }

        public void ApplyCardHoverEffect(FrameworkElement card, HoverEffectType effectType = HoverEffectType.Lift)
        {
            try
            {
                if (card == null || _isReducedMotionEnabled) return;
                
                RemoveExistingAnimations(card);
                
                var storyboard = new Storyboard();
                
                switch (effectType)
                {
                    case HoverEffectType.Lift:
                        ApplyLiftHoverEffect(card, storyboard);
                        break;
                    case HoverEffectType.Scale:
                        ApplyScaleHoverEffect(card, storyboard);
                        break;
                    case HoverEffectType.Shadow:
                        ApplyCardShadowEffect(card, storyboard);
                        break;
                }
                
                SetupHoverEventHandlers(card, storyboard);
                TrackAnimation(card, storyboard);
                
                OnInteractionStarted(card, MicroInteractionType.Hover, $"Card{effectType}");
                
                _logger.LogDebug("Applied {EffectType} hover effect to card", effectType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply card hover effect: {EffectType}", effectType);
            }
        }

        public void ApplyListItemHoverEffect(FrameworkElement item, HoverEffectType effectType = HoverEffectType.Highlight)
        {
            try
            {
                if (item == null || _isReducedMotionEnabled) return;
                
                RemoveExistingAnimations(item);
                
                var storyboard = new Storyboard();
                
                switch (effectType)
                {
                    case HoverEffectType.Highlight:
                        ApplyHighlightHoverEffect(item, storyboard);
                        break;
                    case HoverEffectType.Scale:
                        ApplySubtleScaleEffect(item, storyboard);
                        break;
                    case HoverEffectType.Glow:
                        ApplySubtleGlowEffect(item, storyboard);
                        break;
                }
                
                SetupHoverEventHandlers(item, storyboard);
                TrackAnimation(item, storyboard);
                
                OnInteractionStarted(item, MicroInteractionType.Hover, $"ListItem{effectType}");
                
                _logger.LogDebug("Applied {EffectType} hover effect to list item", effectType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply list item hover effect: {EffectType}", effectType);
            }
        }

        public void ApplyIconHoverEffect(FrameworkElement icon, HoverEffectType effectType = HoverEffectType.Scale)
        {
            try
            {
                if (icon == null || _isReducedMotionEnabled) return;
                
                RemoveExistingAnimations(icon);
                
                var storyboard = new Storyboard();
                
                switch (effectType)
                {
                    case HoverEffectType.Scale:
                        ApplyIconScaleEffect(icon, storyboard);
                        break;
                    case HoverEffectType.Rotate:
                        ApplyIconRotateEffect(icon, storyboard);
                        break;
                    case HoverEffectType.Bounce:
                        ApplyIconBounceEffect(icon, storyboard);
                        break;
                    case HoverEffectType.Glow:
                        ApplyIconGlowEffect(icon, storyboard);
                        break;
                }
                
                SetupHoverEventHandlers(icon, storyboard);
                TrackAnimation(icon, storyboard);
                
                OnInteractionStarted(icon, MicroInteractionType.Hover, $"Icon{effectType}");
                
                _logger.LogDebug("Applied {EffectType} hover effect to icon", effectType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply icon hover effect: {EffectType}", effectType);
            }
        }

        #endregion

        #region Click Interactions

        public void ApplyClickRippleEffect(FrameworkElement element, Point clickPoint, Color rippleColor)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var rippleEffect = CreateRippleEffect(element, clickPoint, rippleColor);
                if (rippleEffect != null)
                {
                    TrackAnimation(element, rippleEffect);
                    rippleEffect.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Click, "Ripple");
                    
                    _logger.LogDebug("Applied ripple effect at point {X}, {Y}", clickPoint.X, clickPoint.Y);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply ripple effect");
            }
        }

        public void ApplyButtonClickEffect(Button button, ClickEffectType effectType = ClickEffectType.Press)
        {
            try
            {
                if (button == null || _isReducedMotionEnabled) return;
                
                var storyboard = new Storyboard();
                
                switch (effectType)
                {
                    case ClickEffectType.Press:
                        ApplyPressEffect(button, storyboard);
                        break;
                    case ClickEffectType.Scale:
                        ApplyClickScaleEffect(button, storyboard);
                        break;
                    case ClickEffectType.Bounce:
                        ApplyClickBounceEffect(button, storyboard);
                        break;
                    case ClickEffectType.Flash:
                        ApplyFlashEffect(button, storyboard);
                        break;
                }
                
                TrackAnimation(button, storyboard);
                storyboard.Begin();
                
                OnInteractionStarted(button, MicroInteractionType.Click, effectType.ToString());
                
                _logger.LogDebug("Applied {EffectType} click effect to button", effectType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply button click effect: {EffectType}", effectType);
            }
        }

        public void ApplyCheckboxToggleEffect(CheckBox checkbox)
        {
            try
            {
                if (checkbox == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateCheckboxToggleAnimation(checkbox);
                if (storyboard != null)
                {
                    TrackAnimation(checkbox, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(checkbox, MicroInteractionType.Click, "CheckboxToggle");
                    
                    _logger.LogDebug("Applied checkbox toggle effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply checkbox toggle effect");
            }
        }

        public void ApplyRadioButtonSelectEffect(RadioButton radioButton)
        {
            try
            {
                if (radioButton == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateRadioButtonSelectAnimation(radioButton);
                if (storyboard != null)
                {
                    TrackAnimation(radioButton, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(radioButton, MicroInteractionType.Click, "RadioButtonSelect");
                    
                    _logger.LogDebug("Applied radio button select effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply radio button select effect");
            }
        }

        #endregion

        #region Focus Interactions

        public void ApplyFocusEffect(FrameworkElement element, FocusEffectType effectType = FocusEffectType.Glow)
        {
            try
            {
                if (element == null) return;
                
                RemoveExistingAnimations(element);
                
                var storyboard = new Storyboard();
                
                switch (effectType)
                {
                    case FocusEffectType.Glow:
                        ApplyFocusGlowEffect(element, storyboard);
                        break;
                    case FocusEffectType.Border:
                        ApplyFocusBorderEffect(element, storyboard);
                        break;
                    case FocusEffectType.Scale:
                        ApplyFocusScaleEffect(element, storyboard);
                        break;
                    case FocusEffectType.Pulse:
                        ApplyFocusPulseEffect(element, storyboard);
                        break;
                }
                
                TrackAnimation(element, storyboard);
                storyboard.Begin();
                
                OnInteractionStarted(element, MicroInteractionType.Focus, effectType.ToString());
                
                _logger.LogDebug("Applied {EffectType} focus effect", effectType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply focus effect: {EffectType}", effectType);
            }
        }

        public void RemoveFocusEffect(FrameworkElement element)
        {
            try
            {
                if (element == null) return;
                
                RemoveExistingAnimations(element);
                
                // Reset element properties
                element.Effect = null;
                element.RenderTransform = Transform.Identity;
                
                _logger.LogDebug("Removed focus effects from element");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove focus effect");
            }
        }

        public void ApplyTabNavigationEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateTabNavigationAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Focus, "TabNavigation");
                    
                    _logger.LogDebug("Applied tab navigation effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply tab navigation effect");
            }
        }

        #endregion

        #region Loading and Progress Interactions

        public void ApplyLoadingSpinnerEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateLoadingSpinnerAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.RepeatBehavior = RepeatBehavior.Forever;
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Loading, "Spinner");
                    
                    _logger.LogDebug("Applied loading spinner effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply loading spinner effect");
            }
        }

        public void ApplyProgressBarPulseEffect(ProgressBar progressBar)
        {
            try
            {
                if (progressBar == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateProgressBarPulseAnimation(progressBar);
                if (storyboard != null)
                {
                    TrackAnimation(progressBar, storyboard);
                    storyboard.RepeatBehavior = RepeatBehavior.Forever;
                    storyboard.Begin();
                    
                    OnInteractionStarted(progressBar, MicroInteractionType.Loading, "ProgressPulse");
                    
                    _logger.LogDebug("Applied progress bar pulse effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply progress bar pulse effect");
            }
        }

        public void ApplySkeletonLoadingEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateSkeletonLoadingAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.RepeatBehavior = RepeatBehavior.Forever;
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Loading, "Skeleton");
                    
                    _logger.LogDebug("Applied skeleton loading effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply skeleton loading effect");
            }
        }

        #endregion

        #region Transition Effects

        public void ApplySlideInEffect(FrameworkElement element, SlideDirection direction)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateSlideInAnimation(element, direction);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Transition, $"SlideIn{direction}");
                    
                    _logger.LogDebug("Applied slide in effect: {Direction}", direction);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply slide in effect: {Direction}", direction);
            }
        }

        public void ApplyFadeInEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateFadeInAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Transition, "FadeIn");
                    
                    _logger.LogDebug("Applied fade in effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply fade in effect");
            }
        }

        public void ApplyScaleInEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateScaleInAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Transition, "ScaleIn");
                    
                    _logger.LogDebug("Applied scale in effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply scale in effect");
            }
        }

        public void ApplyBounceInEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateBounceInAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Transition, "BounceIn");
                    
                    _logger.LogDebug("Applied bounce in effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply bounce in effect");
            }
        }

        #endregion

        #region Notification Interactions

        public void ApplySuccessNotificationEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateSuccessNotificationAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Notification, "Success");
                    
                    _logger.LogDebug("Applied success notification effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply success notification effect");
            }
        }

        public void ApplyErrorNotificationEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateErrorNotificationAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Notification, "Error");
                    
                    _logger.LogDebug("Applied error notification effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply error notification effect");
            }
        }

        public void ApplyWarningNotificationEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateWarningNotificationAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Notification, "Warning");
                    
                    _logger.LogDebug("Applied warning notification effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply warning notification effect");
            }
        }

        public void ApplyInfoNotificationEffect(FrameworkElement element)
        {
            try
            {
                if (element == null || _isReducedMotionEnabled) return;
                
                var storyboard = CreateInfoNotificationAnimation(element);
                if (storyboard != null)
                {
                    TrackAnimation(element, storyboard);
                    storyboard.Begin();
                    
                    OnInteractionStarted(element, MicroInteractionType.Notification, "Info");
                    
                    _logger.LogDebug("Applied info notification effect");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply info notification effect");
            }
        }

        #endregion

        #region Private Animation Creation Methods

        private void ApplyLiftHoverEffect(FrameworkElement element, Storyboard storyboard)
        {
            var transform = EnsureRenderTransform<TranslateTransform>(element);
            
            var liftAnimation = new DoubleAnimation
            {
                To = -DefaultLiftDistance * _animationScale,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            Storyboard.SetTarget(liftAnimation, element);
            Storyboard.SetTargetProperty(liftAnimation, new PropertyPath("RenderTransform.Y"));
            storyboard.Children.Add(liftAnimation);
        }

        private void ApplyScaleHoverEffect(FrameworkElement element, Storyboard storyboard)
        {
            var transform = EnsureRenderTransform<ScaleTransform>(element);
            
            var scaleXAnimation = new DoubleAnimation
            {
                To = DefaultScaleFactor * _animationScale,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            var scaleYAnimation = new DoubleAnimation
            {
                To = DefaultScaleFactor * _animationScale,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            Storyboard.SetTarget(scaleXAnimation, element);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
            storyboard.Children.Add(scaleXAnimation);
            
            Storyboard.SetTarget(scaleYAnimation, element);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
            storyboard.Children.Add(scaleYAnimation);
        }

        private void ApplyGlowHoverEffect(FrameworkElement element, Storyboard storyboard)
        {
            var glowEffect = new DropShadowEffect
            {
                Color = Colors.LightBlue,
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = 0,
                Opacity = 0
            };
            
            element.Effect = glowEffect;
            
            var glowAnimation = new DoubleAnimation
            {
                To = DefaultGlowRadius * _animationScale,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            var opacityAnimation = new DoubleAnimation
            {
                To = 0.6,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            Storyboard.SetTarget(glowAnimation, element);
            Storyboard.SetTargetProperty(glowAnimation, new PropertyPath("Effect.BlurRadius"));
            storyboard.Children.Add(glowAnimation);
            
            Storyboard.SetTarget(opacityAnimation, element);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Effect.Opacity"));
            storyboard.Children.Add(opacityAnimation);
        }

        private Storyboard CreateRippleEffect(FrameworkElement element, Point clickPoint, Color rippleColor)
        {
            var storyboard = new Storyboard();
            
            var scaleAnimation = new DoubleAnimation
            {
                From = 0.1,
                To = 2.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            var opacityAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 0.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.LinearOutSlowInEasing
            };
            
            Storyboard.SetTarget(scaleAnimation, element);
            Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("RenderTransform.ScaleX"));
            storyboard.Children.Add(scaleAnimation);
            
            Storyboard.SetTarget(opacityAnimation, element);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(opacityAnimation);
            
            return storyboard;
        }

        private T EnsureRenderTransform<T>(FrameworkElement element) where T : Transform, new()
        {
            if (element.RenderTransform is not TransformGroup transformGroup)
            {
                transformGroup = new TransformGroup();
                element.RenderTransform = transformGroup;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            
            var existingTransform = transformGroup.Children.OfType<T>().FirstOrDefault();
            if (existingTransform == null)
            {
                existingTransform = new T();
                transformGroup.Children.Add(existingTransform);
            }
            
            return existingTransform;
        }

        #endregion

        #region Event Handling and Tracking

        private void SetupHoverEventHandlers(FrameworkElement element, Storyboard enterStoryboard)
        {
            var exitStoryboard = CreateExitStoryboard(enterStoryboard);
            
            element.MouseEnter += (s, e) =>
            {
                if (_isReducedMotionEnabled) return;
                exitStoryboard.Stop();
                enterStoryboard.Begin();
            };
            
            element.MouseLeave += (s, e) =>
            {
                if (_isReducedMotionEnabled) return;
                enterStoryboard.Stop();
                exitStoryboard.Begin();
            };
        }

        private Storyboard CreateExitStoryboard(Storyboard enterStoryboard)
        {
            var exitStoryboard = new Storyboard();
            
            foreach (var animation in enterStoryboard.Children.OfType<DoubleAnimation>())
            {
                var exitAnimation = new DoubleAnimation
                {
                    To = animation.From ?? 0,
                    Duration = _timingService.DurationFast,
                    EasingFunction = _timingService.AccelerateEasing
                };
                
                Storyboard.SetTarget(exitAnimation, Storyboard.GetTarget(animation));
                Storyboard.SetTargetProperty(exitAnimation, Storyboard.GetTargetProperty(animation));
                exitStoryboard.Children.Add(exitAnimation);
            }
            
            return exitStoryboard;
        }

        private void TrackAnimation(FrameworkElement element, Storyboard storyboard)
        {
            if (!_activeAnimations.ContainsKey(element))
            {
                _activeAnimations[element] = new List<Storyboard>();
            }
            
            _activeAnimations[element].Add(storyboard);
            
            storyboard.Completed += (s, e) =>
            {
                if (_activeAnimations.ContainsKey(element))
                {
                    _activeAnimations[element].Remove(storyboard);
                    if (_activeAnimations[element].Count == 0)
                    {
                        _activeAnimations.Remove(element);
                    }
                }
            };
        }

        private void RemoveExistingAnimations(FrameworkElement element)
        {
            if (_activeAnimations.ContainsKey(element))
            {
                foreach (var storyboard in _activeAnimations[element])
                {
                    storyboard.Stop();
                }
                _activeAnimations[element].Clear();
                _activeAnimations.Remove(element);
            }
        }

        #endregion

        #region Accessibility and Configuration

        public bool IsReducedMotionEnabled
        {
            get => _isReducedMotionEnabled;
            private set
            {
                if (_isReducedMotionEnabled != value)
                {
                    _isReducedMotionEnabled = value;
                    this.RaisePropertyChanged();
                    
                    _logger.LogInformation("Reduced motion {Status} for micro-interactions", 
                        value ? "enabled" : "disabled");
                }
            }
        }

        public void EnableAccessibilityMode(bool enabled)
        {
            try
            {
                IsReducedMotionEnabled = enabled;
                _animationScale = enabled ? 0.5 : 1.0;
                
                _logger.LogInformation("Accessibility mode {Status} for micro-interactions", 
                    enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle accessibility mode");
            }
        }

        public void SetAnimationScale(double scale)
        {
            try
            {
                _animationScale = Math.Max(0.1, Math.Min(2.0, scale));
                
                _logger.LogDebug("Animation scale set to {Scale}", _animationScale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set animation scale");
            }
        }

        public void ConfigureHoverDelay(TimeSpan delay)
        {
            try
            {
                _hoverDelay = delay;
                
                _logger.LogDebug("Hover delay configured to {Delay}ms", delay.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure hover delay");
            }
        }

        public void ConfigureAnimationDuration(MicroInteractionType type, Duration duration)
        {
            try
            {
                _animationDurations[type] = duration;
                
                _logger.LogDebug("Animation duration for {Type} set to {Duration}ms", 
                    type, duration.TimeSpan.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure animation duration for {Type}", type);
            }
        }

        public void EnableDebugMode(bool enabled)
        {
            try
            {
                _isDebugModeEnabled = enabled;
                
                _logger.LogInformation("Debug mode {Status} for micro-interactions", 
                    enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle debug mode");
            }
        }

        #endregion

        #region Initialization and Utility Methods

        private void InitializeMicroInteractionSystem()
        {
            try
            {
                // Initialize default animation durations
                _animationDurations[MicroInteractionType.Hover] = _timingService.DurationFast;
                _animationDurations[MicroInteractionType.Click] = _timingService.DurationFast;
                _animationDurations[MicroInteractionType.Focus] = _timingService.DurationMedium;
                _animationDurations[MicroInteractionType.Loading] = _timingService.DurationSlow;
                _animationDurations[MicroInteractionType.Transition] = _timingService.DurationMedium;
                _animationDurations[MicroInteractionType.Notification] = _timingService.DurationSlow;
                
                _logger.LogDebug("Micro-interaction system initialized with default settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize micro-interaction system");
                throw;
            }
        }

        private void CheckAccessibilitySettings()
        {
            try
            {
                // Check system settings for reduced motion preference
                var reducedMotion = SystemParameters.ClientAreaAnimation == false;
                IsReducedMotionEnabled = reducedMotion;
                
                _logger.LogDebug("Accessibility settings checked for micro-interactions - Reduced motion: {ReducedMotion}", 
                    reducedMotion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check accessibility settings for micro-interactions");
                IsReducedMotionEnabled = false;
            }
        }

        #endregion

        #region Events

        public event EventHandler<MicroInteractionEventArgs> InteractionStarted;
        public event EventHandler<MicroInteractionEventArgs> InteractionCompleted;

        private void OnInteractionStarted(FrameworkElement element, MicroInteractionType type, string effectName)
        {
            try
            {
                InteractionStarted?.Invoke(this, new MicroInteractionEventArgs
                {
                    Element = element,
                    InteractionType = type,
                    EffectName = effectName,
                    Timestamp = DateTime.Now,
                    IsAccessibilityOptimized = _isReducedMotionEnabled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise InteractionStarted event");
            }
        }

        private void OnInteractionCompleted(FrameworkElement element, MicroInteractionType type, string effectName)
        {
            try
            {
                InteractionCompleted?.Invoke(this, new MicroInteractionEventArgs
                {
                    Element = element,
                    InteractionType = type,
                    EffectName = effectName,
                    Timestamp = DateTime.Now,
                    IsAccessibilityOptimized = _isReducedMotionEnabled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise InteractionCompleted event");
            }
        }

        #endregion

        #region IDisposable Support

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Stop all active animations
                    foreach (var animations in _activeAnimations.Values)
                    {
                        foreach (var storyboard in animations)
                        {
                            storyboard.Stop();
                        }
                    }
                    _activeAnimations.Clear();
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

        #region Simplified Animation Creation Methods

        private Storyboard CreateCheckboxToggleAnimation(CheckBox checkbox) => new Storyboard();
        private Storyboard CreateRadioButtonSelectAnimation(RadioButton radioButton) => new Storyboard();
        private Storyboard CreateTabNavigationAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateLoadingSpinnerAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateProgressBarPulseAnimation(ProgressBar progressBar) => new Storyboard();
        private Storyboard CreateSkeletonLoadingAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateSlideInAnimation(FrameworkElement element, SlideDirection direction) => new Storyboard();
        private Storyboard CreateFadeInAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateScaleInAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateBounceInAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateSuccessNotificationAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateErrorNotificationAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateWarningNotificationAnimation(FrameworkElement element) => new Storyboard();
        private Storyboard CreateInfoNotificationAnimation(FrameworkElement element) => new Storyboard();

        #endregion
    }
} 
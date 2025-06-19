using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace AnomaliImportTool.UI.Services
{
    /// <summary>
    /// Service for managing page transitions and navigation animations
    /// Provides smooth, professional navigation experiences with accessibility support
    /// </summary>
    public interface INavigationAnimationService : INotifyPropertyChanged
    {
        // Page Transitions
        Task TransitionToPageAsync(UserControl fromPage, UserControl toPage, PageTransitionType transitionType = PageTransitionType.SlideLeft);
        Task FadeInPageAsync(UserControl page);
        Task FadeOutPageAsync(UserControl page);
        Task SlideInPageAsync(UserControl page, SlideDirection direction);
        Task SlideOutPageAsync(UserControl page, SlideDirection direction);
        
        // Navigation Animations
        Task AnimateNavigationMenuAsync(FrameworkElement menu, NavigationAnimationType animationType);
        Task AnimateBreadcrumbAsync(FrameworkElement breadcrumb, BreadcrumbAnimationType animationType);
        Task AnimateTabSwitchAsync(TabControl tabControl, int fromIndex, int toIndex);
        Task AnimateModalOpenAsync(FrameworkElement modal);
        Task AnimateModalCloseAsync(FrameworkElement modal);
        
        // Loading Transitions
        Task ShowLoadingTransitionAsync(FrameworkElement container);
        Task HideLoadingTransitionAsync(FrameworkElement container);
        Task AnimateContentLoadingAsync(FrameworkElement content);
        
        // Wizard and Stepper Animations
        Task AnimateWizardStepTransitionAsync(FrameworkElement fromStep, FrameworkElement toStep, WizardTransitionDirection direction);
        Task AnimateStepperProgressAsync(FrameworkElement stepper, int currentStep, int totalSteps);
        Task AnimateStepValidationAsync(FrameworkElement step, bool isValid);
        
        // Layout Animations
        Task AnimateLayoutChangeAsync(Panel container, LayoutChangeType changeType);
        Task AnimateItemAdditionAsync(FrameworkElement item, Panel container);
        Task AnimateItemRemovalAsync(FrameworkElement item, Panel container);
        Task AnimateItemReorderAsync(FrameworkElement item, Point fromPosition, Point toPosition);
        
        // Configuration
        bool IsReducedMotionEnabled { get; }
        void SetTransitionDuration(PageTransitionType transitionType, Duration duration);
        void EnableParallaxEffect(bool enabled);
        void SetAnimationQuality(AnimationQuality quality);
        
        // Events
        event EventHandler<NavigationAnimationEventArgs> TransitionStarted;
        event EventHandler<NavigationAnimationEventArgs> TransitionCompleted;
        event EventHandler<NavigationAnimationEventArgs> TransitionFailed;
    }

    public enum PageTransitionType
    {
        None,
        Fade,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
        Scale,
        Flip,
        Cube,
        Push,
        Cover,
        Reveal
    }

    public enum NavigationAnimationType
    {
        SlideIn,
        SlideOut,
        FadeIn,
        FadeOut,
        Expand,
        Collapse,
        Bounce
    }

    public enum BreadcrumbAnimationType
    {
        SlideIn,
        FadeIn,
        TypeWriter,
        Highlight
    }

    public enum WizardTransitionDirection
    {
        Forward,
        Backward,
        Skip,
        Jump
    }

    public enum LayoutChangeType
    {
        Resize,
        Reflow,
        Reorganize,
        Filter,
        Sort
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

    public class NavigationAnimationEventArgs : EventArgs
    {
        public PageTransitionType TransitionType { get; set; }
        public UserControl FromPage { get; set; }
        public UserControl ToPage { get; set; }
        public Duration Duration { get; set; }
        public bool IsAccessibilityOptimized { get; set; }
        public Exception Error { get; set; }
    }

    public class NavigationAnimationService : ReactiveObject, INavigationAnimationService
    {
        private readonly ILogger<NavigationAnimationService> _logger;
        private readonly IMaterialDesignTimingService _timingService;
        private readonly Dictionary<PageTransitionType, Duration> _transitionDurations;
        private readonly Dictionary<FrameworkElement, Storyboard> _activeTransitions;
        
        private bool _isReducedMotionEnabled = false;
        private bool _isParallaxEnabled = true;
        private AnimationQuality _animationQuality = AnimationQuality.High;
        
        // Animation constants
        private const double DefaultSlideDistance = 100.0;
        private const double DefaultScaleFactor = 0.95;
        private const double DefaultFadeOpacity = 0.0;

        public NavigationAnimationService(
            ILogger<NavigationAnimationService> logger,
            IMaterialDesignTimingService timingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timingService = timingService ?? throw new ArgumentNullException(nameof(timingService));
            
            _transitionDurations = new Dictionary<PageTransitionType, Duration>();
            _activeTransitions = new Dictionary<FrameworkElement, Storyboard>();
            
            InitializeNavigationAnimationSystem();
            CheckAccessibilitySettings();
            
            _logger.LogInformation("Navigation Animation Service initialized with smooth transitions");
        }

        #region Page Transitions

        public async Task TransitionToPageAsync(UserControl fromPage, UserControl toPage, PageTransitionType transitionType = PageTransitionType.SlideLeft)
        {
            try
            {
                if (fromPage == null || toPage == null)
                {
                    _logger.LogWarning("Cannot transition with null pages");
                    return;
                }

                if (_isReducedMotionEnabled)
                {
                    transitionType = PageTransitionType.Fade;
                }

                OnTransitionStarted(transitionType, fromPage, toPage);
                
                var duration = GetTransitionDuration(transitionType);
                
                // Prepare target page
                toPage.Opacity = 0;
                toPage.Visibility = Visibility.Visible;
                
                var fromStoryboard = CreateExitAnimation(fromPage, transitionType, duration);
                var toStoryboard = CreateEnterAnimation(toPage, transitionType, duration);
                
                // Track animations
                TrackTransition(fromPage, fromStoryboard);
                TrackTransition(toPage, toStoryboard);
                
                // Start exit animation
                if (fromStoryboard != null)
                {
                    fromStoryboard.Begin();
                }
                
                // Start enter animation with slight delay for smoother transition
                await Task.Delay(50);
                if (toStoryboard != null)
                {
                    toStoryboard.Begin();
                }
                
                // Wait for transition to complete
                await Task.Delay(duration.TimeSpan);
                
                // Clean up
                fromPage.Visibility = Visibility.Collapsed;
                toPage.Opacity = 1;
                
                OnTransitionCompleted(transitionType, fromPage, toPage);
                
                _logger.LogDebug("Page transition completed: {TransitionType}", transitionType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transition pages with type: {TransitionType}", transitionType);
                OnTransitionFailed(transitionType, fromPage, toPage, ex);
                throw;
            }
        }

        public async Task FadeInPageAsync(UserControl page)
        {
            try
            {
                if (page == null) return;
                
                var storyboard = CreateFadeInAnimation(page);
                if (storyboard != null)
                {
                    TrackTransition(page, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Page fade in completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fade in page");
                throw;
            }
        }

        public async Task FadeOutPageAsync(UserControl page)
        {
            try
            {
                if (page == null) return;
                
                var storyboard = CreateFadeOutAnimation(page);
                if (storyboard != null)
                {
                    TrackTransition(page, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Page fade out completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fade out page");
                throw;
            }
        }

        public async Task SlideInPageAsync(UserControl page, SlideDirection direction)
        {
            try
            {
                if (page == null) return;
                
                var storyboard = CreateSlideInAnimation(page, direction);
                if (storyboard != null)
                {
                    TrackTransition(page, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Page slide in completed: {Direction}", direction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to slide in page: {Direction}", direction);
                throw;
            }
        }

        public async Task SlideOutPageAsync(UserControl page, SlideDirection direction)
        {
            try
            {
                if (page == null) return;
                
                var storyboard = CreateSlideOutAnimation(page, direction);
                if (storyboard != null)
                {
                    TrackTransition(page, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Page slide out completed: {Direction}", direction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to slide out page: {Direction}", direction);
                throw;
            }
        }

        #endregion

        #region Navigation Animations

        public async Task AnimateNavigationMenuAsync(FrameworkElement menu, NavigationAnimationType animationType)
        {
            try
            {
                if (menu == null) return;
                
                var storyboard = CreateNavigationMenuAnimation(menu, animationType);
                if (storyboard != null)
                {
                    TrackTransition(menu, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Navigation menu animation completed: {AnimationType}", animationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate navigation menu: {AnimationType}", animationType);
                throw;
            }
        }

        public async Task AnimateBreadcrumbAsync(FrameworkElement breadcrumb, BreadcrumbAnimationType animationType)
        {
            try
            {
                if (breadcrumb == null) return;
                
                var storyboard = CreateBreadcrumbAnimation(breadcrumb, animationType);
                if (storyboard != null)
                {
                    TrackTransition(breadcrumb, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationFast.TimeSpan);
                }
                
                _logger.LogDebug("Breadcrumb animation completed: {AnimationType}", animationType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate breadcrumb: {AnimationType}", animationType);
                throw;
            }
        }

        public async Task AnimateTabSwitchAsync(TabControl tabControl, int fromIndex, int toIndex)
        {
            try
            {
                if (tabControl == null) return;
                
                var storyboard = CreateTabSwitchAnimation(tabControl, fromIndex, toIndex);
                if (storyboard != null)
                {
                    TrackTransition(tabControl, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationFast.TimeSpan);
                }
                
                _logger.LogDebug("Tab switch animation completed: {FromIndex} -> {ToIndex}", fromIndex, toIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate tab switch: {FromIndex} -> {ToIndex}", fromIndex, toIndex);
                throw;
            }
        }

        public async Task AnimateModalOpenAsync(FrameworkElement modal)
        {
            try
            {
                if (modal == null) return;
                
                var storyboard = CreateModalOpenAnimation(modal);
                if (storyboard != null)
                {
                    TrackTransition(modal, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Modal open animation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate modal open");
                throw;
            }
        }

        public async Task AnimateModalCloseAsync(FrameworkElement modal)
        {
            try
            {
                if (modal == null) return;
                
                var storyboard = CreateModalCloseAnimation(modal);
                if (storyboard != null)
                {
                    TrackTransition(modal, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Modal close animation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate modal close");
                throw;
            }
        }

        #endregion

        #region Loading Transitions

        public async Task ShowLoadingTransitionAsync(FrameworkElement container)
        {
            try
            {
                if (container == null) return;
                
                var storyboard = CreateLoadingShowAnimation(container);
                if (storyboard != null)
                {
                    TrackTransition(container, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationFast.TimeSpan);
                }
                
                _logger.LogDebug("Loading transition show completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show loading transition");
                throw;
            }
        }

        public async Task HideLoadingTransitionAsync(FrameworkElement container)
        {
            try
            {
                if (container == null) return;
                
                var storyboard = CreateLoadingHideAnimation(container);
                if (storyboard != null)
                {
                    TrackTransition(container, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationFast.TimeSpan);
                }
                
                _logger.LogDebug("Loading transition hide completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hide loading transition");
                throw;
            }
        }

        public async Task AnimateContentLoadingAsync(FrameworkElement content)
        {
            try
            {
                if (content == null) return;
                
                var storyboard = CreateContentLoadingAnimation(content);
                if (storyboard != null)
                {
                    TrackTransition(content, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Content loading animation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate content loading");
                throw;
            }
        }

        #endregion

        #region Wizard and Stepper Animations

        public async Task AnimateWizardStepTransitionAsync(FrameworkElement fromStep, FrameworkElement toStep, WizardTransitionDirection direction)
        {
            try
            {
                if (fromStep == null || toStep == null) return;
                
                var storyboard = CreateWizardStepTransitionAnimation(fromStep, toStep, direction);
                if (storyboard != null)
                {
                    TrackTransition(fromStep, storyboard);
                    TrackTransition(toStep, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Wizard step transition completed: {Direction}", direction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate wizard step transition: {Direction}", direction);
                throw;
            }
        }

        public async Task AnimateStepperProgressAsync(FrameworkElement stepper, int currentStep, int totalSteps)
        {
            try
            {
                if (stepper == null) return;
                
                var storyboard = CreateStepperProgressAnimation(stepper, currentStep, totalSteps);
                if (storyboard != null)
                {
                    TrackTransition(stepper, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Stepper progress animation completed: {CurrentStep}/{TotalSteps}", currentStep, totalSteps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate stepper progress: {CurrentStep}/{TotalSteps}", currentStep, totalSteps);
                throw;
            }
        }

        public async Task AnimateStepValidationAsync(FrameworkElement step, bool isValid)
        {
            try
            {
                if (step == null) return;
                
                var storyboard = CreateStepValidationAnimation(step, isValid);
                if (storyboard != null)
                {
                    TrackTransition(step, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationFast.TimeSpan);
                }
                
                _logger.LogDebug("Step validation animation completed: {IsValid}", isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate step validation: {IsValid}", isValid);
                throw;
            }
        }

        #endregion

        #region Layout Animations

        public async Task AnimateLayoutChangeAsync(Panel container, LayoutChangeType changeType)
        {
            try
            {
                if (container == null) return;
                
                var storyboard = CreateLayoutChangeAnimation(container, changeType);
                if (storyboard != null)
                {
                    TrackTransition(container, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Layout change animation completed: {ChangeType}", changeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate layout change: {ChangeType}", changeType);
                throw;
            }
        }

        public async Task AnimateItemAdditionAsync(FrameworkElement item, Panel container)
        {
            try
            {
                if (item == null || container == null) return;
                
                var storyboard = CreateItemAdditionAnimation(item);
                if (storyboard != null)
                {
                    TrackTransition(item, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationFast.TimeSpan);
                }
                
                _logger.LogDebug("Item addition animation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate item addition");
                throw;
            }
        }

        public async Task AnimateItemRemovalAsync(FrameworkElement item, Panel container)
        {
            try
            {
                if (item == null || container == null) return;
                
                var storyboard = CreateItemRemovalAnimation(item);
                if (storyboard != null)
                {
                    TrackTransition(item, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationFast.TimeSpan);
                }
                
                _logger.LogDebug("Item removal animation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate item removal");
                throw;
            }
        }

        public async Task AnimateItemReorderAsync(FrameworkElement item, Point fromPosition, Point toPosition)
        {
            try
            {
                if (item == null) return;
                
                var storyboard = CreateItemReorderAnimation(item, fromPosition, toPosition);
                if (storyboard != null)
                {
                    TrackTransition(item, storyboard);
                    storyboard.Begin();
                    
                    await Task.Delay(_timingService.DurationMedium.TimeSpan);
                }
                
                _logger.LogDebug("Item reorder animation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to animate item reorder");
                throw;
            }
        }

        #endregion

        #region Private Animation Creation Methods

        private Storyboard CreateExitAnimation(UserControl page, PageTransitionType transitionType, Duration duration)
        {
            var storyboard = new Storyboard();
            
            switch (transitionType)
            {
                case PageTransitionType.Fade:
                    CreateFadeAnimation(storyboard, page, 1.0, 0.0, duration);
                    break;
                case PageTransitionType.SlideLeft:
                    CreateSlideAnimation(storyboard, page, 0, -DefaultSlideDistance, duration);
                    break;
                case PageTransitionType.SlideRight:
                    CreateSlideAnimation(storyboard, page, 0, DefaultSlideDistance, duration);
                    break;
                case PageTransitionType.Scale:
                    CreateScaleAnimation(storyboard, page, 1.0, DefaultScaleFactor, duration);
                    break;
            }
            
            return storyboard;
        }

        private Storyboard CreateEnterAnimation(UserControl page, PageTransitionType transitionType, Duration duration)
        {
            var storyboard = new Storyboard();
            
            switch (transitionType)
            {
                case PageTransitionType.Fade:
                    CreateFadeAnimation(storyboard, page, 0.0, 1.0, duration);
                    break;
                case PageTransitionType.SlideLeft:
                    SetInitialPosition(page, DefaultSlideDistance, 0);
                    CreateSlideAnimation(storyboard, page, DefaultSlideDistance, 0, duration);
                    break;
                case PageTransitionType.SlideRight:
                    SetInitialPosition(page, -DefaultSlideDistance, 0);
                    CreateSlideAnimation(storyboard, page, -DefaultSlideDistance, 0, duration);
                    break;
                case PageTransitionType.Scale:
                    SetInitialScale(page, DefaultScaleFactor);
                    CreateScaleAnimation(storyboard, page, DefaultScaleFactor, 1.0, duration);
                    break;
            }
            
            return storyboard;
        }

        private void CreateFadeAnimation(Storyboard storyboard, FrameworkElement element, double from, double to, Duration duration)
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeAnimation);
        }

        private void CreateSlideAnimation(Storyboard storyboard, FrameworkElement element, double from, double to, Duration duration)
        {
            EnsureRenderTransform(element);
            
            var slideAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = _timingService.FastOutSlowInEasing
            };
            
            Storyboard.SetTarget(slideAnimation, element);
            Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("RenderTransform.X"));
            storyboard.Children.Add(slideAnimation);
        }

        private void CreateScaleAnimation(Storyboard storyboard, FrameworkElement element, double from, double to, Duration duration)
        {
            EnsureRenderTransform(element);
            
            var scaleXAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            var scaleYAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            Storyboard.SetTarget(scaleXAnimation, element);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
            storyboard.Children.Add(scaleXAnimation);
            
            Storyboard.SetTarget(scaleYAnimation, element);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
            storyboard.Children.Add(scaleYAnimation);
        }

        private void SetInitialPosition(FrameworkElement element, double x, double y)
        {
            EnsureRenderTransform(element);
            if (element.RenderTransform is TransformGroup transformGroup)
            {
                var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                if (translateTransform != null)
                {
                    translateTransform.X = x;
                    translateTransform.Y = y;
                }
            }
        }

        private void SetInitialScale(FrameworkElement element, double scale)
        {
            EnsureRenderTransform(element);
            if (element.RenderTransform is TransformGroup transformGroup)
            {
                var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
                if (scaleTransform != null)
                {
                    scaleTransform.ScaleX = scale;
                    scaleTransform.ScaleY = scale;
                }
            }
        }

        private void EnsureRenderTransform(FrameworkElement element)
        {
            if (element.RenderTransform is not TransformGroup transformGroup)
            {
                transformGroup = new TransformGroup();
                transformGroup.Children.Add(new TranslateTransform());
                transformGroup.Children.Add(new ScaleTransform());
                element.RenderTransform = transformGroup;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        #endregion

        #region Utility Methods

        private Duration GetTransitionDuration(PageTransitionType transitionType)
        {
            if (_transitionDurations.ContainsKey(transitionType))
            {
                return _transitionDurations[transitionType];
            }
            
            return _isReducedMotionEnabled ? _timingService.DurationFast : _timingService.DurationMedium;
        }

        private void TrackTransition(FrameworkElement element, Storyboard storyboard)
        {
            if (_activeTransitions.ContainsKey(element))
            {
                _activeTransitions[element].Stop();
            }
            
            _activeTransitions[element] = storyboard;
            
            storyboard.Completed += (s, e) =>
            {
                if (_activeTransitions.ContainsKey(element))
                {
                    _activeTransitions.Remove(element);
                }
            };
        }

        #endregion

        #region Configuration

        public bool IsReducedMotionEnabled
        {
            get => _isReducedMotionEnabled;
            private set
            {
                if (_isReducedMotionEnabled != value)
                {
                    _isReducedMotionEnabled = value;
                    this.RaisePropertyChanged();
                    
                    _logger.LogInformation("Reduced motion {Status} for navigation animations", 
                        value ? "enabled" : "disabled");
                }
            }
        }

        public void SetTransitionDuration(PageTransitionType transitionType, Duration duration)
        {
            try
            {
                _transitionDurations[transitionType] = duration;
                _logger.LogDebug("Transition duration for {TransitionType} set to {Duration}ms", 
                    transitionType, duration.TimeSpan.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set transition duration for {TransitionType}", transitionType);
            }
        }

        public void EnableParallaxEffect(bool enabled)
        {
            try
            {
                _isParallaxEnabled = enabled;
                _logger.LogInformation("Parallax effect {Status}", enabled ? "enabled" : "disabled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle parallax effect");
            }
        }

        public void SetAnimationQuality(AnimationQuality quality)
        {
            try
            {
                _animationQuality = quality;
                _logger.LogInformation("Animation quality set to {Quality}", quality);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set animation quality");
            }
        }

        #endregion

        #region Initialization

        private void InitializeNavigationAnimationSystem()
        {
            try
            {
                // Initialize default transition durations
                _transitionDurations[PageTransitionType.Fade] = _timingService.DurationMedium;
                _transitionDurations[PageTransitionType.SlideLeft] = _timingService.DurationMedium;
                _transitionDurations[PageTransitionType.SlideRight] = _timingService.DurationMedium;
                _transitionDurations[PageTransitionType.SlideUp] = _timingService.DurationMedium;
                _transitionDurations[PageTransitionType.SlideDown] = _timingService.DurationMedium;
                _transitionDurations[PageTransitionType.Scale] = _timingService.DurationMedium;
                
                _logger.LogDebug("Navigation animation system initialized with default settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize navigation animation system");
                throw;
            }
        }

        private void CheckAccessibilitySettings()
        {
            try
            {
                var reducedMotion = SystemParameters.ClientAreaAnimation == false;
                IsReducedMotionEnabled = reducedMotion;
                
                _logger.LogDebug("Accessibility settings checked for navigation animations - Reduced motion: {ReducedMotion}", 
                    reducedMotion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check accessibility settings for navigation animations");
                IsReducedMotionEnabled = false;
            }
        }

        #endregion

        #region Events

        public event EventHandler<NavigationAnimationEventArgs> TransitionStarted;
        public event EventHandler<NavigationAnimationEventArgs> TransitionCompleted;
        public event EventHandler<NavigationAnimationEventArgs> TransitionFailed;

        private void OnTransitionStarted(PageTransitionType transitionType, UserControl fromPage, UserControl toPage)
        {
            try
            {
                TransitionStarted?.Invoke(this, new NavigationAnimationEventArgs
                {
                    TransitionType = transitionType,
                    FromPage = fromPage,
                    ToPage = toPage,
                    Duration = GetTransitionDuration(transitionType),
                    IsAccessibilityOptimized = _isReducedMotionEnabled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise TransitionStarted event");
            }
        }

        private void OnTransitionCompleted(PageTransitionType transitionType, UserControl fromPage, UserControl toPage)
        {
            try
            {
                TransitionCompleted?.Invoke(this, new NavigationAnimationEventArgs
                {
                    TransitionType = transitionType,
                    FromPage = fromPage,
                    ToPage = toPage,
                    Duration = GetTransitionDuration(transitionType),
                    IsAccessibilityOptimized = _isReducedMotionEnabled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise TransitionCompleted event");
            }
        }

        private void OnTransitionFailed(PageTransitionType transitionType, UserControl fromPage, UserControl toPage, Exception error)
        {
            try
            {
                TransitionFailed?.Invoke(this, new NavigationAnimationEventArgs
                {
                    TransitionType = transitionType,
                    FromPage = fromPage,
                    ToPage = toPage,
                    Duration = GetTransitionDuration(transitionType),
                    IsAccessibilityOptimized = _isReducedMotionEnabled,
                    Error = error
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise TransitionFailed event");
            }
        }

        #endregion

        #region Enhanced Animation Creation Methods

        private Storyboard CreateFadeInAnimation(UserControl page)
        {
            var storyboard = new Storyboard();
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeAnimation);
            
            return storyboard;
        }

        private Storyboard CreateFadeOutAnimation(UserControl page)
        {
            var storyboard = new Storyboard();
            var fadeAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(fadeAnimation, page);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeAnimation);
            
            return storyboard;
        }

        private Storyboard CreateSlideInAnimation(UserControl page, SlideDirection direction)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(page);
            
            var slideAnimation = new DoubleAnimation
            {
                From = GetSlideStartPosition(direction),
                To = 0.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(slideAnimation, page);
            Storyboard.SetTarget(fadeAnimation, page);
            
            if (direction == SlideDirection.Left || direction == SlideDirection.Right)
            {
                Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            }
            else
            {
                Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            }
            
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
            
            storyboard.Children.Add(slideAnimation);
            storyboard.Children.Add(fadeAnimation);
            
            return storyboard;
        }

        private Storyboard CreateSlideOutAnimation(UserControl page, SlideDirection direction)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(page);
            
            var slideAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = GetSlideEndPosition(direction),
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.AccelerateEasing
            };
            
            var fadeAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(slideAnimation, page);
            Storyboard.SetTarget(fadeAnimation, page);
            
            if (direction == SlideDirection.Left || direction == SlideDirection.Right)
            {
                Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            }
            else
            {
                Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            }
            
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
            
            storyboard.Children.Add(slideAnimation);
            storyboard.Children.Add(fadeAnimation);
            
            return storyboard;
        }

        private Storyboard CreateNavigationMenuAnimation(FrameworkElement menu, NavigationAnimationType animationType)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(menu);
            
            switch (animationType)
            {
                case NavigationAnimationType.SlideIn:
                    var slideIn = new DoubleAnimation
                    {
                        From = -menu.ActualWidth,
                        To = 0.0,
                        Duration = _timingService.DurationMedium,
                        EasingFunction = _timingService.DecelerateEasing
                    };
                    Storyboard.SetTarget(slideIn, menu);
                    Storyboard.SetTargetProperty(slideIn, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    storyboard.Children.Add(slideIn);
                    break;
                    
                case NavigationAnimationType.FadeIn:
                    var fadeIn = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = _timingService.DurationMedium,
                        EasingFunction = _timingService.StandardEasing
                    };
                    Storyboard.SetTarget(fadeIn, menu);
                    Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
                    storyboard.Children.Add(fadeIn);
                    break;
                    
                case NavigationAnimationType.Expand:
                    var scaleX = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = _timingService.DurationMedium,
                        EasingFunction = _timingService.DecelerateEasing
                    };
                    var scaleY = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = _timingService.DurationMedium,
                        EasingFunction = _timingService.DecelerateEasing
                    };
                    Storyboard.SetTarget(scaleX, menu);
                    Storyboard.SetTarget(scaleY, menu);
                    Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    storyboard.Children.Add(scaleX);
                    storyboard.Children.Add(scaleY);
                    break;
            }
            
            return storyboard;
        }

        private Storyboard CreateBreadcrumbAnimation(FrameworkElement breadcrumb, BreadcrumbAnimationType animationType)
        {
            var storyboard = new Storyboard();
            
            switch (animationType)
            {
                case BreadcrumbAnimationType.SlideIn:
                    EnsureRenderTransform(breadcrumb);
                    var slideAnimation = new DoubleAnimation
                    {
                        From = breadcrumb.ActualWidth,
                        To = 0.0,
                        Duration = _timingService.DurationFast,
                        EasingFunction = _timingService.DecelerateEasing
                    };
                    Storyboard.SetTarget(slideAnimation, breadcrumb);
                    Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                    storyboard.Children.Add(slideAnimation);
                    break;
                    
                case BreadcrumbAnimationType.FadeIn:
                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 0.0,
                        To = 1.0,
                        Duration = _timingService.DurationFast,
                        EasingFunction = _timingService.StandardEasing
                    };
                    Storyboard.SetTarget(fadeAnimation, breadcrumb);
                    Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
                    storyboard.Children.Add(fadeAnimation);
                    break;
                    
                case BreadcrumbAnimationType.Highlight:
                    var highlightAnimation = new ColorAnimation
                    {
                        From = Colors.Transparent,
                        To = Color.FromArgb(100, 0, 122, 204), // Semi-transparent blue
                        Duration = _timingService.DurationFast,
                        AutoReverse = true,
                        EasingFunction = _timingService.StandardEasing
                    };
                    Storyboard.SetTarget(highlightAnimation, breadcrumb);
                    Storyboard.SetTargetProperty(highlightAnimation, new PropertyPath("(Panel.Background).(SolidColorBrush.Color)"));
                    storyboard.Children.Add(highlightAnimation);
                    break;
            }
            
            return storyboard;
        }

        private Storyboard CreateTabSwitchAnimation(TabControl tabControl, int fromIndex, int toIndex)
        {
            var storyboard = new Storyboard();
            
            if (tabControl.Items.Count <= fromIndex || tabControl.Items.Count <= toIndex)
                return storyboard;
                
            var fromTab = tabControl.ItemContainerGenerator.ContainerFromIndex(fromIndex) as FrameworkElement;
            var toTab = tabControl.ItemContainerGenerator.ContainerFromIndex(toIndex) as FrameworkElement;
            
            if (fromTab != null)
            {
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = _timingService.DurationFast,
                    EasingFunction = _timingService.StandardEasing
                };
                Storyboard.SetTarget(fadeOut, fromTab);
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
                storyboard.Children.Add(fadeOut);
            }
            
            if (toTab != null)
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = _timingService.DurationFast,
                    BeginTime = _timingService.DurationFast.TimeSpan,
                    EasingFunction = _timingService.StandardEasing
                };
                Storyboard.SetTarget(fadeIn, toTab);
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
                storyboard.Children.Add(fadeIn);
            }
            
            return storyboard;
        }

        private Storyboard CreateModalOpenAnimation(FrameworkElement modal)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(modal);
            
            // Scale up from center
            var scaleX = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            var scaleY = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(scaleX, modal);
            Storyboard.SetTarget(scaleY, modal);
            Storyboard.SetTarget(fadeIn, modal);
            
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            
            storyboard.Children.Add(scaleX);
            storyboard.Children.Add(scaleY);
            storyboard.Children.Add(fadeIn);
            
            return storyboard;
        }

        private Storyboard CreateModalCloseAnimation(FrameworkElement modal)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(modal);
            
            // Scale down to center
            var scaleX = new DoubleAnimation
            {
                From = 1.0,
                To = 0.8,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.AccelerateEasing
            };
            
            var scaleY = new DoubleAnimation
            {
                From = 1.0,
                To = 0.8,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.AccelerateEasing
            };
            
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(scaleX, modal);
            Storyboard.SetTarget(scaleY, modal);
            Storyboard.SetTarget(fadeOut, modal);
            
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
            
            storyboard.Children.Add(scaleX);
            storyboard.Children.Add(scaleY);
            storyboard.Children.Add(fadeOut);
            
            return storyboard;
        }

        private Storyboard CreateLoadingShowAnimation(FrameworkElement container)
        {
            var storyboard = new Storyboard();
            
            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(fadeIn, container);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeIn);
            
            return storyboard;
        }

        private Storyboard CreateLoadingHideAnimation(FrameworkElement container)
        {
            var storyboard = new Storyboard();
            
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = _timingService.DurationFast,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(fadeOut, container);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeOut);
            
            return storyboard;
        }

        private Storyboard CreateContentLoadingAnimation(FrameworkElement content)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(content);
            
            // Pulse effect
            var scaleUpX = new DoubleAnimation
            {
                From = 1.0,
                To = 1.02,
                Duration = _timingService.DurationMedium,
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = _timingService.StandardEasing
            };
            
            var scaleUpY = new DoubleAnimation
            {
                From = 1.0,
                To = 1.02,
                Duration = _timingService.DurationMedium,
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(scaleUpX, content);
            Storyboard.SetTarget(scaleUpY, content);
            Storyboard.SetTargetProperty(scaleUpX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTargetProperty(scaleUpY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            
            storyboard.Children.Add(scaleUpX);
            storyboard.Children.Add(scaleUpY);
            
            return storyboard;
        }

        private Storyboard CreateWizardStepTransitionAnimation(FrameworkElement fromStep, FrameworkElement toStep, WizardTransitionDirection direction)
        {
            var storyboard = new Storyboard();
            
            if (fromStep != null)
            {
                EnsureRenderTransform(fromStep);
                
                var slideOut = new DoubleAnimation
                {
                    From = 0.0,
                    To = direction == WizardTransitionDirection.Forward ? -fromStep.ActualWidth : fromStep.ActualWidth,
                    Duration = _timingService.DurationMedium,
                    EasingFunction = _timingService.AccelerateEasing
                };
                
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = _timingService.DurationMedium,
                    EasingFunction = _timingService.StandardEasing
                };
                
                Storyboard.SetTarget(slideOut, fromStep);
                Storyboard.SetTarget(fadeOut, fromStep);
                Storyboard.SetTargetProperty(slideOut, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
                
                storyboard.Children.Add(slideOut);
                storyboard.Children.Add(fadeOut);
            }
            
            if (toStep != null)
            {
                EnsureRenderTransform(toStep);
                
                var slideIn = new DoubleAnimation
                {
                    From = direction == WizardTransitionDirection.Forward ? toStep.ActualWidth : -toStep.ActualWidth,
                    To = 0.0,
                    Duration = _timingService.DurationMedium,
                    BeginTime = TimeSpan.FromMilliseconds(50), // Slight delay for better visual flow
                    EasingFunction = _timingService.DecelerateEasing
                };
                
                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = _timingService.DurationMedium,
                    BeginTime = TimeSpan.FromMilliseconds(50),
                    EasingFunction = _timingService.StandardEasing
                };
                
                Storyboard.SetTarget(slideIn, toStep);
                Storyboard.SetTarget(fadeIn, toStep);
                Storyboard.SetTargetProperty(slideIn, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
                
                storyboard.Children.Add(slideIn);
                storyboard.Children.Add(fadeIn);
            }
            
            return storyboard;
        }

        private Storyboard CreateStepperProgressAnimation(FrameworkElement stepper, int currentStep, int totalSteps)
        {
            var storyboard = new Storyboard();
            
            // Find progress indicator (assuming it's a ProgressBar or similar)
            var progressBar = FindChildOfType<ProgressBar>(stepper);
            if (progressBar != null)
            {
                var progressAnimation = new DoubleAnimation
                {
                    From = progressBar.Value,
                    To = (double)currentStep / totalSteps * 100,
                    Duration = _timingService.DurationMedium,
                    EasingFunction = _timingService.DecelerateEasing
                };
                
                Storyboard.SetTarget(progressAnimation, progressBar);
                Storyboard.SetTargetProperty(progressAnimation, new PropertyPath("Value"));
                storyboard.Children.Add(progressAnimation);
            }
            
            return storyboard;
        }

        private Storyboard CreateStepValidationAnimation(FrameworkElement step, bool isValid)
        {
            var storyboard = new Storyboard();
            
            if (isValid)
            {
                // Success animation - subtle green glow
                var colorAnimation = new ColorAnimation
                {
                    From = Colors.Transparent,
                    To = Color.FromArgb(50, 0, 255, 0), // Semi-transparent green
                    Duration = _timingService.DurationFast,
                    AutoReverse = true,
                    EasingFunction = _timingService.StandardEasing
                };
                
                Storyboard.SetTarget(colorAnimation, step);
                Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Panel.Background).(SolidColorBrush.Color)"));
                storyboard.Children.Add(colorAnimation);
            }
            else
            {
                // Error animation - shake effect
                EnsureRenderTransform(step);
                
                var shakeAnimation = new DoubleAnimationUsingKeyFrames();
                shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-10, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100))));
                shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(10, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200))));
                shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));
                shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400))));
                shakeAnimation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500))));
                
                Storyboard.SetTarget(shakeAnimation, step);
                Storyboard.SetTargetProperty(shakeAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
                storyboard.Children.Add(shakeAnimation);
            }
            
            return storyboard;
        }

        private Storyboard CreateLayoutChangeAnimation(Panel container, LayoutChangeType changeType)
        {
            var storyboard = new Storyboard();
            
            switch (changeType)
            {
                case LayoutChangeType.Resize:
                    // Smooth resize animation
                    var widthAnimation = new DoubleAnimation
                    {
                        Duration = _timingService.DurationMedium,
                        EasingFunction = _timingService.DecelerateEasing
                    };
                    var heightAnimation = new DoubleAnimation
                    {
                        Duration = _timingService.DurationMedium,
                        EasingFunction = _timingService.DecelerateEasing
                    };
                    
                    Storyboard.SetTarget(widthAnimation, container);
                    Storyboard.SetTarget(heightAnimation, container);
                    Storyboard.SetTargetProperty(widthAnimation, new PropertyPath("Width"));
                    Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("Height"));
                    
                    storyboard.Children.Add(widthAnimation);
                    storyboard.Children.Add(heightAnimation);
                    break;
                    
                case LayoutChangeType.Reflow:
                    // Subtle pulse to indicate reflow
                    EnsureRenderTransform(container);
                    var scaleAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.01,
                        Duration = _timingService.DurationFast,
                        AutoReverse = true,
                        EasingFunction = _timingService.StandardEasing
                    };
                    
                    Storyboard.SetTarget(scaleAnimation, container);
                    Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    storyboard.Children.Add(scaleAnimation);
                    break;
            }
            
            return storyboard;
        }

        private Storyboard CreateItemAdditionAnimation(FrameworkElement item)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(item);
            
            // Scale in from 0
            var scaleX = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.BounceEasing
            };
            
            var scaleY = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.BounceEasing
            };
            
            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(scaleX, item);
            Storyboard.SetTarget(scaleY, item);
            Storyboard.SetTarget(fadeIn, item);
            
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));
            
            storyboard.Children.Add(scaleX);
            storyboard.Children.Add(scaleY);
            storyboard.Children.Add(fadeIn);
            
            return storyboard;
        }

        private Storyboard CreateItemRemovalAnimation(FrameworkElement item)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(item);
            
            // Scale out to 0
            var scaleX = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.AccelerateEasing
            };
            
            var scaleY = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.AccelerateEasing
            };
            
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.StandardEasing
            };
            
            Storyboard.SetTarget(scaleX, item);
            Storyboard.SetTarget(scaleY, item);
            Storyboard.SetTarget(fadeOut, item);
            
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("Opacity"));
            
            storyboard.Children.Add(scaleX);
            storyboard.Children.Add(scaleY);
            storyboard.Children.Add(fadeOut);
            
            return storyboard;
        }

        private Storyboard CreateItemReorderAnimation(FrameworkElement item, Point fromPosition, Point toPosition)
        {
            var storyboard = new Storyboard();
            EnsureRenderTransform(item);
            
            var moveX = new DoubleAnimation
            {
                From = fromPosition.X,
                To = toPosition.X,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            var moveY = new DoubleAnimation
            {
                From = fromPosition.Y,
                To = toPosition.Y,
                Duration = _timingService.DurationMedium,
                EasingFunction = _timingService.DecelerateEasing
            };
            
            Storyboard.SetTarget(moveX, item);
            Storyboard.SetTarget(moveY, item);
            Storyboard.SetTargetProperty(moveX, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            Storyboard.SetTargetProperty(moveY, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));
            
            storyboard.Children.Add(moveX);
            storyboard.Children.Add(moveY);
            
            return storyboard;
        }

        // Helper methods for slide animations
        private double GetSlideStartPosition(SlideDirection direction)
        {
            return direction switch
            {
                SlideDirection.Left => DefaultSlideDistance,
                SlideDirection.Right => -DefaultSlideDistance,
                SlideDirection.Up => DefaultSlideDistance,
                SlideDirection.Down => -DefaultSlideDistance,
                _ => 0.0
            };
        }

        private double GetSlideEndPosition(SlideDirection direction)
        {
            return direction switch
            {
                SlideDirection.Left => -DefaultSlideDistance,
                SlideDirection.Right => DefaultSlideDistance,
                SlideDirection.Up => -DefaultSlideDistance,
                SlideDirection.Down => DefaultSlideDistance,
                _ => 0.0
            };
        }

        // Helper method to find child controls
        private T FindChildOfType<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childOfType = FindChildOfType<T>(child);
                if (childOfType != null)
                    return childOfType;
            }

            return null;
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
                    // Stop all active transitions
                    foreach (var storyboard in _activeTransitions.Values)
                    {
                        storyboard.Stop();
                    }
                    _activeTransitions.Clear();
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
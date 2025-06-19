using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AnomaliImportTool.Core.Interfaces;
using AnomaliImportTool.Core.Models;

namespace AnomaliImportTool.WinUI.Services
{
    /// <summary>
    /// Voice control service interface for Windows Speech Recognition integration
    /// </summary>
    public interface IVoiceControlService
    {
        /// <summary>
        /// Initialize voice control with speech recognition and synthesis
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Start continuous speech recognition
        /// </summary>
        Task<bool> StartListeningAsync();

        /// <summary>
        /// Stop speech recognition
        /// </summary>
        Task StopListeningAsync();

        /// <summary>
        /// Register a voice command with the recognition system
        /// </summary>
        void RegisterVoiceCommand(string command, string description, Func<Task<bool>> handler);

        /// <summary>
        /// Unregister a voice command
        /// </summary>
        void UnregisterVoiceCommand(string command);

        /// <summary>
        /// Speak text using text-to-speech
        /// </summary>
        Task SpeakAsync(string text, VoicePriority priority = VoicePriority.Normal);

        /// <summary>
        /// Stop current speech synthesis
        /// </summary>
        Task StopSpeakingAsync();

        /// <summary>
        /// Enable or disable voice control
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Set voice recognition confidence threshold
        /// </summary>
        void SetConfidenceThreshold(double threshold);

        /// <summary>
        /// Observable for voice recognition events
        /// </summary>
        IObservable<VoiceRecognitionEvent> RecognitionEvents { get; }

        /// <summary>
        /// Observable for voice synthesis events
        /// </summary>
        IObservable<VoiceSynthesisEvent> SynthesisEvents { get; }

        /// <summary>
        /// Current voice control state
        /// </summary>
        VoiceControlState State { get; }

        /// <summary>
        /// Available voice commands
        /// </summary>
        IReadOnlyList<VoiceCommand> AvailableCommands { get; }
    }

    /// <summary>
    /// Voice control state enumeration
    /// </summary>
    public enum VoiceControlState
    {
        Uninitialized,
        Initializing,
        Ready,
        Listening,
        Processing,
        Speaking,
        Error,
        Disabled
    }

    /// <summary>
    /// Voice priority for speech synthesis
    /// </summary>
    public enum VoicePriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Voice recognition event data
    /// </summary>
    public class VoiceRecognitionEvent
    {
        public string RecognizedText { get; set; }
        public string Command { get; set; }
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool WasHandled { get; set; }
        public string ErrorMessage { get; set; }
        public SpeechRecognitionResultStatus Status { get; set; }
    }

    /// <summary>
    /// Voice synthesis event data
    /// </summary>
    public class VoiceSynthesisEvent
    {
        public string Text { get; set; }
        public VoicePriority Priority { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Voice command configuration
    /// </summary>
    public class VoiceCommand
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public List<string> Variations { get; set; } = new List<string>();
        public Func<Task<bool>> Handler { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Category { get; set; } = "General";
        public double MinConfidence { get; set; } = 0.7;
    }

    /// <summary>
    /// Professional voice control service with Windows Speech Recognition integration
    /// </summary>
    public class VoiceControlService : IVoiceControlService, IDisposable
    {
        private readonly ILogger<VoiceControlService> _logger;
        private readonly IAccessibilityService _accessibilityService;
        private readonly IAudioFeedbackService _audioFeedbackService;
        private readonly IKeyboardNavigationService _keyboardNavigationService;

        private SpeechRecognizer _speechRecognizer;
        private SpeechSynthesizer _speechSynthesizer;
        private readonly Dictionary<string, VoiceCommand> _voiceCommands;
        private readonly Subject<VoiceRecognitionEvent> _recognitionEvents;
        private readonly Subject<VoiceSynthesisEvent> _synthesisEvents;

        private VoiceControlState _state = VoiceControlState.Uninitialized;
        private bool _isEnabled = true;
        private double _confidenceThreshold = 0.7;
        private bool _isListening = false;
        private bool _isSpeaking = false;

        // Voice control constants
        private const int RECOGNITION_TIMEOUT_MS = 10000;
        private const int SYNTHESIS_TIMEOUT_MS = 30000;
        private const double DEFAULT_CONFIDENCE_THRESHOLD = 0.7;

        public VoiceControlService(
            ILogger<VoiceControlService> logger,
            IAccessibilityService accessibilityService,
            IAudioFeedbackService audioFeedbackService,
            IKeyboardNavigationService keyboardNavigationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessibilityService = accessibilityService ?? throw new ArgumentNullException(nameof(accessibilityService));
            _audioFeedbackService = audioFeedbackService ?? throw new ArgumentNullException(nameof(audioFeedbackService));
            _keyboardNavigationService = keyboardNavigationService ?? throw new ArgumentNullException(nameof(keyboardNavigationService));

            _voiceCommands = new Dictionary<string, VoiceCommand>();
            _recognitionEvents = new Subject<VoiceRecognitionEvent>();
            _synthesisEvents = new Subject<VoiceSynthesisEvent>();

            _logger.LogInformation("VoiceControlService initialized");
        }

        public IObservable<VoiceRecognitionEvent> RecognitionEvents => _recognitionEvents.AsObservable();
        public IObservable<VoiceSynthesisEvent> SynthesisEvents => _synthesisEvents.AsObservable();
        public VoiceControlState State => _state;
        public IReadOnlyList<VoiceCommand> AvailableCommands => _voiceCommands.Values.ToList().AsReadOnly();

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing voice control service");
                _state = VoiceControlState.Initializing;

                // Check if speech recognition is available
                if (!await IsSpeechRecognitionAvailableAsync())
                {
                    _logger.LogWarning("Speech recognition is not available on this system");
                    _state = VoiceControlState.Error;
                    return false;
                }

                // Initialize speech recognizer
                await InitializeSpeechRecognizerAsync();

                // Initialize speech synthesizer
                await InitializeSpeechSynthesizerAsync();

                // Register default voice commands
                RegisterDefaultVoiceCommands();

                _state = VoiceControlState.Ready;
                _logger.LogInformation("Voice control service initialized successfully");

                // Announce initialization
                await SpeakAsync("Voice control is now active", VoicePriority.High);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize voice control service");
                _state = VoiceControlState.Error;
                return false;
            }
        }

        public async Task<bool> StartListeningAsync()
        {
            try
            {
                if (_state != VoiceControlState.Ready || !_isEnabled || _isListening)
                {
                    return false;
                }

                _logger.LogInformation("Starting voice recognition");
                _state = VoiceControlState.Listening;
                _isListening = true;

                // Start continuous recognition
                var result = await _speechRecognizer.RecognizeAsync();
                await ProcessRecognitionResultAsync(result);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start voice recognition");
                _state = VoiceControlState.Error;
                _isListening = false;
                return false;
            }
        }

        public async Task StopListeningAsync()
        {
            try
            {
                if (!_isListening) return;

                _logger.LogInformation("Stopping voice recognition");
                _isListening = false;

                if (_speechRecognizer != null)
                {
                    await _speechRecognizer.StopRecognitionAsync();
                }

                _state = VoiceControlState.Ready;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop voice recognition");
            }
        }

        public void RegisterVoiceCommand(string command, string description, Func<Task<bool>> handler)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command) || handler == null)
                {
                    _logger.LogWarning("Invalid voice command registration attempt");
                    return;
                }

                var voiceCommand = new VoiceCommand
                {
                    Command = command.ToLowerInvariant(),
                    Description = description,
                    Handler = handler,
                    IsEnabled = true
                };

                _voiceCommands[voiceCommand.Command] = voiceCommand;

                // Update speech recognizer constraints
                _ = Task.Run(UpdateSpeechRecognizerConstraintsAsync);

                _logger.LogDebug("Registered voice command: {Command}", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register voice command: {Command}", command);
            }
        }

        public void UnregisterVoiceCommand(string command)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(command)) return;

                var normalizedCommand = command.ToLowerInvariant();
                if (_voiceCommands.Remove(normalizedCommand))
                {
                    // Update speech recognizer constraints
                    _ = Task.Run(UpdateSpeechRecognizerConstraintsAsync);

                    _logger.LogDebug("Unregistered voice command: {Command}", command);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unregister voice command: {Command}", command);
            }
        }

        public async Task SpeakAsync(string text, VoicePriority priority = VoicePriority.Normal)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text) || _speechSynthesizer == null)
                {
                    return;
                }

                _logger.LogDebug("Speaking text: {Text} (Priority: {Priority})", text, priority);

                // Stop current speech if higher priority
                if (_isSpeaking && priority >= VoicePriority.High)
                {
                    await StopSpeakingAsync();
                }

                _isSpeaking = true;
                _state = VoiceControlState.Speaking;

                // Create speech synthesis stream
                var stream = await _speechSynthesizer.SynthesizeTextToStreamAsync(text);

                // Fire synthesis event
                _synthesisEvents.OnNext(new VoiceSynthesisEvent
                {
                    Text = text,
                    Priority = priority,
                    IsCompleted = false
                });

                // Play the speech (implementation would depend on audio framework)
                // For now, we'll simulate completion
                await Task.Delay(text.Length * 50); // Approximate speech duration

                _synthesisEvents.OnNext(new VoiceSynthesisEvent
                {
                    Text = text,
                    Priority = priority,
                    IsCompleted = true
                });

                _isSpeaking = false;
                _state = VoiceControlState.Ready;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to speak text: {Text}", text);
                _isSpeaking = false;
                _state = VoiceControlState.Ready;

                _synthesisEvents.OnNext(new VoiceSynthesisEvent
                {
                    Text = text,
                    Priority = priority,
                    ErrorMessage = ex.Message
                });
            }
        }

        public async Task StopSpeakingAsync()
        {
            try
            {
                if (!_isSpeaking) return;

                _logger.LogDebug("Stopping current speech synthesis");
                
                // Stop synthesis (implementation would depend on audio framework)
                _isSpeaking = false;
                _state = VoiceControlState.Ready;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop speech synthesis");
            }
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Voice control {Status}", enabled ? "enabled" : "disabled");

            if (!enabled && _isListening)
            {
                _ = Task.Run(StopListeningAsync);
            }

            if (enabled)
            {
                _state = VoiceControlState.Ready;
            }
            else
            {
                _state = VoiceControlState.Disabled;
            }

            // Announce state change
            _ = Task.Run(() => SpeakAsync($"Voice control {(enabled ? "enabled" : "disabled")}", VoicePriority.High));
        }

        public void SetConfidenceThreshold(double threshold)
        {
            _confidenceThreshold = Math.Max(0.0, Math.Min(1.0, threshold));
            _logger.LogInformation("Voice recognition confidence threshold set to {Threshold}", _confidenceThreshold);
        }

        private async Task<bool> IsSpeechRecognitionAvailableAsync()
        {
            try
            {
                var recognizer = new SpeechRecognizer();
                var result = await recognizer.CompileConstraintsAsync();
                recognizer.Dispose();
                return result.Status == SpeechRecognitionResultStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private async Task InitializeSpeechRecognizerAsync()
        {
            try
            {
                _speechRecognizer = new SpeechRecognizer();

                // Configure recognition settings
                _speechRecognizer.Timeouts.InitialSilenceTimeout = TimeSpan.FromMilliseconds(RECOGNITION_TIMEOUT_MS);
                _speechRecognizer.Timeouts.BabbleTimeout = TimeSpan.FromMilliseconds(RECOGNITION_TIMEOUT_MS);
                _speechRecognizer.Timeouts.EndSilenceTimeout = TimeSpan.FromMilliseconds(2000);

                // Set up event handlers
                _speechRecognizer.RecognitionQualityDegrading += OnRecognitionQualityDegrading;
                _speechRecognizer.StateChanged += OnRecognizerStateChanged;

                // Configure UI language
                var currentLanguage = ApplicationLanguages.Languages.FirstOrDefault() ?? "en-US";
                _speechRecognizer.UIOptions.AudiblePrompt = "Listening for voice commands";
                _speechRecognizer.UIOptions.ExampleText = "Say 'Help' to see available commands";

                _logger.LogInformation("Speech recognizer initialized with language: {Language}", currentLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize speech recognizer");
                throw;
            }
        }

        private async Task InitializeSpeechSynthesizerAsync()
        {
            try
            {
                _speechSynthesizer = new SpeechSynthesizer();

                // Configure synthesis settings
                var voices = SpeechSynthesizer.AllVoices;
                var defaultVoice = voices.FirstOrDefault(v => v.Language.StartsWith("en")) ?? voices.FirstOrDefault();
                
                if (defaultVoice != null)
                {
                    _speechSynthesizer.Voice = defaultVoice;
                    _logger.LogInformation("Speech synthesizer initialized with voice: {Voice}", defaultVoice.DisplayName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize speech synthesizer");
                throw;
            }
        }

        private void RegisterDefaultVoiceCommands()
        {
            // Navigation commands
            RegisterVoiceCommand("help", "Show help information", ShowHelpAsync);
            RegisterVoiceCommand("next", "Move to next element", () => Task.FromResult(_keyboardNavigationService.FocusNext()));
            RegisterVoiceCommand("previous", "Move to previous element", () => Task.FromResult(_keyboardNavigationService.FocusPrevious()));
            RegisterVoiceCommand("first", "Move to first element", () => Task.FromResult(_keyboardNavigationService.FocusFirst()));
            RegisterVoiceCommand("last", "Move to last element", () => Task.FromResult(_keyboardNavigationService.FocusLast()));

            // Application commands
            RegisterVoiceCommand("start listening", "Start voice recognition", async () => { await StartListeningAsync(); return true; });
            RegisterVoiceCommand("stop listening", "Stop voice recognition", async () => { await StopListeningAsync(); return true; });
            RegisterVoiceCommand("repeat", "Repeat last message", RepeatLastMessageAsync);
            RegisterVoiceCommand("cancel", "Cancel current operation", CancelCurrentOperationAsync);

            // Mode switching commands
            RegisterVoiceCommand("dashboard", "Switch to dashboard mode", SwitchToDashboardAsync);
            RegisterVoiceCommand("wizard", "Switch to wizard mode", SwitchToWizardAsync);
            RegisterVoiceCommand("advanced", "Switch to advanced mode", SwitchToAdvancedAsync);

            _logger.LogInformation("Registered {Count} default voice commands", _voiceCommands.Count);
        }

        private async Task UpdateSpeechRecognizerConstraintsAsync()
        {
            try
            {
                if (_speechRecognizer == null) return;

                // Clear existing constraints
                _speechRecognizer.Constraints.Clear();

                // Create list constraint with all commands
                var commands = _voiceCommands.Values
                    .Where(c => c.IsEnabled)
                    .SelectMany(c => new[] { c.Command }.Concat(c.Variations))
                    .ToList();

                if (commands.Any())
                {
                    var listConstraint = new SpeechRecognitionListConstraint(commands, "VoiceCommands");
                    _speechRecognizer.Constraints.Add(listConstraint);
                }

                // Compile constraints
                var result = await _speechRecognizer.CompileConstraintsAsync();
                if (result.Status != SpeechRecognitionResultStatus.Success)
                {
                    _logger.LogWarning("Failed to compile speech recognition constraints: {Status}", result.Status);
                }
                else
                {
                    _logger.LogDebug("Updated speech recognition constraints with {Count} commands", commands.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update speech recognizer constraints");
            }
        }

        private async Task ProcessRecognitionResultAsync(SpeechRecognitionResult result)
        {
            try
            {
                var recognitionEvent = new VoiceRecognitionEvent
                {
                    RecognizedText = result.Text,
                    Confidence = result.Confidence == SpeechRecognitionConfidence.High ? 0.9 :
                                result.Confidence == SpeechRecognitionConfidence.Medium ? 0.7 : 0.5,
                    Status = result.Status
                };

                if (result.Status == SpeechRecognitionResultStatus.Success && 
                    recognitionEvent.Confidence >= _confidenceThreshold)
                {
                    var normalizedText = result.Text.ToLowerInvariant();
                    
                    if (_voiceCommands.TryGetValue(normalizedText, out var command))
                    {
                        _logger.LogInformation("Executing voice command: {Command} (Confidence: {Confidence})", 
                            command.Command, recognitionEvent.Confidence);

                        recognitionEvent.Command = command.Command;
                        
                        _state = VoiceControlState.Processing;
                        
                        try
                        {
                            var handled = await command.Handler();
                            recognitionEvent.WasHandled = handled;

                            if (handled)
                            {
                                await _audioFeedbackService.PlaySuccessSoundAsync();
                                await SpeakAsync($"Command {command.Description} executed", VoicePriority.Low);
                            }
                            else
                            {
                                await _audioFeedbackService.PlayErrorSoundAsync();
                                await SpeakAsync("Command could not be executed", VoicePriority.Normal);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error executing voice command: {Command}", command.Command);
                            recognitionEvent.ErrorMessage = ex.Message;
                            await _audioFeedbackService.PlayErrorSoundAsync();
                            await SpeakAsync("Command execution failed", VoicePriority.Normal);
                        }
                        finally
                        {
                            _state = VoiceControlState.Ready;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Unrecognized voice command: {Text}", result.Text);
                        await SpeakAsync("Command not recognized. Say 'help' for available commands.", VoicePriority.Normal);
                    }
                }
                else
                {
                    _logger.LogDebug("Voice recognition result below confidence threshold: {Text} (Confidence: {Confidence})", 
                        result.Text, recognitionEvent.Confidence);
                    
                    if (result.Status != SpeechRecognitionResultStatus.Success)
                    {
                        recognitionEvent.ErrorMessage = $"Recognition failed: {result.Status}";
                    }
                }

                _recognitionEvents.OnNext(recognitionEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process recognition result");
            }
        }

        private void OnRecognitionQualityDegrading(SpeechRecognizer sender, SpeechRecognitionQualityDegradingEventArgs args)
        {
            _logger.LogWarning("Speech recognition quality degrading: {Problem}", args.Problem);
            
            // Provide feedback to user
            _ = Task.Run(() => SpeakAsync("Audio quality is poor. Please speak more clearly.", VoicePriority.High));
        }

        private void OnRecognizerStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            _logger.LogDebug("Speech recognizer state changed to: {State}", args.State);
        }

        // Voice command handlers
        private async Task<bool> ShowHelpAsync()
        {
            var helpText = "Available voice commands: " + 
                          string.Join(", ", _voiceCommands.Values.Where(c => c.IsEnabled).Select(c => c.Command));
            
            await SpeakAsync(helpText, VoicePriority.High);
            return true;
        }

        private async Task<bool> RepeatLastMessageAsync()
        {
            await SpeakAsync("Repeating last message", VoicePriority.Normal);
            return true;
        }

        private async Task<bool> CancelCurrentOperationAsync()
        {
            await SpeakAsync("Operation cancelled", VoicePriority.Normal);
            return true;
        }

        private async Task<bool> SwitchToDashboardAsync()
        {
            await SpeakAsync("Switching to dashboard mode", VoicePriority.Normal);
            return true;
        }

        private async Task<bool> SwitchToWizardAsync()
        {
            await SpeakAsync("Switching to wizard mode", VoicePriority.Normal);
            return true;
        }

        private async Task<bool> SwitchToAdvancedAsync()
        {
            await SpeakAsync("Switching to advanced mode", VoicePriority.Normal);
            return true;
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("Disposing VoiceControlService");

                // Stop listening
                if (_isListening)
                {
                    _ = Task.Run(StopListeningAsync);
                }

                // Stop speaking
                if (_isSpeaking)
                {
                    _ = Task.Run(StopSpeakingAsync);
                }

                // Dispose speech recognizer
                if (_speechRecognizer != null)
                {
                    _speechRecognizer.RecognitionQualityDegrading -= OnRecognitionQualityDegrading;
                    _speechRecognizer.StateChanged -= OnRecognizerStateChanged;
                    _speechRecognizer.Dispose();
                    _speechRecognizer = null;
                }

                // Dispose speech synthesizer
                _speechSynthesizer?.Dispose();
                _speechSynthesizer = null;

                // Clean up collections
                _voiceCommands.Clear();

                // Dispose observables
                _recognitionEvents?.Dispose();
                _synthesisEvents?.Dispose();

                _state = VoiceControlState.Uninitialized;

                _logger.LogInformation("VoiceControlService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing VoiceControlService");
            }
        }
    }
} 
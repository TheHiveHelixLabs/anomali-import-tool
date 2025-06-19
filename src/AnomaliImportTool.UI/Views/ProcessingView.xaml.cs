using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AnomaliImportTool.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AnomaliImportTool.UI.Views
{
    /// <summary>
    /// Interaction logic for ProcessingView.xaml
    /// </summary>
    public partial class ProcessingView : UserControl
    {
        public event EventHandler ProcessingCompleted;
        
        private ObservableCollection<ProcessingStep> _steps = new ObservableCollection<ProcessingStep>();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isPaused = false;
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        private IDocumentProcessor _documentProcessor;
        private DispatcherTimer _timer;
        private Stopwatch _currentFileStopwatch;

        public ProcessingView()
        {
            InitializeComponent();
            processingSteps.ItemsSource = _steps;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_currentFileStopwatch != null && _currentFileStopwatch.IsRunning)
            {
                var currentStep = _steps.FirstOrDefault(s => s.Status == "Processing...");
                if (currentStep != null)
                {
                    currentStep.Duration = FormatDuration(_currentFileStopwatch.Elapsed);
                }
            }
        }

        public async Task StartProcessing(ObservableCollection<FileItem> files, bool groupByNamePattern)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _documentProcessor = App.Services?.GetService<IDocumentProcessor>();
                
                if (_documentProcessor == null)
                {
                    ShowError("Document processor service not available.");
                    return;
                }

                btnCancel.IsEnabled = true;
                btnPause.IsEnabled = true;
                btnContinue.IsEnabled = false;
                
                _timer.Start();
                
                await ProcessFiles(files, groupByNamePattern, _cancellationTokenSource.Token);
                
                // Processing completed successfully
                ProcessingCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                UpdateOverallStatus("Processing cancelled", 0);
            }
            catch (Exception ex)
            {
                ShowError($"Processing failed: {ex.Message}");
            }
            finally
            {
                _timer.Stop();
                btnCancel.IsEnabled = false;
                btnPause.IsEnabled = false;
                btnContinue.IsEnabled = true;
                btnContinue.Content = "View Results";
            }
        }

        private async Task ProcessFiles(ObservableCollection<FileItem> files, bool groupByNamePattern, CancellationToken cancellationToken)
        {
            var totalFiles = files.Count;
            var processedFiles = 0;
            
            UpdateOverallStatus($"Processing {totalFiles} files...", 0);
            
            // Group files if requested
            var fileGroups = groupByNamePattern 
                ? GroupFilesByPattern(files) 
                : files.Select(f => new[] { f }).ToList();
            
            foreach (var group in fileGroups)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                // Wait if paused
                _pauseEvent.Wait(cancellationToken);
                
                foreach (var file in group)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    await ProcessSingleFile(file, cancellationToken);
                    
                    processedFiles++;
                    var percentage = (processedFiles * 100) / totalFiles;
                    UpdateOverallStatus($"Processed {processedFiles} of {totalFiles} files", percentage);
                }
            }
            
            UpdateOverallStatus("Processing complete!", 100);
        }

        private async Task ProcessSingleFile(FileItem file, CancellationToken cancellationToken)
        {
            _currentFileStopwatch = Stopwatch.StartNew();
            
            UpdateCurrentFile(file.FileName, file.FileIcon);
            
            var step = new ProcessingStep
            {
                StepName = file.FileName,
                Status = "Processing...",
                StatusIcon = "ProgressClock",
                StatusColor = "#1976D2"
            };
            
            _steps.Add(step);
            
            try
            {
                // Simulate processing steps
                await UpdateFileProgress("Opening file...", 10, cancellationToken);
                await Task.Delay(500, cancellationToken);
                
                await UpdateFileProgress("Extracting text content...", 30, cancellationToken);
                await Task.Delay(1000, cancellationToken);
                
                await UpdateFileProgress("Parsing threat indicators...", 60, cancellationToken);
                await Task.Delay(1500, cancellationToken);
                
                await UpdateFileProgress("Validating indicators...", 80, cancellationToken);
                await Task.Delay(1000, cancellationToken);
                
                await UpdateFileProgress("Complete", 100, cancellationToken);
                
                // Update step status
                step.Status = "Completed successfully";
                step.StatusIcon = "CheckCircle";
                step.StatusColor = "#43A047";
            }
            catch (Exception ex)
            {
                step.Status = $"Failed: {ex.Message}";
                step.StatusIcon = "AlertCircle";
                step.StatusColor = "#E53935";
                
                if (chkStopOnError.IsChecked == true)
                {
                    ShowError(ex.Message);
                    throw;
                }
            }
            finally
            {
                _currentFileStopwatch.Stop();
                step.Duration = FormatDuration(_currentFileStopwatch.Elapsed);
            }
        }

        private async Task UpdateFileProgress(string status, int percentage, CancellationToken cancellationToken)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                txtCurrentFileStatus.Text = status;
                currentFileProgress.Value = percentage;
            });
        }

        private void UpdateCurrentFile(string fileName, string fileIcon)
        {
            Dispatcher.Invoke(() =>
            {
                txtCurrentFile.Text = fileName;
                currentFileIcon.Kind = (MahApps.Metro.IconPacks.PackIconMaterialKind)Enum.Parse(
                    typeof(MahApps.Metro.IconPacks.PackIconMaterialKind), fileIcon);
            });
        }

        private void UpdateOverallStatus(string status, int percentage)
        {
            Dispatcher.Invoke(() =>
            {
                txtOverallStatus.Text = status;
                txtOverallPercentage.Text = $"{percentage}%";
                overallProgress.Value = percentage;
            });
        }

        private void ShowError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                txtErrorMessage.Text = message;
                errorPanel.Visibility = Visibility.Visible;
            });
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalSeconds < 1)
                return "< 1s";
            if (duration.TotalMinutes < 1)
                return $"{duration.Seconds}s";
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";
        }

        private List<FileItem[]> GroupFilesByPattern(ObservableCollection<FileItem> files)
        {
            // Simple grouping by file name prefix
            return files.GroupBy(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f.FileName);
                // Group by first word or up to first number
                var match = System.Text.RegularExpressions.Regex.Match(name, @"^[^\d_-]+");
                return match.Success ? match.Value : name;
            })
            .Select(g => g.ToArray())
            .ToList();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = true;
            _pauseEvent.Reset();
            
            btnPause.Visibility = Visibility.Collapsed;
            btnContinue.Visibility = Visibility.Visible;
            btnContinue.IsEnabled = true;
            btnContinue.Content = "Resume";
            
            UpdateOverallStatus("Processing paused", (int)overallProgress.Value);
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                _isPaused = false;
                _pauseEvent.Set();
                
                btnPause.Visibility = Visibility.Visible;
                btnContinue.Visibility = Visibility.Collapsed;
                
                UpdateOverallStatus("Processing resumed", (int)overallProgress.Value);
            }
            else
            {
                // View results
                ProcessingCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public class ProcessingStep : INotifyPropertyChanged
    {
        private string _stepName;
        private string _status;
        private string _statusIcon;
        private string _statusColor;
        private string _duration;

        public string StepName
        {
            get => _stepName;
            set { _stepName = value; OnPropertyChanged(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string StatusIcon
        {
            get => _statusIcon;
            set { _statusIcon = value; OnPropertyChanged(); }
        }

        public string StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        public string Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 
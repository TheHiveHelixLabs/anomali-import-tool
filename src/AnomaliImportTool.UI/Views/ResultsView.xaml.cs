using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AnomaliImportTool.UI.Views
{
    /// <summary>
    /// Interaction logic for ResultsView.xaml
    /// </summary>
    public partial class ResultsView : UserControl
    {
        private ObservableCollection<ImportResult> _results = new ObservableCollection<ImportResult>();

        public ResultsView()
        {
            InitializeComponent();
            resultsGrid.ItemsSource = _results;
            
            // Load sample data for demonstration
            LoadSampleResults();
        }

        private void LoadSampleResults()
        {
            // This would normally be populated from the processing results
            _results.Add(new ImportResult
            {
                FileName = "APT29-Report-2024.pdf",
                Status = "Success",
                StatusIcon = "CheckCircle",
                StatusColor = "#43A047",
                IndicatorCount = 47,
                ImportTime = "2.3s",
                Message = "Successfully imported all indicators"
            });

            _results.Add(new ImportResult
            {
                FileName = "Ransomware-Analysis-Q1.docx",
                Status = "Success",
                StatusIcon = "CheckCircle",
                StatusColor = "#43A047",
                IndicatorCount = 23,
                ImportTime = "1.8s",
                Message = "Successfully imported all indicators"
            });

            _results.Add(new ImportResult
            {
                FileName = "IOC-List-March.xlsx",
                Status = "Partial",
                StatusIcon = "AlertCircle",
                StatusColor = "#FF9800",
                IndicatorCount = 15,
                ImportTime = "1.2s",
                Message = "3 indicators skipped due to invalid format"
            });

            _results.Add(new ImportResult
            {
                FileName = "Threat-Intel-Weekly.pdf",
                Status = "Failed",
                StatusIcon = "CloseCircle",
                StatusColor = "#E53935",
                IndicatorCount = 0,
                ImportTime = "0.5s",
                Message = "Unable to extract text from encrypted PDF"
            });

            UpdateSummary();
        }

        public void SetResults(ObservableCollection<ImportResult> results)
        {
            _results.Clear();
            foreach (var result in results)
            {
                _results.Add(result);
            }
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            txtTotalProcessed.Text = _results.Count.ToString();
            txtSuccessful.Text = _results.Count(r => r.Status == "Success").ToString();
            txtIndicators.Text = _results.Sum(r => r.IndicatorCount).ToString();
            txtFailed.Text = _results.Count(r => r.Status == "Failed").ToString();
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "CSV Files|*.csv|All Files|*.*",
                FileName = $"AnomaliImport_Results_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Export Results to CSV"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("File Name,Status,Indicators,Import Time,Message");
                    
                    foreach (var result in _results)
                    {
                        csv.AppendLine($"\"{result.FileName}\",\"{result.Status}\",{result.IndicatorCount},\"{result.ImportTime}\",\"{result.Message}\"");
                    }
                    
                    File.WriteAllText(dialog.FileName, csv.ToString());
                    
                    MessageBox.Show("Results exported successfully!", "Export Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export results: {ex.Message}", "Export Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportJson_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON Files|*.json|All Files|*.*",
                FileName = $"AnomaliImport_Results_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                Title = "Export Results to JSON"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var exportData = new
                    {
                        ExportDate = DateTime.Now,
                        Summary = new
                        {
                            TotalProcessed = _results.Count,
                            Successful = _results.Count(r => r.Status == "Success"),
                            Failed = _results.Count(r => r.Status == "Failed"),
                            TotalIndicators = _results.Sum(r => r.IndicatorCount)
                        },
                        Results = _results.Select(r => new
                        {
                            r.FileName,
                            r.Status,
                            r.IndicatorCount,
                            r.ImportTime,
                            r.Message
                        })
                    };
                    
                    var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    File.WriteAllText(dialog.FileName, json);
                    
                    MessageBox.Show("Results exported successfully!", "Export Complete", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export results: {ex.Message}", "Export Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewLogs_Click(object sender, RoutedEventArgs e)
        {
            // In a real implementation, this would open a log viewer
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "AnomaliImportTool", "logs");
            
            if (Directory.Exists(logPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", logPath);
            }
            else
            {
                MessageBox.Show("No log files found.", "Logs", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void NewImport_Click(object sender, RoutedEventArgs e)
        {
            // Clear results and navigate back to file selection
            _results.Clear();
            
            // Get parent window and navigate to file selection
            var window = Window.GetWindow(this) as WindowView;
            window?.NavigateToFileSelection_Click(sender, e);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window?.Close();
        }
    }

    public class ImportResult : INotifyPropertyChanged
    {
        private string _fileName;
        private string _status;
        private string _statusIcon;
        private string _statusColor;
        private int _indicatorCount;
        private string _importTime;
        private string _message;

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
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

        public int IndicatorCount
        {
            get => _indicatorCount;
            set { _indicatorCount = value; OnPropertyChanged(); }
        }

        public string ImportTime
        {
            get => _importTime;
            set { _importTime = value; OnPropertyChanged(); }
        }

        public string Message
        {
            get => _message;
            set { _message = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 
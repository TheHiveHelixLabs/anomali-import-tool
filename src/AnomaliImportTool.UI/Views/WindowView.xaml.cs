using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using AnomaliImportTool.WPF.ViewModels;

namespace AnomaliImportTool.WPF.Views
{
    /// <summary>
    /// Interaction logic for WindowView.xaml
    /// </summary>
    public partial class WindowView : MetroWindow
    {
        private FileSelectionView _fileSelectionView;
        private ProcessingView _processingView;
        private ResultsView _resultsView;
        private SettingsView _settingsView;

        public WindowView()
        {
            InitializeComponent();
            
            // Navigate to file selection on startup
            NavigateToFileSelection();
        }

        private void NavigateToFileSelection_Click(object sender, RoutedEventArgs e)
        {
            NavigateToFileSelection();
        }

        private void NavigateToProcessing_Click(object sender, RoutedEventArgs e)
        {
            NavigateToProcessing();
        }

        private void NavigateToResults_Click(object sender, RoutedEventArgs e)
        {
            NavigateToResults();
        }

        private void NavigateToSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigateToSettings();
        }

        private void NavigateToFileSelection()
        {
            _fileSelectionView ??= new FileSelectionView();
            _fileSelectionView.FilesSelected += OnFilesSelected;
            
            contentFrame.Navigate(_fileSelectionView);
            txtPageTitle.Text = "File Selection";
            UpdateNavigationButtonStates("FileSelection");
        }

        private void NavigateToProcessing()
        {
            _processingView ??= new ProcessingView();
            _processingView.ProcessingCompleted += OnProcessingCompleted;
            
            contentFrame.Navigate(_processingView);
            txtPageTitle.Text = "Processing Documents";
            UpdateNavigationButtonStates("Processing");
        }

        private void NavigateToResults()
        {
            _resultsView ??= new ResultsView();
            
            contentFrame.Navigate(_resultsView);
            txtPageTitle.Text = "Import Results";
            UpdateNavigationButtonStates("Results");
        }

        private void NavigateToSettings()
        {
            _settingsView ??= new SettingsView();
            _settingsView.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            contentFrame.Navigate(_settingsView);
            txtPageTitle.Text = "Settings";
            UpdateNavigationButtonStates("Settings");
        }

        private void UpdateNavigationButtonStates(string currentPage)
        {
            // Reset all buttons
            btnFileSelection.IsEnabled = true;
            btnProcessing.IsEnabled = _processingView != null;
            btnResults.IsEnabled = _resultsView != null;
            btnSettings.IsEnabled = true;

            // Highlight current page (you could add visual states here)
            switch (currentPage)
            {
                case "FileSelection":
                    // Add visual indication for active button
                    break;
                case "Processing":
                    btnProcessing.IsEnabled = false; // Disable while processing
                    break;
                case "Results":
                    break;
                case "Settings":
                    break;
            }
        }

        private void OnFilesSelected(object sender, EventArgs e)
        {
            // Enable processing navigation
            btnProcessing.IsEnabled = true;
            UpdateStatus("Files selected. Ready to process.");
        }

        private void OnProcessingCompleted(object sender, EventArgs e)
        {
            // Enable results navigation
            btnResults.IsEnabled = true;
            UpdateStatus("Processing completed.");
            
            // Auto-navigate to results
            NavigateToResults();
        }

        private void OnConnectionStatusChanged(object sender, ConnectionStatusEventArgs e)
        {
            UpdateConnectionStatus(e.IsConnected, e.StatusMessage);
        }

        public void UpdateStatus(string status)
        {
            txtStatus.Text = status;
        }

        public void UpdateProgress(string message, double percentage)
        {
            if (percentage > 0)
            {
                txtProgress.Text = message;
                progressBar.Value = percentage;
                txtProgress.Visibility = Visibility.Visible;
                progressBar.Visibility = Visibility.Visible;
            }
            else
            {
                txtProgress.Visibility = Visibility.Collapsed;
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        public void UpdateConnectionStatus(bool isConnected, string message)
        {
            connectionIndicator.Fill = isConnected 
                ? new SolidColorBrush(Colors.LimeGreen) 
                : new SolidColorBrush(Colors.Gray);
            
            txtConnectionStatus.Text = message ?? (isConnected ? "Connected" : "Not Connected");
        }
    }

    // Event args for connection status changes
    public class ConnectionStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string StatusMessage { get; set; }
    }
}

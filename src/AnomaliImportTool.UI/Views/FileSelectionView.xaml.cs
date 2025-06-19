using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace AnomaliImportTool.UI.Views
{
    /// <summary>
    /// Interaction logic for FileSelectionView.xaml
    /// </summary>
    public partial class FileSelectionView : UserControl
    {
        public event EventHandler FilesSelected;
        
        private ObservableCollection<FileItem> _files = new ObservableCollection<FileItem>();
        private static readonly string[] SupportedExtensions = { ".pdf", ".docx", ".xlsx" };

        public FileSelectionView()
        {
            InitializeComponent();
            lstFiles.ItemsSource = _files;
            _files.CollectionChanged += Files_CollectionChanged;
        }

        private void Files_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            var hasFiles = _files.Count > 0;
            
            dropZone.Visibility = hasFiles ? Visibility.Collapsed : Visibility.Visible;
            fileListPanel.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;
            
            txtFileCount.Text = $" ({_files.Count})";
            btnNext.IsEnabled = hasFiles;
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddFiles(files);
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                dropZone.BorderBrush = (Brush)FindResource("MahApps.Brushes.Accent");
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            dropZone.BorderBrush = (Brush)FindResource("MahApps.Brushes.Gray6");
        }

        private void BrowseFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Supported Files|*.pdf;*.docx;*.xlsx|PDF Files|*.pdf|Word Documents|*.docx|Excel Files|*.xlsx|All Files|*.*",
                Title = "Select Documents to Import"
            };

            if (dialog.ShowDialog() == true)
            {
                AddFiles(dialog.FileNames);
            }
        }

        private void AddMoreFiles_Click(object sender, RoutedEventArgs e)
        {
            BrowseFiles_Click(sender, e);
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            _files.Clear();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _files.Clear();
            // Navigation will be handled by parent
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_files.Count > 0)
            {
                FilesSelected?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AddFiles(string[] filePaths)
        {
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                    continue;

                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (!SupportedExtensions.Contains(extension))
                    continue;

                // Check if file already added
                if (_files.Any(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var fileInfo = new FileInfo(filePath);
                var fileItem = new FileItem
                {
                    FileName = fileInfo.Name,
                    FilePath = fileInfo.FullName,
                    FileSize = FormatFileSize(fileInfo.Length),
                    FileIcon = GetFileIcon(extension),
                    FileIconColor = GetFileIconColor(extension)
                };

                fileItem.RemoveCommand = new RelayCommand(() => _files.Remove(fileItem));
                _files.Add(fileItem);
            }
        }

        private string GetFileIcon(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "FilePdfBox",
                ".docx" => "FileWord",
                ".xlsx" => "FileExcel",
                _ => "File"
            };
        }

        private string GetFileIconColor(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "#E53935",
                ".docx" => "#1976D2",
                ".xlsx" => "#388E3C",
                _ => "#757575"
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public ObservableCollection<FileItem> GetSelectedFiles()
        {
            return _files;
        }

        public bool GroupByNamePattern => chkGroupByName.IsChecked ?? true;
    }

    public class FileItem : INotifyPropertyChanged
    {
        private string _fileName;
        private string _filePath;
        private string _fileSize;
        private string _fileIcon;
        private string _fileIconColor;
        private ICommand _removeCommand;

        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        public string FilePath
        {
            get => _filePath;
            set { _filePath = value; OnPropertyChanged(); }
        }

        public string FileSize
        {
            get => _fileSize;
            set { _fileSize = value; OnPropertyChanged(); }
        }

        public string FileIcon
        {
            get => _fileIcon;
            set { _fileIcon = value; OnPropertyChanged(); }
        }

        public string FileIconColor
        {
            get => _fileIconColor;
            set { _fileIconColor = value; OnPropertyChanged(); }
        }

        public ICommand RemoveCommand
        {
            get => _removeCommand;
            set { _removeCommand = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
} 
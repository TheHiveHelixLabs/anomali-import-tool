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
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private bool _isApiKeyVisible = false;
        private TextBox _apiKeyTextBox;

        public SettingsView()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            this.DataContext = App.Services!.GetRequiredService<SettingsViewModel>();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Set up two-way binding for the API key
            if (DataContext is SettingsViewModel viewModel)
            {
                // Create a binding for the password box
                var binding = new Binding(nameof(viewModel.ApiKey))
                {
                    Source = viewModel,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                // Set up password changed event to update the view model
                ApiKeyBox.PasswordChanged += (s, args) =>
                {
                    if (ApiKeyBox.Password != viewModel.ApiKey)
                    {
                        viewModel.ApiKey = ApiKeyBox.Password;
                    }
                };

                // Set initial password value
                ApiKeyBox.Password = viewModel.ApiKey ?? string.Empty;

                // Listen for changes from the view model
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(viewModel.ApiKey) && 
                        ApiKeyBox.Password != viewModel.ApiKey)
                    {
                        ApiKeyBox.Password = viewModel.ApiKey ?? string.Empty;
                    }
                };
            }
        }

        private void ToggleApiKeyVisibility(object sender, RoutedEventArgs e)
        {
            if (_isApiKeyVisible)
            {
                // Switch to password box
                if (_apiKeyTextBox != null)
                {
                    var parent = _apiKeyTextBox.Parent as Panel;
                    if (parent != null)
                    {
                        var index = parent.Children.IndexOf(_apiKeyTextBox);
                        parent.Children.RemoveAt(index);
                        parent.Children.Insert(index, ApiKeyBox);
                        
                        // Update the password box with the text box value
                        ApiKeyBox.Password = _apiKeyTextBox.Text;
                        ApiKeyBox.Focus();
                    }
                }
                _isApiKeyVisible = false;
            }
            else
            {
                // Switch to text box
                var parent = ApiKeyBox.Parent as Panel;
                if (parent != null)
                {
                    if (_apiKeyTextBox == null)
                    {
                        _apiKeyTextBox = new TextBox();
                        _apiKeyTextBox.SetResourceReference(StyleProperty, "InputStyle");
                        _apiKeyTextBox.ToolTip = "Your Anomali ThreatStream API key (stored securely)";
                        
                        // Set up binding for the text box
                        if (DataContext is SettingsViewModel viewModel)
                        {
                            var binding = new Binding(nameof(viewModel.ApiKey))
                            {
                                Source = viewModel,
                                Mode = BindingMode.TwoWay,
                                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                            };
                            _apiKeyTextBox.SetBinding(TextBox.TextProperty, binding);
                        }
                    }
                    
                    var index = parent.Children.IndexOf(ApiKeyBox);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, _apiKeyTextBox);
                    
                    // Update the text box with the password box value
                    _apiKeyTextBox.Text = ApiKeyBox.Password;
                    _apiKeyTextBox.Focus();
                }
                _isApiKeyVisible = true;
            }
        }
    }

    /// <summary>
    /// Converter to invert boolean values
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public static readonly InverseBooleanConverter Instance = new InverseBooleanConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }
    }

    /// <summary>
    /// Converter to invert boolean to visibility
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public static readonly InverseBooleanToVisibilityConverter Instance = new InverseBooleanToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Visibility visibility)
                return visibility == Visibility.Collapsed;
            return false;
        }
    }
}
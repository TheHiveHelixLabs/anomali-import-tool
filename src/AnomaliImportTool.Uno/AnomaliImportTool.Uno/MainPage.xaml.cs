using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.Logging;
using AnomaliImportTool.Uno.ViewModels;

namespace AnomaliImportTool.Uno;

/// <summary>
/// Main page for the Anomali Import Tool cross-platform application
/// </summary>
public sealed partial class MainPage : Page
{
    /// <summary>
    /// Initializes a new instance of MainPage
    /// </summary>
    public MainPage()
    {
        this.InitializeComponent();
        
        // Create logger for the MainViewModel
        // In a real application, this would come from DI container
        var loggerFactory = LoggerFactory.Create(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        var logger = loggerFactory.CreateLogger<MainViewModel>();
        
        // Create and set the DataContext
        DataContext = new MainViewModel(logger);
    }
}

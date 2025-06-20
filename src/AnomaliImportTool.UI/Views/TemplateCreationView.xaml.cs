using System.Windows.Controls;
using AnomaliImportTool.WPF.ViewModels;

namespace AnomaliImportTool.WPF.Views;

/// <summary>
/// Interaction logic for TemplateCreationView.xaml
/// </summary>
public partial class TemplateCreationView : UserControl
{
    public TemplateCreationView()
    {
        InitializeComponent();
        DataContext = new TemplateCreationViewModel();
    }
} 
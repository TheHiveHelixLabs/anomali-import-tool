using System.Windows.Controls;
using AnomaliImportTool.WPF.ViewModels;

namespace AnomaliImportTool.WPF.Views;

/// <summary>
/// Interaction logic for TemplateSelectionView.xaml
/// </summary>
public partial class TemplateSelectionView : UserControl
{
    public TemplateSelectionView()
    {
        InitializeComponent();
        // ViewModel will be fully implemented in 4.2; using stub for now
        DataContext = new TemplateSelectionViewModel();
    }
} 
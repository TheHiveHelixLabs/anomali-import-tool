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
using MahApps.Metro.Controls;

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using AnomaliImportTool.WPF.ViewModels;

namespace AnomaliImportTool.WPF.Views
{
    public partial class NavbarView : UserControl
    {
        public NavbarView()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this))
                return;
            this.DataContext = App.Services!.GetRequiredService<NavbarViewModel>();
        }

        private void HamburgerMenuControl_OnItemClick(object sender, MahApps.Metro.Controls.ItemClickEventArgs args)
        {
            this.HamburgerMenuControl.SetCurrentValue(ContentProperty, args.ClickedItem);
            this.HamburgerMenuControl.SetCurrentValue(HamburgerMenu.IsPaneOpenProperty, false);
        }
    }
}

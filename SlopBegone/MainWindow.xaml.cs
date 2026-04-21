using Dark.Net;
using SlopBegone.Handlers;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SlopBegone
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            DarkNet.Instance.SetWindowThemeWpf(this, Theme.Dark);
        }

        private void RemoveSlop_Click(object sender, RoutedEventArgs e)
        {
            RemoveSlop.IsEnabled = false;
            RemoveSlop.Content = "Removing Slop...";
            AiSlopHandler.DisableSlop();
            RemoveSlop.Content = "Slop Removed. Please reboot!";

        }
    }
}
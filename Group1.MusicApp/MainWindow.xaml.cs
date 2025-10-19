using Group1.ApiClient;
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

namespace Group1.MusicApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MusicAPI _musicApi;
        public MainWindow()
        {
            InitializeComponent();
            string clientId = "d6a90f5d97e4404b98dd3c17b3943d1d"; 
            string clientSecret = "c76a0e245e144fabbfe7be1b6c0cfd63";
            _musicApi = new MusicAPI(clientId, clientSecret);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string jsonNewReleases = await _musicApi.GetNewReleasesAsync(10);
                System.Diagnostics.Debug.WriteLine(jsonNewReleases);
                txtResult.Text = jsonNewReleases;
            }
            catch (Exception ex)
            {
                txtResult.Text = "Error: " + ex.Message;
            }
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using System.Threading.Tasks;

namespace OpenUtau.App.Views {
    public partial class ProjectSettingsDialog : Window {
        public ProjectSettingsDialog() {
            InitializeComponent();
        }

        void OkClicked(object sender, RoutedEventArgs e) {
            (DataContext as ProjectSettingsViewModel)?.Apply();
            Close();
        }

        void CancelClicked(object sender, RoutedEventArgs e) {
            Close();
        }

        async void LoadTunClicked(object sender, RoutedEventArgs e) {
            var path = await FilePicker.OpenFile(this, "pjsettings.loadtun", FilePicker.TUN);
            if (path != null) {
                (DataContext as ProjectSettingsViewModel)?.LoadTun(path);
            }
        }
    }
}

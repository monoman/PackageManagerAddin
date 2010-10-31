using System.Windows;
using System.Windows.Documents;

using Microsoft.VisualStudio.PlatformUI;

namespace NuGet.Dialog.PackageManagerUI {
    /// <summary>
    /// Interaction logic for LicenseAcceptanceWindow.xaml
    /// </summary>
    public partial class LicenseAcceptanceWindow : DialogWindow {
        public LicenseAcceptanceWindow() {
            InitializeComponent();
        }

        private void OnDeclineButtonClick(object sender, RoutedEventArgs e) {
            this.DialogResult = false;
        }

        private void OnAcceptButtonClick(object sender, RoutedEventArgs e) {
            this.DialogResult = true;
        }

        private void OnViewLicenseTermsRequestNavigate(object sender, RoutedEventArgs e) {
            Hyperlink hyperlink = (Hyperlink)sender;
            var licenseUrl = hyperlink.NavigateUri;
            UriHelper.OpenExternalLink(licenseUrl);
        }
    }
}

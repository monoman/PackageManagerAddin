﻿using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows;
using System.Windows.Input;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ExtensionsExplorer.UI;
using Microsoft.VisualStudio.PlatformUI;

using NuPack.Dialog.Providers;
using NuPack.VisualStudio;

using DTEPackage = Microsoft.VisualStudio.Shell.Package;

namespace NuPack.Dialog.PackageManagerUI {
    /// <summary>
    /// Interaction logic for PackageManagerWindow.xaml
    /// </summary>
    public partial class PackageManagerWindow : DialogWindow, INotifyPropertyChanged {
        private const string F1Keyword = "vs.ExtensionManager";
        private DTEPackage _ownerPackage;

        ///// <summary>
        ///// Constructor for the Extension Manager Window
        ///// </summary>
        public PackageManagerWindow(DTEPackage package)
            : base(F1Keyword) {

            InitializeComponent();

            System.Diagnostics.Debug.Assert(package != null);
            _ownerPackage = package;

            InstalledPackagesProvider installedPackagesProvider = new InstalledPackagesProvider(Resources);
            this.explorer.Providers.Add(installedPackagesProvider);

            UpdatePackagesProvider updatePackagesProvider = new UpdatePackagesProvider(Resources);
            this.explorer.Providers.Add(updatePackagesProvider);

            OnlinePackagesProvider onlinePackagesProvider = new OnlinePackagesProvider(Resources, false);
            this.explorer.Providers.Add(onlinePackagesProvider);
            this.explorer.SelectedProvider = onlinePackagesProvider;
        }

        protected void Window_Loaded(object sender, EventArgs e) {
        }


        void HandleRequestNavigate(object sender, RoutedEventArgs e) {
        }

        private void ExecutedUninstallExtension(object sender, ExecutedRoutedEventArgs e) {
        }

        private void CanExecuteUninstallExtension(object sender, CanExecuteRoutedEventArgs e) {
        }

        private void ExecutedToggleExtensionEnabledState(object sender, ExecutedRoutedEventArgs e) {

        }

        private void CanExecuteToggleExtensionEnabledState(object sender, CanExecuteRoutedEventArgs e) {
        }

        private void ExecutedUpdateExtension(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                return;
            }

            OnlinePackagesProvider provider = control.SelectedProvider as OnlinePackagesProvider;
            if (provider == null) {
                return;
            }

            bool accepted = ShowLicenseWindowIfRequired(selectedItem);
            if (accepted) {
                provider.Update(selectedItem.Id, new Version(selectedItem.Version));
            }
        }

        private void CanExecuteUpdateExtension(object sender, CanExecuteRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                e.CanExecute = false;
                return;
            }
            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = !selectedItem.IsUpdated;
        }


        private void ExecutedRestartVisualStudio(object sender, ExecutedRoutedEventArgs e) {
        }

        private void CanExecuteRestartVisualStudio(object sender, CanExecuteRoutedEventArgs e) {
        }


        private void ExecutedClose(object sender, ExecutedRoutedEventArgs e) {
            this.Close();
        }

        private void CanExecuteClose(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }


        private void ExecutedSelectOnlineProvider(object sender, ExecutedRoutedEventArgs e) {
        }

        private void ExecutedShowOptionsPage(object sender, ExecutedRoutedEventArgs e) {
            this.Close();
            OpenOptionsPage();
        }

        private void OpenOptionsPage() {
            _ownerPackage.ShowOptionPage(typeof(ToolsOptionsUI.ToolsOptionsPage));
        }

        private void ExecutedDownloadExtension(object sender, ExecutedRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                return;
            }

            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                return;
            }

            OnlinePackagesProvider provider = control.SelectedProvider as OnlinePackagesProvider;
            if (provider == null) {
                return;
            }

            bool accepted = ShowLicenseWindowIfRequired(selectedItem);
            if (accepted) {
                provider.Install(selectedItem.Id, new Version(selectedItem.Version));
            }
        }

        private void CanExecuteDownloadExtension(object sender, CanExecuteRoutedEventArgs e) {
            VSExtensionsExplorerCtl control = e.Source as VSExtensionsExplorerCtl;
            if (control == null) {
                e.CanExecute = false;
                return;
            }
            OnlinePackagesItem selectedItem = control.SelectedExtension as OnlinePackagesItem;
            if (selectedItem == null) {
                e.CanExecute = false;
                return;
            }

            if (selectedItem.IsInstalled) {
                //Don't allow the download command on extensions that are already installed.
                e.CanExecute = false;
                return;
            }

            e.CanExecute = true;
        }

        private void ExecutedFocusOnSearchBox(object sender, ExecutedRoutedEventArgs e) {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnNotifyPropertyChanged(string propertyName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void DialogWindow_Closed(object sender, EventArgs e) {
            // TODO: investigate to see if there is a better fix in CTP2
            this.explorer.Providers.Clear();
        }

        private bool ShowLicenseWindowIfRequired(OnlinePackagesItem selectedItem) {
            if (selectedItem.RequireLicenseAcceptance) {
                var licenseWidow = new LicenseAcceptanceWindow() {
                    Owner = this,
                    DataContext = selectedItem
                };

                bool? dialogResult = licenseWidow.ShowDialog();
                return dialogResult ?? false;
            }
            else {
                return true;
            }
        }
    }
}
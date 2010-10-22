using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoDevelop.Projects;
using NuPack;

namespace PackageReferenceAddin.Dialogs
{
    public partial class AddPackageReferenceDialog : Gtk.Dialog
    {
        private DotNetProject project;

        public IPackage SelectedPackage { get; private set; }

        public AddPackageReferenceDialog()
		{
			this.Build ();
		}
		
        public AddPackageReferenceDialog SetProject(DotNetProject project) 
        {
            this.project = project;
            return this;
        }

        protected virtual void OnButtonCloseClicked(object sender, System.EventArgs e)
        {
            this.Destroy();
        }

        protected virtual void OnButtonSettingsClicked(object sender, System.EventArgs e)
        {
            // TODO: Show configuration dialog
        }
    }
}

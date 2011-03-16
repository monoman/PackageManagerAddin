using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoDevelop.Projects;
using NuGet;

namespace PackageReferenceAddin.Dialogs
{
    public partial class AddPackageReferenceDialog : Gtk.Dialog
    {
        private DotNetProject project;
		private IPackageRepository repository;

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

		public AddPackageReferenceDialog SetPackageRepository(IPackageRepository repository)
		{
			this.repository = repository;
			return this;
		}

		protected virtual void OnButtonCloseClicked(object sender, System.EventArgs e)
		{
			// hardcode for now
			SelectedPackage = repository == null ? null : repository.FindPackage("NUnit");
			this.Destroy();
		}

        protected virtual void OnButtonSettingsClicked(object sender, System.EventArgs e)
        {
            // TODO: Show configuration dialog
        }
    }
}

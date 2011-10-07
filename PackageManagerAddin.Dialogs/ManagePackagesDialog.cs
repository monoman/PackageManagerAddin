using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoDevelop.Projects;
using NuGet;

namespace PackageManagerAddin.Dialogs
{
	public partial class ManagePackagesDialog : Gtk.Dialog
	{
		private DotNetProject project;
		private IPackageRepository repository;

		public IPackage SelectedPackage { get; private set; }

		public ManagePackagesDialog()
		{
			this.Build();
		}

		public ManagePackagesDialog SetProject(DotNetProject project)
		{
			this.project = project;
			return this;
		}

		public ManagePackagesDialog SetPackageRepository(IPackageRepository repository)
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

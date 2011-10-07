using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using PackageManagerAddin.Dialogs;
using PackageManagerAddin.Extensions;
using NuGet;

namespace PackageManagerAddin.Commands
{
	public enum PackageManagerCommands
	{
		Manage
	}

	public class PackageManagerCommandHandler : NodeCommandHandler
	{
		[CommandHandler(PackageManagerCommands.Manage)]
		public void ManagePackages()
		{
			// Get the project and project folder
			DotNetProject project = CurrentNode.GetParentDataItem(typeof(DotNetProject), true) as DotNetProject;
			IPackageRepository repository = CreateRepository();
			ManagePackagesDialog dialog = new ManagePackagesDialog().SetProject(project).SetPackageRepository(repository);

			try {
				if (MessageService.RunCustomDialog(dialog) == (int)Gtk.ResponseType.Ok) {
					project.AddPackage(dialog.SelectedPackage, repository);
					IdeApp.ProjectOperations.Save(project);
				}
			} catch (Exception exception) {
				MessageService.ShowException(exception);
			} finally {
				dialog.Destroy();
			}
		}

		public static IPackageRepository CreateRepository()
		{
			return new PackageRepositoryFactory().CreateRepository("https://packages.nuget.org/v1/FeedService.svc/");
		}
	}
}

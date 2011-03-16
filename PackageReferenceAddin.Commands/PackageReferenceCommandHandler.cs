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
using PackageReferenceAddin.Dialogs;
using PackageReferenceAddin.Extensions;
using NuGet;

namespace PackageReferenceAddin.Commands
{
    public enum PackageReferenceCommands
    {
        Add,
        Update,
        UpdateAll,
        Delete,
        DeleteAll
    }

    public class PackageReferenceCommandHandler : NodeCommandHandler
    {
        [CommandHandler(PackageReferenceCommands.Add)]
        public void NewPackageReference()
        {
            // Get the project and project folder
            DotNetProject project = CurrentNode.GetParentDataItem(typeof(DotNetProject), true) as DotNetProject;
			IPackageRepository repository = CreateRepository();
			AddPackageReferenceDialog dialog = new AddPackageReferenceDialog().SetProject(project).SetPackageRepository(repository);

            try
            {
                if (MessageService.RunCustomDialog(dialog) == (int)Gtk.ResponseType.Ok)
                {
                    project.AddPackage(dialog.SelectedPackage, repository);
                    IdeApp.ProjectOperations.Save(project);
                }
            }
            catch (Exception exception)
            {
                MessageService.ShowException(exception);
            }
            finally
            {
                dialog.Destroy();
            }
        }

		public static IPackageRepository CreateRepository()
		{
			IPackageRepositoryFactory factory = new PackageRepositoryFactory();
			IPackageRepository repository = factory.CreateRepository(new PackageSource("https://packages.nuget.org/v1/FeedService.svc/", "NuGet.org"));
			return repository;
		}
    }
}

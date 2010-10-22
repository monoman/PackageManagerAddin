using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;

using PackageReferenceAddin.Dialogs;
using PackageReferenceAddin.Extensions;

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

            AddPackageReferenceDialog dialog = new AddPackageReferenceDialog().SetProject(project);

            try
            {
                if (MessageService.RunCustomDialog(dialog) == (int)Gtk.ResponseType.Ok)
                {
                    project.AddPackage(dialog.SelectedPackage); // TODO create extension method for DotNetProject
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
    }
}

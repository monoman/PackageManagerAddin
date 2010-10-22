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

using NuPackAddin.Dialogs;
using NuPackAddin.Extensions;

namespace NuPackAddin.Commands
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
        /// <summary>Execute the command for adding a new web reference to a project.</summary>
        [CommandHandler(PackageReferenceCommands.Add)]
        public void NewPackageReference()
        {
            // Get the project and project folder
            DotNetProject project = CurrentNode.GetParentDataItem(typeof(DotNetProject), true) as DotNetProject;

            AddPackageReferenceDialog dialog = new AddPackageReferenceDialog(project);

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

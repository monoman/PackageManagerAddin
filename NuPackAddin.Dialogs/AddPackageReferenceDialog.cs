using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoDevelop.Projects;
using NuPack;

namespace NuPackAddin.Dialogs
{
    class AddPackageReferenceDialog : Gtk.Dialog
    {
        private readonly DotNetProject project;

        public IPackage SelectedPackage { get; private set; }

        public AddPackageReferenceDialog(DotNetProject project)
        {
            this.project = project;
        }
    }
}

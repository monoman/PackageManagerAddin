﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using EnvDTE;

namespace NuPack.VisualStudio.Cmdlets {

    /// <summary>
    /// This command creates new package file.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Package")]
    public class NewPackageCmdlet : NuPackBaseCmdlet {

        private static readonly HashSet<string> _exclude =
            new HashSet<string>(new[] { Constants.PackageExtension, Constants.ManifestExtension }, StringComparer.OrdinalIgnoreCase);

        #region Parameters

        [Parameter(Position = 0)]
        public string Project { get; set; }

        [Parameter(Position = 1)]
        [Alias("Spec")]
        public string SpecFile { get; set; }

        [Parameter(Position = 2)]
        public string TargetFile { get; set; }

        #endregion

        protected override void ProcessRecordCore() {

            string projectName = Project;
            if (String.IsNullOrEmpty(projectName)) {
                projectName = DefaultProjectName;
            }

            if (String.IsNullOrEmpty(projectName)) {
                WriteError("Missing -Project parameter and the default project is not set.");
                return;
            }

            var projectIns = GetProjectFromName(projectName);
            if (projectIns == null) {
                WriteError(String.Format(CultureInfo.CurrentCulture, "Project '{0}' is not found.", projectName));
                return;
            }

            var specItem = FindSpecFile(projectIns, SpecFile);
            if (specItem == null) {
                WriteError("Unable to locate a .nuspec file in the specified project.");
                return;
            }

            var specFilePath = specItem.FileNames[0];
            var builder = NuPack.PackageBuilder.ReadFrom(specFilePath);
            builder.Modified = builder.Created = DateTime.Now;
            // Remove the output file or the package spec might try to include it (which is default behavior)
            builder.Files.RemoveAll(file => _exclude.Contains(Path.GetExtension(file.Path)));

            string outputFile = TargetFile;
            if (String.IsNullOrEmpty(outputFile)) {
                outputFile = String.Join(".", builder.Id, builder.Version, Constants.PackageExtension.TrimStart('.'));
            }

            if (!Path.IsPathRooted(outputFile)) {
                // if the path is a relative, prepend the project path to it
                string folder = Path.GetDirectoryName(projectIns.FullName);
                outputFile = Path.Combine(folder, outputFile);
            }

            WriteLine("Creating package at " + outputFile + "...");

            using (Stream stream = File.Create(outputFile)) {
                builder.Save(stream);
            }

            WriteLine("Package file successfully created...");
        }

        private ProjectItem FindSpecFile(EnvDTE.Project projectIns, string specFile) {
            if (!String.IsNullOrEmpty(specFile)) {
                return projectIns.ProjectItems.Item(specFile);
            }
            else {
                // Verify if the project has eaxtly one file with the .nuspec extension. 
                // If found, use it as the manifest file for package creation.
                int count = 0;
                ProjectItem foundItem = null;

                foreach (ProjectItem item in projectIns.ProjectItems) {
                    if (item.Name.EndsWith(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase)) {
                        foundItem = item;
                        count++;
                        if (count > 1) {
                            break;
                        }
                    }
                }

                return (count == 1) ? foundItem : null;
            }
        }
    }
}
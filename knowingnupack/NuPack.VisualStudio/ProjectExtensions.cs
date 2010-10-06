﻿namespace NuPack.VisualStudio {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using EnvDTE;

    internal static class ProjectExtensions {
        // List of project types
        // http://www.mztools.com/articles/2008/MZ2008017.aspx
        private static readonly string[] _supportedProjectTypes = new[] { VSConstants.WebSiteProjectKind, 
                                                                          VSConstants.CsharpProjectKind, 
                                                                          VSConstants.VbProjectKind };

        private static readonly char[] PathSeparatorChars = new[] { Path.DirectorySeparatorChar };
        // Get the ProjectItems for a folder path
        public static ProjectItems GetProjectItems(this Project project, string folderPath, bool createIfNotExists = false) {
            // Traverse the path to get at the directory
            string[] pathParts = folderPath.Split(PathSeparatorChars, StringSplitOptions.RemoveEmptyEntries);
            return pathParts.Aggregate(project.ProjectItems, (projectItems, folderName) => GetOrCreateFolder(projectItems, folderName, createIfNotExists));
        }

        public static ProjectItem GetProjectItem(this Project project, string path) {
            string folderPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            ProjectItems container = GetProjectItems(project, folderPath);

            ProjectItem projectItem;
            // If we couldn't get the folder, or the file doesn't exist, return null
            if (container == null || !container.TryGetProjectItem(fileName, out projectItem)) {
                return null;
            }

            return projectItem;
        }

        public static bool DeleteProjectItem(this Project project, string path) {
            ProjectItem projectItem = GetProjectItem(project, path);
            if (projectItem == null) {
                return false;
            }

            projectItem.Delete();
            return true;
        }

        public static bool TryGetProjectItem(this ProjectItems projectItems, string name, out ProjectItem projectItem) {
            projectItem = (from ProjectItem p in projectItems
                           where p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                           select p).FirstOrDefault();

            return projectItem != null;
        }

        public static IEnumerable<ProjectItem> GetChildItems(this Project project, string path, string filter, params string[] kinds) {
            ProjectItems projectItems = GetProjectItems(project, path);

            if (projectItems == null) {
                return Enumerable.Empty<ProjectItem>();
            }

            Regex matcher = GetFilterRegex(filter);

            return from ProjectItem p in projectItems
                   where kinds.Contains(p.Kind) && matcher.IsMatch(p.Name)
                   select p;
        }

        public static T GetPropertyValue<T>(this Project project, string propertyName) {
            Property property = project.Properties.Item(propertyName);
            if (property != null) {
                return (T)property.Value;
            }
            return default(T);
        }

        private static Regex GetFilterRegex(string wildcard) {
            string pattern = String.Join(String.Empty, wildcard.Split('.').Select(GetPattern));
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }

        private static string GetPattern(string token) {
            return token == "*" ? @"(.*)" : @"(" + token + ")";
        }

        private static ProjectItems GetOrCreateFolder(ProjectItems projectItems, string folderName, bool createIfNotExists) {
            if (projectItems == null) {
                return null;
            }

            ProjectItem subFolder;
            if (projectItems.TryGetProjectItem(folderName, out subFolder)) {
                // Get the sub folder
                return subFolder.ProjectItems;
            }
            else if (createIfNotExists) {
                Property property = projectItems.Parent.Properties.Item("FullPath");

                Debug.Assert(property != null, "Unable to get full path property from the project item");
                // Get the full path of this folder on disk and add it
                string fullPath = Path.Combine(property.Value, folderName);

                return projectItems.AddFromDirectory(fullPath).ProjectItems;
            }

            return null;
        }

        public static bool IsSupported(this Project project) {
            return project.Kind != null &&
                   _supportedProjectTypes.Contains(project.Kind, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsWebSite(this Project project) {
            return project.Kind != null && project.Kind.Equals(VSConstants.WebSiteProjectKind, StringComparison.OrdinalIgnoreCase);
        }

    }
}

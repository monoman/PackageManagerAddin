﻿namespace NuPack.VisualStudio {
    using System.Collections.Generic;
    using System.Linq;
    using EnvDTE;

    internal static class SolutionExtensions {
        /// <summary>
        /// Get the list of all supported projects in the current solution. This method
        /// recursively iterates through all projects.
        /// </summary>
        public static IEnumerable<Project> GetAllProjects(this Solution solution) {
            if (solution == null || !solution.IsOpen) {
                yield break;
            }

            var projects = new Stack<Project>();
            foreach (Project project in solution.Projects) {
                projects.Push(project);
            }

            while (projects.Any()) {
                Project project = projects.Pop();

                if (project.IsSupported()) {
                    yield return project;
                }

                foreach (ProjectItem projectItem in project.ProjectItems) {
                    if (projectItem.SubProject != null) {
                        projects.Push(projectItem.SubProject);
                    }
                }
            }
        }
    }
}

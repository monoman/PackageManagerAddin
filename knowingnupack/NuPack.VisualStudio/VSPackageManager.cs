﻿namespace NuPack.VisualStudio {
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.ComponentModelHost;

    public class VSPackageManager : PackageManager {
        private static readonly ConcurrentDictionary<Solution, VSPackageManager> _packageManagerCache = new ConcurrentDictionary<Solution, VSPackageManager>();        
        private Dictionary<Project, ProjectManager> _projectManagers = null;

        private readonly DTE _dte;
        private readonly SolutionEvents _solutionEvents;
        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public VSPackageManager(DTE dte, IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IFileSystem fileSystem)
            : base(sourceRepository, pathResolver, fileSystem) {
            if (dte == null) {
                throw new ArgumentNullException("dte");
            }

            _dte = dte;
            // Apparently you must hold on to the instance you get from here, or it may be garbage collected and you
            // lose your events.
            _solutionEvents = _dte.Events.SolutionEvents;

            _solutionEvents.ProjectAdded += OnProjectAdded;
            _solutionEvents.ProjectRemoved += OnProjectRemoved;
            _solutionEvents.BeforeClosing += OnBeforeClosing;
        }

        private IEnumerable<ProjectManager> ProjectManagers {
            get {
                EnsureProjectManagers();
                return _projectManagers.Values;
            }
        }

        public ProjectManager GetProjectManager(Project project) {
            EnsureProjectManagers();
            ProjectManager projectManager;
            _projectManagers.TryGetValue(project, out projectManager);
            return projectManager;
        }

        private void EnsureProjectManagers() {
            // Cache the list of projects
            if (_projectManagers == null) {
                _projectManagers = new Dictionary<Project, ProjectManager>();

                foreach (Project project in _dte.Solution.GetAllProjects()) {
                    // Create a project manager for each of the projects
                    var projectManager = CreateProjectManager(project);

                    _projectManagers.Add(project, projectManager);
                }
            }
        }

        public void UpdatePackage(string packageId, Version version, bool updateDependencies) {
            var projectManagers = GetProjectsWithPackage(packageId, version);
            if (projectManagers.Any()) {
                foreach (var projectManager in projectManagers) {
                    projectManager.UpdatePackageReference(packageId, version, updateDependencies);
                }
            }
            else {
                InstallPackage(packageId, version);
            }
        }

        public override void UninstallPackage(IPackage package, bool force = false, bool removeDependencies = false) {
            // Remove reference from projects that reference this package
            var projectManagers = GetProjectsWithPackage(package.Id, package.Version);
            if (projectManagers.Any()) {
                // We don't need to actually call uninstall since uninstalling it from all the projects
                // already has a side effect of removing it from the package manager
                foreach (ProjectManager projectManager in projectManagers) {
                    projectManager.RemovePackageReference(package.Id, force, removeDependencies);
                }
            }
            else {
                base.UninstallPackage(package, force, removeDependencies);
            }
        }

        internal void OnPackageReferenceRemoved(IPackage removedPackage, bool force = false, bool removeDependencies = false) {
            if (!IsPackageReferenced(removedPackage)) {
                // There are no packages that depend on this one so just uninstall it
                base.UninstallPackage(removedPackage.Id, removedPackage.Version, force, removeDependencies);
            }
        }

        private bool IsPackageReferenced(IPackage package) {
            return GetProjectsWithPackage(package.Id, package.Version).Any();
        }

        // Need object overloads so that the powershell script can call into it
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "dte", Justification = "dte is the vs automation object")]
        public static VSPackageManager GetPackageManager(object dte) {
            return GetPackageManager((DTE)dte);
        }

        public static VSPackageManager GetPackageManager(DTE dte) {
            // Since we can't change the repository of an existing package manager
            // we need to create an entry that is based on the soltuion and the repository source            
            VSPackageManager packageManager;
            if (!_packageManagerCache.TryGetValue(dte.Solution, out packageManager)) {
                // Get the file system for the solution folder
                IFileSystem solutionFileSystem = GetFileSystem(dte);

                // Create a new vs package manager
                packageManager = new VSPackageManager(dte, 
                                                      VSPackageSourceProvider.GetRepository(dte), 
                                                      new DefaultPackagePathResolver(solutionFileSystem), solutionFileSystem);

                // Add it to the cache
                _packageManagerCache.TryAdd(dte.Solution, packageManager);
            }
            return packageManager;
        }

        private static IFileSystem GetFileSystem(DTE dte) {
            // Get the component model service from dte                               
            var componentModel = dte.GetService<IComponentModel>(typeof(SComponentModel));

            Debug.Assert(componentModel != null, "Component model service is null");

            // Get the source control providers
            var providers = componentModel.GetExtensions<ISourceControlFileSystemProvider>();

            // Get the packages path
            string path = Path.Combine(Path.GetDirectoryName(dte.Solution.FullName), "packages");
            IFileSystem fileSystem = null;

            var sourceControl = (SourceControl2)dte.SourceControl;
            if (providers.Any() && sourceControl != null) {
                SourceControlBindings binding = null;
                try {
                    // Get the binding for this solution
                    binding = sourceControl.GetBindings(dte.Solution.FullName);
                }
                catch (NotImplementedException) {
                    // Some source control providers don't bother to implement this.
                    // TFS might be the only one using it
                }

                if (binding != null) {
                    fileSystem = providers.Select(provider => GetFileSystemFromProvider(provider, path, binding))
                                          .Where(fs => fs != null)
                                          .FirstOrDefault();
                }
            }

            return fileSystem ?? new FileBasedProjectSystem(path);
        }

        private static IFileSystem GetFileSystemFromProvider(ISourceControlFileSystemProvider provider, string path, SourceControlBindings binding) {
            try {
                return provider.GetFileSystem(path, binding);
            }
            catch {
                // Ignore exceptions that can happen when some binaries are missing. e.g. TfsSourceControlFileSystemProvider
                // would throw a jitting error if TFS is not installed
            }

            return null;
        }

        private ProjectManager CreateProjectManager(Project project) {
            return new VSProjectManager(this, PathResolver, project);
        }

        private IEnumerable<ProjectManager> GetProjectsWithPackage(string packageId, Version version) {
            return from projectManager in ProjectManagers
                   let package = projectManager.LocalRepository.FindPackage(packageId)
                   where package != null && (version == null || (version != null && package.Version.Equals(version)))
                   select projectManager;
        }

        private void OnBeforeClosing() {
            // Invalidate our cache on closing
            _projectManagers = null;

            // Remove this item from the cache
            VSPackageManager removed;
            _packageManagerCache.TryRemove(_dte.Solution, out removed);
        }

        private void OnProjectRemoved(Project project) {
            if (_projectManagers != null) {
                _projectManagers.Remove(project);
            }
        }

        private void OnProjectAdded(Project project) {
            // Only add supported projects
            if (project.IsSupported()) {
                EnsureProjectManagers();
                // If _projectManagers was null then EnsureProjectManagers would have populated 
                // the cache with this project already.
                if (!_projectManagers.ContainsKey(project)) {
                    _projectManagers.Add(project, CreateProjectManager(project));
                }
            }
        }
    }
}
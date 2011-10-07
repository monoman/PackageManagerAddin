using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using MonoDevelop.Projects;
using NuGet;
using System.Runtime.Versioning;

namespace PackageManagerAddin.Extensions
{
	public static class DotNetProjectExtensions
	{
		private class ProjectSystemWrapper : PhysicalFileSystem, IProjectSystem
		{
			private readonly DotNetProject project;

			public ProjectSystemWrapper(DotNetProject project)
				: base(project.ParentSolution.BaseDirectory)
			{
				this.project = project;
			}

			public void AddReference(string referencePath, System.IO.Stream stream)
			{
				throw new NotImplementedException();
			}

			public dynamic GetPropertyValue(string propertyName)
			{
				throw new NotImplementedException();
			}

			public bool IsSupportedFile(string path)
			{
				throw new NotImplementedException();
			}

			public string ProjectName
			{
				get { return project.Name; }
			}

			public bool ReferenceExists(string name)
			{
				return project.References.Count(r => r.Reference.Equals(name, StringComparison.InvariantCultureIgnoreCase)) > 0;
			}

			public void RemoveReference(string name)
			{
				throw new NotImplementedException();
			}

			public FrameworkName TargetFramework
			{
				get { return new FrameworkName(project.TargetFramework.ToString()); }
			}


			public void AddFrameworkReference(string name)
			{
				throw new NotImplementedException();
			}

			public string ResolvePath(string path)
			{
				throw new NotImplementedException();
			}
		}

		public static void AddPackage(this DotNetProject project, IPackage package, IPackageRepository repository)
		{
			var packageManager = new PackageManager(repository, project.BaseDirectory);
			packageManager.InstallPackage(package, false);
			var projectSystem = new ProjectSystemWrapper(project);
			var projectManager = new ProjectManager(repository, packageManager.PathResolver, projectSystem, packageManager.LocalRepository);
			projectManager.AddPackageReference(package.Id);
			project.NeedsReload = true;
		}

		private class Logger : ILogger
		{
			public void Log(MessageLevel level, string message, params object[] args)
			{
				// TODO: make it work with MD console pads
				Console.WriteLine(message, args);
			}
		}

	}
}

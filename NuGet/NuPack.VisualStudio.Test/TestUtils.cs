using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Castle.DynamicProxy.Generators;
using EnvDTE;
using Moq;

namespace NuGet.VisualStudio.Test {
    internal static class TestUtils {
        private static readonly Func<bool> actionWrapper = () => { AttributesToAvoidReplicating.Add<TypeIdentifierAttribute>(); return true; };
        private static readonly Lazy<bool> lazyAction = new Lazy<bool>(actionWrapper);

        public static Project GetProject(string name, string kind = VsConstants.CsharpProjectKind, IEnumerable<string> projectFiles = null) {
            Debug.Assert(lazyAction.Value, "Lazy action must have been initialized by now");

            Mock<Project> project = new Mock<Project>();
            project.SetupGet(p => p.Name).Returns(name);
            project.SetupGet(p => p.FullName).Returns(name);
            project.SetupGet(p => p.UniqueName).Returns(name);
            project.SetupGet(p => p.Kind).Returns(kind);

            Mock<Properties> properties = new Mock<Properties>();
            Mock<Property> fullName = new Mock<Property>();
            fullName.Setup(c => c.Value).Returns(name);
            properties.Setup(p => p.Item(It.IsAny<string>())).Returns(fullName.Object);
            project.SetupGet(p => p.Properties).Returns(properties.Object);
            if (projectFiles != null) {

                var lookup = new Dictionary<object, ProjectItem>();
                var projectItems = new Mock<ProjectItems>();

                foreach (var file in projectFiles) {
                    var item = new Mock<ProjectItem>();
                    item.Setup(i => i.Name).Returns(file);
                    lookup[file] = item.Object;
                    projectItems.Setup(i => i.Item(It.IsAny<object>())).Returns((object index) => lookup[index]);
                }
                projectItems.Setup(c => c.GetEnumerator()).Returns(lookup.Values.GetEnumerator());
                project.Setup(p => p.ProjectItems).Returns(projectItems.Object);
            }
            return project.Object;
        }

        public static ISolutionManager GetSolutionManager(bool isSolutionOpen = true, string defaultProjectName = null, IEnumerable<Project> projects = null) {
            var solutionManager = new Mock<ISolutionManager>();
            solutionManager.SetupGet(c => c.DefaultProjectName).Returns(defaultProjectName);
            solutionManager.Setup(c => c.GetProjects()).Returns(projects ?? Enumerable.Empty<Project>());
            solutionManager.Setup(c => c.GetProject(It.IsAny<string>()))
                .Returns((string name) => projects.FirstOrDefault(p => p.Name == name));
            solutionManager.SetupGet(c => c.IsSolutionOpen).Returns(isSolutionOpen);
            return solutionManager.Object;
        }

        public static DTE GetDTE() {
            Debug.Assert(lazyAction.Value);
            return new Mock<DTE>().Object;
        }
    }
}

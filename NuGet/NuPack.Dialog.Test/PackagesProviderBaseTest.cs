﻿using System.Windows;
using Microsoft.VisualStudio.ExtensionsExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NuGet.Dialog.Providers;
using NuGet.Test;
using NuGet.Test.Mocks;
using NuGet.VisualStudio;

namespace NuGet.Dialog.Test {

    [TestClass]
    public class PackagesProviderBaseTest {

        [TestMethod]
        public void CtorThrowsIfPackageManagerArgumentIsNull() {
            ResourceDictionary resources = new ResourceDictionary();

            ExceptionAssert.ThrowsArgNull(
                () => new ConcretePackagesProvider(null, new Mock<IProjectManager>().Object, resources),
                "packageManager");
        }

        [TestMethod]
        public void CtorThrowsIfProjectManagerArgumentIsNull() {
            ResourceDictionary resources = new ResourceDictionary();

            ExceptionAssert.ThrowsArgNull(
                () => new ConcretePackagesProvider(new Mock<IVsPackageManager>().Object, null, resources),
                "projectManager");
        }

        [TestMethod]
        public void CtorThrowsIfResourcesArgumentIsNull() {
            ExceptionAssert.ThrowsArgNull(
                () => new ConcretePackagesProvider(null),
                "resources");
        }

        [TestMethod]
        public void ToStringMethodReturnsNameValue() {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // act
            string providerName = provider.ToString();

            // Assert
            Assert.AreEqual("Test Provider", providerName);
        }

        [TestMethod]
        public void PropertyRefreshOnNodeSelectionIsFalse() {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act & Assert
            Assert.IsFalse(provider.RefreshOnNodeSelection);
        }

        [TestMethod]
        public void ExtensionsTreeIsNotNull() {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act && Assert
            Assert.IsNotNull(provider.ExtensionsTree);
        }

        [TestMethod]
        public void ExtensionsTreeIsPopulatedWithOneNode() {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act && Assert
            Assert.AreEqual(1, provider.ExtensionsTree.Nodes.Count);
            Assert.IsInstanceOfType(provider.ExtensionsTree.Nodes[0], typeof(SimpleTreeNode));
            Assert.AreEqual("All", provider.ExtensionsTree.Nodes[0].Name);
        }

        [TestMethod]
        public void SearchMethodCreatesNewTreeNode() {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();
            provider.SelectedNode = (PackagesTreeNodeBase)provider.ExtensionsTree.Nodes[0];

            // Act
            IVsExtensionsTreeNode searchNode = provider.Search("hello");

            // Assert
            Assert.IsNotNull(searchNode);
            Assert.IsInstanceOfType(searchNode, typeof(PackagesSearchNode));
            Assert.IsTrue(provider.ExtensionsTree.Nodes.Contains(searchNode));
        }

        [TestMethod]
        public void MediumIconDataTemplate() {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();

            // Act && Assert
            Assert.IsNotNull(provider.MediumIconDataTemplate);
        }

        [TestMethod]
        public void DetailViewDataTemplate() {
            // Arrange
            PackagesProviderBase provider = CreatePackagesProviderBase();
            
            // Act && Assert
            Assert.IsNotNull(provider.DetailViewDataTemplate);
        }

        private PackagesProviderBase CreatePackagesProviderBase() {
            ResourceDictionary resources = new ResourceDictionary();
            resources.Add("PackageItemTemplate", new DataTemplate());
            resources.Add("PackageDetailTemplate", new DataTemplate());

            return new ConcretePackagesProvider(resources);
        }

        private class ConcretePackagesProvider : PackagesProviderBase {

            public ConcretePackagesProvider(ResourceDictionary resources) :
                this(new Mock<IVsPackageManager>().Object, new Mock<IProjectManager>().Object, resources) {
            }

            public ConcretePackagesProvider(IVsPackageManager packageManager, IProjectManager projectManager, ResourceDictionary resources) :
                base(packageManager, projectManager, resources) {
            }

            public override IVsExtension CreateExtension(IPackage package) {
                return new Mock<IVsExtension>().Object;
            }

            public override bool CanExecute(PackageItem item) {
                return false;
            }

            public override void Execute(PackageItem item, PackageManagerUI.ILicenseWindowOpener licenseWindowOpener) {
            }

            protected override void FillRootNodes() {
                var repository = new MockPackageRepository();
                repository.AddPackage(PackageUtility.CreatePackage("hello", "1.0"));
                repository.AddPackage(PackageUtility.CreatePackage("world", "2.0"));
                repository.AddPackage(PackageUtility.CreatePackage("nuget", "3.0"));

                RootNode.Nodes.Add(new SimpleTreeNode(new MockPackagesProvider(), "All", RootNode, repository));
            }

            public override string Name {
                get {
                    return "Test Provider";
                }
            }
        }
    }
}

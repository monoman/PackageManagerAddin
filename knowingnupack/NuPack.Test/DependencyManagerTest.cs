﻿namespace NuPack.Test {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NuPack.Test.Mocks;
    using Moq;

    [TestClass]
    public class DependencyManagerTest {
        [TestMethod]
        public void ReverseDependencyWalkerUsersVersionAndIdToDetermineVisited() {            
            // Arrange
            // A 1.0 -> B 1.0
            IPackage packageA1 = PackageUtility.CreatePackage("A",
                                                            "1.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 PackageDependency.CreateDependency("B", version:new Version("1.0"))
                                                             });
            // A 2.0 -> B 2.0
            IPackage packageA2 = PackageUtility.CreatePackage("A",
                                                            "2.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 PackageDependency.CreateDependency("B", version:new Version("2.0"))
                                                             });

            IPackage packageB1 = PackageUtility.CreatePackage("B", "1.0");
            IPackage packageB2 = PackageUtility.CreatePackage("B", "2.0");

            var mockRepository = new MockPackageRepository();
            mockRepository.AddPackage(packageA1);
            mockRepository.AddPackage(packageA2);
            mockRepository.AddPackage(packageB1);
            mockRepository.AddPackage(packageB2);
            
            // Act 
            DependentLookup lookup = DependentLookup.Create(mockRepository);

            // Assert
            Assert.AreEqual(0, lookup.GetDependents(packageA1).Count());
            Assert.AreEqual(0, lookup.GetDependents(packageA2).Count());
            Assert.AreEqual(1, lookup.GetDependents(packageB1).Count());
            Assert.AreEqual(1, lookup.GetDependents(packageB2).Count());
        }

        [TestMethod]
        public void ResolveDependenciesForInstallPackageWithUnknownDependencyThrows() {
            // Arrange            
            IPackage package = PackageUtility.CreatePackage("A",
                                                            "1.0",
                                                             dependencies: new List<PackageDependency> {
                                                                 PackageDependency.CreateDependency("B")
                                                             });

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => DependencyManager.ResolveDependenciesForInstall(package,
                                                                                                                    new MockPackageRepository(),
                                                                                                                    new MockPackageRepository()), "Unable to resolve dependency 'B'");
        }

        [TestMethod]
        public void ResolveDependenciesForInstallCircularReferenceThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("A")
                                                                });

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => DependencyManager.ResolveDependenciesForProjectInstall(packageA, localRepository, sourceRepository), "Circular dependency detected 'A 1.0 => B 1.0 => A 1.0'");
        }

        [TestMethod]
        public void ResolveDependenciesForInstallDiamondDependencyGraph() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            // A -> [B, C]
            // B -> [D]
            // C -> [D]
            //    A
            //   / \
            //  B   C
            //   \ /
            //    D 

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B"),
                                                                    PackageDependency.CreateDependency("C")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D")
                                                                });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D")
                                                                });

            IPackage packageD = PackageUtility.CreatePackage("D", "1.0");

            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);
            sourceRepository.AddPackage(packageC);
            sourceRepository.AddPackage(packageD);

            // Act
            var packages = DependencyManager.ResolveDependenciesForInstall(packageA, localRepository, sourceRepository).ToList();

            // Assert
            var dict = packages.ToDictionary(p => p.Id);
            Assert.AreEqual(4, packages.Count);
            Assert.IsNotNull(dict["A"]);
            Assert.IsNotNull(dict["B"]);
            Assert.IsNotNull(dict["C"]);
            Assert.IsNotNull(dict["D"]);
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallDiamondDependencyGraph() {
            // Arrange
            var localRepository = new MockPackageRepository();
            // A -> [B, C]
            // B -> [D]
            // C -> [D]
            //    A
            //   / \
            //  B   C
            //   \ /
            //    D 

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B"),
                                                                    PackageDependency.CreateDependency("C")
                                                                });


            IPackage packageB = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D")
                                                                });

            IPackage packageC = PackageUtility.CreatePackage("C", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("D")
                                                                });

            IPackage packageD = PackageUtility.CreatePackage("D", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);
            localRepository.AddPackage(packageD);

            // Act
            var packages = DependencyManager.ResolveDependenciesForUninstall(packageA, localRepository, removeDependencies: true)
                                            .ToDictionary(p => p.Id);

            // Assert
            Assert.AreEqual(4, packages.Count);
            Assert.IsNotNull(packages["A"]);
            Assert.IsNotNull(packages["B"]);
            Assert.IsNotNull(packages["C"]);
            Assert.IsNotNull(packages["D"]);
        }


        [TestMethod]
        public void ResolveDependencyForInstallCircularReferenceWithDifferentVersionOfPackageReferenceThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA10 = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageA15 = PackageUtility.CreatePackage("A", "1.5",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });


            IPackage packageB10 = PackageUtility.CreatePackage("B", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("A", version: Version.Parse("1.5"))
                                                                });

            sourceRepository.AddPackage(packageA10);
            sourceRepository.AddPackage(packageA15);
            sourceRepository.AddPackage(packageB10);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => DependencyManager.ResolveDependenciesForInstall(packageA10, localRepository, sourceRepository), "Circular dependency detected 'A 1.0 => B 1.0 => A 1.5'");
        }

        [TestMethod]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetMinimumVersionThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();
            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", minVersion: Version.Parse( "1.5"))
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.4");
            sourceRepository.AddPackage(packageA);
            sourceRepository.AddPackage(packageB);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => DependencyManager.ResolveDependenciesForInstall(packageA, localRepository, sourceRepository), "Unable to resolve dependency 'B (>= 1.5)'");
        }

        [TestMethod]
        public void ResolveDependencyForInstallPackageWithDependencyThatDoesntMeetExactVersionThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();
            var sourceRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B", version: Version.Parse( "1.5"))
                                                                });

            sourceRepository.AddPackage(packageA);

            IPackage packageB = PackageUtility.CreatePackage("B", "1.4");
            sourceRepository.AddPackage(packageB);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => DependencyManager.ResolveDependenciesForInstall(packageA, localRepository, sourceRepository), "Unable to resolve dependency 'B (= 1.5)'");
        }

        [TestMethod]
        public void ResolveDependenciesForInstallPackageWithDependencyReturnsPackageAndDependency() {
            // Arrange            
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            // Act
            var packages = DependencyManager.ResolveDependenciesForUninstall(packageA, localRepository, removeDependencies: true)
                                            .ToDictionary(p => p.Id);

            // Assert
            Assert.AreEqual(2, packages.Count);
            Assert.IsNotNull(packages["A"]);
            Assert.IsNotNull(packages["B"]);
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithMissingDependencyAndRemoveDependenciesThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => DependencyManager.ResolveDependenciesForUninstall(packageA, localRepository, removeDependencies: true), "Unable to locate dependency 'B'. It may have been uninstalled");
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithDependentThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => DependencyManager.ResolveDependenciesForUninstall(packageB, localRepository), "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it");
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithDependentAndRemoveDependenciesThrows() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            // Act & Assert
            ExceptionAssert.Throws<InvalidOperationException>(() => DependencyManager.ResolveDependenciesForUninstall(packageB, localRepository, removeDependencies: true), "Unable to uninstall 'B 1.0' because 'A 1.0' depends on it");
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithDependentAndForceReturnsPackage() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B")
                                                                });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);

            // Act
            var packages = DependencyManager.ResolveDependenciesForUninstall(packageB, localRepository, force: true)
                             .ToDictionary(p => p.Id);

            // Assert
            Assert.AreEqual(1, packages.Count);
            Assert.IsNotNull(packages["B"]);
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesUninstallsUnusedDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                PackageDependency.CreateDependency("B"),
                                                                PackageDependency.CreateDependency("C")
                                                            });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0");
            IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                PackageDependency.CreateDependency("C"),
                                                            });

            localRepository.AddPackage(packageD);
            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);

            // Act
            var packages = DependencyManager.ResolveDependenciesForUninstall(packageA, localRepository, removeDependencies: true)
                                            .ToDictionary(p => p.Id);

            // Assert     
            Assert.AreEqual(2, packages.Count);
            Assert.IsNotNull(packages["A"]);
            Assert.IsNotNull(packages["B"]);
        }

        [TestMethod]
        public void ResolveDependenciesForUninstallPackageWithRemoveDependenciesSetAndForceReturnsAllDependencies() {
            // Arrange
            var localRepository = new MockPackageRepository();

            IPackage packageA = PackageUtility.CreatePackage("A", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                    PackageDependency.CreateDependency("B"),
                                                                    PackageDependency.CreateDependency("C")
                                                            });

            IPackage packageB = PackageUtility.CreatePackage("B", "1.0");
            IPackage packageC = PackageUtility.CreatePackage("C", "1.0");
            IPackage packageD = PackageUtility.CreatePackage("D", "1.0",
                                                            dependencies: new List<PackageDependency> {
                                                                PackageDependency.CreateDependency("C"),
                                                            });

            localRepository.AddPackage(packageA);
            localRepository.AddPackage(packageB);
            localRepository.AddPackage(packageC);
            localRepository.AddPackage(packageD);

            // Act
            var packages = DependencyManager.ResolveDependenciesForUninstall(packageA, localRepository, force: true, removeDependencies: true)
                                            .ToDictionary(p => p.Id);

            // Assert            
            Assert.IsNotNull(packages["A"]);
            Assert.IsNotNull(packages["B"]);
            Assert.IsNotNull(packages["C"]);
        }

    }
}

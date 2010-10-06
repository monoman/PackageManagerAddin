﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NuPack {
    internal class XmlTransfomer : IPackageFileTransformer {
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "We are creating a new stream for the caller to use")]
        public void TransformFile(IPackageFile file, string targetPath, ProjectSystem projectSystem, ILogger listener) {
            // Get the xml fragment
            XElement xmlFragment = GetXml(file);

            XDocument transformDocument = XmlUtility.GetOrCreateDocument(xmlFragment.Name, projectSystem, targetPath);

            // Do a merge
            transformDocument.Root.MergeWith(xmlFragment);


            projectSystem.AddFile(targetPath, transformDocument.Save);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "We are creating a new stream for the caller to use")]
        public void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, ProjectSystem projectSystem, ILogger listener) {
            // Get the xml snippet
            XElement xmlFragment = GetXml(file);

            XDocument document = XmlUtility.GetOrCreateDocument(xmlFragment.Name, projectSystem, targetPath);

            // Merge the other xml elements into one element within this xml hierarchy (matching the config file path)
            var mergedFragments = matchingFiles.Select(GetXml)
                                               .Aggregate(new XElement(xmlFragment.Name), (left, right) => left.MergeWith(right));

            // Take the difference of the xml and remove it from the main xml file
            document.Root.Except(xmlFragment.Except(mergedFragments));

            // Save the new content to the file system
            projectSystem.AddFile(targetPath, document.Save);            
        }

        private static XElement GetXml(IPackageFile file) {
            using (Stream stream = file.Open()) {
                return XElement.Load(stream);
            }
        }
    }
}
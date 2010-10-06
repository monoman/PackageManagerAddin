﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NuPack.Resources;

namespace NuPack {
    /// <summary>
    /// Simple token replacement system for content files.
    /// </summary>
    public class Preprocessor : IPackageFileTransformer {
        private static readonly Regex _tokenRegex = new Regex(@"\$(?<propertyName>\w+)\$");

        public void TransformFile(IPackageFile file, string targetPath, ProjectSystem projectSystem, ILogger listener) {
            if (!projectSystem.FileExists(targetPath)) {
                using (Stream stream = Process(file, projectSystem).AsStream()) {
                    projectSystem.AddFile(targetPath, stream);
                }
            }
        }

        public void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, ProjectSystem projectSystem, ILogger listener) {
            Func<Stream> streamFactory = () => Process(file, projectSystem).AsStream();
            FileSystemExtensions.DeleteFile(projectSystem, targetPath, streamFactory, listener);
        }
        
        private string Process(IPackageFile file, ProjectSystem projectSystem) {
            string text = file.Open().ReadToEnd();
            return _tokenRegex.Replace(text, match => ReplaceToken(match, projectSystem));
        }

        private string ReplaceToken(Match match, ProjectSystem projectSystem) {
            string propertyName = match.Groups["propertyName"].Value;
            var value = projectSystem.GetPropertyValue(propertyName);
            if (value == null) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuPackResources.TokenHasNoValue, propertyName));
            }
            return value;
        }
    }
}
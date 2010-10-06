﻿using System;
using System.IO;

namespace NuPack {
    internal static class PathResolver {
        public static PathSearchFilter ResolvePath(string basePath, string source) {
            basePath = basePath ?? String.Empty;
            string pathFromBase = Path.Combine(basePath, source.TrimStart(Path.DirectorySeparatorChar));

            if (pathFromBase.Contains("*")) {
                return GetPathSearchFilter(pathFromBase);
            }
            else { 
                pathFromBase = Path.GetFullPath(pathFromBase.TrimStart(Path.DirectorySeparatorChar));
                string directory = Path.GetDirectoryName(pathFromBase);
                string searchFilter = Path.GetFileName(pathFromBase);
                return new PathSearchFilter(NormalizeSearchDirectory(directory), NormalizeSearchFilter(searchFilter), SearchOption.TopDirectoryOnly);
            }
        }

        private static PathSearchFilter GetPathSearchFilter(string path) {
            int recursiveSearchIndex = path.IndexOf("**", StringComparison.OrdinalIgnoreCase);
            if (recursiveSearchIndex != -1) {
                // Recursive searches are of the format /foo/bar/**/*[.abc]
                string searchPattern = path.Substring(recursiveSearchIndex + 2).TrimStart(Path.DirectorySeparatorChar);
                string searchDirectory = recursiveSearchIndex == 0 ? "." : path.Substring(0, recursiveSearchIndex - 1);
                return new PathSearchFilter(NormalizeSearchDirectory(searchDirectory), NormalizeSearchFilter(searchPattern), SearchOption.AllDirectories);
            }
            else {
                string searchDirectory;
                searchDirectory = Path.GetDirectoryName(path);
                if (String.IsNullOrEmpty(searchDirectory)) {
                    // Path starts with a wildcard e.g. *, *.foo, *foo, foo*
                    // Set the current directory to be the search path
                    searchDirectory = ".";
                }
                string searchPattern = Path.GetFileName(path);

                return new PathSearchFilter(NormalizeSearchDirectory(searchDirectory), NormalizeSearchFilter(searchPattern), 
                    SearchOption.TopDirectoryOnly);
            }
        }

        /// <summary>
        /// Resolves the path of a file inside of a package 
        /// For paths that are relative, the destination path is resovled as the path relative to the basePath (path to the manifest file)
        /// For all other paths, the path is resolved as the first path portion that does not contain 
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="actualPath"></param>
        /// <param name="searchPath"></param>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public static string ResolvePackagePath(string basePath, string actualPath, string targetPath) {
            if (String.IsNullOrEmpty(basePath)) {
                basePath = ".";
            }
            basePath = Path.GetFullPath(basePath);
            actualPath = Path.GetFullPath(actualPath);
            string packagePath = null;
            if (actualPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) {
                packagePath = actualPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            else{
                packagePath = Path.GetFileName(actualPath);
            }
            return Path.Combine(targetPath ?? String.Empty, packagePath);
        }

        private static string NormalizeSearchDirectory(string directory) {
            return Path.GetFullPath(String.IsNullOrEmpty(directory) ? "." : directory);
        }

        private static string NormalizeSearchFilter(string filter) {
            return String.IsNullOrEmpty(filter) ? "*" : filter;
        }
    }


}

﻿namespace NuPack {
    public sealed class Constants {
        public static readonly string PackageExtension = ".nupkg";
        public static readonly string ManifestExtension = ".nuspec";        
        public static readonly string ManifestSchemaNamespace = SchemaNamespace + "nuspec.xsd";
        public static readonly string ContentDirectory = "content";
        public static readonly string PackageReferenceFile = "packages.config";


        internal const string SchemaNamespace = "http://schemas.microsoft.com/packaging/2010/07/";
    }
}

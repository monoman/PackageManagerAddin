﻿namespace NuPack {
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Packaging;
    
    internal class ZipPackageFile : IPackageFile {
        private readonly Func<MemoryStream> _streamFactory;
        private readonly string _path;

        public ZipPackageFile(PackagePart part) {           
            Debug.Assert(part != null, "part should not be null");

            byte[] buffer;
            using (Stream partStream = part.GetStream()) {
                using (var stream = new MemoryStream()) {
                    partStream.CopyTo(stream);
                    buffer = stream.ToArray();
                }
            }
            _path = UriHelper.GetPath(part.Uri);            
            _streamFactory = () => new MemoryStream(buffer);
        }

        public string Path {
            get {
                return _path;
            }
        }

        public Stream Open() {
            return _streamFactory();
        }

        public override string ToString() {
            return Path;
        }
    }
}

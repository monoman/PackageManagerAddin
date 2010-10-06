﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuPack.VisualStudio.Resources;

namespace NuPack.VisualStudio {
    public class VSPackageSourceProvider {

        internal const string DefaultPackageSource = "http://go.microsoft.com/fwlink/?LinkID=199193";

        private PackageSourceSettingsManager _settingsManager;
        private HashSet<PackageSource> _packageSources;
        private PackageSource _activePackageSource;

        private static readonly ConcurrentDictionary<_DTE, VsPackageSourceCacheItem> _cache = new ConcurrentDictionary<_DTE, VsPackageSourceCacheItem>();

        private VSPackageSourceProvider(IServiceProvider serviceProvider) {
            _settingsManager = new PackageSourceSettingsManager(serviceProvider);

            DeserializePackageSources();
            DeserializeActivePackageSource();
        }

        public static VSPackageSourceProvider GetSourceProvider(_DTE dte) {
            return GetCacheItem(dte).Provider;
        }

        public static IPackageRepository GetRepository(_DTE dte) {
            return GetCacheItem(dte).Repository;
        }

        private static VsPackageSourceCacheItem GetCacheItem(_DTE dte) {
            return _cache.GetOrAdd(
                dte,
                dteValue => {
                    IServiceProvider serviceProvider = dteValue.GetServiceProvider();
                    var provider = new VSPackageSourceProvider(serviceProvider);
                    return new VsPackageSourceCacheItem(provider, new VSPackageSourceRepository(provider));
                }
            );
        }

        private void DeserializePackageSources() {
            string propertyString = _settingsManager.PackageSourcesString;

            if (!String.IsNullOrEmpty(propertyString)) {
                _packageSources = SerializationHelper.Deserialize<HashSet<PackageSource>>(propertyString);
            }
            
            if (_packageSources == null) {
                _packageSources = new HashSet<PackageSource>();
            }
        }

        private void DeserializeActivePackageSource() {
            var packageSource = SerializationHelper.Deserialize<PackageSource>(_settingsManager.ActivePackageSourceString);
            if (packageSource != null) {
                // this is to guard against corrupted VS user settings store
                AddPackageSource(packageSource);

                ActivePackageSource = packageSource;
            }
            else if (_settingsManager.IsFirstRunning) {
                packageSource = new PackageSource("NuPack official package source", DefaultPackageSource);
                AddPackageSource(packageSource);
                _settingsManager.IsFirstRunning = false;
            }
        }
        
        public PackageSource ActivePackageSource {
            get {
                return _activePackageSource;
            }
            set {
                if (value != null && !_packageSources.Contains(value)) {
                    throw new ArgumentException(VsResources.PackageSource_Invalid);
                }

                _activePackageSource = value;

                // persist the value into VS settings store
                _settingsManager.ActivePackageSourceString = SerializationHelper.Serialize(value);
            }
        }

        public IEnumerable<PackageSource> GetPackageSources() {
            return _packageSources;
        }

        public void AddPackageSource(PackageSource source) {

            if (source == null) {
                throw new ArgumentNullException("source");
            }

            if (!_packageSources.Contains(source)) {
                _packageSources.Add(source);
                
                // if the package source that we just added is the only one, make it the default
                if (ActivePackageSource == null && _packageSources.Count == 1) {
                    ActivePackageSource = source;
                }

                PersistPackageSources();
            }
        }

        public bool RemovePackageSource(PackageSource source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            bool result = _packageSources.Remove(source);
            if (result) {
                PersistPackageSources();
                if (source.Equals(ActivePackageSource)) {
                    ActivePackageSource = null;
                }
            }

            return result;
        }

        public void SetPackageSources(IEnumerable<PackageSource> sources) {
            _packageSources.Clear();
            ActivePackageSource = null;

            if (sources != null) {
                foreach (var s in sources) {
                    _packageSources.Add(s);
                }
            }

            PersistPackageSources();
        }

        private void PersistPackageSources() {
            _settingsManager.PackageSourcesString = SerializationHelper.Serialize(_packageSources);
        }

        private class VsPackageSourceCacheItem {
            public VsPackageSourceCacheItem(VSPackageSourceProvider provider, IPackageRepository repository) {
                Provider = provider;
                Repository = repository;
            }
            public VSPackageSourceProvider Provider { get; private set; }
            public IPackageRepository Repository { get; private set; }
        }
    }
}
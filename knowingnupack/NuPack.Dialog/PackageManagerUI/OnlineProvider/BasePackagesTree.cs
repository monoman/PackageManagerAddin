﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.ExtensionsExplorer;

namespace NuPack.Dialog.Providers {
    /// <summary>
    /// Base implementation of IVsExtensionsTreeNode
    /// </summary>
    public class BasePackagesTree : IVsExtensionsTreeNode {
        private IList<IVsExtension> extensions = new ObservableCollection<IVsExtension>();
        private IList<IVsExtensionsTreeNode> nodes = new ObservableCollection<IVsExtensionsTreeNode>();

        #region IVsExtensionsTreeNode Members

        public IList<IVsExtension> Extensions {
            get { return extensions; }
        }

        public bool IsSearchResultsNode {
            get { return false; }
        }

        public bool IsExpanded {
            get;
            set;
        }

        public bool IsSelected {
            get;
            set;
        }

        public string Name {
            get;
            set;
        }

        public IList<IVsExtensionsTreeNode> Nodes {
            get { return nodes; }
        }

        public IVsExtensionsTreeNode Parent {
            get;
            set;
        }

        #endregion

        public BasePackagesTree(IVsExtensionsTreeNode parent, string name) {
            this.Parent = parent;
            this.Name = name;
        }
    }
}

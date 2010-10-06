﻿using System;
using System.ComponentModel.Composition;

namespace NuPackConsole
{
    /// <summary>
    /// Specifies a MEF host name metadata to uniquely identify a host type. This is
    /// required for a host provider to be recognized by PowerConsole. PowerConsole
    /// also uses the HostName to find the associated ICommandTokenizerProvider and
    /// ICommandExpansionProvider for a host.
    /// 
    /// To avoid host name collision, a host can use its full class name (or
    /// a guid).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    [MetadataAttribute]
    public class HostNameAttribute : Attribute
    {
        /// <summary>
        /// The unique name for a host.
        /// </summary>
        public string HostName { get; private set; }

        /// <summary>
        /// Specifies a unique MEF host name metadata.
        /// </summary>
        public HostNameAttribute(string hostName)
        {
            UtilityMethods.ThrowIfArgumentNull(hostName);
            this.HostName = hostName;
        }
    }
}

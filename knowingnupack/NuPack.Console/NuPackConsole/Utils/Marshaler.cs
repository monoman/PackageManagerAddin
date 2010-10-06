﻿using System;
using Microsoft.VisualStudio.Shell;

namespace NuPackConsole
{
    class Marshaler<T>
    {
        protected readonly T _impl;

        protected Marshaler(T impl)
        {
            _impl = impl;
        }

        static ThreadHelper ThreadHelper
        {
            get { return ThreadHelper.Generic; }
        }

        /// <summary>
        /// Invoke an action on the main UI thread.
        /// </summary>
        protected void Invoke(Action action)
        {
            ThreadHelper.Invoke(action);
        }

        /// <summary>
        /// Invoke a function on the main UI thread.
        /// </summary>
        protected TResult Invoke<TResult>(Func<TResult> func)
        {
            return ThreadHelper.Invoke(func);
        }
    }
}

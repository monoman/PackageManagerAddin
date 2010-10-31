using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;

namespace NuGetConsole {
    static class ExtensionMethods {

        public static SnapshotPoint GetEnd(this ITextSnapshot snapshot) {
            return new SnapshotPoint(snapshot, snapshot.Length);
        }

        /// <summary>
        /// Removes a ReadOnlyRegion and clears the reference (set to null).
        /// </summary>
        public static void ClearReadOnlyRegion(this IReadOnlyRegionEdit readOnlyRegionEdit, ref IReadOnlyRegion readOnlyRegion) {
            if (readOnlyRegion != null) {
                readOnlyRegionEdit.RemoveReadOnlyRegion(readOnlyRegion);
                readOnlyRegion = null;
            }
        }

        public static void Raise<T>(this EventHandler<EventArgs<T>> ev, object sender, T arg) {
            if (ev != null) {
                ev(sender, new EventArgs<T>(arg));
            }
        }

        /// <summary>
        /// Execute a VS command on the wpfTextView CommandTarget.
        /// </summary>
        public static void Execute(this IOleCommandTarget target, Guid guidCommand, uint idCommand, object args = null) {
            IntPtr varIn = IntPtr.Zero;
            try {
                if (args != null) {
                    varIn = Marshal.AllocHGlobal(NuGetConsole.NativeMethods.VariantSize);
                    Marshal.GetNativeVariantForObject(args, varIn);
                }

                int hr = target.Exec(ref guidCommand, idCommand, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, varIn, IntPtr.Zero);
                ErrorHandler.ThrowOnFailure(hr);
            }
            finally {
                if (varIn != IntPtr.Zero) {
                    NuGetConsole.NativeMethods.VariantClear(varIn);
                    Marshal.FreeHGlobal(varIn);
                }
            }
        }

        /// <summary>
        /// Execute a default VSStd2K command.
        /// </summary>
        public static void Execute(this IOleCommandTarget target, VSConstants.VSStd2KCmdID idCommand, object args = null) {
            target.Execute(VSConstants.VSStd2K, (uint)idCommand, args);
        }
    }
}

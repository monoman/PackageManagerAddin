﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace NuPackConsole.Implementation.Console
{
    class WpfConsoleCompletionSource : ObjectWithFactory<WpfConsoleService>, ICompletionSource
    {
        ITextBuffer TextBuffer { get; set; }

        public WpfConsoleCompletionSource(WpfConsoleService factory, ITextBuffer textBuffer)
            : base(factory)
        {
            UtilityMethods.ThrowIfArgumentNull(textBuffer);
            this.TextBuffer = textBuffer;
        }

        WpfConsole _console;
        WpfConsole Console
        {
            get
            {
                if (_console == null)
                {
                    TextBuffer.Properties.TryGetProperty<WpfConsole>(typeof(IConsole), out _console);
                    Debug.Assert(_console != null);
                }

                return _console;
            }
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (Console == null || Console.InputLineStart == null)
            {
                return;
            }

            SimpleExpansion simpleExpansion;
            if (session.Properties.TryGetProperty<SimpleExpansion>("TabExpansion", out simpleExpansion))
            {
                List<Completion> completions = new List<Completion>();
                foreach (string s in simpleExpansion.Expansions)
                {
                    completions.Add(new Completion(s, s, null, null, null));
                }

                SnapshotPoint inputStart = Console.InputLineStart.Value;
                ITrackingSpan span = inputStart.Snapshot.CreateTrackingSpan(
                    new SnapshotSpan(inputStart + simpleExpansion.Start, simpleExpansion.Length),
                    SpanTrackingMode.EdgeInclusive);

                completionSets.Add(new CompletionSet(
                    Console.ContentTypeName, Console.ContentTypeName, span, completions, null));
            }
        }

        #region IDispose
        public void Dispose()
        {
            // Nothing to do.
        }
        #endregion
    }
}

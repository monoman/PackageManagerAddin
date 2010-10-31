using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace NuGetConsole.Implementation.Console {
    class WpfConsoleClassifier : ObjectWithFactory<WpfConsoleService>, IClassifier {
        ITextBuffer TextBuffer { get; set; }
        ComplexCommandSpans _commandLineSpans = new ComplexCommandSpans();
        OrderedTupleSpans<IClassificationType> _colorSpans = new OrderedTupleSpans<IClassificationType>();

        public WpfConsoleClassifier(WpfConsoleService factory, ITextBuffer textBuffer)
            : base(factory) {
            this.TextBuffer = textBuffer;
            TextBuffer.Changed += TextBuffer_Changed;
        }

        void TextBuffer_Changed(object sender, TextContentChangedEventArgs e) {
            // When input line changes, raise ClassificationChanged event
            if (HasConsole && Console.InputLineStart != null) {
                SnapshotSpan commandExtent = Console.InputLineExtent;
                if (e.Changes.Any((c) => c.OldPosition >= commandExtent.Span.Start)) {
                    if (_commandLineSpans.Count > 0) {
                        int i = _commandLineSpans.FindCommandStart(_commandLineSpans.Count - 1);
                        commandExtent = new SnapshotSpan(
                            new SnapshotPoint(commandExtent.Snapshot, _commandLineSpans[i].Item1.Start),
                            commandExtent.End);
                    }
                    this.ClassificationChanged.Raise(this, new ClassificationChangedEventArgs(commandExtent));
                }
            }
        }

        /// <summary>
        /// The CommandTokenizer for this console if available.
        /// </summary>
        ICommandTokenizer CommandTokenizer { get; set; }

        WpfConsole _console;
        WpfConsole Console {
            get {
                if (_console == null) {
                    TextBuffer.Properties.TryGetProperty<WpfConsole>(typeof(IConsole), out _console);
                    if (_console != null) {
                        // Only processing command lines when we have a CommandTokenizer
                        CommandTokenizer = Factory.GetCommandTokenizer(_console);
                        if (CommandTokenizer != null) {
                            _console.Dispatcher.ExecuteInputLine += Console_ExecuteInputLine;
                        }

                        _console.NewColorSpan += Console_NewColorSpan;
                        _console.ConsoleCleared += Console_ConsoleCleared;
                    }
                }

                return _console;
            }
        }

        void Console_ExecuteInputLine(object sender, EventArgs<Tuple<SnapshotSpan, bool>> e) {
            // Don't add empty spans (e.g. executed "cls")
            SnapshotSpan snapshotSpan = e.Arg.Item1.TranslateTo(Console.WpfTextView.TextSnapshot, SpanTrackingMode.EdgePositive);
            if (!snapshotSpan.IsEmpty) {
                _commandLineSpans.Add(snapshotSpan, e.Arg.Item2);
            }
        }

        void Console_NewColorSpan(object sender, EventArgs<Tuple<SnapshotSpan, Color?, Color?>> e) {
            // At least one of foreground or background must be specified, otherwise we don't care.
            if (e.Arg.Item2 != null || e.Arg.Item3 != null) {
                _colorSpans.Add(Tuple.Create(
                    e.Arg.Item1.Span,
                    TextFormatClassifier.GetClassificationType(e.Arg.Item2, e.Arg.Item3)));

                ClassificationChanged.Raise(this, new ClassificationChangedEventArgs(e.Arg.Item1));
            }
        }

        void Console_ConsoleCleared(object sender, EventArgs e) {
            ClearCachedCommandLineClassifications();
            _commandLineSpans.Clear();
            _colorSpans.Clear();
        }

        ITextFormatClassifier _textFormatClassifier;
        ITextFormatClassifier TextFormatClassifier {
            get {
                if (_textFormatClassifier == null) {
                    _textFormatClassifier = Factory.TextFormatClassifierProvider.GetTextFormatClassifier(
                        Console.WpfTextView);
                }
                return _textFormatClassifier;
            }
        }

        bool HasConsole {
            get { return Console != null; }
        }

        #region IClassifier

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span) {
            List<ClassificationSpan> classificationSpans = new List<ClassificationSpan>();
            if (HasConsole) {
                ITextSnapshot snapshot = span.Snapshot;

                // Check command line spans
                if (CommandTokenizer != null) {
                    bool hasInputLine = Console.InputLineStart != null;
                    if (hasInputLine) {
                        // Add current input line temporarily
                        _commandLineSpans.Add(Console.InputLineExtent, false);
                    }
                    try {
                        foreach (var cmdSpans in _commandLineSpans.Overlap(span)) {
                            if (cmdSpans.Count > 0) {
                                classificationSpans.AddRange(GetCommandLineClassifications(snapshot, cmdSpans));
                            }
                        }
                    }
                    finally {
                        if (hasInputLine) {
                            // Remove added current input line
                            _commandLineSpans.PopLast();
                        }
                    }
                }

                // Check color spans
                foreach (var t in _colorSpans.Overlap(span)) {
                    classificationSpans.Add(new ClassificationSpan(
                        new SnapshotSpan(snapshot, t.Item1), t.Item2));
                }
            }
            return classificationSpans;
        }

        /// <summary>
        /// Get classifications for one complex command.
        /// </summary>
        /// <param name="snapshot">The snapshot for the command spans.</param>
        /// <param name="cmdSpans">The command spans.</param>
        /// <returns>List of classifications for the given command spans.</returns>
        /// <remarks>
        /// The editor queries for classifications line by line. For a n-line complex command, this will be
        /// called n times for the same command. This implementation caches one parsed results for the last
        /// command.
        /// </remarks>
        IList<ClassificationSpan> GetCommandLineClassifications(ITextSnapshot snapshot, IList<Span> cmdSpans) {
            IList<ClassificationSpan> cachedCommandLineClassifications;
            if (TryGetCachedCommandLineClassifications(snapshot, cmdSpans, out cachedCommandLineClassifications)) {
                return cachedCommandLineClassifications;
            }
            else {
                List<ClassificationSpan> spans = new List<ClassificationSpan>();
                spans.AddRange(GetTokenizerClassifications(snapshot, cmdSpans));
                SaveCachedCommandLineClassifications(snapshot, cmdSpans, spans);
                return spans;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public IList<ClassificationSpan> GetTokenizerClassifications(ITextSnapshot snapshot, IList<Span> spans) {
            List<ClassificationSpan> classificationSpans = new List<ClassificationSpan>();

            string[] lines = spans.Select((span) => snapshot.GetText(span)).ToArray();
            try {
                IEnumerable<Token> tokens = CommandTokenizer.Tokenize(lines);
                foreach (Token token in tokens) {
                    IClassificationType classificationType = Factory.GetTokenTypeClassification(token.Type);
                    for (int i = token.StartLine; i <= token.EndLine; i++) {
                        // Tokenize() may append \r\n, resulting in more lines than spans lines
                        if (i - 1 < spans.Count) {
                            Span span = spans[i - 1];
                            int start = (i == token.StartLine) ? span.Start + (token.StartColumn - 1) : span.Start;
                            int end = (i == token.EndLine) ? span.Start + (token.EndColumn - 1) : span.End;

                            classificationSpans.Add(new ClassificationSpan(
                                new SnapshotSpan(snapshot, start, end - start), classificationType));
                        }
                    }
                }
            }
            catch (Exception x) {
                // Don't care about parser run-time exceptions
                Debug.Print(x.ToString());
            }

            return classificationSpans;
        }

        #region CommandLineClassifications cache
        ITextSnapshot _cacheSnapshot;
        int _cacheCommandStartPosition;
        WeakReference _cacheClassifications;

        /// <summary>
        /// Clear cached command line classifications.
        /// </summary>
        void ClearCachedCommandLineClassifications() {
            _cacheSnapshot = null;
            _cacheClassifications = null;
        }

        /// <summary>
        /// Save command line classifications to cache.
        /// </summary>
        /// <param name="snapshot">The snapshot for the command.</param>
        /// <param name="cmdSpans">The command spans.</param>
        /// <param name="spans">Classification results of the command.</param>
        void SaveCachedCommandLineClassifications(ITextSnapshot snapshot, IList<Span> cmdSpans, IList<ClassificationSpan> spans) {
            _cacheSnapshot = snapshot;
            _cacheCommandStartPosition = cmdSpans[0].Start;
            _cacheClassifications = new WeakReference(spans);
        }

        /// <summary>
        /// Try get classifications from cache if a command classifications are cached.
        /// </summary>
        /// <param name="snapshot">The snapshot for the command.</param>
        /// <param name="cmdSpans">The command spans.</param>
        /// <param name="cachedCommandLineClassifications">The cached classifications if found.</param>
        /// <returns>If cached results are found.</returns>
        bool TryGetCachedCommandLineClassifications(ITextSnapshot snapshot, IList<Span> cmdSpans, out IList<ClassificationSpan> cachedCommandLineClassifications) {
            // The cached command is identified by text snapshot and command start position.
            if (_cacheSnapshot == snapshot && _cacheCommandStartPosition == cmdSpans[0].Start) {
                IList<ClassificationSpan> spans = _cacheClassifications.Target as IList<ClassificationSpan>;
                if (spans != null) {
                    cachedCommandLineClassifications = spans;
                    return true;
                }
                else {
                    ClearCachedCommandLineClassifications(); // weak reference is gone
                }
            }

            cachedCommandLineClassifications = null;
            return false;
        }
        #endregion

        #endregion
    }
}

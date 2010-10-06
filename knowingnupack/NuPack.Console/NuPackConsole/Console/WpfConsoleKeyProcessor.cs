﻿using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio;

namespace NuPackConsole.Implementation.Console
{
    class WpfConsoleKeyProcessor : OleCommandFilter
    {
        WpfConsole WpfConsole { get; set; }
        IWpfTextView WpfTextView { get; set; }

        ICommandExpansion CommandExpansion { get; set; }

        public WpfConsoleKeyProcessor(WpfConsole wpfConsole)
            : base(wpfConsole.VsTextView)
        {
            this.WpfConsole = wpfConsole;
            this.WpfTextView = wpfConsole.WpfTextView;
            this.CommandExpansion = wpfConsole.Factory.GetCommandExpansion(wpfConsole);
        }

        /// <summary>
        /// Check if Caret is in read only region. This is true if the console is currently not
        /// in input mode, or the caret is before current prompt.
        /// </summary>
        bool IsCaretInReadOnlyRegion
        {
            get
            {
                return WpfConsole.InputLineStart == null // shortcut -- no inut allowed
                    || WpfTextView.TextBuffer.IsReadOnly(WpfTextView.Caret.Position.BufferPosition.Position);
            }
        }

        /// <summary>
        /// Check if Caret is on InputLine, including before or after Prompt.
        /// </summary>
        bool IsCaretOnInputLine
        {
            get
            {
                SnapshotPoint? inputStart = WpfConsole.InputLineStart;
                if (inputStart != null)
                {
                    SnapshotSpan inputExtent = inputStart.Value.GetContainingLine().ExtentIncludingLineBreak;
                    SnapshotPoint caretPos = CaretPosition;
                    return inputExtent.Contains(caretPos) || inputExtent.End == caretPos;
                }

                return false;
            }
        }

        /// <summary>
        /// Check if Caret is exactly on InputLineStart. Do nothing when HOME/Left keys are pressed here.
        /// When caret is right to this position, HOME/Left moves caret to this position.
        /// </summary>
        bool IsCaretAtInputLineStart
        {
            get
            {
                return WpfConsole.InputLineStart == WpfTextView.Caret.Position.BufferPosition;
            }
        }

        SnapshotPoint CaretPosition
        {
            get { return WpfTextView.Caret.Position.BufferPosition; }
        }

        bool IsSelectionReadonly
        {
            get
            {
                if (!WpfTextView.Selection.IsEmpty)
                {
                    ITextBuffer buffer = WpfTextView.TextBuffer;
                    return WpfTextView.Selection.SelectedSpans.Any(span => buffer.IsReadOnly(span));
                }
                return false;
            }
        }

        /// <summary>
        /// Manually execute a command on the OldChain (so this filter won't participate in the command filtering).
        /// </summary>
        void ExecuteCommand(VSConstants.VSStd2KCmdID idCommand, object args = null)
        {
            OldChain.Execute(idCommand, args);
        }

        protected override int InternalExec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            int hr = OLECMDERR_E_NOTSUPPORTED;

            if (pguidCmdGroup == VSConstants.GUID_VSStandardCommandSet97)
            {
                Debug.Print("Exec: GUID_VSStandardCommandSet97: {0}", (VSConstants.VSStd97CmdID)nCmdID);

                switch ((VSConstants.VSStd97CmdID)nCmdID)
                {
                    case VSConstants.VSStd97CmdID.Paste:
                        if (IsCaretInReadOnlyRegion || IsSelectionReadonly)
                        {
                            hr = VSConstants.S_OK; // eat it
                        }
                        else
                        {
                            PasteText(ref hr);
                        }
                        break;
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                Debug.Print("Exec: VSStd2K: {0}", (VSConstants.VSStd2KCmdID)nCmdID);

                switch ((VSConstants.VSStd2KCmdID)nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                        if (IsCompletionSessionActive)
                        {
                            char ch = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
                            if (IsCommitChar(ch))
                            {
                                if (_completionSession.SelectedCompletionSet.SelectionStatus.IsSelected)
                                {
                                    _completionSession.Commit();
                                }
                                else
                                {
                                    _completionSession.Dismiss();
                                }
                            }
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.LEFT:
                    case VSConstants.VSStd2KCmdID.LEFT_EXT:
                    case VSConstants.VSStd2KCmdID.LEFT_EXT_COL:
                    case VSConstants.VSStd2KCmdID.WORDPREV:
                    case VSConstants.VSStd2KCmdID.WORDPREV_EXT:
                    case VSConstants.VSStd2KCmdID.WORDPREV_EXT_COL:
                        if (IsCaretAtInputLineStart)
                        {
                            //
                            // Note: This simple implementation depends on Prompt containing a trailing space.
                            // When caret is on the right of InputLineStart, editor will handle it correctly,
                            // and caret won't move left to InputLineStart because of the trailing space.
                            //
                            hr = VSConstants.S_OK; // eat it
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.BOL:
                    case VSConstants.VSStd2KCmdID.BOL_EXT:
                    case VSConstants.VSStd2KCmdID.BOL_EXT_COL:
                        if (IsCaretOnInputLine)
                        {
                            VirtualSnapshotPoint oldCaretPoint = WpfTextView.Caret.Position.VirtualBufferPosition;

                            WpfTextView.Caret.MoveTo(WpfConsole.InputLineStart.Value);
                            WpfTextView.Caret.EnsureVisible();

                            if ((VSConstants.VSStd2KCmdID)nCmdID == VSConstants.VSStd2KCmdID.BOL)
                            {
                                WpfTextView.Selection.Clear();
                            }
                            else if ((VSConstants.VSStd2KCmdID)nCmdID != VSConstants.VSStd2KCmdID.BOL) // extend selection
                            {
                                VirtualSnapshotPoint anchorPoint = WpfTextView.Selection.IsEmpty ?
                                    oldCaretPoint.TranslateTo(WpfTextView.TextSnapshot) : WpfTextView.Selection.AnchorPoint;
                                WpfTextView.Selection.Select(anchorPoint, WpfTextView.Caret.Position.VirtualBufferPosition);
                            }

                            hr = VSConstants.S_OK;
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.UP:
                        if (!IsCompletionSessionActive)
                        {
                            if (!IsCaretInReadOnlyRegion)
                            {
                                WpfConsole.NavigateHistory(-1);
                                hr = VSConstants.S_OK;
                            }
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.DOWN:
                        if (!IsCompletionSessionActive)
                        {
                            if (!IsCaretInReadOnlyRegion)
                            {
                                WpfConsole.NavigateHistory(+1);
                                hr = VSConstants.S_OK;
                            }
                        }
                        break;

                    case VSConstants.VSStd2KCmdID.RETURN:
                        if (IsCompletionSessionActive)
                        {
                            if (_completionSession.SelectedCompletionSet.SelectionStatus.IsSelected)
                            {
                                _completionSession.Commit();
                            }
                            else
                            {
                                _completionSession.Dismiss();
                            }
                        }
                        else if (IsCaretOnInputLine || !IsCaretInReadOnlyRegion)
                        {
                            ExecuteCommand(VSConstants.VSStd2KCmdID.END);
                            ExecuteCommand(VSConstants.VSStd2KCmdID.RETURN);

                            WpfConsole.EndInputLine();
                        }
                        hr = VSConstants.S_OK;
                        break;

                    case VSConstants.VSStd2KCmdID.TAB:
                        if (!IsCaretInReadOnlyRegion)
                        {
                            if (IsCompletionSessionActive)
                            {
                                _completionSession.Commit();
                            }
                            else
                            {
                                TriggerCompletion();
                            }
                        }
                        hr = VSConstants.S_OK;
                        break;

                    case VSConstants.VSStd2KCmdID.CANCEL:
                        if (IsCompletionSessionActive)
                        {
                            _completionSession.Dismiss();
                            hr = VSConstants.S_OK;
                        }
                        else if (!IsCaretInReadOnlyRegion)
                        {
                            // Delete all text after InputLineStart
                            WpfTextView.TextBuffer.Delete(WpfConsole.AllInputExtent);
                            hr = VSConstants.S_OK;
                        }
                        break;
                }
            }

            return hr;
        }

        static readonly char[] NEWLINE_CHARS = new char[] { '\n', '\r' };

        void PasteText(ref int hr)
        {
            string text = System.Windows.Clipboard.GetText();
            int iLineStart = 0;
            int iNewLine = -1;
            char c;
            if (!string.IsNullOrEmpty(text) && (iNewLine = text.IndexOfAny(NEWLINE_CHARS)) >= 0)
            {
                ITextBuffer textBuffer = WpfTextView.TextBuffer;
                while (iLineStart < text.Length)
                {
                    string pasteLine = (iNewLine >= 0 ?
                        text.Substring(iLineStart, iNewLine - iLineStart) : text.Substring(iLineStart));

                    if (iLineStart == 0)
                    {
                        if (!WpfTextView.Selection.IsEmpty)
                        {
                            textBuffer.Replace(WpfTextView.Selection.SelectedSpans[0], pasteLine);
                        }
                        else
                        {
                            textBuffer.Insert(WpfTextView.Caret.Position.BufferPosition.Position, pasteLine);
                        }

                        this.Execute(VSConstants.VSStd2KCmdID.RETURN);
                    }
                    else
                    {
                        WpfConsole.Dispatcher.PostInputLine(
                            new InputLine(pasteLine, iNewLine >= 0));
                    }

                    if (iNewLine < 0)
                    {
                        break;
                    }

                    iLineStart = iNewLine + 1;
                    if (iLineStart < text.Length
                        && (c = text[iLineStart]) != text[iNewLine]
                        && (c == '\n' || c == '\r'))
                    {
                        iLineStart++;
                    }
                    iNewLine = (iLineStart < text.Length ? text.IndexOfAny(NEWLINE_CHARS, iLineStart) : -1);
                }

                hr = VSConstants.S_OK; // completed, eat it
            }
        }

        #region completion

        static bool IsCommitChar(char c)
        {
            // TODO: CommandExpansion determines this
            return (char.IsPunctuation(c) && c != '-' && c != '_') || char.IsWhiteSpace(c);
        }

        ICompletionBroker CompletionBroker
        {
            get { return WpfConsole.Factory.CompletionBroker; }
        }

        ICompletionSession _completionSession;

        bool IsCompletionSessionActive
        {
            get
            {
                return _completionSession != null && !_completionSession.IsDismissed;
            }
        }

        void TriggerCompletion()
        {
            if (CommandExpansion == null)
            {
                return; // Host CommandExpansion service not available
            }

            if (IsCompletionSessionActive)
            {
                _completionSession.Dismiss();
                _completionSession = null;
            }

            string line = WpfConsole.InputLineText;
            int caretIndex = CaretPosition - WpfConsole.InputLineStart.Value;
            Debug.Assert(caretIndex >= 0);

            SimpleExpansion simpleExpansion = null;
            try
            {
                simpleExpansion = CommandExpansion.GetExpansions(line, caretIndex);
            }
            catch (Exception x)
            {
                // Ignore exception from expansion
                Debug.Print(x.ToString());
            }

            if (simpleExpansion != null && simpleExpansion.Expansions != null)
            {
                string[] expansions = simpleExpansion.Expansions;
                if (expansions.Length == 1) // Shortcut for 1 TabExpansion candidate
                {
                    ReplaceTabExpansion(simpleExpansion.Start, simpleExpansion.Length, expansions[0]);
                }
                else if (expansions.Length > 1) // Only start intellisense session for multiple expansion candidates
                {
                    _completionSession = CompletionBroker.CreateCompletionSession(
                        WpfTextView,
                        WpfTextView.TextSnapshot.CreateTrackingPoint(CaretPosition.Position, PointTrackingMode.Positive),
                        true);
                    _completionSession.Properties.AddProperty("TabExpansion", simpleExpansion);
                    _completionSession.Dismissed += CompletionSession_Dismissed;
                    _completionSession.Start();
                }
            }
        }

        void ReplaceTabExpansion(int lastWordIndex, int length, string expansion)
        {
            if (!string.IsNullOrEmpty(expansion))
            {
                SnapshotSpan extent = WpfConsole.GetInputLineExtent(lastWordIndex, length);
                WpfTextView.TextBuffer.Replace(extent, expansion);
            }
        }

        void CompletionSession_Dismissed(object sender, EventArgs e)
        {
            Debug.Assert(this._completionSession == sender);
            this._completionSession.Dismissed -= CompletionSession_Dismissed;
            this._completionSession = null;
        }

        #endregion
    }
}

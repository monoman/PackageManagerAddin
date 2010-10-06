﻿using System;
using System.Linq;

namespace NuPackConsole.Host
{
    /// <summary>
    /// Common ITabExpansion based command expansion provider implementation. This
    /// provider creates an ITabExpansion based CommandExpansion if a given host
    /// implements ITabExpansion.
    /// </summary>
    class CommandExpansionProvider : ICommandExpansionProvider
    {
        public ICommandExpansion Create(IHost host)
        {
            ITabExpansion tabExpansion = host as ITabExpansion;
            return tabExpansion != null ? CreateTabExpansion(tabExpansion) : null;
        }

        /// <summary>
        /// Create a ITabExpansion based command expansion instance. This base implementation
        /// creates a CommandExpansion instance.
        /// </summary>
        protected virtual ICommandExpansion CreateTabExpansion(ITabExpansion tabExpansion)
        {
            return new CommandExpansion(tabExpansion);
        }
    }
    
    /// <summary>
    /// Common ITabExpansion based command expansion implementation.
    /// </summary>
    class CommandExpansion : ICommandExpansion
    {
        protected ITabExpansion TabExpansion { get; private set; }

        public CommandExpansion(ITabExpansion tabExpansion)
        {
            UtilityMethods.ThrowIfArgumentNull(tabExpansion);
            this.TabExpansion = tabExpansion;
        }

        #region ICommandExpansion
        public SimpleExpansion GetExpansions(string line, int caretIndex)
        {
            // Find end of lastword -- To allow expansion in middle line
            int lastWordEnd = caretIndex;
            while (lastWordEnd < line.Length)
            {
                //
                // Try to expand to the right as far as possible, but do not include unexpected
                // extra text. Doing so will result in less accurate lastWord and make TabExpansion
                // fail to return any results.
                //
                char c = line[lastWordEnd];
                if (char.IsSeparator(c) || char.IsPunctuation(c))
                {
                    break;
                }

                lastWordEnd++;
            }

            // Find begin of lastword
            int lastWordBegin = caretIndex - 1;
            while (lastWordBegin >= 0 && !char.IsSeparator(line, lastWordBegin))
            {
                lastWordBegin--;
            }
            lastWordBegin++;

            // Adjust line and lastword
            if (lastWordEnd != line.Length)
            {
                line = line.Substring(0, lastWordEnd);
            }
            string lastWord = line.Substring(lastWordBegin);

            // Get host TabExpansion result
            string[] expansions = TabExpansion.GetExpansions(line, lastWord);

            if (expansions != null && expansions.Length > 0)
            {
                // Adjust expansions so that common words like "$dte.Commands." don't appear in intellisense
                string leftWord = line.Substring(lastWordBegin, caretIndex - lastWordBegin);
                string commonWord = AdjustExpansions(leftWord, ref expansions);
                int commonWordLength = !string.IsNullOrEmpty(commonWord) ? commonWord.Length : 0;

                return new SimpleExpansion(
                    lastWordBegin + commonWordLength,
                    lastWord.Length - commonWordLength,
                    expansions);
            }
            else if (TabExpansion is IPathExpansion)
            {
                return ((IPathExpansion)TabExpansion).GetPathExpansions(line);
            }

            return null;
        }

        static readonly char[] EXPANSION_SEPARATORS = new char[] { '.', ' ' };

        /// <summary>
        /// Adjust host TabExpansion results to hide some common parts, e.g. "$dte.Commands.", so
        /// that the intellisense pop up looks more Visual Studio friendly.
        /// </summary>
        /// <param name="leftWord">The text in the last word left to caret.</param>
        /// <param name="expansions">TabExpansion results.</param>
        /// <returns>The common start word shared by leftWord and expansions that could be hide.</returns>
        string AdjustExpansions(string leftWord, ref string[] expansions)
        {
            string commonWord = null;

            if (!string.IsNullOrEmpty(leftWord) && expansions != null)
            {
                int lastSepIndex = leftWord.Length - 1;
                while (true)
                {
                    // Check the longest possible common starting word
                    lastSepIndex = leftWord.LastIndexOfAny(EXPANSION_SEPARATORS, lastSepIndex);
                    if (lastSepIndex < 0)
                    {
                        commonWord = null;
                        break;
                    }

                    commonWord = leftWord.Substring(0, lastSepIndex + 1);
                    if (expansions.All(s => s.StartsWith(commonWord, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        break; // Found
                    }
                }
            }

            if (!string.IsNullOrEmpty(commonWord))
            {
                for (int i = 0; i < expansions.Length; i++)
                {
                    expansions[i] = expansions[i].Substring(commonWord.Length);
                }
            }

            return commonWord;
        }
        #endregion
    }
}

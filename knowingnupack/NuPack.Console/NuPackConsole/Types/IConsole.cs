﻿using System.Windows.Media;

namespace NuPackConsole
{
    /// <summary>
    /// Represents a console (editor) which a user interacts with.
    /// </summary>
    public interface IConsole
    {
        /// <summary>
        /// The host associated with this console. Each console is associated with 1 host
        /// to perform command interpretation. The console creator is responsible to
        /// setup this association.
        /// </summary>
        IHost Host { get; set; }

        /// <summary>
        /// Get the console dispatcher which dispatches user interaction.
        /// </summary>
        IConsoleDispatcher Dispatcher { get; }

        /// <summary>
        /// Get the console width measured by chars.
        /// </summary>
        int ConsoleWidth { get; }

        /// <summary>
        /// Write text to the console.
        /// </summary>
        /// <param name="text">The text content.</param>
        void Write(string text);

        /// <summary>
        /// Write a line of text to the console. This appends a newline to text content.
        /// </summary>
        /// <param name="text">The text content.</param>
        void WriteLine(string text);

        /// <summary>
        /// Write text to the console with color.
        /// </summary>
        /// <param name="text">The text content.</param>
        /// <param name="foreground">Optional foreground color.</param>
        /// <param name="background">Optional background color.</param>
        void Write(string text, Color? foreground, Color? background);

        /// <summary>
        /// Clear the console content.
        /// 
        /// Note that this can only be called in a user command execution. If you need to
        /// clear console from outside user command execution, use Dispatcher.ClearConsole.
        /// </summary>
        void Clear();
    }
}

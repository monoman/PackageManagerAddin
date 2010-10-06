﻿using System;
using System.Collections.Generic;

namespace NuPackConsole
{
    /// <summary>
    /// Represents a command host that executes user input commands (synchronously).
    /// </summary>
    public interface IHost
    {
        /// <summary>
        /// Get the current command prompt used by this host.
        /// </summary>
        string Prompt { get; }

        /// <summary>
        /// Execute a command on this host.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>true if the command is executed. In the case of async host, this indicates
        /// that the command is being executed and ExecuteEnd event would signal the end of
        /// execution.</returns>
        bool Execute(string command);

        /// <summary>
        /// Abort the current execution if this host is executing a command, or discard currently
        /// constructing multiple-line command if any.
        /// </summary>
        void Abort();

        string Setting { get; set; }

        string[] AvailableSettings { get; }

        string DefaultProject { get; set; }

        string[] AvailableProjects { get; }
    }

    /// <summary>
    /// Represents a command host that executes commands asynchronously. The console depends on
    /// ExecuteEnd event to detect end of command execution.
    /// </summary>
    public interface IAsyncHost : IHost
    {
        /// <summary>
        /// Occurs when an async command execution is completed, disregarding if it succeeded, failed or
        /// aborted. The console depends on this event to prompt for next user input.
        /// </summary>
        event EventHandler ExecuteEnd;
    }
}

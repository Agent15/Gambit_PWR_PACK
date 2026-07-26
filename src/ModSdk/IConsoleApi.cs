using System;
using System.Collections.Generic;

namespace Gambonanza.ModSdk
{
    /// <summary>
    /// Severity / colour band for a console line. Maps to the renderer's palette.
    /// </summary>
    public enum ConsoleLineColor
    {
        Default = 0,
        Info    = 1,
        Warn    = 2,
        Error   = 3,
        Echo    = 4, // user input echoed back
    }

    /// <summary>
    /// Provides candidate completions for a single argument of a command.
    /// Called by the console UI when the user presses Tab on that argument.
    ///
    ///   args        - args parsed so far (everything after the command name).
    ///                 Length is &gt;= argIndex + 1; the entry at argIndex is the
    ///                 partial token currently being completed (may be empty).
    ///   argIndex    - which argument the cursor is on (0-based).
    ///
    /// Return ALL candidates (don't filter by prefix); the console filters and
    /// ranks. Return null or empty when there is nothing to suggest.
    /// </summary>
    public delegate IEnumerable<string> ConsoleArgumentCompleter(string[] args, int argIndex);

    /// <summary>
    /// In-game developer console. Print to it, register commands against it.
    ///
    /// Lifetime: the console is created during ModHost.LoadAll(), BEFORE any mod's
    /// OnLoad runs, so it is always valid by the time you receive it via
    /// <see cref="IModContext.Console"/>.
    ///
    /// Toggle: F1 or backtick (`) by default, overridable via env vars
    /// GAMBONANZA_CONSOLE_KEY / GAMBONANZA_CONSOLE_KEY2.
    ///
    /// Threading: all methods are main-thread only. Calling Print from a
    /// non-main thread (e.g. logMessageReceived hook) is not supported.
    /// </summary>
    public interface IConsoleApi
    {
        // ----- output ------------------------------------------------------

        /// <summary>Append a line at <paramref name="color"/>.</summary>
        void Print(string message, ConsoleLineColor color = ConsoleLineColor.Default);
        void PrintInfo(string message);
        void PrintWarn(string message);
        void PrintError(string message);

        // ----- commands ----------------------------------------------------

        /// <summary>
        /// Register a command. Multi-word names ("gambit give") are supported and
        /// matched longest-first when parsing input. <paramref name="handler"/>
        /// receives the args AFTER the command name (e.g. for input "gambit give
        /// foo bar" against command "gambit give", handler gets ["foo", "bar"]).
        ///
        /// <paramref name="completer"/> is called when the user presses Tab on an
        /// argument; pass null if your command takes no args (or you're fine
        /// without per-arg completion).
        ///
        /// <paramref name="help"/> shows up in the `help` listing - keep it short.
        ///
        /// Re-registering an existing name overwrites the previous handler. Use
        /// <see cref="UnregisterCommand"/> when your mod is disabled.
        /// </summary>
        void RegisterCommand(
            string name,
            string help,
            Action<string[]> handler,
            ConsoleArgumentCompleter completer = null);

        /// <summary>Remove a previously-registered command. No-op if unknown.</summary>
        void UnregisterCommand(string name);

        // ----- visibility --------------------------------------------------

        bool IsOpen { get; }
        void Open();
        void Close();
        void Toggle();
    }
}

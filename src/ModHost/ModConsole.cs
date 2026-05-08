using System;
using System.Collections.Generic;
using System.Linq;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.ModHost
{
    /// <summary>
    /// Console state + command registry + parser. Pure logic — no Unity UI here.
    /// The UI ({<see cref="ModConsoleUI"/>}) reads/writes this and signals visibility.
    ///
    /// Created exactly once during ModHost.LoadAll, before any mod's OnLoad. The
    /// singleton is exposed to mods through <see cref="IModContext.Console"/>;
    /// ModHost itself uses it directly via <see cref="Instance"/> for built-in
    /// commands and the "N mods loaded" boot summary.
    /// </summary>
    internal sealed class ModConsole : IConsoleApi
    {
        public static ModConsole Instance { get; private set; }

        public static ModConsole CreateOnce()
        {
            if (Instance == null) Instance = new ModConsole();
            return Instance;
        }

        // ----- buffer ------------------------------------------------------

        public sealed class Line
        {
            public string Text;
            public ConsoleLineColor Color;
        }

        private const int MaxLines = 500;
        private readonly LinkedList<Line> _lines = new LinkedList<Line>();
        public IEnumerable<Line> Lines => _lines;

        /// <summary>Bumped every time the buffer changes; UI re-renders on change.</summary>
        public int Revision { get; private set; }

        public void Print(string message, ConsoleLineColor color = ConsoleLineColor.Default)
        {
            // Split on newlines so each rendered row is a single line — keeps
            // wrapping predictable and the scrollback responsive.
            if (string.IsNullOrEmpty(message))
            {
                Append("", color);
                return;
            }
            foreach (var part in message.Split('\n'))
                Append(part.TrimEnd('\r'), color);
        }

        public void PrintInfo(string message)  => Print(message, ConsoleLineColor.Info);
        public void PrintWarn(string message)  => Print(message, ConsoleLineColor.Warn);
        public void PrintError(string message) => Print(message, ConsoleLineColor.Error);

        private void Append(string text, ConsoleLineColor color)
        {
            _lines.AddLast(new Line { Text = text ?? "", Color = color });
            while (_lines.Count > MaxLines) _lines.RemoveFirst();
            Revision++;
        }

        public void Clear()
        {
            _lines.Clear();
            Revision++;
        }

        // ----- command registry --------------------------------------------

        private sealed class Command
        {
            public string Name;            // canonical, lowercased, single-spaced
            public string[] NameTokens;    // pre-split tokens
            public string Help;
            public Action<string[]> Handler;
            public ConsoleArgumentCompleter Completer;
        }

        // Keyed by canonical name; Values stored sorted by descending token count
        // for greedy longest-match parsing (mods can register both "gambit" and
        // "gambit give" — the latter wins when input is "gambit give …").
        private readonly Dictionary<string, Command> _commands =
            new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<(string Name, string Help)> AllCommands()
            => _commands.Values
                        .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(c => (c.Name, c.Help));

        public void RegisterCommand(
            string name, string help, Action<string[]> handler,
            ConsoleArgumentCompleter completer = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name is required", nameof(name));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var canonical = string.Join(" ", Tokenize(name));
            if (canonical.Length == 0) throw new ArgumentException("name has no tokens", nameof(name));
            _commands[canonical] = new Command
            {
                Name       = canonical,
                NameTokens = canonical.Split(' '),
                Help       = help ?? "",
                Handler    = handler,
                Completer  = completer,
            };
        }

        public void UnregisterCommand(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var canonical = string.Join(" ", Tokenize(name));
            _commands.Remove(canonical);
        }

        // ----- visibility (UI sets these) ----------------------------------

        public bool IsOpen { get; internal set; }

        // The UI subscribes; ModConsole signals via these. Decoupled so the UI
        // can reload (e.g. for testing) without breaking handlers.
        internal event Action OnOpenRequested;
        internal event Action OnCloseRequested;

        public void Open()   { OnOpenRequested?.Invoke();  }
        public void Close()  { OnCloseRequested?.Invoke(); }
        public void Toggle() { if (IsOpen) Close(); else Open(); }

        // ----- input handling ----------------------------------------------

        /// <summary>
        /// Parse and run an input line. Echoes the input, dispatches the command,
        /// catches handler exceptions, prints "unknown command" if no match.
        /// </summary>
        public void Submit(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return;
            Print("> " + input, ConsoleLineColor.Echo);

            var tokens = Tokenize(input);
            if (tokens.Count == 0) return;

            // Greedy longest-match: try the full token list as a command, drop
            // the tail and try again, until we hit an empty prefix or find one.
            for (int take = Math.Min(tokens.Count, MaxCommandTokens); take >= 1; take--)
            {
                var name = string.Join(" ", tokens.Take(take));
                if (!_commands.TryGetValue(name, out var cmd)) continue;
                var args = tokens.Skip(take).ToArray();
                try { cmd.Handler(args); }
                catch (Exception ex) { PrintError($"command '{name}' threw: {ex.Message}"); }
                return;
            }
            PrintError($"unknown command '{tokens[0]}'. Type 'help'.");
        }

        // Cap on multi-word command name length — keeps the lookup loop O(k) on
        // input length rather than O(k²) on a million-token paste.
        private const int MaxCommandTokens = 4;

        /// <summary>
        /// Compute autocomplete candidates for the current cursor position.
        /// </summary>
        /// <param name="input">Full input text.</param>
        /// <param name="cursor">Cursor position (we complete the token under it; if cursor is past the end, we complete the trailing token / suggest a new one).</param>
        public IReadOnlyList<string> Complete(string input, int cursor)
        {
            input ??= "";
            cursor = Math.Clamp(cursor, 0, input.Length);

            // Tokenize while tracking which token (and which char in it) the
            // cursor sits in. We treat trailing whitespace as "starting a fresh
            // token" so "gambit " + Tab suggests gambit-give's first arg.
            var (tokens, currentTokenIdx, currentTokenPrefix) = TokenizeWithCursor(input, cursor);

            // No tokens yet → suggest top-level command first words.
            if (tokens.Count == 0 || currentTokenIdx == 0)
            {
                var prefix = currentTokenPrefix ?? "";
                var candidates = _commands.Keys
                    .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .Select(n => n.Split(' ')[0])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return candidates;
            }

            // The cursor is past the first token. Find every command whose token
            // path is a prefix of the input up to currentTokenIdx − 1, then ask
            // either (a) the command's completer for arg completions, or (b) the
            // registry for additional sibling subcommands.
            //
            // Strategy: find longest matching command prefix; if the matched
            // command has more tokens than we've consumed at currentTokenIdx,
            // suggest the next subword. Otherwise call the completer.
            string longestMatch = null;
            int longestMatchTokens = 0;
            foreach (var c in _commands.Values)
            {
                if (c.NameTokens.Length > currentTokenIdx + 1) continue;
                bool match = true;
                for (int i = 0; i < c.NameTokens.Length; i++)
                {
                    if (!string.Equals(c.NameTokens[i], tokens[i], StringComparison.OrdinalIgnoreCase))
                    { match = false; break; }
                }
                if (!match) continue;
                if (c.NameTokens.Length > longestMatchTokens)
                {
                    longestMatchTokens = c.NameTokens.Length;
                    longestMatch = c.Name;
                }
            }

            // Always offer subcommand siblings whose first N tokens match the
            // entered prefix and whose token at currentTokenIdx starts with the
            // current partial.
            var prefixTokens = tokens.Take(currentTokenIdx).ToList();
            var subwords = new List<string>();
            foreach (var c in _commands.Values)
            {
                if (c.NameTokens.Length <= currentTokenIdx) continue;
                bool match = true;
                for (int i = 0; i < currentTokenIdx; i++)
                {
                    if (!string.Equals(c.NameTokens[i], prefixTokens[i], StringComparison.OrdinalIgnoreCase))
                    { match = false; break; }
                }
                if (!match) continue;
                var word = c.NameTokens[currentTokenIdx];
                if (word.StartsWith(currentTokenPrefix ?? "", StringComparison.OrdinalIgnoreCase))
                    subwords.Add(word);
            }

            // If we resolved a full command match, ask its completer too.
            var completerSuggestions = new List<string>();
            if (longestMatch != null && _commands.TryGetValue(longestMatch, out var cmd) && cmd.Completer != null)
            {
                int argIndex = currentTokenIdx - longestMatchTokens;
                if (argIndex >= 0)
                {
                    // Build the args array as the completer expects: everything
                    // after the command name, with the current partial token in
                    // its slot.
                    var argsList = tokens.Skip(longestMatchTokens).ToList();
                    while (argsList.Count <= argIndex) argsList.Add("");
                    argsList[argIndex] = currentTokenPrefix ?? "";
                    var args = argsList.ToArray();
                    IEnumerable<string> raw = null;
                    try { raw = cmd.Completer(args, argIndex); }
                    catch (Exception ex) { PrintError($"completer for '{longestMatch}' threw: {ex.Message}"); }
                    if (raw != null)
                    {
                        var partial = currentTokenPrefix ?? "";
                        foreach (var s in raw)
                        {
                            if (string.IsNullOrEmpty(s)) continue;
                            if (s.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                                completerSuggestions.Add(s);
                        }
                    }
                }
            }

            return subwords
                .Concat(completerSuggestions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ----- tokenization ------------------------------------------------

        // Splits on runs of whitespace. No quoting / escapes in v1 — gambit IDs
        // and most cheats don't contain spaces.
        private static List<string> Tokenize(string s)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(s)) return result;
            int i = 0;
            while (i < s.Length)
            {
                while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
                int start = i;
                while (i < s.Length && !char.IsWhiteSpace(s[i])) i++;
                if (i > start) result.Add(s.Substring(start, i - start));
            }
            return result;
        }

        private static (List<string> tokens, int currentIdx, string currentPrefix)
            TokenizeWithCursor(string s, int cursor)
        {
            var tokens = new List<string>();
            int currentIdx = 0;
            string currentPrefix = "";
            int i = 0;
            int tokenStart = -1;
            int caretToken = -1;
            int caretCharInToken = 0;
            while (i <= s.Length)
            {
                bool atEnd = i == s.Length;
                bool ws = !atEnd && char.IsWhiteSpace(s[i]);
                if (!ws && tokenStart < 0) tokenStart = i;
                if (i == cursor)
                {
                    caretToken = tokenStart >= 0 ? tokens.Count : tokens.Count;
                    caretCharInToken = tokenStart >= 0 ? cursor - tokenStart : 0;
                }
                if (atEnd || ws)
                {
                    if (tokenStart >= 0)
                    {
                        tokens.Add(s.Substring(tokenStart, i - tokenStart));
                        tokenStart = -1;
                    }
                }
                if (atEnd) break;
                i++;
            }

            if (caretToken < 0) caretToken = tokens.Count;
            currentIdx = caretToken;

            // The caret-token computation above gives us the index of the token
            // the cursor is INSIDE OR AT THE END OF. Compute the partial as the
            // substring of that token before the cursor.
            if (caretToken < tokens.Count)
                currentPrefix = tokens[caretToken].Substring(0, Math.Min(caretCharInToken, tokens[caretToken].Length));
            else
                currentPrefix = "";
            return (tokens, currentIdx, currentPrefix);
        }

        /// <summary>
        /// Replace the token under the cursor with <paramref name="completion"/>
        /// and return (newText, newCursor). Used by the UI when accepting Tab.
        /// </summary>
        public static (string text, int cursor) ApplyCompletion(string input, int cursor, string completion)
        {
            input ??= "";
            cursor = Math.Clamp(cursor, 0, input.Length);

            // Find token boundaries for the token under the cursor (or the empty
            // slot after trailing whitespace).
            int start = cursor;
            while (start > 0 && !char.IsWhiteSpace(input[start - 1])) start--;
            int end = cursor;
            while (end < input.Length && !char.IsWhiteSpace(input[end])) end++;

            var newText = input.Substring(0, start) + completion + input.Substring(end);
            int newCursor = start + completion.Length;
            return (newText, newCursor);
        }
    }
}

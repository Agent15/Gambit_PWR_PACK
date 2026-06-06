using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.ModHost
{
    internal static class ModCheats
    {
        private static readonly string[] PieceNames = { "pawn", "rook", "knight", "bishop", "queen", "king" };

        public static void Register(IConsoleApi console)
        {
            console.RegisterCommand("give", "give money, stock pieces, or gambits: give money 50 | give piece queen 2 | give gambit thunder", args => Give(console, args), CompleteGive);
            console.RegisterCommand("set money", "set current money: set money 999", args => SetMoney(console, args));
            console.RegisterCommand("run", "show current run state", _ => PrintRunState(console));
            console.RegisterCommand("wave set", "set current wave index: wave set 10", args => SetWave(console, args));
            console.RegisterCommand("wave add", "add to current wave index: wave add 5", args => AddWave(console, args));
            console.RegisterCommand("list pieces", "list piece ids usable by give piece", _ => console.PrintInfo(string.Join(", ", PieceNames)));
            console.RegisterCommand("list gambits", "list gambit ids; optional filter: list gambits thunder", args => ListGambits(console, args), CompleteGambitNames);
        }

        private static void Give(IConsoleApi console, string[] args)
        {
            if (args == null || args.Length < 2)
            {
                console.PrintWarn("usage: give money <amount> | give piece <piece> [amount] | give gambit <name> [amount]");
                return;
            }

            switch (Norm(args[0]))
            {
                case "money":
                case "coin":
                case "coins":
                    if (!TryInt(args[1], out var amount)) { console.PrintWarn("money amount must be a number."); return; }
                    AddMoney(console, amount);
                    return;

                case "piece":
                    if (args.Length < 2) { console.PrintWarn("usage: give piece <pawn|rook|knight|bishop|queen|king> [amount]"); return; }
                    var pieceAmount = args.Length >= 3 && TryInt(args[2], out var pa) ? Math.Max(1, pa) : 1;
                    GivePiece(console, args[1], pieceAmount);
                    return;

                case "gambit":
                    var gambitAmount = args.Length >= 3 && TryInt(args[args.Length - 1], out var ga) ? Math.Max(1, ga) : 1;
                    var nameParts = args.Skip(1).Take(args.Length - 1 - (args.Length >= 3 && TryInt(args[args.Length - 1], out _) ? 1 : 0));
                    GiveGambit(console, string.Join(" ", nameParts.ToArray()), gambitAmount);
                    return;

                default:
                    console.PrintWarn("unknown give target. Use money, piece, or gambit.");
                    return;
            }
        }

        private static void AddMoney(IConsoleApi console, int amount)
        {
            var chess = Instance("Blukulele.CHE.ChessDataManager");
            if (chess == null) { console.PrintWarn("ChessDataManager not ready — start/load a run first."); return; }
            try
            {
                if (amount >= 0) Invoke(chess, "IncreaseCoin", amount);
                else Invoke(chess, "DecreaseCoin", -amount);
                TryInvoke(chess, "IncreaseTextCoin", true);
                console.PrintInfo($"money {(amount >= 0 ? "+" : "")}{amount}; now {GetProp(chess, "Coins")}");
            }
            catch (Exception ex) { console.PrintWarn("give money failed: " + Short(ex)); }
        }

        private static void SetMoney(IConsoleApi console, string[] args)
        {
            if (args == null || args.Length < 1 || !TryInt(args[0], out var target))
            {
                console.PrintWarn("usage: set money <amount>");
                return;
            }
            var chess = Instance("Blukulele.CHE.ChessDataManager");
            if (chess == null) { console.PrintWarn("ChessDataManager not ready — start/load a run first."); return; }
            var current = Convert.ToInt32(GetProp(chess, "Coins"));
            AddMoney(console, target - current);
        }

        private static void GivePiece(IConsoleApi console, string pieceName, int amount)
        {
            var stock = Instance("Blukulele.CHE.StockManager");
            if (stock == null) { console.PrintWarn("StockManager not ready — start/load a run first."); return; }
            var pieceEnum = ParsePiece(pieceName);
            if (pieceEnum == null) { console.PrintWarn("unknown piece. Use: " + string.Join(", ", PieceNames)); return; }

            try
            {
                // Use the full AddPiece(piece, sourcePosition, ...) overload, not
                // AddPiece(piece, bool). The short overload only fills the internal
                // StockManager.Pieces array; it does not set CurrentTile/Tile.Piece
                // or run the same placement follow-up, leaving ghost-ish pieces that
                // can be sold but not moved/selected correctly.
                var method = stock.GetType().GetMethods().FirstOrDefault(m =>
                {
                    if (m.Name != "AddPiece") return false;
                    var p = m.GetParameters();
                    return p.Length >= 2 && p[0].ParameterType.IsEnum && p[1].ParameterType == typeof(Vector3);
                });
                if (method == null) { console.PrintWarn("StockManager.AddPiece(piece, Vector3, ...) not found."); return; }

                int given = 0;
                for (int i = 0; i < amount; i++)
                {
                    var freePos = FindFreeStockSlotPosition(stock) ?? ((Component)stock).transform.position;
                    var p = method.GetParameters();
                    var call = new object[p.Length];
                    call[0] = pieceEnum;
                    call[1] = freePos;
                    for (int j = 2; j < call.Length; j++) call[j] = p[j].DefaultValue is DBNull ? false : p[j].DefaultValue;
                    method.Invoke(stock, call);
                    given++;
                }
                console.PrintInfo($"gave {given} {pieceName}(s) to stock.");
            }
            catch (Exception ex) { console.PrintWarn("give piece failed: " + Short(ex)); }
        }

        private static void GiveGambit(IConsoleApi console, string query, int amount)
        {
            var lib = Instance("Blukulele.CHE.GambitLibrary");
            var manager = Instance("Blukulele.CHE.GambitManager");
            if (lib == null || manager == null) { console.PrintWarn("Gambit systems not ready — start/load a run first."); return; }
            var gambit = FindGambit(query);
            if (gambit == null) { console.PrintWarn($"unknown gambit '{query}'. Try: list gambits {query}"); return; }

            try
            {
                var id = (string)Field(gambit, "ID").GetValue(gambit);
                var isFull = (bool)Invoke(manager, "IsFull");
                if (isFull) { console.PrintWarn("gambit bar is full."); return; }
                int given = 0;
                for (int i = 0; i < amount; i++)
                {
                    if ((bool)Invoke(manager, "IsFull")) break;
                    Invoke(lib, "SpawnGambit", id, ((Component)manager).transform);
                    given++;
                }
                console.PrintInfo($"gave {given} gambit(s): {id}");
            }
            catch (Exception ex) { console.PrintWarn("give gambit failed: " + Short(ex)); }
        }

        private static void PrintRunState(IConsoleApi console)
        {
            var gm = Instance("Blukulele.Core.GameManager");
            var chess = Instance("Blukulele.CHE.ChessDataManager");
            if (gm != null) console.PrintInfo($"state: {GetFieldOrProp(gm, "CurrentState")} (prev {GetFieldOrProp(gm, "PreviousState")})");
            if (chess != null) console.PrintInfo($"money: {GetProp(chess, "Coins")} | wave: {GetProp(chess, "CurrentWave")}/{GetProp(chess, "LastWave")}");
            var stock = Instance("Blukulele.CHE.StockManager");
            if (stock != null) console.PrintInfo($"stock: {Invoke(stock, "GetPieceInStockCount")}/{Invoke(stock, "GetMaxCount")}");
            var gambitMgr = Instance("Blukulele.CHE.GambitManager");
            if (gambitMgr != null) console.PrintInfo($"gambits full: {Invoke(gambitMgr, "IsFull")}");
        }

        private static void SetWave(IConsoleApi console, string[] args)
        {
            if (args == null || args.Length < 1 || !TryInt(args[0], out var wave)) { console.PrintWarn("usage: wave set <number>"); return; }
            var chess = Instance("Blukulele.CHE.ChessDataManager");
            if (chess == null) { console.PrintWarn("ChessDataManager not ready."); return; }
            SetProp(chess, "CurrentWave", Math.Max(0, wave));
            console.PrintInfo($"wave set to {GetProp(chess, "CurrentWave")}");
        }

        private static void AddWave(IConsoleApi console, string[] args)
        {
            if (args == null || args.Length < 1 || !TryInt(args[0], out var delta)) { console.PrintWarn("usage: wave add <number>"); return; }
            var chess = Instance("Blukulele.CHE.ChessDataManager");
            if (chess == null) { console.PrintWarn("ChessDataManager not ready."); return; }
            var current = Convert.ToInt32(GetProp(chess, "CurrentWave"));
            SetProp(chess, "CurrentWave", Math.Max(0, current + delta));
            console.PrintInfo($"wave is now {GetProp(chess, "CurrentWave")}");
        }

        private static void ListGambits(IConsoleApi console, string[] args)
        {
            var filter = args != null && args.Length > 0 ? string.Join(" ", args) : "";
            var matches = GambitIds(filter).Take(40).ToArray();
            console.PrintInfo(matches.Length == 0 ? "no gambits matched." : string.Join(", ", matches));
        }

        private static Vector3? FindFreeStockSlotPosition(object stock)
        {
            try
            {
                var places = GetProp(stock, "Places") as IEnumerable;
                if (places == null) return null;
                foreach (var place in places)
                {
                    if (place == null) continue;
                    var c = (Component)place;
                    var hasPiece = c.GetComponentInChildren(GameType("Blukulele.CHE.BasePieceBehaviour"), true) != null;
                    if (!hasPiece) return c.transform.position;
                }
            }
            catch { }
            return null;
        }

        private static IEnumerable<string> CompleteGive(string[] args, int argIndex)
        {
            if (args == null || args.Length == 0) return new[] { "money ", "piece ", "gambit " };
            var head = Norm(args[0]);
            if (args.Length == 1) return new[] { "money", "piece", "gambit" }.Where(x => x.StartsWith(head));
            if (head == "piece") return PieceNames.Where(p => p.StartsWith(Norm(args[1]))).Select(p => "piece " + p);
            if (head == "gambit") return GambitIds(string.Join(" ", args.Skip(1).ToArray())).Take(8).Select(g => "gambit " + g);
            return Enumerable.Empty<string>();
        }

        private static IEnumerable<string> CompleteGambitNames(string[] args, int argIndex) => GambitIds(args == null ? "" : string.Join(" ", args)).Take(8);

        private static IEnumerable<string> GambitIds(string filter)
        {
            var lib = Instance("Blukulele.CHE.GambitLibrary");
            if (lib == null) return Enumerable.Empty<string>();
            var field = Field(lib, "GambitsInfo");
            if (field == null || !(field.GetValue(lib) is IEnumerable list)) return Enumerable.Empty<string>();
            var nf = Norm(filter);
            return list.Cast<object>()
                .Select(g => (string)Field(g, "ID")?.GetValue(g))
                .Where(id => !string.IsNullOrEmpty(id))
                .Where(id => string.IsNullOrEmpty(nf) || Norm(id).Contains(nf))
                .OrderBy(id => id);
        }

        private static object FindGambit(string query)
        {
            var lib = Instance("Blukulele.CHE.GambitLibrary");
            var field = Field(lib, "GambitsInfo");
            if (field == null || !(field.GetValue(lib) is IEnumerable list)) return null;
            var q = Norm(query);
            if (string.IsNullOrEmpty(q)) return null;
            var gambits = list.Cast<object>().ToArray();
            return gambits.FirstOrDefault(g => Norm((string)Field(g, "ID")?.GetValue(g)) == q)
                ?? gambits.FirstOrDefault(g => Norm(((string)Field(g, "ID")?.GetValue(g)) ?? "").Contains(q))
                ?? gambits.FirstOrDefault(g => Norm(((string)Field(g, "GambitName")?.GetValue(g)) ?? "").Replace("name", "").Contains(q));
        }

        private static object ParsePiece(string name)
        {
            var enumType = GameType("Blukulele.CHE.PieceType");
            if (enumType == null) return null;
            var n = Norm(name);
            if (n == "horse") n = "knight";
            if (!PieceNames.Contains(n)) return null;
            return Enum.Parse(enumType, n.ToUpperInvariant());
        }

        private static object Instance(string typeName)
        {
            var t = GameType(typeName);
            if (t == null) return null;
            try
            {
                var all = Resources.FindObjectsOfTypeAll(t).Cast<object>().Where(o => o != null).ToArray();
                return all.FirstOrDefault(o => o is Component c && c.gameObject.scene.isLoaded)
                    ?? all.FirstOrDefault();
            }
            catch { return null; }
        }

        private static Type GameType(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(name, throwOnError: false))
            .FirstOrDefault(t => t != null);

        private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Any)?.Invoke(target, args);
        private static void TryInvoke(object target, string name, params object[] args) { try { Invoke(target, name, args); } catch { } }
        private static object GetProp(object target, string name) => target.GetType().GetProperty(name, Any)?.GetValue(target, null);
        private static void SetProp(object target, string name, object value) => target.GetType().GetProperty(name, Any)?.SetValue(target, value, null);
        private static object GetFieldOrProp(object target, string name) => target.GetType().GetField(name, Any)?.GetValue(target) ?? GetProp(target, name);
        private static FieldInfo Field(object target, string name) => target?.GetType().GetField(name, Any);
        private const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static string Norm(string s) => (s ?? "").Trim().ToLowerInvariant().Replace("_", "-");
        private static bool TryInt(string s, out int value) => int.TryParse(s, out value);
        private static string Short(Exception ex) => ex is TargetInvocationException tie && tie.InnerException != null ? tie.InnerException.Message : ex.Message;
    }
}

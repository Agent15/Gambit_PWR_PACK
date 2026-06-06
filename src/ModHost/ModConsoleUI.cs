namespace Gambonanza.ModHost
{
    // The polished console implementation is self-contained in ModConsole.
    // Kept as a no-op compatibility shell for branches that still reference it.
    internal sealed class ModConsoleUI
    {
        public static void SpawnOnce(ModConsole console) { }
        public static void RegisterUpdate(System.Action action) { }
        public static void UnregisterUpdate(System.Action action) { }
    }
}

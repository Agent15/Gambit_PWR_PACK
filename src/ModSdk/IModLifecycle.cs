namespace Gambonanza.ModSdk
{
    /// <summary>
    /// Optional. Mods that implement this support hot enable/disable from the in-game
    /// mod manager without a game restart.
    ///
    /// Lifecycle:
    ///   OnLoad(ctx)       - called once when the DLL is loaded. Wire up subscriptions only.
    ///   OnEnable()        - called after OnLoad if the mod is enabled, AND on every toggle-on.
    ///   OnDisable()       - called on every toggle-off. Mod must restore any game state it
    ///                       mutated (destroy GameObjects, unsubscribe, restore values).
    ///
    /// Mods that don't implement IModLifecycle are treated as load-once: toggling them off
    /// in-session writes their mod.json but the change only takes effect on next launch.
    /// </summary>
    public interface IModLifecycle
    {
        void OnEnable();
        void OnDisable();
    }
}

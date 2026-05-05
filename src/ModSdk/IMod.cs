namespace Gambonanza.ModSdk
{
    /// <summary>
    /// All mods must implement this interface and have a public parameterless constructor.
    /// ModHost instantiates the entry type and calls OnLoad once during game startup.
    /// </summary>
    public interface IMod
    {
        void OnLoad(IModContext context);
    }
}

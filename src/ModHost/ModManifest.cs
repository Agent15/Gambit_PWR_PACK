using System;
using UnityEngine;

namespace Gambonanza.ModHost
{
    /// <summary>
    /// Wire format of mod.json. Flat by design so Unity's JsonUtility can deserialize it.
    /// Public fields, not properties — JsonUtility ignores properties.
    /// </summary>
    [Serializable]
    public class ModManifest
    {
        public string id;
        public string name;
        public string version;
        public string author;
        /// <summary>Fully qualified IMod entry type, e.g. "Gambonanza.SpeedMod.SpeedModMain".</summary>
        public string entry;
        /// <summary>If false, ModHost skips this mod entirely.</summary>
        public bool enabled = true;
        /// <summary>Optional. Currently informational; future use for compatibility checks.</summary>
        public string gameVersion;

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(id))    { error = "missing 'id'";    return false; }
            if (string.IsNullOrWhiteSpace(entry)) { error = "missing 'entry'"; return false; }
            error = null;
            return true;
        }

        public static ModManifest TryParse(string json, out string error)
        {
            try
            {
                var m = JsonUtility.FromJson<ModManifest>(json);
                if (m == null) { error = "empty or invalid JSON"; return null; }
                error = null;
                return m;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }
    }
}

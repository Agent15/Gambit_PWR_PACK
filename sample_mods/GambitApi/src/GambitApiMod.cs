using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.GambitApi
{
    /// <summary>
    /// Entry point for the GambitApi library mod. Other mods reference this DLL and use
    /// <c>GambitBuilder</c> to register custom gambits — the actual registration is deferred
    /// here until the vanilla <c>GambitLibrary</c> singleton has fully initialised, since
    /// custom gambits can only be inserted after the game has built its internal lookup tables.
    /// </summary>
    public class GambitApiMod : IMod
    {
        public static GambitApiMod Instance { get; private set; }
        public IModContext Context { get; private set; }

        public void OnLoad(IModContext context)
        {
            Instance = this;
            Context = context;
            context.LogLine("[GambitApi] OnLoad called.");
            Debug.Log("[GambitApi] OnLoad called. Creating host...");

            var host = new GameObject("GambitApiHost");
            UnityEngine.Object.DontDestroyOnLoad(host);
            var runner = host.AddComponent<GambitApiHost>();
            host.AddComponent<CollectionInputHandler>();
            runner.StartCoroutine(InitializeRoutine());
        }

        private static IEnumerator InitializeRoutine()
        {
            Debug.Log("[GambitApi] InitializeRoutine started. Waiting for GambitLibrary to be fully initialized...");

            int waitFrames = 0;
            while (true)
            {
                var library = SingletonMonoBehaviour<GambitLibrary>.Instance;
                bool libraryExists = library != null;
                bool hasInfo = libraryExists && library.GambitsInfo != null && library.GambitsInfo.Count > 0;

                // Check if Initialize() has run by looking for m_FocusMap
                bool initialized = false;
                if (libraryExists)
                {
                    var focusMapField = typeof(GambitLibrary).GetField("m_FocusMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    initialized = focusMapField?.GetValue(library) != null;
                }

                if (libraryExists && hasInfo && initialized)
                {
                    Debug.Log($"[GambitApi] GambitLibrary fully initialized after {waitFrames} frames. Count={library.GambitsInfo.Count}, m_FocusMap exists={initialized}");
                    break;
                }

                if (waitFrames % 60 == 0)
                {
                    Debug.Log($"[GambitApi] Waiting for GambitLibrary... exists={libraryExists}, hasInfo={hasInfo}, initialized={initialized}, frames={waitFrames}");
                }

                yield return null;
                waitFrames++;

                if (waitFrames > 600)
                {
                    Debug.LogError("[GambitApi] TIMED OUT waiting for GambitLibrary Initialize() after 600 frames (~10s).");
                    break;
                }
            }

            yield return null;

            Debug.Log("[GambitApi] Processing pending registrations...");
            GambitRegistry.ProcessPending();
            Debug.Log("[GambitApi] InitializeRoutine complete.");
        }
    }

    public class GambitApiHost : MonoBehaviour { }
}

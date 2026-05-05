using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.KamikazeGambit
{
    /// <summary>
    /// Debug hotkeys.
    ///   F9 — inject the kamikaze gambit into a free slot of the current run.
    ///   F8 — start a brand new run from the main menu and inject the gambit once it's ready.
    ///
    /// Both keys log loudly so it's clear whether the press registered. Persists into the run
    /// save (CurrentGambits stores GambitName, which is the localization key the load path matches on).
    /// </summary>
    public class KamikazeDebugHotkey : MonoBehaviour
    {
        private const string GambitId = "KamikazeGambit_Kamikaze";
        private bool _injectQueued;

        private void Start()
        {
            Debug.Log("[KamikazeGambit][debug] KamikazeDebugHotkey active. Press F9 to inject in-run, F8 to start a new run with kamikaze.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                Debug.Log("[KamikazeGambit][debug] F9 pressed.");
                if (!TryInject(out string reason))
                    Debug.LogWarning($"[KamikazeGambit][debug] F9 inject failed: {reason}");
            }
            else if (Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("[KamikazeGambit][debug] F8 pressed — start new run + inject.");
                StartCoroutine(StartRunThenInject());
            }
        }

        private bool TryInject(out string reason)
        {
            reason = null;

            if (!SingletonMonoBehaviour<GambitLibrary>.IsCreated())
            { reason = "GambitLibrary singleton not created."; return false; }

            var library = SingletonMonoBehaviour<GambitLibrary>.Instance;
            var info = library.GetGambitPerId(GambitId);
            if (info == null)
            { reason = $"'{GambitId}' is not registered in the library."; return false; }
            int idx = library.GambitsInfo.IndexOf(info);
            if (idx < 0 || idx >= library.Gambits.Count)
            { reason = "library prefab list out of sync with info list."; return false; }

            if (!SingletonMonoBehaviour<GambitManager>.IsCreated())
            { reason = "GambitManager singleton not created (no run in progress?)."; return false; }
            var manager = SingletonMonoBehaviour<GambitManager>.Instance;
            if (manager.GambitPlaces == null || manager.GambitPlaces.Length == 0)
            { reason = "GambitPlaces array empty (run not initialized)."; return false; }
            if (manager.IsFull())
            { reason = "all 5 gambit slots already full."; return false; }

            var place = manager.GetGambitPlace();
            if (place == null)
            { reason = "GetGambitPlace returned null."; return false; }

            var prefab = library.Gambits[idx];
            // Parent under GambitParent and reset local transform so the gambit sits centered
            // in the slot. Vanilla's instantiation pattern uses DOFollow(GambitParent) to settle
            // the gambit at the parent's origin; setting localPosition=zero achieves the same
            // resting position without needing the tween. Without this, the gambit appears in a
            // permanently "lifted" hover position because place.transform.position is not the
            // GambitParent's origin.
            var instance = Object.Instantiate(prefab, place.GambitParent);
            place.CurrentGambit = instance;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var data = DataManager.Instance?.Data;
            if (data != null && data.CurrentGambits != null)
            {
                for (int i = 0; i < data.CurrentGambits.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(data.CurrentGambits[i]))
                    {
                        data.CurrentGambits[i] = info.GambitName;
                        break;
                    }
                }
            }

            Debug.Log($"[KamikazeGambit][debug] Injected '{GambitId}' into a free slot.");
            return true;
        }

        private IEnumerator StartRunThenInject()
        {
            if (_injectQueued)
            {
                Debug.Log("[KamikazeGambit][debug] StartRun already pending, ignoring repeat F8.");
                yield break;
            }
            _injectQueued = true;

            if (!SingletonMonoBehaviour<GameManager>.IsCreated())
            {
                Debug.LogWarning("[KamikazeGambit][debug] GameManager not yet available; can't start a run.");
                _injectQueued = false;
                yield break;
            }

            // Mirror DebugMenu.OnSkipTutorial: mark tutorial / boss intros as seen so StartNewRun
            // routes straight into a real run instead of replaying the tutorial.
            var data = DataManager.Instance?.Data;
            if (data != null)
            {
                data.TutorialDone_Playtest = true;
                data.TutorialStockToBoard_Seen = true;
                data.SecondShopDone = true;
                data.Boss_Computer_Seen = true;
                data.Boss_Geisha_Seen = true;
                data.Boss_Mask_Seen = true;
                data.Boss_Portal_Seen = true;
                data.Boss_Clock_Seen = true;
                data.Boss_Fish_Seen = true;
                data.Boss_Deer_Seen = true;
                data.Boss_Final_Seen = true;
                Debug.Log("[KamikazeGambit][debug] Tutorial flags set; new run will skip tutorial.");
            }

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            Debug.Log($"[KamikazeGambit][debug] Calling GameManager.StartNewRun() (current state: {gm.CurrentState}).");
            gm.StartNewRun();

            // Wait for the run to come up: GambitManager populated and at least one place exists.
            float elapsed = 0f;
            while (elapsed < 10f)
            {
                if (SingletonMonoBehaviour<GambitManager>.IsCreated())
                {
                    var mgr = SingletonMonoBehaviour<GambitManager>.Instance;
                    if (mgr.GambitPlaces != null && mgr.GambitPlaces.Length > 0)
                        break;
                }
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            // One more frame so vanilla initialization has a chance to settle.
            yield return null;

            if (TryInject(out string reason))
                Debug.Log("[KamikazeGambit][debug] F8 chain complete — kamikaze injected into the new run.");
            else
                Debug.LogWarning($"[KamikazeGambit][debug] F8 chain failed at inject step: {reason}");

            _injectQueued = false;
        }
    }
}

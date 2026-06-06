using System;
using System.Linq;
using System.Reflection;
using Blukulele.CHE;
using Blukulele.Core;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.MightyKasparovEveryStage
{
    public sealed class MightyKasparovEveryStageMod : IMod, IModLifecycle
    {
        private IModContext _context;
        private KasparovBossOrderRunner _runner;

        public void OnLoad(IModContext context)
        {
            _context = context;
            _context?.LogLine("loaded; disabled by default unless enabled in mod.json/console.");
        }

        public void OnEnable()
        {
            if (_runner != null) return;
            var go = new GameObject("__MightyKasparovEveryStageRunner");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _runner = go.AddComponent<KasparovBossOrderRunner>();
            _runner.Bind(_context);
            _context?.LogLine("enabled — every boss-stage encounter will be Mighty Kasparov.");
        }

        public void OnDisable()
        {
            if (_runner == null) return;
            _runner.RestoreOriginalOrder();
            UnityEngine.Object.Destroy(_runner.gameObject);
            _runner = null;
            _context?.LogLine("disabled — restored the boss order captured at enable time.");
        }
    }

    internal sealed class KasparovBossOrderRunner : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;
        private IModContext _context;
        private float _nextRefresh;
        private Boss[] _originalRunBosses;
        private Boss[] _originalSavedBosses;
        private bool _capturedOriginal;

        public void Bind(IModContext context)
        {
            _context = context;
            Apply(forceLog: true);
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + RefreshSeconds;
            Apply(forceLog: false);
        }

        public void RestoreOriginalOrder()
        {
            try
            {
                if (_originalRunBosses != null && SingletonMonoBehaviour<BossManager>.IsCreated())
                    SetRunBosses(Clone(_originalRunBosses));

                var data = DataManager.Instance?.Data;
                if (_originalSavedBosses != null && data != null)
                    data.BossOrder = Clone(_originalSavedBosses);
            }
            catch (Exception ex)
            {
                _context?.LogLine("restore failed: " + ex.Message);
            }
        }

        private void Apply(bool forceLog)
        {
            try
            {
                CaptureOriginalOnce();
                var changedRuntime = ForceRuntimeBossOrder();
                var changedSaved = ForceSavedBossOrder();
                if (forceLog || changedRuntime || changedSaved)
                    _context?.LogLine("boss order forced: Mighty Kasparov for every boss stage.");
            }
            catch (Exception ex)
            {
                _context?.LogLine("apply failed: " + ex.Message);
            }
        }

        private void CaptureOriginalOnce()
        {
            if (_capturedOriginal) return;
            _capturedOriginal = true;

            try
            {
                if (SingletonMonoBehaviour<BossManager>.IsCreated())
                {
                    var run = SingletonMonoBehaviour<BossManager>.Instance.RunBosses;
                    if (run != null) _originalRunBosses = Clone(run);
                }

                var data = DataManager.Instance?.Data;
                if (data?.BossOrder != null) _originalSavedBosses = Clone(data.BossOrder);
            }
            catch { }
        }

        private bool ForceRuntimeBossOrder()
        {
            if (!SingletonMonoBehaviour<BossManager>.IsCreated()) return false;
            var manager = SingletonMonoBehaviour<BossManager>.Instance;
            var runBosses = manager.RunBosses;
            if (runBosses == null || runBosses.Length < 6)
            {
                runBosses = new Boss[6];
                SetRunBosses(runBosses);
            }
            return FillKasparovOrder(runBosses);
        }

        private bool ForceSavedBossOrder()
        {
            var data = DataManager.Instance?.Data;
            if (data == null) return false;
            if (data.BossOrder == null || data.BossOrder.Length < 5)
                data.BossOrder = new Boss[5];
            return FillKasparovOrder(data.BossOrder);
        }

        private static bool FillKasparovOrder(Boss[] order)
        {
            if (order == null || order.Length == 0) return false;
            var changed = false;
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == Boss.FINAL) continue;
                order[i] = Boss.FINAL;
                changed = true;
            }
            return changed;
        }

        private static Boss[] Clone(Boss[] value)
        {
            return value == null ? null : value.ToArray();
        }

        private static void SetRunBosses(Boss[] order)
        {
            var manager = SingletonMonoBehaviour<BossManager>.Instance;
            var field = typeof(BossManager).GetField("m_RunBosses", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(manager, order);
        }
    }
}

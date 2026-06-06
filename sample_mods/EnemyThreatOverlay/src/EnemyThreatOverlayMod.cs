using System;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.EnemyThreatOverlay
{
    public sealed class EnemyThreatOverlayMod : IMod, IModLifecycle
    {
        private IModContext _context;
        private EnemyThreatOverlayRunner _runner;

        public void OnLoad(IModContext context)
        {
            _context = context;
            _context?.LogLine("loaded.");
        }

        public void OnEnable()
        {
            if (_runner != null) return;
            try
            {
                var go = new GameObject("__EnemyThreatOverlayRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                _runner = go.AddComponent<EnemyThreatOverlayRunner>();
                _runner.Bind(_context);
                _context?.LogLine("enabled.");
            }
            catch (Exception ex)
            {
                _context?.LogLine("enable failed: " + ex);
            }
        }

        public void OnDisable()
        {
            try
            {
                if (_runner != null)
                {
                    _runner.TearDown();
                    UnityEngine.Object.Destroy(_runner.gameObject);
                    _runner = null;
                }
                _context?.LogLine("disabled.");
            }
            catch (Exception ex)
            {
                _context?.LogLine("disable failed: " + ex);
            }
        }
    }
}

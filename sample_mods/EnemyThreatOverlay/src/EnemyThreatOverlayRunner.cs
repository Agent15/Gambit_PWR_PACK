using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.EnemyThreatOverlay
{
    [DefaultExecutionOrder(int.MaxValue - 32)]
    internal sealed class EnemyThreatOverlayRunner : MonoBehaviour
    {
        private ThreatOverlayController _controller;
        private UnityThreatOverlayLog _log;

        public void Bind(IModContext context)
        {
            _log = new UnityThreatOverlayLog(context);
            _controller = new ThreatOverlayController(
                new UnityKeybindInput(context),
                new UnityGameStateSource(),
                new ThreatCollector(new UnityPieceSource()),
                new UnityTileVisuals(),
                _log,
                0.15f);
        }

        private void Update()
        {
            _controller?.Tick(Time.unscaledTime);
        }

        public void TearDown()
        {
            _controller?.Hide();
            _controller = null;
        }

        private void OnDestroy()
        {
            TearDown();
        }
    }
}

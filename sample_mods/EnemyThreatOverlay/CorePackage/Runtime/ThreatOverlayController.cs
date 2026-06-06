using System;
using System.Collections.Generic;

namespace Gambonanza.EnemyThreatOverlay
{
    public sealed class ThreatOverlayController
    {
        private readonly IThreatOverlayInput _input;
        private readonly IThreatOverlayGameStateSource _state;
        private readonly ThreatCollector _collector;
        private readonly IThreatOverlayTileVisuals _visuals;
        private readonly IThreatOverlayLog _log;
        private readonly float _refreshSeconds;
        private ThreatTileSet _shown;
        private bool _active;
        private float _nextRefreshAt;

        public ThreatOverlayController(
            IThreatOverlayInput input,
            IThreatOverlayGameStateSource state,
            ThreatCollector collector,
            IThreatOverlayTileVisuals visuals,
            IThreatOverlayLog log,
            float refreshSeconds)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _collector = collector ?? throw new ArgumentNullException(nameof(collector));
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _log = log;
            _refreshSeconds = refreshSeconds <= 0f ? 0.2f : refreshSeconds;
        }

        public bool IsActive => _active;

        public void Tick(float now)
        {
            try
            {
                if (!CanShow(_state.CurrentState) || !_input.IsMiddleMouseHeld)
                {
                    Hide();
                    return;
                }

                if (!_active || now >= _nextRefreshAt)
                {
                    Refresh(now);
                }
            }
            catch (Exception ex)
            {
                _log?.Line("overlay tick failed: " + ex);
                Hide();
            }
        }

        public void Hide()
        {
            if (_shown == null)
            {
                _active = false;
                return;
            }

            foreach (var tile in Snapshot(_shown.EndangerTiles)) _visuals.HideEndanger(tile);
            _shown = null;
            _active = false;
        }

        private static bool CanShow(ThreatOverlayGameState state)
        {
            return state == ThreatOverlayGameState.InGame
                || state == ThreatOverlayGameState.BoardPlacement;
        }

        private void Refresh(float now)
        {
            Hide();
            _shown = _collector.Collect();
            foreach (var tile in _shown.EndangerTiles) _visuals.ShowEndanger(tile);
            _active = true;
            _nextRefreshAt = now + _refreshSeconds;
        }

        private static List<IThreatOverlayTile> Snapshot(IEnumerable<IThreatOverlayTile> tiles)
        {
            return tiles == null ? new List<IThreatOverlayTile>() : new List<IThreatOverlayTile>(tiles);
        }
    }
}

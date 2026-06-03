using System.Collections.Generic;

namespace Gambonanza.EnemyThreatOverlay
{
    public sealed class ThreatTileSet
    {
        private readonly HashSet<IThreatOverlayTile> _endanger = new HashSet<IThreatOverlayTile>();

        public IEnumerable<IThreatOverlayTile> EndangerTiles => _endanger;
        public int EndangerCount => _endanger.Count;

        public void AddEndanger(IThreatOverlayTile tile)
        {
            if (tile == null || tile.IsStock || tile.HasFell) return;
            _endanger.Add(tile);
        }
    }
}

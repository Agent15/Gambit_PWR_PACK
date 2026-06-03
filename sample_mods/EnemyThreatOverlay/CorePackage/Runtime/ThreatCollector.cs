using System;

namespace Gambonanza.EnemyThreatOverlay
{
    public sealed class ThreatCollector
    {
        private readonly IThreatOverlayPieceSource _pieces;

        public ThreatCollector(IThreatOverlayPieceSource pieces)
        {
            _pieces = pieces ?? throw new ArgumentNullException(nameof(pieces));
        }

        public ThreatTileSet Collect()
        {
            var result = new ThreatTileSet();
            foreach (var piece in _pieces.GetPieces())
            {
                if (!IsValidEnemy(piece)) continue;
                if (piece.IsPawn)
                {
                    AddPawnEatTiles(result, piece);
                }
                else
                {
                    AddTiles(result, piece.GetThreatTiles());
                    AddTiles(result, piece.GetOccupiedTiles());
                }
            }
            return result;
        }

        private static void AddPawnEatTiles(ThreatTileSet result, IThreatOverlayPiece piece)
        {
            var tiles = piece.GetPawnEatTiles();
            if (tiles == null) return;
            foreach (var tile in tiles)
            {
                result.AddEndanger(tile);
            }
        }

        private static void AddTiles(ThreatTileSet result, System.Collections.Generic.IEnumerable<IThreatOverlayTile> tiles)
        {
            if (tiles == null) return;
            foreach (var tile in tiles)
            {
                result.AddEndanger(tile);
            }
        }

        private static bool IsValidEnemy(IThreatOverlayPiece piece)
        {
            return piece != null
                && piece.IsEnemy
                && piece.IsEnabled
                && !piece.IsDead
                && !piece.InStock;
        }
    }
}

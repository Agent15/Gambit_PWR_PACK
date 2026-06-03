using System.Collections.Generic;

namespace Gambonanza.EnemyThreatOverlay
{
    public enum ThreatOverlayGameState
    {
        Other,
        BoardPlacement,
        InGame
    }

    public interface IThreatOverlayInput
    {
        bool IsMiddleMouseHeld { get; }
    }

    public interface IThreatOverlayGameStateSource
    {
        ThreatOverlayGameState CurrentState { get; }
    }

    public interface IThreatOverlayPiece
    {
        bool IsEnemy { get; }
        bool IsPawn { get; }
        bool IsDead { get; }
        bool IsEnabled { get; }
        bool InStock { get; }
        IEnumerable<IThreatOverlayTile> GetThreatTiles();
        IEnumerable<IThreatOverlayTile> GetOccupiedTiles();
        IEnumerable<IThreatOverlayTile> GetPawnEatTiles();
    }

    public interface IThreatOverlayTile
    {
        bool IsStock { get; }
        bool HasFell { get; }
    }

    public interface IThreatOverlayPieceSource
    {
        IEnumerable<IThreatOverlayPiece> GetPieces();
    }

    public interface IThreatOverlayTileVisuals
    {
        void ShowEndanger(IThreatOverlayTile tile);
        void HideEndanger(IThreatOverlayTile tile);
    }

    public interface IThreatOverlayLog
    {
        void Line(string message);
    }
}

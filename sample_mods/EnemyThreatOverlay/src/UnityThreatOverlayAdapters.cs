using System.Collections.Generic;
using System.Linq;
using Blukulele.CHE;
using Blukulele.Core;
using Gambonanza.ModSdk;
using UnityEngine;

namespace Gambonanza.EnemyThreatOverlay
{
    internal sealed class UnityKeybindInput : IThreatOverlayInput
    {
        private readonly IModContext _context;

        public UnityKeybindInput(IModContext context)
        {
            _context = context;
        }

        public bool IsMiddleMouseHeld => _context != null && _context.IsKeybindHeld("threatDisplay");
    }

    internal sealed class UnityGameStateSource : IThreatOverlayGameStateSource
    {
        public ThreatOverlayGameState CurrentState
        {
            get
            {
                var gameManager = SingletonMonoBehaviour<GameManager>.Instance;
                if (gameManager == null) return ThreatOverlayGameState.Other;
                if (gameManager.CurrentState == State.INGAME) return ThreatOverlayGameState.InGame;
                if (gameManager.CurrentState == State.BOARD_PLACEMENT) return ThreatOverlayGameState.BoardPlacement;
                return ThreatOverlayGameState.Other;
            }
        }
    }

    internal sealed class UnityPieceSource : IThreatOverlayPieceSource
    {
        public IEnumerable<IThreatOverlayPiece> GetPieces()
        {
            var cached = GameplayObjectsCache.Pieces;
            var pieces = cached != null && cached.Count > 0
                ? cached
                : Object.FindObjectsByType<BasePieceBehaviour>().ToList();

            foreach (var piece in pieces)
            {
                if (piece != null) yield return new UnityThreatPiece(piece);
            }
        }
    }

    internal sealed class UnityThreatPiece : IThreatOverlayPiece
    {
        private readonly BasePieceBehaviour _piece;

        public UnityThreatPiece(BasePieceBehaviour piece)
        {
            _piece = piece;
        }

        public bool IsEnemy => _piece.PieceColor == PieceColor.BLACK;
        public bool IsPawn => _piece is PawnPieceBehaviour;
        public bool IsDead => _piece.IsDead;
        public bool IsEnabled => _piece.enabled && _piece.gameObject.activeInHierarchy;
        public bool InStock => _piece.InStock;
        public IThreatOverlayTile CurrentTile => _piece.CurrentTile == null ? null : new UnityThreatTile(_piece.CurrentTile);

        public IEnumerable<IThreatOverlayTile> GetThreatTiles()
        {
            return Wrap(_piece.GetTilesAvailable());
        }

        public IEnumerable<IThreatOverlayTile> GetOccupiedTiles()
        {
            return Wrap(_piece.GetOccupiedTiles());
        }

        public IEnumerable<IThreatOverlayTile> GetPawnEatTiles()
        {
            var pawn = _piece as PawnPieceBehaviour;
            return pawn == null ? new IThreatOverlayTile[0] : Wrap(pawn.GetEatPlaces());
        }

        private static IEnumerable<IThreatOverlayTile> Wrap(IEnumerable<TileBehaviour> tiles)
        {
            if (tiles == null) yield break;
            foreach (var tile in tiles)
            {
                if (tile != null) yield return new UnityThreatTile(tile);
            }
        }
    }

    internal sealed class UnityThreatTile : IThreatOverlayTile
    {
        private readonly TileBehaviour _tile;

        public UnityThreatTile(TileBehaviour tile)
        {
            _tile = tile;
        }

        public TileBehaviour Tile => _tile;
        public bool IsStock => _tile.IsStock;
        public bool HasFell => _tile.HasFell;

        public override bool Equals(object obj)
        {
            return obj is UnityThreatTile other && ReferenceEquals(_tile, other._tile);
        }

        public override int GetHashCode()
        {
            return _tile != null ? _tile.GetHashCode() : 0;
        }
    }

    internal sealed class UnityTileVisuals : IThreatOverlayTileVisuals
    {
        public void ShowEndanger(IThreatOverlayTile tile)
        {
            AsUnity(tile)?.ShowEndangerTiles();
        }

        public void HideEndanger(IThreatOverlayTile tile)
        {
            AsUnity(tile)?.HideEndangerTiles();
        }

        private static TileBehaviour AsUnity(IThreatOverlayTile tile)
        {
            return (tile as UnityThreatTile)?.Tile;
        }
    }

    internal sealed class UnityThreatOverlayLog : IThreatOverlayLog
    {
        private readonly IModContext _context;

        public UnityThreatOverlayLog(IModContext context)
        {
            _context = context;
        }

        public void Line(string message)
        {
            if (_context != null) _context.LogLine(message);
            else Debug.Log("[EnemyThreatOverlay] " + message);
        }
    }
}

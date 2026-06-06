using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.CaltropsGambit
{
    /// <summary>
    /// Runtime behaviour attached to the custom Caltrops gambit prefab.
    ///
    /// Important mental model:
    /// - GambitBuilder creates/registers the card and tells the game to attach this
    ///   MonoBehaviour to the in-run gambit object.
    /// - This script then listens to real vanilla game events.
    /// - There is no custom TileModManager/TileModType API in Gambonanza, so we do
    ///   not register a new tile type. Instead, we reuse vanilla TRAP tiles and
    ///   make their normal trap effect lethal while this gambit is present.
    /// - Note: the UI/localization calls these TRAP tiles, but the internal game
    ///   API still uses older names like IsHunter and OnHunterTileUsed.
    /// </summary>
    public sealed class GambitCaltrops : BaseGambit
    {
        private bool _subscribed;

        private void Start()
        {
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;

            // EnemyManager owns enemy AI turns. Its OnMove event is the closest real
            // equivalent to the hallucinated "OnTileModApplied" idea:
            // it fires whenever an enemy piece has chosen and moved to a destination
            // tile. The event gives us both the moving piece and the destination tile.
            if (!SingletonMonoBehaviour<EnemyManager>.IsCreated()) return;

            var enemyManager = SingletonMonoBehaviour<EnemyManager>.Instance;
            if (enemyManager == null) return;

            // Vanilla code often uses Delegate.Combine/Remove instead of +=/-=, so
            // we mirror that style. It is equivalent to subscribing to an event, but
            // works because OnMove is a public Action field, not a C# event.
            enemyManager.OnMove = (Action<BasePieceBehaviour, TileBehaviour>)
                Delegate.Combine(enemyManager.OnMove, new Action<BasePieceBehaviour, TileBehaviour>(OnEnemyMove));
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (!SingletonMonoBehaviour<EnemyManager>.IsCreated()) return;

            var enemyManager = SingletonMonoBehaviour<EnemyManager>.Instance;
            if (enemyManager == null) return;

            // Always unsubscribe in OnDestroy. Otherwise old handlers can survive on
            // the manager and keep calling into destroyed gambit objects after the run
            // changes or the mod is disabled.
            enemyManager.OnMove = (Action<BasePieceBehaviour, TileBehaviour>)
                Delegate.Remove(enemyManager.OnMove, new Action<BasePieceBehaviour, TileBehaviour>(OnEnemyMove));
            _subscribed = false;
        }

        private void OnEnemyMove(BasePieceBehaviour piece, TileBehaviour destination)
        {
            if (piece == null || destination == null) return;

            // Enemy pieces are black in Gambonanza. This check keeps the behaviour
            // narrow even if another system unexpectedly reuses the same event shape.
            if (piece.PieceColor != PieceColor.BLACK) return;

            // We intentionally use the vanilla TRAP tile flag. Internally the game
            // still names this `IsHunter`, but players see it as a TRAP tile. The
            // game already knows how to create, save, display, and explain it;
            // Caltrops only changes what happens when enemies step on it.
            if (!destination.IsHunter) return;

            // EnemyManager invokes OnMove BEFORE it starts the visible movement tween:
            //   OnMove(...)
            //   piece.transform.parent = destination.PlaceToPutPieces
            //   piece.transform.DOLocalMove(Vector3.zero, 0.2f)
            // So waiting only one frame fixes the references, but the piece may still
            // be visually near its old tile. We wait for the tween duration too, then
            // destroy it at the destination so the particles appear on the trap tile.
            StartCoroutine(CO_CaptureAfterMoveFinishes(piece, destination));
        }

        private IEnumerator CO_CaptureAfterMoveFinishes(BasePieceBehaviour piece, TileBehaviour tile)
        {
            // First frame: let EnemyManager finish assigning parent/tile/current-tile.
            yield return null;

            // Vanilla enemy movement uses DOLocalMove(..., 0.2f). Wait a little longer
            // than 0.2 game-seconds so the visual movement has actually finished.
            // WaitForSeconds is scaled by Time.timeScale, matching DOTween's default
            // scaled-time behaviour, so this still lines up if SpeedMod is active.
            yield return new WaitForSeconds(0.24f);

            if (piece == null || tile == null) yield break;
            if (piece.PieceColor != PieceColor.BLACK) yield break;
            if (!tile.IsHunter) yield break;

            // Snap exactly to the destination anchor before CaptureEffect. This is a
            // safety net for tiny tween/easing timing differences: particles emitted
            // by CaptureEffect use the piece's current transform position.
            if (tile.PlaceToPutPieces != null)
            {
                piece.transform.parent = tile.PlaceToPutPieces;
                piece.transform.localPosition = Vector3.zero;
            }

            CaptureEnemy(piece, tile);
            Trigger();
        }

        private static void CaptureEnemy(BasePieceBehaviour piece, TileBehaviour tile)
        {
            // Notify vanilla systems that a TRAP tile was used. The internal event
            // is still named OnHunterTileUsed in the game code. This keeps stats,
            // achievements, sounds/feedback listeners, and other gambits closer to
            // the behaviour they expect from a normal Hunter tile trigger.
            try
            {
                if (SingletonMonoBehaviour<TileManager>.IsCreated())
                    SingletonMonoBehaviour<TileManager>.Instance.OnHunterTileUsed?.Invoke(piece, tile);
            }
            catch { }

            // Vanilla HunterTilePower sets this save flag; do the same for parity.
            try { DataManager.Instance.Data.HunterTileUsed = true; } catch { }

            // Break tile <-> piece links before destroying the GameObject. If we skip
            // this, the board can keep a stale reference to a destroyed piece.
            if (tile.Piece == piece) tile.Piece = null;
            if (piece.CurrentTile != null && piece.CurrentTile.Piece == piece)
                piece.CurrentTile.Piece = null;

            // Remove the piece from runtime managers that track active pieces. The
            // try/catch blocks make the gambit resilient if a game update renames a
            // manager or if this fires during scene teardown.
            try
            {
                if (SingletonMonoBehaviour<EnemyManager>.IsCreated())
                    SingletonMonoBehaviour<EnemyManager>.Instance.EnemyPieces.Remove(piece);
            }
            catch { }

            try
            {
                if (SingletonMonoBehaviour<PieceManager>.IsCreated())
                    SingletonMonoBehaviour<PieceManager>.Instance.UnregisterPiece(piece);
            }
            catch { }

            // CaptureEffect gives the piece its normal death visual before we destroy
            // it shortly after. The delay lets the visual effect actually appear.
            try { piece.IsDead = true; } catch { }
            try { piece.CaptureEffect(); } catch { }
            UnityEngine.Object.Destroy(piece.gameObject, 0.25f);
        }

        public override void Trigger()
        {
            try { VisualEffect(); } catch { }
        }
    }
}

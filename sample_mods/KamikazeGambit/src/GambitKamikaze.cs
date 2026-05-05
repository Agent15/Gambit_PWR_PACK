using System;
using System.Collections;
using System.Reflection;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.KamikazeGambit
{
    /// <summary>
    /// Kamikaze Gambit behaviour.
    ///
    /// Intercepts stock-to-board placement. Once per game, if the player lands on
    /// an enemy piece, both the enemy and the player's piece are destroyed.
    /// </summary>
    public class GambitKamikaze : BaseGambit
    {
        private bool _kamikazeUsed;
        private BasePieceBehaviour _selectedStockPiece;

        private void Start()
        {
            var sm = SingletonMonoBehaviour<SelectionManager>.Instance;
            if (sm != null)
            {
                sm.OnSelectStockPiece += OnSelectStockPiece;
                sm.OnReleaseStockPiece += OnReleaseStockPiece;
            }

            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm != null)
            {
                gm.onStateChanged += OnStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (SingletonMonoBehaviour<SelectionManager>.IsCreated())
            {
                var sm = SingletonMonoBehaviour<SelectionManager>.Instance;
                sm.OnSelectStockPiece -= OnSelectStockPiece;
                sm.OnReleaseStockPiece -= OnReleaseStockPiece;
            }

            if (SingletonMonoBehaviour<GameManager>.IsCreated())
            {
                var gm = SingletonMonoBehaviour<GameManager>.Instance;
                gm.onStateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(State state)
        {
            if (state == State.INGAME || state == State.LOAD_RUN)
            {
                _kamikazeUsed = false;
            }
        }

        private void OnSelectStockPiece(BasePieceBehaviour piece)
        {
            _selectedStockPiece = piece;

            // Vanilla TileBehaviour.ShowBlockedPlacement subscribes to OnSelectStockPiece and
            // shows a black "forbidden" overlay on every tile where !CanBeLandedOn. With the
            // kamikaze gambit available, enemy-occupied tiles ARE landable (they trigger the
            // sacrifice), so showing them as forbidden is misleading. Hide the overlay one
            // frame later so we run AFTER vanilla's show. Vanilla's HideBlockedPlacement on
            // OnReleaseStockPiece will restore state cleanly.
            if (!_kamikazeUsed)
                StartCoroutine(CO_HideBlockedOnEnemyTiles());
        }

        private IEnumerator CO_HideBlockedOnEnemyTiles()
        {
            // Wait one frame so vanilla TileBehaviour.ShowBlockedPlacement has already kicked
            // off its DOFade, then disable the renderer outright. Disabling the SpriteRenderer
            // bypasses the in-flight tween without us having to wrestle DOTween directly.
            yield return null;
            var bm = SingletonMonoBehaviour<BoardManager>.IsCreated() ? SingletonMonoBehaviour<BoardManager>.Instance : null;
            if (bm == null) yield break;
            var board = bm.Board;
            if (board == null) yield break;

            var spriteField = typeof(TileBehaviour).GetField("m_FeedbackBlockedPlacement", BindingFlags.NonPublic | BindingFlags.Instance);
            if (spriteField == null) yield break;

            int rows = board.GetLength(0);
            int cols = board.GetLength(1);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var tile = board[r, c];
                    if (tile == null) continue;
                    var p = tile.Piece;
                    if (p == null || p.PieceColor != PieceColor.BLACK) continue;
                    var sr = spriteField.GetValue(tile) as SpriteRenderer;
                    if (sr == null) continue;
                    sr.enabled = false;
                }
            }
        }

        // Re-enable the BlockedPlacement renderers we hid, so the next stock-piece selection
        // (or unrelated UI flow) sees the same starting state vanilla expects.
        private void RestoreBlockedSpriteRenderers()
        {
            var bm = SingletonMonoBehaviour<BoardManager>.IsCreated() ? SingletonMonoBehaviour<BoardManager>.Instance : null;
            if (bm == null) return;
            var board = bm.Board;
            if (board == null) return;
            var spriteField = typeof(TileBehaviour).GetField("m_FeedbackBlockedPlacement", BindingFlags.NonPublic | BindingFlags.Instance);
            if (spriteField == null) return;
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var tile = board[r, c];
                    if (tile == null) continue;
                    var sr = spriteField.GetValue(tile) as SpriteRenderer;
                    if (sr == null) continue;
                    if (!sr.enabled) sr.enabled = true;
                }
            }
        }

        private void OnReleaseStockPiece(BasePieceBehaviour piece)
        {
            // Always restore the BlockedPlacement renderers we may have disabled in
            // OnSelectStockPiece, regardless of whether the kamikaze actually fires.
            RestoreBlockedSpriteRenderers();

            if (_kamikazeUsed) return;
            if (_selectedStockPiece == null) return;
            if (piece == null) return;

            // Find the tile under the pointer. SelectionManager.PointerPosition is already in
            // world coordinates (vanilla's HandleMovementInInGame uses the same value for its
            // raycasts). Don't use Input.mousePosition directly — those are screen pixels and
            // Physics2D.Raycast expects world units.
            var sm = SingletonMonoBehaviour<SelectionManager>.Instance;
            if (sm == null) return;
            Vector3 pointerPos = sm.PointerPosition;
            var hit = Physics2D.Raycast(pointerPos, Vector3.forward, 20f, 64);
            if (!hit.transform) return;

            var tile = hit.transform.GetComponent<TileBehaviour>();
            if (tile == null) return;

            // Need an enemy piece on the tile — that's the whole gambit.
            // (Don't gate on tile.CanBeLandedOn; vanilla returns false for any occupied tile,
            // which would block the kamikaze before we get a chance to clear the enemy.)
            var enemy = tile.Piece;
            if (enemy == null) return;
            if (enemy.PieceColor != PieceColor.BLACK) return;

            // Check board capacity limit (so we don't bypass normal restrictions)
            var boardManager = SingletonMonoBehaviour<BoardManager>.Instance;
            var chessData = SingletonMonoBehaviour<ChessDataManager>.Instance;
            var gambitManager = SingletonMonoBehaviour<GambitManager>.Instance;
            if (boardManager != null && chessData != null)
            {
                int bonus = (gambitManager != null && gambitManager.OshiyaGambit) ? 1 : 0;
                int maxPieces = chessData.MaxPieceOnBoard + bonus;
                if (boardManager.GetPlayerPiecesInBoardCount() >= maxPieces)
                    return;
            }

            // ===== KAMIKAZE! =====
            _kamikazeUsed = true;

            // Kill the enemy piece (same logic as a normal capture)
            PerformKamikazeKill(enemy, tile, piece);

            // Clear the tile and force-mark it landable so SelectionManager.HandleMovementInInGame
            // routes to the empty-tile placement path. The order in vanilla is:
            //   tile.Piece null check → tile.CanBeLandedOn check → capacity check → place.
            // Both flags must be set before vanilla's release code runs (synchronously after this).
            // Capture the original CanBeLandedOn so we can restore it after the placement
            // settles — otherwise an enemy-zone tile we kamikaze'd would stay landable forever
            // and show as a normal stock-placement target later in the run.
            bool originalCanBeLandedOn = tile.CanBeLandedOn;
            tile.Piece = null;
            tile.CanBeLandedOn = true;

            // If a pawn lands on the last row, vanilla would call SelectionManager.Promotion()
            // which spawns a promotion UI for our about-to-be-destroyed piece — softlocking the
            // game (the UI has no piece to promote, so nothing dismisses it).
            //
            // Vanilla's gate is: `if (tile.IsEnd && !tile.PromoteColor && piece.PieceHierarchy == PAWN)`.
            // We can't force CheckIfShouldSkipPromotion() to return true here (it requires
            // EnemyPieces.Count==0 which isn't the case after a single kamikaze), so instead we
            // make vanilla's gate fail outright by flipping tile.PromoteColor to non-zero. The
            // gate's `!PromoteColor` term goes false → no promotion path is taken. We restore
            // the original PromoteColor a couple frames later so we don't break vanilla state
            // for the next pawn that lands here.
            bool pawnOnLastRow = tile.IsEnd && piece.PieceHierarchy == PieceHierarchy.PAWN;
            PieceColor originalPromoteColor = PieceColor.WHITE;
            if (pawnOnLastRow)
            {
                originalPromoteColor = tile.PromoteColor;
                tile.PromoteColor = PieceColor.BLACK;
            }

            if (piece.VisualEffect != null)
                piece.VisualEffect.CaptureEffect();

            StartCoroutine(CO_DestroyPlayerPieceAfterLanding(piece, tile, originalCanBeLandedOn));
            if (pawnOnLastRow)
                StartCoroutine(CO_RestorePromoteColor(tile, originalPromoteColor));
        }

        private IEnumerator CO_RestorePromoteColor(TileBehaviour tile, PieceColor original)
        {
            // Two frames is enough: vanilla's IsEnd/Pawn promotion gate runs synchronously
            // after our handler returns, so the no-promotion branch has already been chosen
            // by the time the next frame ticks.
            yield return null;
            yield return null;
            if (tile != null) tile.PromoteColor = original;
        }

        private IEnumerator CO_DestroyPlayerPieceAfterLanding(BasePieceBehaviour piece, TileBehaviour tile, bool originalCanBeLandedOn)
        {
            // Wait two frames so the game can finish placing the piece on the board
            // (PieceManager registration, tile.Piece assignment, etc.) before we tear it down.
            yield return null;
            yield return null;
            DestroyPlayerPiece(piece, tile);
            // Restore CanBeLandedOn now that the placement has settled and the piece is gone.
            // We had to flip it on so vanilla's release path took the empty-tile branch; leaving
            // it on permanently would turn this enemy-zone tile into a regular stock-placement
            // target for the rest of the run.
            if (tile != null) tile.CanBeLandedOn = originalCanBeLandedOn;
        }

        private void DestroyPlayerPiece(BasePieceBehaviour piece, TileBehaviour tile)
        {
            if (piece == null) return;

            piece.IsDead = true;

            var pieceManager = SingletonMonoBehaviour<PieceManager>.Instance;
            if (pieceManager != null)
                pieceManager.UnregisterPiece(piece);

            if (piece.VisualEffect != null)
                piece.VisualEffect.Disappear(0.25f);

            piece.enabled = false;
            UnityEngine.Object.Destroy(piece.gameObject, 0.6f);

            if (tile != null && tile.Piece == piece)
                tile.Piece = null;
        }

        private void PerformKamikazeKill(BasePieceBehaviour enemy, TileBehaviour tile, BasePieceBehaviour attacker)
        {
            enemy.IsDead = true;

            var pieceManager = SingletonMonoBehaviour<PieceManager>.Instance;
            if (pieceManager != null)
                pieceManager.UnregisterPiece(enemy);

            var enemyManager = SingletonMonoBehaviour<EnemyManager>.Instance;
            if (enemyManager != null)
                enemyManager.EnemyPieces.Remove(enemy);

            if (enemy.VisualEffect != null)
                enemy.VisualEffect.Disappear(0.25f);

            enemy.enabled = false;
            UnityEngine.Object.Destroy(enemy.gameObject, 0.6f);

            var chessData = SingletonMonoBehaviour<ChessDataManager>.Instance;
            if (chessData != null)
                chessData.PiecesCaptured++;

            // Fire the capture event so other systems (achievements, unlocks, etc.) know a capture happened
            var selectionManager = SingletonMonoBehaviour<SelectionManager>.Instance;
            if (selectionManager != null)
                selectionManager.OnCapture?.Invoke(attacker, enemy, tile);

            // Shockwave effect
            var shockWaveManager = SingletonMonoBehaviour<ShockWaveManager>.Instance;
            if (shockWaveManager != null)
            {
                if (enemyManager != null && enemyManager.EnemyPieces.Count == 0)
                    shockWaveManager.StartWave(tile.GetWaveBehaviour(), 0.7f);
                else
                    shockWaveManager.StartWave(tile.GetWaveBehaviour());
            }
        }

        private Vector3 GetPointerPosition()
        {
            if (Input.touchCount > 0)
                return Input.GetTouch(0).position;
            return Input.mousePosition;
        }

        public override void Trigger()
        {
            VisualEffect();
        }
    }
}

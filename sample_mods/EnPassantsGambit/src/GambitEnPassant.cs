using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using Gambonanza.PointAMngr;

namespace Gambonanza.EnPassantsGambit
{
    /// <summary>
    /// En Passant's Gambit: You can capture the last enemy piece to move by moving a piece
    /// over its original tile
    ///
    /// This gambit has two majot functions
    /// 1: After every enemy move, create a phantom "after-image" copy of the moved piece over its original tile
    /// 2: If the player moves onto that tile, capture the recently moved piece
    ///
    /// This gambit takes inspiration from
    /// - PointAManager ("Original tile" recal)
    /// - Lich's gambit (Phantom piece board placement)
    /// - Lobbyist's gambit (Unconditional piece capture)
    ///
    /// WIP: Winning by crumble tile leaves an after-image on the board until next game. Fix it dummy :P
    /// </summary>
    public sealed class GambitEnPassant : BaseGambit
    {
        private BasePieceBehaviour enPassantImage = null;
        private TileBehaviour enPassantTarget = null;
        private TileBehaviour enPassantVoodoo = null;
        private void Start()
        {
            // Every time a piece moves, create an afterimage
            EnemyManager.Instance.OnMove += CO_DisplayImage;
            // Every time the player moves, check for a trigger
            SelectionManager.Instance.OnMove += Behave;
            SelectionManager.Instance.OnPawnPromotionAsMoved += Behave;
            // In case the game ends on the enemy's turn, cleanup the board of afterimages
            GameManager.Instance.onStateChanged += CO_Cleanup;
            // In case this gambit was earned mid-game. Reset PointAManager's pieceTracker
            PointAManager.Instance.InstantFill();
        }

        private void OnDestroy()
        {
            // Unassign action calls
            EnemyManager.Instance.OnMove -= CO_DisplayImage;
            SelectionManager.Instance.OnMove -= Behave;
            SelectionManager.Instance.OnPawnPromotionAsMoved -= Behave;
            GameManager.Instance.onStateChanged -= CO_Cleanup;
        }

        // This method is an intermediary between the argument requirements of OnMove and DisplayTarget
        private void CO_DisplayImage(BasePieceBehaviour piece, TileBehaviour tile)
        {
            base.StartCoroutine(DisplayImage(piece, tile, 0.1f));
        }

        //Trigger the gambit effect if this move is equal to last move, update the last move otherwise
        private IEnumerator DisplayImage(BasePieceBehaviour movedPiece, TileBehaviour tile, float delay)
        {
            // Wait for PointAManager to update its attributes first
            yield return new WaitForSeconds(delay);
            // Check for any existing afterImages in case of things like player turn skipping
            if (enPassantImage != null)
            {
                PhantomDisappear(enPassantImage);
                enPassantImage = null;
                enPassantTarget = null;
                enPassantVoodoo = null;
            }
            // We don't want to make an afterImage if the enemy piece is Elite or Stasis
            if (!movedPiece.EnemyAbilityModifier.IsBoss && !movedPiece.EnemyAbilityModifier.IsClock)
            {
                // Find the piece's original tile
                TileBehaviour pointA = PointAManager.Instance.EnemyPointA;

                // Generate the afterImage on pointA
                BasePieceBehaviour afterImage = Instantiate<BasePieceBehaviour>(SingletonMonoBehaviour<Library>.Instance.GetPiece(movedPiece.GetPieceType(true), PieceColor.BLACK), pointA.PlaceToPutPieces.position, Quaternion.identity, pointA.PlaceToPutPieces);
                afterImage.GetComponent<PieceApparitionEffect>().LaunchAnimationAtStart = false;
                afterImage.Modifier.TurnToPhantom();
                afterImage.GetComponent<PieceVisualEffect>().TurnToPhantom();

                // Update the gambit's logic
                enPassantImage = afterImage;
                enPassantTarget = pointA;
                enPassantVoodoo = movedPiece.CurrentTile;
            }
        }

        private void Behave(BasePieceBehaviour piece, TileBehaviour tile)
        {
            // Make the afterImage disappear if it exists
            if (enPassantImage != null)
            {
                PhantomDisappear(enPassantImage);
            }
            // In case the last moved piece has already been destroyed, check for a piece on the voodoo tile
            if (enPassantVoodoo != null && enPassantVoodoo.Piece != null && tile == enPassantTarget)
            {
                // Capture the voodoo piece
                PerformEnPassantKill(enPassantVoodoo.Piece, enPassantVoodoo);
                // Inform the other managers that the player captured on THEIR piece's tile
                SelectionManager.Instance.OnCapture.Invoke(piece, enPassantVoodoo.Piece, tile);
                // BOING!
                VisualEffect();
            }
            enPassantImage = null;
            enPassantTarget = null;
            enPassantVoodoo = null;
        }

        //Deletes a piece from the board without an attacker, and notifies
        //every necessary manager to delete the piece from the board.
        //Credit to Bentrd for laying the groundwork with Kamikaze's gambit
        private void PerformEnPassantKill(BasePieceBehaviour enemy, TileBehaviour tile)
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

            try { enemy.CaptureEffect(); } catch { }
        }

        public override void Trigger()
        {
            //BOING!
            VisualEffect();
        }

        // This is a copy of PhantomPieceManager's behavior to make a phantom piece disappear from the board.
        // It can't be called from outside its own class, so I copy/pasted and refactored it into this gambit.
        private void PhantomDisappear(BasePieceBehaviour pieceBehaviour)
        {
            Instantiate<GameObject>(PhantomPieceManager.Instance.ParticlePhantomDisappear, pieceBehaviour.transform.position + Vector3.up * 0.5f, Quaternion.identity);
            pieceBehaviour.VisualEffect.PhantomDisappearEffect();
            Destroy(pieceBehaviour.gameObject, 0.25f);
        }

        private void CO_Cleanup(State state)
        {
            base.StartCoroutine(Cleanup(state, 0.3f));
        }

        // If a game is won on the enemy's turn (eg: by crumble tile), an afterimage is left on the board
        // This method triggers at the end of a game and removes any afterimage if it exists.
        private IEnumerator Cleanup(State state, float delay)
        {
            // OnMove and OnStateChanged execute so close together that enPassantImage
            // was still null by the time this method executed, so I'm adding a delay
            yield return new WaitForSeconds(delay);
            // At the end of a game...
            if (state == State.WIN)
            {
                // If there is still an afterimage on the board...
                if(enPassantImage is not null)
                {
                    // Remove the afterimage
                    PhantomDisappear(enPassantImage);
                    enPassantImage = null;
                    enPassantTarget = null;
                    enPassantVoodoo = null;
                }
            }
        }
    }
}

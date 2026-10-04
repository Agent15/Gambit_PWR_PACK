using Blukulele.CHE;

namespace Gambonanza.SkeletonsGambit
{
    /// <summary>
    /// Skeleton's Gambit: If you only have 1 piece on the board, 1/3 chance to protect it each turn.
	///
    /// This gambit uses BoardManager's GetPlayerPiecesInBoardCount() method to determine if the player
    /// has one piece on the board, then runs a 1/3 chance to protect it.
    ///
    /// NOTE: This gambit technically works with an external trigger (eg: Jumper cables), but will
    /// favor one specific piece on the board.
	/// </summary>
    public sealed class GambitSkeleton : BaseGambit
    {
        private void Start()
        {
            TurnManager.Instance.OnPlayerTurn += CO_Trigger;
        }

        private void OnDestroy()
        {
            TurnManager.Instance.OnPlayerTurn -= CO_Trigger;
        }

        private void CO_Trigger()
        {
            // If the player has one piece on the board...
            if (BoardManager.Instance.GetPlayerPiecesInBoardCount() == 1)
            {
                //Roll a 1/3 chance
                if (ChanceManager.Instance.ComputeChance(1f, 3f, "SKELETON_OCCURRANCE"))
                {// The chance passed, Execute the Trigger() method
                    Trigger();
                }
                else
                {// The chance failed. Play the "No luck" animation
                    this.m_Gambit.Nope();
                }
            }
        }

        public override void Trigger()
        {
            // Find the first player piece on the board and protect it
            BasePieceBehaviour skeletonPiece = BoardManager.Instance.GetPlayerPiecesInBoard()[0];
            skeletonPiece.Modifier.Protect();
            // Alter other objects that a piece has been protected
            if(PieceManager.Instance.OnProtectPiece is not null)
                PieceManager.Instance.OnProtectPiece.Invoke(skeletonPiece);
            // BOING!
            VisualEffect();
        }
    }
}

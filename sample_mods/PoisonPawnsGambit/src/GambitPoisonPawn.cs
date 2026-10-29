using Blukulele.CHE;

namespace Gambonanza.PoisonPawnsGambit
{
    /// <summary>
    /// PoisonPawn's Gambit: If one of your pawns is captured by a non-pawn piece, skip the enemy's turn.
	///
    /// This gambit listens to EnemyManager's OnCapture call, skips the turn if a pawn is captured by a
    /// non-pawn, and also uses a hint text to indicate when it was responsible for a turn skip.
	/// </summary>
    public sealed class GambitPoisonPawn : BaseGambit
    {
        private bool IDidThat = false;
        private void Start()
        {
            EnemyManager.Instance.OnCapture += Behave;
            EnemyManager.Instance.OnSkip += Hint;
        }

        private void OnDestroy()
        {
            EnemyManager.Instance.OnCapture -= Behave;
            EnemyManager.Instance.OnSkip -= Hint;
        }

        private void Behave(BasePieceBehaviour attacker, BasePieceBehaviour victim, TileBehaviour x)
        {
            if (attacker.GetPieceType() != PieceType.PAWN && victim.GetPieceType() == PieceType.PAWN)
            {
                Trigger();
            }
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
            // Skip the enemy's turn
            EnemyManager.Instance.SkipTurn();
            // Leave a hint on the next skipped enemy turn.
            IDidThat = true;
        }

        private void Hint()
        {
            if(IDidThat)
            {
                this.m_FeedbackIncrementor.Spawn("Turn<br>skip");
                this.m_FeedbackIncrementor.IncrementSound(0f);
                IDidThat = false;
            }
        }
    }
}

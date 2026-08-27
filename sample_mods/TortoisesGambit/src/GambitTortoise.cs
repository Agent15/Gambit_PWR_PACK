using Blukulele.CHE;

namespace Gambonanza.TortoisesGambit
{
    /// <summary>
    /// Tortoise's Gambit: Every king move has a 1/2 chance to also count as waiting.
    /// 
    /// The function of this gambit is simple. Listen for the player's OnMove action call, check if 
    /// the piece moved was a king, roll a 1/2 chance and fire the onWait action call if successful.
    /// </summary>
    public sealed class GambitTortoise : BaseGambit
    {
        private void Start()
        {
            // After every player move, execute the Behave() method
            SelectionManager.Instance.OnMove += Behave;
        }

        private void OnDestroy()
        {
            // Unassign axtion calls
            SelectionManager.Instance.OnMove -= Behave;
        }

        private void Behave(BasePieceBehaviour piece, TileBehaviour x)
        {
            // If the piece that moved is a "king"...
            if (piece.GetPieceType() == PieceType.KING || GambitManager.Instance.AnarchistEnable)
            {
                // Roll a 1/2 chance
                if (!ChanceManager.Instance.ComputeChance((float)1, (float)2, "TORTOISE_OCCURRANCE"))
                {
                    // End now if the chance fails
                    this.m_Gambit.Nope();
                    return;
                }
                // The chance passes, initiate a wait call
                Trigger();
            }
        }
        public override void Trigger()
        {
            // Send out an OnWait call
            WaitManager.Instance.OnWait.Invoke();
            // BOING!
            VisualEffect();
        }
    }
}

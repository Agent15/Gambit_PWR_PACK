using Blukulele.CHE;

namespace Gambonanza.FrankensteinsGambit
{
    /// <summary>
    /// Frankenstein's Gambit: After 3 of your pieces are captured, earn a random piece among those 3.
	///
	/// This gambit keeps a list of three PieceTypes. If one of your pieces is captured, that piece's
    /// type is added to the list and the description is updated. After three piece types have been 
    /// added, we select one at random to give and clear the list
    /// </summary>
    public sealed class GambitFrankenstein : BaseGambit
    {
        PieceType[] capturedTypes = { PieceType.NONE, PieceType.NONE, PieceType.NONE };
        int index = 0;
        private void Start()
        {
            // After every enemy capture, add a piece to the list and potentially trigger
            EnemyManager.Instance.OnCapture += Behave;
        }

        private void OnDestroy()
        {
            // Update the description to default
            FrankensteinsGambitBuild.UpdateDescription();
            // Unassign action calls
            EnemyManager.Instance.OnCapture -= Behave;
        }

        private void Behave(BasePieceBehaviour attacker, BasePieceBehaviour victim, TileBehaviour x)
        {
            // Add the captured piece type to the list and move the index one down
            capturedTypes[index] = victim.GetPieceType(true);
            index++;
            // If we just assigned the last item in the list...
            if (index == 3)
            {
                // Earn a piece through the Trigger() method
                Trigger();
                // Update the description to default
                FrankensteinsGambitBuild.UpdateDescription();
                // Reset the index and capturedTypes list
                index = 0;
                for (int i = 0; i < capturedTypes.Length; i++)
                {
                    capturedTypes[i] = PieceType.NONE;
                }
            }
            // Otherwise, we need to continue filling the list...
            else
            {
                // Add the captured piece type to the description
                FrankensteinsGambitBuild.UpdateDescription(capturedTypes);
                // Show a small indicator that a piece has been added to the list
                this.m_FeedbackIncrementor.Spawn("+1");
                this.m_FeedbackIncrementor.IncrementSound(0f);
            }
        }

        public override void Trigger()
        {
            // Earn a random piece from the gambit's position
            if (StockManager.Instance.RoomAvailable() && index > 0)
                StockManager.Instance.AddPiece(capturedTypes[UnityEngine.Random.Range(0, index)], this.transform.position);
            // BOING!
            VisualEffect();
        }
    }
}

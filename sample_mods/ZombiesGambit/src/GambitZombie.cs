using Blukulele.CHE;

namespace Gambonanza.ZombiesGambit
{
    /// <summary>
    /// Zombie's Gambit: Capturing with a king has a 1/2 chance to earn the captured piece
    ///
    /// The mechanics of this gambit are simple. After every player capture, check if the
    /// attacking piece is a "king" and reward a king if it is
    /// </summary>
    public sealed class GambitZombie : BaseGambit
    {
        private void Start()
        {
            // After every capture, check if the capturing piece is a king
            SelectionManager.Instance.OnCapture += Behave;
        }

        private void OnDestroy()
        {
            // Unassign action calls
            SelectionManager.Instance.OnCapture -= Behave;
        }

        private void Behave(BasePieceBehaviour attacker, BasePieceBehaviour victim, TileBehaviour x)
        {
            // If there isn't any room on the stock
            if(!StockManager.Instance.RoomAvailable()) return;
            // If the attacking piece is a "king"...
            if(attacker.GetPieceType() == PieceType.KING || GambitManager.Instance.AnarchistEnable)
            {
                // Earn a king from the attacker's position
                StockManager.Instance.AddPiece(PieceType.KING, attacker.transform.position);
                // BOING!
                VisualEffect();
            }
        }
        public override void Trigger()
        {
            // In case of an external trigger, earn a king from the gambit's position
            if(StockManager.Instance.RoomAvailable())
                StockManager.Instance.AddPiece(PieceType.KING, this.transform.position);
            // BOING!
            VisualEffect();
        }
    }
}

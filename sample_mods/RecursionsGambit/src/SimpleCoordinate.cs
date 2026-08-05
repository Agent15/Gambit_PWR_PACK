using Blukulele.CHE;

namespace Gambonanza.RecursionsGambit
{
    public class SimpleCoordinate
    {
        public BasePieceBehaviour piece;
        public int x = 0;
        public int y = 0;

        public SimpleCoordinate(BasePieceBehaviour piece, int x, int y)
        {
            this.piece = piece;
            this.x = x;
            this.y = y;
        }
    }
}
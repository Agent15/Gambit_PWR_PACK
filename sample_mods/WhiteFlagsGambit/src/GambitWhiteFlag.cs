using Blukulele.CHE;
using Blukulele.Core;

namespace Gambonanza.WhiteFlagsGambit
{
    /// <summary>
    /// WhiteFlag's Gambit: Sell this gambit to force a stalemate.
	///
    /// This gambit doesn't listen to any action calls, instead it invokes the game's
    /// OnPat action when destroyed (sold) and immediately sells itself if it's created
    /// after a stalemete it forced.
	/// </summary>
    public sealed class GambitWhiteFlag : BaseGambit
    {
        private void Start()
        {
            // If a previous stalemete was caused by this gambit, immediately sell itself
            if(WhiteFlagsGambitBuild.IDidThat)
                m_Gambit.Sell();
		}

        // This method triggers any time the gambit's game object is destroyed
        // including closing the game, gambit destructions, and in this case, selling
        private void OnDestroy()
        {
            // If a white flag stalemate was triggered already,
            // reset the bool and end the execution early
            if(WhiteFlagsGambitBuild.IDidThat)
            {
                WhiteFlagsGambitBuild.IDidThat = false;
                return;
            }
            // We don't want to trigger anywhere other than mid-game
            if (GameManager.Instance.CurrentState != State.INGAME)
            {
                return;
            }
            // BOING!
            VisualEffect();
            // Mark this as a white-flag-triggered stalemate
            WhiteFlagsGambitBuild.IDidThat = true;
            // Trigger a stalemete
            TurnManager.Instance.OnPat.Invoke();
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
            // Just for fun, I'm gonna invoke a stalemate on an external trigger too :3
            TurnManager.Instance.OnPat.Invoke();
        }
    }
}

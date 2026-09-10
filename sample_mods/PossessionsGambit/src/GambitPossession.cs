using System;
using System.Collections;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.PossessionsGambit
{
    /// <summary>
    /// Possession's Gambit: Capturing with a phantom piece transforms it into a default copy of the captured piece
	///
	/// </summary>
    public sealed class GambitPossession : BaseGambit
    {
        private void Start()
        {
            SelectionManager.Instance.OnCapture += Behave;
            GambitManager.Instance.OnGetGambit += ExorcistCheck;
            ExorcistCheck();
        }

        private void OnDestroy()
        {
            SelectionManager.Instance.OnCapture -= Behave;
            GambitManager.Instance.OnGetGambit -= ExorcistCheck;
        }

        private void Behave(BasePieceBehaviour attacker, BasePieceBehaviour victim, TileBehaviour tile)
        {
            if (attacker.Modifier.IsPhantom)
            {
                base.StartCoroutine(CO_Possess(0.3f, attacker, victim.GetPieceType()));
            }
        }
        
        // This method is an isolated sequence to convert an enemy piece into a player's piece
        // This logic is largely copy/pasted from Evangelist's gambit with a few tweaks
        private IEnumerator CO_Possess(float delay, BasePieceBehaviour attacker, PieceType victimType)
        {
            yield return new WaitForSeconds(delay);
            // Create a new piece with the victim's type, attacker's position, and white piece color
            BasePieceBehaviour newPiece = Instantiate<BasePieceBehaviour>(SingletonMonoBehaviour<Library>.Instance.GetPiece(victimType, PieceColor.WHITE), attacker.transform.position, Quaternion.identity);
            // We don't want a flashy promotion-like animation
            newPiece.GetComponent<PieceApparitionEffect>().LaunchAnimationAtStart = false;
            // The piece specific gambit visual effects are difficult to parse from C# alone,
            // so I'm adopting the transformation effect of polymorphic pieces
            newPiece.VisualEffect.MagicianEffect();
            // Register the new piece with PieceManager with any of the attacker's
            // spacial characteristics (protected, blessed, etc)
            SingletonMonoBehaviour<PieceManager>.Instance.CopyPieceCaractericstics(attacker, newPiece, attacker.CurrentTile);
            // Destroy the original piece
            Destroy(attacker.gameObject);
            // BOING!
            VisualEffect();
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }

        private void ExorcistCheck()
        {
            // For every gambit in the stock...
            List<GambitPlaceBehaviour> gambitStock = new List<GambitPlaceBehaviour>(GambitManager.Instance.GambitPlaces);
            foreach (GambitPlaceBehaviour g in gambitStock)
            {
                // If this gambit is Exorcist's Gambit...
                if (g.CurrentGambit is not null && g.CurrentGambit.Info.ID.Equals("exorcist"))
                {
                    // Trigger Exorcist's Gambit
                    g.CurrentGambit.HighlightEffect();
                    // Execute the Spasm Coroutine
                    base.StartCoroutine(Spasm());
                    // End this method early
                    return;
                }
            }
        }

        public IEnumerator Spasm()
        {
            // Trigger 20 times in quick succession
            for (int i = 0; i < 20; i++)
            {
                // BOING!
                VisualEffect();
                // Wait for the specified amount of time
                yield return new WaitForSeconds(0.1f);
            }
            // This gambit sells itself
            m_Gambit.Sell();
        }
    }
}

using System;
using System.Collections.Generic;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.HeadlessHorsemansGambit
{
    /// <summary>
    /// Headless Horseman's Gambit: Moving a king adjacent to a knight (or vice versa) earns
    /// $5 (once per game)
    /// 
    /// 
    /// </summary>
    public sealed class GambitHeadlessHorseman : BaseGambit
    {
        // Define a boolean to remember if this gambit has already triggered this game
        private bool used = false;
        private void Start()
        {
            // On every move, check if this gambit's condition is met
            SelectionManager.Instance.OnMove += Behave;
            // On certain state changes, reset the "used" tracker
            GameManager.Instance.onStateChanged += Reset;
        }

        private void OnDestroy()
        {
            SelectionManager.Instance.OnMove -= Behave;
            GameManager.Instance.onStateChanged -= Reset;
        }

        private void Behave(BasePieceBehaviour piece, TileBehaviour tile)
        {
            try{
            // If this gambit was already used this game, end the trigger early
            if(used) return;
            // Define a variable to determine what piece type to check for
            PieceType typeToMatch = PieceType.NONE;
            // If this piece is a "knight", we'll need to look for a king
            if(CountAsTranslate(piece) == PieceType.KNIGHT)
            {
                typeToMatch = PieceType.KING;
            }
            // If this piece is a "king", we'll need to look for a knight
            else if(CountAsTranslate(piece) == PieceType.KING)
            {
                typeToMatch = PieceType.KNIGHT;
            }
            // If it's neither, end this trigger early
            else return;
            // Collect every neighboring piece
            List<TileBehaviour> neighbors = tile.GetNeighbourTiles();
            // If there are no neighboring pieces, end the trigger early
            if(neighbors.Count == 0) return;
            // For every neighboring tile...
            foreach(TileBehaviour t in neighbors)
            {
                // If this piece is the type we're looking for
                if(t.Piece is not null && CountAsTranslate(t.Piece) == typeToMatch)
                {
                    // We found a match, earn the money and mark this gambit as used
                    Trigger();
                    used = true;
                    // To prevent multiple triggers, end the trigger now
                    return;
                }
            }
            }catch (Exception e){UpdateDescription(e.ToString());}
        }

        public override void Trigger()
        {
            //Update the dollar count
            SingletonMonoBehaviour<ChessDataManager>.Instance.IncreaseCoin(5);
            //Generate floating money symbols
			SingletonMonoBehaviour<MoneyAnimationManager>.Instance.SpawnMoney(base.transform, 5);
            // BOING!
            VisualEffect();
        }

        private void Reset(State state)
        {
            // If we've entered the shop, unmark the "used" bool
            if(state == State.SHOP)
                used = false;
        }

        // There's a lot to consider when determining if a piece is a "knight" or "king"
        // so I moved that logic to a separate method to simplify my if conditions
        // NOTE: With this logic, there are still some edge cases that don't trigger the gambit
        // even though they should, but I give up at this point ._.
        private PieceType CountAsTranslate(BasePieceBehaviour piece)
        {
            // If this piece is actually a knight or king type, don't fuss with "count as"
            if(piece.GetPieceType() == PieceType.KING || piece.GetPieceType() == PieceType.KNIGHT)
            {
                return piece.GetPieceType();
            }
            // With pegasus' gambit, every promoted piece is a "knight"
            else if(piece.Modifier.IsPromoted && GambitManager.Instance.PegasusActivated)
            {
                return PieceType.KNIGHT;
            }
            // With Anarchist's gambit, every piece is a "king"
            else if(GambitManager.Instance.AnarchistEnable)
            {
                return PieceType.KING;
            }
            // Other than that, we don't care what this piece type is
            else return PieceType.NONE;
        }
        //DEBUG
        public static void UpdateDescription(string s)
        {
            var locManager = SingletonMonoBehaviour<LocalizationManager>.Instance;
            if (locManager == null)
            return;

            var traduction = locManager.GetTraduction();
            if (traduction == null)
            return;

            var gambitNode = traduction["gambit"];
            if (gambitNode == null)
            return;

            
            gambitNode[$"headless-horseman_description"] = s;
        }  
    }
}

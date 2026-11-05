using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.BulletTimesGambit
{
    /// <summary>
    /// Bullet Time's Gambit: Skipping the enemy's turn also delays crumbling tiles from falling
	///
    /// I couldn't get this to work. Triggering on turn skip doesn't catch falling tiles
    /// fast enough. And I can't think of a way to quickly and reliably determine if the 
    /// enemy's turn was skipped otherwise.
	/// </summary>
    public sealed class GambitBulletTime : BaseGambit
    {
        public List<TileBehaviour> managerCrumbleTiles = null;
        public List<TileBehaviour> bulletCrumbleTiles = new();
        private bool turnSkipped;
        private void Start()
        {
            try
            {
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

                FieldInfo crumbleTiles = typeof(CrumbleManager).GetField("m_CrumbleTiles", flags);
                managerCrumbleTiles = (List<TileBehaviour>)crumbleTiles?.GetValue(CrumbleManager.Instance);
            }
            catch (Exception e)
            {
                UpdateDescription(e.ToString());
            }
            EnemyManager.Instance.OnSkip += CrumbleCatch;
            SelectionManager.Instance.OnMove += Fall;
        }

        private void OnDestroy()
        {
            if (bulletCrumbleTiles.Count > 0)
            {
                foreach (TileBehaviour tile in bulletCrumbleTiles)
                {
                    tile.Fall();
                }
            }
            EnemyManager.Instance.OnSkip -= CrumbleCatch;
            SelectionManager.Instance.OnMove += Fall;
        }

        private void CrumbleCatch()
        {
            turnSkipped = true;
            if (managerCrumbleTiles is not null)
            {
                foreach (TileBehaviour tile in managerCrumbleTiles)
                {
                    bulletCrumbleTiles.Add(tile);
                }
                managerCrumbleTiles.Clear();
                UpdateDescription(bulletCrumbleTiles.Count.ToString()); //DEBUG
            }
        }

        private void Fall(BasePieceBehaviour x, TileBehaviour y)
        {
            if (turnSkipped)
            {
                turnSkipped = false;
            }
            else
            {
                if (bulletCrumbleTiles.Count > 0)
                {
                    base.StartCoroutine(CO_Fall(0.5f));
                }
            }
        }

        private IEnumerator CO_Fall(float delay)
        {
            yield return new WaitForSeconds(delay);
            foreach(TileBehaviour tile in bulletCrumbleTiles)
            {
                tile.Fall();
            }
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }

        // DEBUG
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

            gambitNode["bullet-time_description"] = s;
        }
    }
}

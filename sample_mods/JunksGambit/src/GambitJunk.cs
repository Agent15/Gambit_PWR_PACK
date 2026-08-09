using System;
using System.Collections;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;
using DG.Tweening;

namespace Gambonanza.JunksGambit
{
    /// <summary>
    /// Junk's Gambit: It's useless, but in a funny way. Trust me ;)
    /// 
    /// There's nothing to see here. The bulk of this gambit's behavior comes from its ability to
    /// update descriptions which is handled in the JunksGambitBuild class
    /// </summary>
    public sealed class GambitJunk : BaseGambit
    {
        private void Start()
        {}

        private void OnDestroy()
        {
            // Select a new description for the next appearance
            JunksGambitBuild.UpdateJunkDescription();
        }

        public override void Trigger()
        {
            // BOING!
            VisualEffect();
        }
    }
}

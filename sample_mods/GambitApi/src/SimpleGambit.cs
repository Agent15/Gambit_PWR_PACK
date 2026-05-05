using Blukulele.CHE;
using UnityEngine;

namespace Gambonanza.GambitApi
{
    /// <summary>
    /// A lightweight BaseGambit implementation that invokes a delegate.
    /// Perfect for quick gambits that don't need a full custom class.
    /// </summary>
    public class SimpleGambit : BaseGambit
    {
        public System.Action<GambitBehaviour> OnTriggerAction { get; set; }

        public override void Trigger()
        {
            OnTriggerAction?.Invoke(m_Gambit);
        }
    }
}

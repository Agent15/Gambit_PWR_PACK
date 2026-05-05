using System.Reflection;
using Blukulele.CHE;
using Blukulele.Core;
using TMPro;
using UnityEngine;

namespace Gambonanza.GambitApi
{
    /// <summary>
    /// Attached to every RunInfoCanvas (and its CollectionCanvas subclass) so the
    /// "X/200" denominator stays accurate when modded gambits raise the library count.
    ///
    /// Vanilla hardcodes the right side as "/200" in <see cref="RunInfoCanvas.ComputeGambitCount"/>,
    /// so we just rewrite the TMP_Text contents on enable and again whenever the count drifts.
    /// On disable we put the vanilla string back so removing the mod leaves no trace.
    /// </summary>
    public class GambitCountPatch : MonoBehaviour
    {
        private static readonly FieldInfo s_TextField = typeof(RunInfoCanvas)
            .GetField("m_TXT_GambitCount", BindingFlags.NonPublic | BindingFlags.Instance);

        private RunInfoCanvas _canvas;
        private TMP_Text _label;
        private string _vanillaText;

        private void Awake()
        {
            _canvas = GetComponent<RunInfoCanvas>();
            if (_canvas == null || s_TextField == null)
            {
                Destroy(this);
                return;
            }
            _label = s_TextField.GetValue(_canvas) as TMP_Text;
        }

        private void OnEnable() { Apply(); }

        private void Update()
        {
            // Vanilla rewrites the text in OnEnable / on certain interactions. Re-apply if
            // it drifted back to the hardcoded "/200" denominator.
            if (_label != null && !string.IsNullOrEmpty(_label.text) && _label.text.EndsWith("/200"))
                Apply();
        }

        private void OnDisable()
        {
            if (_label != null && _vanillaText != null)
                _label.text = _vanillaText;
        }

        private void Apply()
        {
            if (_label == null) return;
            var lib = SingletonMonoBehaviour<GambitLibrary>.Instance;
            if (lib == null || lib.GambitsInfo == null) return;
            int total = lib.GambitsInfo.Count;
            if (total <= 200) return; // vanilla path is correct, leave it alone

            if (_vanillaText == null) _vanillaText = _label.text;
            int unlocked = DataManager.Instance?.Data?.GambitUnlocked?.Count ?? 0;
            _label.text = unlocked + "/" + total;
        }
    }
}

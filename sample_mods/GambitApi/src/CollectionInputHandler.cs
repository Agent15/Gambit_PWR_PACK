using System.Collections.Generic;
using System.Reflection;
using Blukulele.CHE;
using Blukulele.Core;
using UnityEngine;

namespace Gambonanza.GambitApi
{
    /// <summary>
    /// Global input handler for collection pagination.
    /// Attached to GambitApiHost (always active) and intercepts arrow keys
    /// whenever the collection is open, bypassing broken vanilla pagination.
    /// </summary>
    public class CollectionInputHandler : MonoBehaviour
    {
        private bool _collectionOpen;
        private float _lastNavTime;
        private const float NAV_COOLDOWN = 0.15f;

        private void Update()
        {
            var gm = SingletonMonoBehaviour<GameManager>.Instance;
            if (gm == null) return;

            bool wasOpen = _collectionOpen;
            _collectionOpen = gm.CurrentState == State.COLLECTION;

            // Reset cooldown when collection first opens
            if (_collectionOpen && !wasOpen)
                _lastNavTime = 0f;

            if (!_collectionOpen) return;

            // Time-based cooldown to prevent accidental double-page
            if (Time.unscaledTime - _lastNavTime < NAV_COOLDOWN) return;

            int direction = 0;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                direction = 1;
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                direction = -1;

            if (direction != 0)
            {
                NavigateCollection(direction);
                _lastNavTime = Time.unscaledTime;
            }
        }

        private void NavigateCollection(int direction)
        {
            // Find the active collection slide
            var slides = FindObjectsOfType<GambitCollectionSlide>();
            if (slides == null || slides.Length == 0)
            {
                Debug.Log("[GambitApi] No active GambitCollectionSlide found.");
                return;
            }

            foreach (var slide in slides)
            {
                if (slide == null) continue;
                // Only operate on the one that's actually visible/active
                if (!slide.gameObject.activeInHierarchy) continue;

                var ordererField = typeof(GambitCollectionSlide).GetField("m_GambitOrderer", BindingFlags.NonPublic | BindingFlags.Instance);
                var indexField = typeof(GambitCollectionSlide).GetField("m_Index", BindingFlags.NonPublic | BindingFlags.Instance);
                var initField = typeof(GambitCollectionSlide).GetField("m_Initialize", BindingFlags.NonPublic | BindingFlags.Instance);
                var updateMethod = typeof(GambitCollectionSlide).GetMethod("UpdateUI", BindingFlags.NonPublic | BindingFlags.Instance);

                // Ensure initialized
                bool initialized = (bool)(initField?.GetValue(slide) ?? false);
                if (!initialized)
                {
                    initField?.SetValue(slide, false);
                    updateMethod?.Invoke(slide, null);
                }

                var orderer = ordererField?.GetValue(slide) as List<SO_Gambit>;
                if (orderer == null || orderer.Count == 0)
                {
                    Debug.Log("[GambitApi] Orderer list is empty.");
                    return;
                }

                int currentIndex = (int)(indexField?.GetValue(slide) ?? 0);
                int pageCount = Mathf.CeilToInt(orderer.Count / 10f);

                int newIndex = currentIndex + direction;
                if (newIndex >= pageCount) newIndex = 0;
                if (newIndex < 0) newIndex = pageCount - 1;

                indexField?.SetValue(slide, newIndex);
                updateMethod?.Invoke(slide, null);

                Blukulele.Module.Audio.AudioManager.Play(
                    Blukulele.Audio.AudioEvents.UI_ButtonCollection,
                    loop: false,
                    UnityEngine.Random.Range(0.9f, 1.1f)
                );

                Debug.Log($"[GambitApi] Collection page: {newIndex + 1}/{pageCount} (gambits: {orderer.Count})");
                return; // Only operate on the first active slide
            }
        }
    }
}

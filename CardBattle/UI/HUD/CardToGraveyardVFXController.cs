using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Spawns flying ghost cards for presentation only. Notifies <see cref="PileCounterUI"/> when visuals arrive.
    /// </summary>
    public class CardToGraveyardVFXController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform vfxContainer;
        [SerializeField] private FlyingCardGhostUI ghostPrefab;
        [SerializeField] private PileCounterUI pileCounterUI;

        [Header("Batch")]
        [SerializeField] private float batchStartStaggerMax = 0.02f;
        [SerializeField] private float batchHorizontalSpread = 220f;
        [SerializeField] private float batchBaseArcHeight = 120f;
        [SerializeField] private float batchEdgeArcBonus = 70f;
        [SerializeField] private float batchSideArcBias = 30f;
        [SerializeField] private float batchArcRandomRange = 12f;

        private Canvas ResolvedCanvas =>
            rootCanvas != null
                ? rootCanvas
                : pileCounterUI != null
                    ? pileCounterUI.GetComponentInParent<Canvas>()
                    : GetComponentInParent<Canvas>();

        private static Camera GetCanvasEventCamera(Canvas canvas)
        {
            if (canvas == null)
                return null;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        /// <summary>Spawn one ghost from a hand card view. Call before <see cref="DeckController.PlayCardFromHand"/> so the source still exists.</summary>
        public void PlaySingleCardToGraveyard(CardViewUI sourceView)
        {
            if (pileCounterUI == null)
                return;

            if (vfxContainer == null || ghostPrefab == null)
            {
                pileCounterUI.OnSingleGhostArrived();
                return;
            }

            var canvas = ResolvedCanvas;
            Sprite sprite = sourceView != null ? sourceView.GetArtworkSnapshotForVfx() : null;
            RectTransform sourceRt = sourceView != null ? sourceView.LayoutRect : null;

            if (!TryGetEndpoints(sourceRt, canvas, out var startLocal, out var endLocal))
            {
                pileCounterUI.OnSingleGhostArrived();
                return;
            }

            var ghost = Instantiate(ghostPrefab, vfxContainer);
            ghost.SetUseDynamicArc(true);
            ghost.BeginFlight(sprite, startLocal, endLocal, pileCounterUI.OnSingleGhostArrived);
        }

        /// <summary>Spawn parallel ghosts for all sources. Call before discard so hand views still exist.</summary>
        public void PlayBatchCardsToGraveyard(IReadOnlyList<CardViewUI> sourceViews)
        {
            if (pileCounterUI == null)
                return;

            if (sourceViews == null || sourceViews.Count == 0)
                return;

            if (vfxContainer == null || ghostPrefab == null)
            {
                pileCounterUI.OnBatchGhostArrived(sourceViews.Count);
                return;
            }

            var canvas = ResolvedCanvas;
            var graveyardRt = ResolveGraveyardRect();
            if (canvas == null || graveyardRt == null)
            {
                pileCounterUI.OnBatchGhostArrived(sourceViews.Count);
                return;
            }

            if (!TryWorldToContainerLocal(vfxContainer, canvas, GetWorldCenter(graveyardRt), out var endLocal))
            {
                pileCounterUI.OnBatchGhostArrived(sourceViews.Count);
                return;
            }

            int total = sourceViews.Count;
            int arrived = 0;

            for (int i = 0; i < total; i++)
            {
                var view = sourceViews[i];
                Sprite sprite = view != null ? view.GetArtworkSnapshotForVfx() : null;
                RectTransform sourceRt = view != null ? view.LayoutRect : null;

                if (!TryGetEndpoints(sourceRt, canvas, out var startLocal, out var _))
                {
                    startLocal = endLocal;
                }

                float stagger = 0f;
                if (batchStartStaggerMax > 0f && total > 1)
                    stagger = (i / (float)(total - 1)) * batchStartStaggerMax;

                float t = total > 1 ? (i / (float)(total - 1)) : 0.5f;
                float centered = t - 0.5f;
                float side = centered * 2f; // [-1, 1]
                float edgeWeight = Mathf.Abs(side); // center=0, edges=1

                // Non-linear spread keeps center cards closer while pushing outer cards wider.
                float spreadWeight = Mathf.Pow(edgeWeight, 0.75f);
                float horizontalOffset = Mathf.Sign(side) * spreadWeight * batchHorizontalSpread;

                // Deterministic shape drives readability; random is only a small secondary wobble.
                float edgeArcOffset = edgeWeight * batchEdgeArcBonus;
                float sideArcOffset = side * batchSideArcBias;
                float randomArcOffset = Random.Range(-batchArcRandomRange, batchArcRandomRange);
                float finalArc = batchBaseArcHeight + edgeArcOffset + sideArcOffset + randomArcOffset;

                StartCoroutine(CoSpawnGhostAfterDelay(
                    sprite,
                    startLocal,
                    endLocal,
                    stagger,
                    finalArc,
                    horizontalOffset,
                    () =>
                {
                    arrived++;
                    if (arrived >= total)
                        pileCounterUI.OnBatchGhostArrived(total);
                }));
            }
        }

        private IEnumerator CoSpawnGhostAfterDelay(
            Sprite sprite,
            Vector2 startLocal,
            Vector2 endLocal,
            float delay,
            float arcHeight,
            float horizontalOffset,
            System.Action onThisArrived)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            var ghost = Instantiate(ghostPrefab, vfxContainer);
            ghost.SetUseDynamicArc(false);
            ghost.SetArcHeight(arcHeight);
            ghost.SetHorizontalOffset(horizontalOffset);
            ghost.BeginFlight(sprite, startLocal, endLocal, onThisArrived);
        }

        private RectTransform ResolveGraveyardRect()
        {
            if (pileCounterUI == null)
                return null;
            var anchor = pileCounterUI.GraveyardAnchor;
            if (anchor != null)
                return anchor;
            return pileCounterUI.transform as RectTransform;
        }

        private bool TryGetEndpoints(RectTransform sourceRt, Canvas canvas, out Vector2 startLocal, out Vector2 endLocal)
        {
            startLocal = default;
            endLocal = default;

            var graveyardRt = ResolveGraveyardRect();
            if (canvas == null || vfxContainer == null || graveyardRt == null)
                return false;

            if (sourceRt != null &&
                TryWorldToContainerLocal(vfxContainer, canvas, GetWorldCenter(sourceRt), out startLocal) &&
                TryWorldToContainerLocal(vfxContainer, canvas, GetWorldCenter(graveyardRt), out endLocal))
            {
                return true;
            }

            if (TryWorldToContainerLocal(vfxContainer, canvas, GetWorldCenter(graveyardRt), out endLocal))
            {
                startLocal = endLocal;
                return true;
            }

            return false;
        }

        private static Vector3 GetWorldCenter(RectTransform rt)
        {
            if (rt == null)
                return Vector3.zero;
            return rt.TransformPoint(rt.rect.center);
        }

        private static bool TryWorldToContainerLocal(
            RectTransform container,
            Canvas canvas,
            Vector3 worldPos,
            out Vector2 local)
        {
            local = default;
            if (container == null || canvas == null)
                return false;

            var cam = GetCanvasEventCamera(canvas);
            var screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, cam, out local);
        }
    }
}

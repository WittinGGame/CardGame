using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only reshuffle VFX: flies ghosts from graveyard anchor to deck anchor.
    /// Use to gate "draw remaining cards" timing after reshuffle visuals.
    /// </summary>
    public class GraveyardToDeckVFXController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform vfxContainer;
        [SerializeField] private RectTransform ghostPrefab;
        [SerializeField] private PileCounterUI pileCounterUI;

        [Header("Timing")]
        [SerializeField] private float flyDuration = 0.36f;
        [SerializeField] private float spawnInterval = 0.022f;
        [SerializeField] private float fallbackWait = 0.2f;

        [Header("Batch")]
        [SerializeField] private int maxVisualGhosts = 10;
        [SerializeField] private float startScatter = 36f;
        [SerializeField] private float endScatter = 12f;
        [SerializeField] private float arcHeight = 110f;
        [SerializeField] private float endScaleMultiplier = 0.7f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Canvas ResolvedCanvas =>
            rootCanvas != null
                ? rootCanvas
                : pileCounterUI != null
                    ? pileCounterUI.GetComponentInParent<Canvas>()
                    : GetComponentInParent<Canvas>();

        /// <summary>
        /// Plays graveyard->deck visual transfer for reshuffle timing.
        /// Returns only after all visual ghosts arrive (or fallback delay on missing refs).
        /// </summary>
        public IEnumerator PlayReshuffleVfx(int graveyardCardCount)
        {
            if (graveyardCardCount <= 0)
                yield break;

            pileCounterUI?.BeginReshufflePresentation(graveyardCardCount);

            if (!TryResolveEndpoints(out var start, out var end))
            {
                pileCounterUI?.OnReshuffleGhostLaunched(graveyardCardCount);
                pileCounterUI?.OnReshuffleGhostArrivedAtDeck(graveyardCardCount);
                yield return new WaitForSecondsRealtime(fallbackWait);
                pileCounterUI?.CompleteReshufflePresentation();
                yield break;
            }

            int visualCount = Mathf.Clamp(graveyardCardCount, 1, Mathf.Max(1, maxVisualGhosts));
            int arrived = 0;
            int[] transferByGhost = BuildTransferByGhost(graveyardCardCount, visualCount);

            for (int i = 0; i < visualCount; i++)
            {
                Vector2 startJitter = Random.insideUnitCircle * startScatter;
                Vector2 endJitter = Random.insideUnitCircle * endScatter;
                float side = visualCount > 1 ? ((i / (float)(visualCount - 1)) - 0.5f) * 2f : 0f;
                float sideArc = side * 32f;
                int transferAmount = transferByGhost[i];

                pileCounterUI?.OnReshuffleGhostLaunched(transferAmount);

                StartCoroutine(CoFlyGhost(
                    start + startJitter,
                    end + endJitter,
                    arcHeight + sideArc,
                    () =>
                    {
                        arrived++;
                        pileCounterUI?.OnReshuffleGhostArrivedAtDeck(transferAmount);
                    }));

                if (spawnInterval > 0f && i < visualCount - 1)
                    yield return new WaitForSecondsRealtime(spawnInterval);
            }

            yield return new WaitUntil(() => arrived >= visualCount);
            pileCounterUI?.CompleteReshufflePresentation();
        }

        private IEnumerator CoFlyGhost(Vector2 start, Vector2 end, float arc, System.Action onArrived)
        {
            if (ghostPrefab == null || vfxContainer == null)
            {
                onArrived?.Invoke();
                yield break;
            }

            var ghost = Instantiate(ghostPrefab, vfxContainer);
            var rt = ghost as RectTransform;
            if (rt == null)
            {
                Destroy(ghost.gameObject);
                onArrived?.Invoke();
                yield break;
            }

            var group = ghost.GetComponent<CanvasGroup>();
            if (group == null)
                group = ghost.gameObject.AddComponent<CanvasGroup>();

            rt.anchoredPosition = start;
            rt.localScale = Vector3.one;
            group.alpha = 1f;

            float duration = Mathf.Max(0.01f, flyDuration);
            float t = 0f;
            Vector2 control = (start + end) * 0.5f + Vector2.up * arc;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = moveCurve.Evaluate(Mathf.Clamp01(t / duration));

                rt.anchoredPosition = EvaluateQuadraticBezier(start, control, end, u);
                float s = Mathf.Lerp(1f, endScaleMultiplier, u);
                rt.localScale = new Vector3(s, s, 1f);
                group.alpha = Mathf.Lerp(1f, 0.7f, u);

                yield return null;
            }

            onArrived?.Invoke();
            Destroy(ghost.gameObject);
        }

        private bool TryResolveEndpoints(out Vector2 startLocal, out Vector2 endLocal)
        {
            startLocal = default;
            endLocal = default;

            if (vfxContainer == null || pileCounterUI == null)
                return false;

            var canvas = ResolvedCanvas;
            if (canvas == null)
                return false;

            var graveAnchor = ResolveGraveyardAnchor();
            var deckAnchor = ResolveDeckAnchor();
            if (graveAnchor == null || deckAnchor == null)
                return false;

            return TryWorldToContainerLocal(vfxContainer, canvas, graveAnchor.position, out startLocal) &&
                   TryWorldToContainerLocal(vfxContainer, canvas, deckAnchor.position, out endLocal);
        }

        private RectTransform ResolveGraveyardAnchor()
        {
            if (pileCounterUI == null)
                return null;

            if (pileCounterUI.GraveyardAnchor != null)
                return pileCounterUI.GraveyardAnchor;

            return pileCounterUI.transform as RectTransform;
        }

        private RectTransform ResolveDeckAnchor()
        {
            if (pileCounterUI == null)
                return null;

            if (pileCounterUI.DeckAnchor != null)
                return pileCounterUI.DeckAnchor;

            return pileCounterUI.transform as RectTransform;
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

            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, cam, out local);
        }

        private static Vector2 EvaluateQuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float omt = 1f - t;
            return omt * omt * a + 2f * omt * t * b + t * t * c;
        }

        private static int[] BuildTransferByGhost(int totalCards, int ghostCount)
        {
            int[] result = new int[Mathf.Max(0, ghostCount)];
            if (totalCards <= 0 || ghostCount <= 0)
                return result;

            int baseAmount = totalCards / ghostCount;
            int remainder = totalCards % ghostCount;

            for (int i = 0; i < ghostCount; i++)
                result[i] = baseAmount + (i < remainder ? 1 : 0);

            return result;
        }
    }
}

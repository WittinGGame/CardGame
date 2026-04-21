================================================================================
FILE: BattleHUDController.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/BattleHUDController.cs
================================================================================
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class BattleHUDController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;
        [SerializeField] private BattleActionRunner battleActionRunner;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerApText;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private GameObject playerBlockRoot;
        [SerializeField] private TextMeshProUGUI playerBlockText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private HpBarUI playerHpBar;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;

        private void Start()
        {
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnClickEndTurn);
            }

            BindEnemyStatusUI();
            if (player != null)
            {
                HandlePlayerHpChanged(player.CurrentHp, player.MaxHp);
                HandlePlayerApChanged(player.CurrentAp, player.ApPerRound);
                HandlePlayerBlockChanged(player.CurrentBlock);
            }
            RefreshUIExternal();
        }

        private void OnEnable()
        {
            if (player != null)
            {
                player.OnHpChangedEvent += HandlePlayerHpChanged;
                player.OnApChangedEvent += HandlePlayerApChanged;
                player.OnTurnStateChanged += HandleTurnStateChanged;
                player.OnBlockChangedEvent += HandlePlayerBlockChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged += HandleBusyStateChanged;
        }

        private void OnDisable()
        {
            if (player != null)
            {
                player.OnHpChangedEvent -= HandlePlayerHpChanged;
                player.OnApChangedEvent -= HandlePlayerApChanged;
                player.OnTurnStateChanged -= HandleTurnStateChanged;
                player.OnBlockChangedEvent -= HandlePlayerBlockChanged;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged -= HandleBusyStateChanged;
        }

        private void OnClickEndTurn()
        {
            if (battleActionRunner == null)
                return;

            if (targetSelectionSystem != null && targetSelectionSystem.IsSelectingTarget)
                targetSelectionSystem.CancelTargetSelection();

            battleActionRunner.TryEndTurn();
        }

        public void RefreshUIExternal()
        {
            RefreshEndTurnButtonState();
        }

        private void BindEnemyStatusUI()
        {
            if (enemyActionSystem == null)
                return;

            var enemies = enemyActionSystem.Enemies;
        }

        private void HandlePlayerHpChanged(int currentHp, int maxHp)
        {
            if (playerHpText != null)
                playerHpText.text = $"{currentHp}/{maxHp}";

            if (playerHpBar != null)
                playerHpBar.SetHp(currentHp, maxHp);
        }

        private void HandlePlayerApChanged(int currentAp, int maxAp)
        {
            if (playerApText != null)
                playerApText.text = $"{currentAp}";
        }

        private void HandlePlayerBlockChanged(int currentBlock)
        {
            if (playerBlockText != null)
                playerBlockText.text = currentBlock.ToString();

            if (playerBlockRoot != null)
                playerBlockRoot.SetActive(currentBlock > 0);
        }

        private void HandleTurnStateChanged(bool canAct)
        {
            RefreshEndTurnButtonState();
        }

        private void HandleBusyStateChanged(bool isBusy)
        {
            RefreshEndTurnButtonState();
        }

        private void RefreshEndTurnButtonState()
        {
            if (endTurnButton == null || player == null)
                return;

            bool canClick = player.CanAct;

            if (battleActionRunner != null)
                canClick = canClick && battleActionRunner.CanAcceptInput;

            endTurnButton.interactable = canClick;
        }

        private bool HasAliveEnemy()
        {
            if (enemyActionSystem == null)
                return false;

            var enemies = enemyActionSystem.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                    return true;
            }

            return false;
        }
    }
}

================================================================================
FILE: HpBarUI.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/HpBarUI.cs
================================================================================
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class HpBarUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform colorBarRect;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("Bar Size")]
        [SerializeField] private float maxWidth = 620f;

        public void SetHp(int currentHp, int maxHp)
        {
            if (maxHp <= 0)
                maxHp = 1;

            currentHp = Mathf.Clamp(currentHp, 0, maxHp);

            float percent = (float)currentHp / maxHp;
            float width = maxWidth * percent;

            if (colorBarRect != null)
            {
                colorBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            if (hpText != null)
                hpText.text = $"{currentHp}/{maxHp}";
        }
    }
}

================================================================================
FILE: PlayerHpBarBinder.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/PlayerHpBarBinder.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    public class PlayerHpBarBinder : MonoBehaviour
    {
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HpBarUI hpBarUI;

        private void OnEnable()
        {
            if (player != null)
                player.OnHpChangedEvent += HandleHpChanged;
        }

        private void Start()
        {
            RefreshNow();
        }

        private void OnDisable()
        {
            if (player != null)
                player.OnHpChangedEvent -= HandleHpChanged;
        }

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            if (hpBarUI != null)
                hpBarUI.SetHp(currentHp, maxHp);
        }

        [ContextMenu("Refresh Now")]
        public void RefreshNow()
        {
            if (player == null || hpBarUI == null)
                return;

            hpBarUI.SetHp(player.CurrentHp, player.MaxHp);
        }
    }
}

================================================================================
FILE: PileCounterUI.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/PileCounterUI.cs
================================================================================
using System.Collections;
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Displays deck / graveyard counts; graveyard display can lag real counts until VFX arrival.
    /// </summary>
    public class PileCounterUI : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private DeckController deckController;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI deckCountText;
        [SerializeField] private TextMeshProUGUI graveyardCountText;
        [SerializeField] private RectTransform graveyardAnchor;

        [Header("Display")]
        [SerializeField] private bool initializeDisplayFromRealOnStart = true;
        [SerializeField] private bool punchGraveyardLabelOnGhostArrival = true;
        [SerializeField] private float punchScale = 1.12f;
        [SerializeField] private float punchDuration = 0.12f;

        private int realGraveyardCount;
        private int displayedGraveyardCount;
        private Vector3 graveyardLabelBaseScale = Vector3.one;
        private Coroutine punchRoutine;

        public RectTransform GraveyardAnchor => graveyardAnchor;

        private void Start()
        {
            if (graveyardCountText != null)
                graveyardLabelBaseScale = graveyardCountText.rectTransform.localScale;

            SyncFromDeckControllerInitialize();
        }

        private void OnEnable()
        {
            if (deckController != null)
                deckController.OnPilesChanged += OnPilesChanged;
        }

        private void OnDisable()
        {
            if (deckController != null)
                deckController.OnPilesChanged -= OnPilesChanged;
        }

        private void SyncFromDeckControllerInitialize()
        {
            if (deckController == null)
                return;

            realGraveyardCount = deckController.Graveyard.Count;
            displayedGraveyardCount = initializeDisplayFromRealOnStart
                ? realGraveyardCount
                : 0;

            RefreshDeckText();
            RefreshGraveyardText();
        }

        private void OnPilesChanged()
        {
            if (deckController == null)
                return;

            realGraveyardCount = deckController.Graveyard.Count;
            if (displayedGraveyardCount > realGraveyardCount)
                displayedGraveyardCount = realGraveyardCount;

            RefreshDeckText();
            RefreshGraveyardText();
        }

        [ContextMenu("Refresh UI")]
        public void RefreshUI()
        {
            OnPilesChanged();
        }

        /// <summary>After a single ghost reaches the graveyard anchor, bumps display by 1 (clamped to real).</summary>
        public void OnSingleGhostArrived()
        {
            displayedGraveyardCount = Mathf.Min(displayedGraveyardCount + 1, realGraveyardCount);
            RefreshGraveyardText();
            TriggerGraveyardPunch();
        }

        /// <summary>After a discard batch finishes flying, bumps display by the whole batch (clamped).</summary>
        public void OnBatchGhostArrived(int amount)
        {
            if (amount <= 0)
                return;

            displayedGraveyardCount = Mathf.Min(displayedGraveyardCount + amount, realGraveyardCount);
            RefreshGraveyardText();
            TriggerGraveyardPunch();
        }

        public void ForceSyncDisplayedToReal()
        {
            if (deckController != null)
                realGraveyardCount = deckController.Graveyard.Count;
            displayedGraveyardCount = realGraveyardCount;
            RefreshGraveyardText();
        }

        private void RefreshDeckText()
        {
            if (deckCountText != null && deckController != null)
                deckCountText.text = deckController.Deck.Count.ToString();
        }

        private void RefreshGraveyardText()
        {
            if (graveyardCountText != null)
                graveyardCountText.text = displayedGraveyardCount.ToString();
        }

        private void TriggerGraveyardPunch()
        {
            if (!punchGraveyardLabelOnGhostArrival || graveyardCountText == null)
                return;

            if (punchRoutine != null)
                StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(CoPunchGraveyardLabel());
        }

        private IEnumerator CoPunchGraveyardLabel()
        {
            var rt = graveyardCountText.rectTransform;
            float half = punchDuration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                float s = Mathf.Lerp(1f, punchScale, u);
                rt.localScale = graveyardLabelBaseScale * s;
                yield return null;
            }

            t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / half);
                float s = Mathf.Lerp(punchScale, 1f, u);
                rt.localScale = graveyardLabelBaseScale * s;
                yield return null;
            }

            rt.localScale = graveyardLabelBaseScale;
            punchRoutine = null;
        }
    }
}

================================================================================
FILE: CardToGraveyardVFXController.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/CardToGraveyardVFXController.cs
================================================================================
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

================================================================================
FILE: FlyingCardGhostUI.cs
PATH: Assets/Scripts/CardBattle/UI/HUD/FlyingCardGhostUI.cs
================================================================================
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only ghost card that flies toward a graveyard anchor and self-destructs on arrival.
    /// Supports a curved path for smoother motion.
    /// </summary>
    public class FlyingCardGhostUI : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image artworkImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Motion")]
        [SerializeField] private float flyDuration = 0.42f;
        [SerializeField] private AnimationCurve motionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float endScaleMultiplier = 0.72f;
        [SerializeField] private float endAlphaMultiplier = 0.78f;

        [Header("Curved Path")]
        [SerializeField] private bool useCurvedPath = true;
        [SerializeField] private bool useDynamicArcHeight = true;
        [SerializeField] private float arcHeight = 140f;
        [SerializeField] private float arcDistanceMultiplier = 0.18f;
        [SerializeField] private float minArcHeight = 60f;
        [SerializeField] private float maxArcHeight = 180f;
        [SerializeField] private float horizontalControlOffset = 0f;

        private void Awake()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (artworkImage == null)
                artworkImage = GetComponentInChildren<Image>(true);
        }

        public void BeginFlight(
            Sprite artwork,
            Vector2 startAnchoredPosition,
            Vector2 endAnchoredPosition,
            System.Action onArrived)
        {
            if (artworkImage != null)
            {
                artworkImage.sprite = artwork;
                artworkImage.enabled = artwork != null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = startAnchoredPosition;
                rectTransform.localScale = Vector3.one;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            StartCoroutine(CoFly(startAnchoredPosition, endAnchoredPosition, onArrived));
        }

        public void SetArcHeight(float value)
        {
            arcHeight = value;
        }

        public void SetHorizontalOffset(float value)
        {
            horizontalControlOffset = value;
        }

        public void SetUseDynamicArc(bool value)
        {
            useDynamicArcHeight = value;
        }

        private IEnumerator CoFly(
            Vector2 start,
            Vector2 end,
            System.Action onArrived)
        {
            float dur = Mathf.Max(0.01f, flyDuration);
            float t = 0f;

            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            float endAlpha = startAlpha * endAlphaMultiplier;

            float resolvedArcHeight = arcHeight;
            if (useDynamicArcHeight)
            {
                float distance = Vector2.Distance(start, end);
                resolvedArcHeight = Mathf.Clamp(distance * arcDistanceMultiplier, minArcHeight, maxArcHeight);
            }

            Vector2 control = (start + end) * 0.5f;
            control += Vector2.up * resolvedArcHeight;
            control += Vector2.right * horizontalControlOffset;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = motionCurve.Evaluate(Mathf.Clamp01(t / dur));

                if (rectTransform != null)
                {
                    Vector2 pos = useCurvedPath
                        ? EvaluateQuadraticBezier(start, control, end, u)
                        : Vector2.LerpUnclamped(start, end, u);

                    rectTransform.anchoredPosition = pos;

                    float s = Mathf.Lerp(1f, endScaleMultiplier, u);
                    rectTransform.localScale = new Vector3(s, s, 1f);
                }

                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, u);

                yield return null;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = end;
                rectTransform.localScale = new Vector3(endScaleMultiplier, endScaleMultiplier, 1f);
            }

            if (canvasGroup != null)
                canvasGroup.alpha = endAlpha;

            onArrived?.Invoke();
            Destroy(gameObject);
        }

        private static Vector2 EvaluateQuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float omt = 1f - t;
            return omt * omt * a + 2f * omt * t * b + t * t * c;
        }
    }
}

================================================================================
FILE: WorldToUIFollow.cs
PATH: Assets/Scripts/CardBattle/UI/World/WorldToUIFollow.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    public class WorldToUIFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 worldOffset = Vector3.zero;

        [Header("UI References")]
        [SerializeField] private RectTransform uiRectTransform;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private Camera targetCamera;

        [Header("Options")]
        [SerializeField] private bool hideWhenBehindCamera = true;

        private void Reset()
        {
            uiRectTransform = transform as RectTransform;
            parentCanvas = GetComponentInParent<Canvas>();

            if (Camera.main != null)
                targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (target == null || uiRectTransform == null || parentCanvas == null)
                return;

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera == null)
                return;

            Vector3 worldPos = target.position + worldOffset;
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            if (hideWhenBehindCamera && screenPos.z < 0f)
            {
                if (uiRectTransform.gameObject.activeSelf)
                    uiRectTransform.gameObject.SetActive(false);
                return;
            }

            if (!uiRectTransform.gameObject.activeSelf)
                uiRectTransform.gameObject.SetActive(true);

            RectTransform canvasRect = parentCanvas.transform as RectTransform;

            Camera eventCamera = null;
            if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = targetCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPos,
                    eventCamera,
                    out Vector2 localPoint))
            {
                uiRectTransform.localPosition = localPoint;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetParentCanvas(Canvas canvas)
        {
            parentCanvas = canvas;
        }

        public void SetTargetCamera(Camera cam)
        {
            targetCamera = cam;
        }
    }
}

================================================================================
FILE: FloatingTextUI.cs
PATH: Assets/Scripts/CardBattle/UI/Floating/FloatingTextUI.cs
================================================================================
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Simple floating TMP line: rises in canvas space, fades out, then self-destroys.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class FloatingTextUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float lifetime = 1.2f;
        [SerializeField] private float riseSpeedPixelsPerSecond = 72f;

        private RectTransform _rect;
        private float _elapsed;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Configure display and reset motion state. Call right after instantiate.</summary>
        public void Play(string text, Color color)
        {
            if (label != null)
            {
                label.text = text;
                label.color = color;
            }

            _elapsed = 0f;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
        }

        private void Update()
        {
            if (_rect != null)
                _rect.anchoredPosition += Vector2.up * (riseSpeedPixelsPerSecond * Time.deltaTime);

            _elapsed += Time.deltaTime;
            float t = lifetime > 0f ? Mathf.Clamp01(_elapsed / lifetime) : 1f;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            if (_elapsed >= lifetime)
                Destroy(gameObject);
        }
    }
}


================================================================================
FILE: BattleFloatingTextSpawner.cs
PATH: Assets/Scripts/CardBattle/UI/Floating/BattleFloatingTextSpawner.cs
================================================================================
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Listens for damage/heal on the player and registered enemies; spawns floating text at a world anchor.
    /// </summary>
    public class BattleFloatingTextSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform container;
        [SerializeField] private FloatingTextUI floatingTextPrefab;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private EnemyActionSystem enemyActionSystem;

        [Header("Spawn anchors")]
        [SerializeField] private Transform playerAnchor;
        [SerializeField] private Vector3 defaultWorldOffset = new Vector3(0f, 0.5f, 0f);

        [Header("Screen space offsets")]
        [SerializeField] private Vector2 playerScreenOffset = Vector2.zero;
        [SerializeField] private Vector2 enemyScreenOffset = Vector2.zero;

        [Header("World cameras")]
        [Tooltip("Used for WorldToScreenPoint from world-space units. Falls back to Camera.main.")]
        [SerializeField] private Camera worldCamera;

        [Header("Presentation")]
        [SerializeField] private Color damageColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color healColor = new Color(0.45f, 1f, 0.55f, 1f);

        private void Awake()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private void OnEnable()
        {
            SubscribePlayer();
            SubscribeEnemies();
        }

        private void OnDisable()
        {
            UnsubscribePlayer();
            UnsubscribeEnemies();
        }

        /// <summary>Call after registering new enemies at runtime so they receive floating text.</summary>
        public void RefreshEnemySubscriptions()
        {
            UnsubscribeEnemies();
            SubscribeEnemies();
        }

        private void SubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent += HandleUnitDamageTaken;
            player.OnHealedEvent += HandleUnitHealed;
        }

        private void UnsubscribePlayer()
        {
            if (player == null)
                return;

            player.OnDamageTakenEvent -= HandleUnitDamageTaken;
            player.OnHealedEvent -= HandleUnitHealed;
        }

        private void SubscribeEnemies()
        {
            if (enemyActionSystem == null)
                return;

            var list = enemyActionSystem.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var enemy = list[i];
                if (enemy == null)
                    continue;

                enemy.OnDamageTakenEvent += HandleUnitDamageTaken;
                enemy.OnHealedEvent += HandleUnitHealed;
            }
        }

        private void UnsubscribeEnemies()
        {
            if (enemyActionSystem == null)
                return;

            var list = enemyActionSystem.Enemies;
            for (int i = 0; i < list.Count; i++)
            {
                var enemy = list[i];
                if (enemy == null)
                    continue;

                enemy.OnDamageTakenEvent -= HandleUnitDamageTaken;
                enemy.OnHealedEvent -= HandleUnitHealed;
            }
        }

        private void HandleUnitDamageTaken(BattleUnit unit, int amount)
        {
            SpawnAtUnit(unit, amount, false);
        }

        private void HandleUnitHealed(BattleUnit unit, int amount)
        {
            SpawnAtUnit(unit, amount, true);
        }

        private void SpawnAtUnit(BattleUnit unit, int amount, bool isHeal)
        {
            if (unit == null || floatingTextPrefab == null || container == null)
                return;

            Vector3 worldPos = GetSpawnWorldPosition(unit);
            if (!TryWorldToContainerLocal(worldPos, out Vector2 localPoint))
                return;

            string text = isHeal ? $"+{amount}" : $"-{amount}";
            Color color = isHeal ? healColor : damageColor;

            var instance = Instantiate(floatingTextPrefab, container);
            var rt = instance.GetComponent<RectTransform>();
            if (rt != null)
                rt.anchoredPosition = localPoint + GetScreenOffset(unit);

            instance.Play(text, color);
        }

        private Vector2 GetScreenOffset(BattleUnit unit)
        {
            if (unit == null)
                return Vector2.zero;

            if (player != null && unit == player)
                return playerScreenOffset;

            if (unit is EnemyBattleUnit)
                return enemyScreenOffset;

            return Vector2.zero;
        }

        private Vector3 GetSpawnWorldPosition(BattleUnit unit)
        {
            if (unit is EnemyBattleUnit enemy)
            {
                if (enemy.UIAnchorDamage != null)
                    return enemy.UIAnchorDamage.position;

                if (enemy.UIAnchorHP != null)
                    return enemy.UIAnchorHP.position;
            }

            if (player != null && unit == player && playerAnchor != null)
                return playerAnchor.position;

            return unit.transform.position + defaultWorldOffset;
        }

        private bool TryWorldToContainerLocal(Vector3 worldPosition, out Vector2 localPoint)
        {
            localPoint = default;

            if (canvas == null)
                canvas = container.GetComponentInParent<Canvas>();
            if (canvas == null)
                return false;

            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            if (cam == null)
                return false;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPosition);
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera != null ? canvas.worldCamera : cam;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPoint, eventCamera, out localPoint);
        }
    }
}
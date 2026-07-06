## FILE: BattleHUDController.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/BattleHUDController.cs`
```csharp
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

        [Header("Audio")]
        [SerializeField] private UISFXController uiSfx;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerApText;
        [SerializeField] private TextMeshProUGUI playerHpText;
        [SerializeField] private GameObject playerBlockRoot;
        [SerializeField] private TextMeshProUGUI playerBlockText;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private HpBarUI playerHpBar;
        [SerializeField] private TargetSelectionSystem targetSelectionSystem;
        [SerializeField] private GameObject buffRoot;
        [SerializeField] private TextMeshProUGUI buffText;

        [Header("Status UI")]
        [SerializeField] private BattleStatusTextUI playerStatusTextUI;

        [Header("Status Icon UI")]
        [SerializeField] private BattleStatusIconPanelUI playerStatusIconPanelUI;

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
                UpdateBuffUI(player.DebugBuffCount);

                if (playerStatusTextUI != null)
                    playerStatusTextUI.SetTarget(player);

                if (playerStatusIconPanelUI != null)
                    playerStatusIconPanelUI.SetTarget(player);
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
                player.OnDebugBuffChanged += UpdateBuffUI;
                UpdateBuffUI(player.DebugBuffCount);
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
                player.OnDebugBuffChanged -= UpdateBuffUI;
            }

            if (battleActionRunner != null)
                battleActionRunner.OnBusyStateChanged -= HandleBusyStateChanged;
        }

        private void OnClickEndTurn()
        {
            if (battleActionRunner == null || player == null)
                return;

            if (!player.CanAct || !player.IsAlive || !battleActionRunner.CanAcceptInput)
                return;

            if (targetSelectionSystem != null && targetSelectionSystem.IsSelectingTarget)
                targetSelectionSystem.CancelTargetSelection();

            uiSfx?.PlayEndTurn();
            battleActionRunner.TryEndTurn();
        }

        public void RefreshUIExternal()
        {
            RefreshEndTurnButtonState();
            playerStatusTextUI?.Refresh();
            playerStatusIconPanelUI?.Refresh();
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

        private void UpdateBuffUI(int value)
        {
            if (buffRoot == null || buffText == null)
                return;

            if (value > 0)
            {
                buffRoot.SetActive(true);
                buffText.text = value.ToString();
            }
            else
            {
                buffRoot.SetActive(false);
            }
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
```

## FILE: BattleStatusTextUI.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/BattleStatusTextUI.cs`
```csharp
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleStatusTextUI : MonoBehaviour
    {
        [SerializeField] private BattleUnit target;
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private bool hideWhenEmpty = true;
        [SerializeField] private bool refreshOnStart = true;
        [SerializeField] private bool verboseLogs = false;

        private StatusController subscribedStatusController;

        public BattleUnit Target => target;

        private void Start()
        {
            if (refreshOnStart)
                Refresh();
        }

        private void OnEnable()
        {
            SubscribeStatusController();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeStatusController();
        }

        private void OnDestroy()
        {
            UnsubscribeStatusController();
        }

        public void SetTarget(BattleUnit unit)
        {
            if (target == unit)
            {
                Refresh();
                return;
            }

            UnsubscribeStatusController();
            target = unit;
            SubscribeStatusController();
            Refresh();
        }

        public void ClearTarget()
        {
            SetTarget(null);
        }

        public void Refresh()
        {
            if (target == null ||
                !target.IsAlive ||
                target.StatusController == null)
            {
                ShowEmpty();
                return;
            }

            string displayText = ResolveDisplayText(target.StatusController);
            if (string.IsNullOrEmpty(displayText))
            {
                ShowEmpty();
                return;
            }

            if (statusText != null)
                statusText.text = displayText;

            if (root != null)
                root.SetActive(true);

            if (verboseLogs)
                Debug.Log($"[BattleStatusTextUI] {target.name}: {displayText}", this);
        }

        private void SubscribeStatusController()
        {
            if (target?.StatusController == null || subscribedStatusController == target.StatusController)
                return;

            UnsubscribeStatusController();
            subscribedStatusController = target.StatusController;
            subscribedStatusController.OnStatusesChanged += HandleStatusesChanged;
        }

        private void UnsubscribeStatusController()
        {
            if (subscribedStatusController == null)
                return;

            subscribedStatusController.OnStatusesChanged -= HandleStatusesChanged;
            subscribedStatusController = null;
        }

        private void HandleStatusesChanged()
        {
            Refresh();
        }

        private void ShowEmpty()
        {
            if (statusText != null)
                statusText.text = string.Empty;

            if (root != null && hideWhenEmpty)
                root.SetActive(false);
        }

        private static string ResolveDisplayText(StatusController controller)
        {
            if (controller == null)
                return string.Empty;

            string displayText = controller.BuildStatusDisplayText();
            if (!string.IsNullOrEmpty(displayText))
                return displayText;

            string debugText = controller.BuildDebugText();
            return debugText == "(none)" ? string.Empty : debugText;
        }

#if UNITY_EDITOR
        [ContextMenu("Debug Refresh Status UI")]
        private void DebugRefreshStatusUI()
        {
            Refresh();
        }
#endif
    }
}
```

## FILE: HpBarUI.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/HpBarUI.cs`
```csharp
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
```

## FILE: PlayerHpBarBinder.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/PlayerHpBarBinder.cs`
```csharp
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
```

## FILE: PileCounterUI.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/PileCounterUI.cs`
```csharp
using System.Collections;
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Displays deck / graveyard counts with buffered presentation hooks for pile VFX.
    /// </summary>
    public class PileCounterUI : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private DeckController deckController;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI deckCountText;
        [SerializeField] private TextMeshProUGUI graveyardCountText;
        [SerializeField] private RectTransform deckAnchor;
        [SerializeField] private RectTransform graveyardAnchor;

        [Header("Display")]
        [SerializeField] private bool initializeDisplayFromRealOnStart = true;
        [SerializeField] private bool punchGraveyardLabelOnGhostArrival = true;
        [SerializeField] private float punchScale = 1.12f;
        [SerializeField] private float punchDuration = 0.12f;

        private int realDeckCount;
        private int displayedDeckCount;
        private int realGraveyardCount;
        private int displayedGraveyardCount;
        private bool reshufflePresentationActive;
        private Vector3 graveyardLabelBaseScale = Vector3.one;
        private Coroutine punchRoutine;

        public RectTransform DeckAnchor => deckAnchor;
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

            realDeckCount = deckController.Deck.Count;
            realGraveyardCount = deckController.Graveyard.Count;
            displayedDeckCount = realDeckCount;
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

            realDeckCount = deckController.Deck.Count;
            realGraveyardCount = deckController.Graveyard.Count;
            if (!reshufflePresentationActive)
            {
                displayedDeckCount = realDeckCount;
                if (displayedGraveyardCount > realGraveyardCount)
                    displayedGraveyardCount = realGraveyardCount;
            }

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
            {
                realDeckCount = deckController.Deck.Count;
                realGraveyardCount = deckController.Graveyard.Count;
            }
            displayedDeckCount = realDeckCount;
            displayedGraveyardCount = realGraveyardCount;
            reshufflePresentationActive = false;
            RefreshGraveyardText();
            RefreshDeckText();
        }

        private void RefreshDeckText()
        {
            if (deckCountText != null)
                deckCountText.text = displayedDeckCount.ToString();
        }

        private void RefreshGraveyardText()
        {
            if (graveyardCountText != null)
                graveyardCountText.text = displayedGraveyardCount.ToString();
        }

        /// <summary>Locks deck/graveyard display into reshuffle presentation mode.</summary>
        public void BeginReshufflePresentation(int transferCount)
        {
            if (transferCount <= 0)
                return;

            if (deckController != null)
            {
                realDeckCount = deckController.Deck.Count;
                realGraveyardCount = deckController.Graveyard.Count;
            }

            displayedDeckCount = realDeckCount;
            displayedGraveyardCount = Mathf.Min(displayedGraveyardCount, realGraveyardCount);
            reshufflePresentationActive = true;
            RefreshDeckText();
            RefreshGraveyardText();
        }

        /// <summary>Presentation step: a reshuffle ghost leaves graveyard, so graveyard display drops immediately.</summary>
        public void OnReshuffleGhostLaunched(int amount)
        {
            if (amount <= 0)
                return;

            if (!reshufflePresentationActive)
                BeginReshufflePresentation(amount);

            displayedGraveyardCount = Mathf.Max(0, displayedGraveyardCount - amount);
            RefreshGraveyardText();
        }

        /// <summary>Presentation step: a reshuffle ghost reaches deck, so deck display increases on arrival.</summary>
        public void OnReshuffleGhostArrivedAtDeck(int amount)
        {
            if (amount <= 0)
                return;

            if (!reshufflePresentationActive)
                BeginReshufflePresentation(amount);

            displayedDeckCount += amount;
            RefreshDeckText();
        }

        /// <summary>Ends reshuffle presentation mode and snaps display to real piles.</summary>
        public void CompleteReshufflePresentation()
        {
            reshufflePresentationActive = false;
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
```

## FILE: GraveyardToDeckVFXController.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/GraveyardToDeckVFXController.cs`
```csharp
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
```

## FILE: CardToGraveyardVFXController.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/CardToGraveyardVFXController.cs`
```csharp
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
```

## FILE: FlyingCardGhostUI.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/FlyingCardGhostUI.cs`
```csharp
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
```

## FILE: TargetGuideLineUI.cs
**Path:** `Assets/Scripts/CardBattle/UI/HUD/TargetGuideLineUI.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class TargetGuideLineUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform container;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private Camera worldCamera;

        [Header("Line")]
        [SerializeField] private Image segmentPrefab;
        [SerializeField] private int segmentCount = 16;
        [SerializeField] private float curveHeight = 120f;
        [SerializeField] private float followSmoothTime = 0.06f;
        [SerializeField] private float stretchMultiplier = 0.15f;
        [SerializeField] private float maxStretch = 40f;

        private readonly List<RectTransform> segments = new List<RectTransform>();

        private RectTransform startRect;
        private Transform worldStartTarget;
        private Vector2 currentEndPos;
        private Vector2 targetEndPos;
        private Vector2 velocity;
        private bool isVisible;

        private void Awake()
        {
            BuildSegments();
            Hide();
        }

        private void Update()
        {
            if (!isVisible)
                return;

            Vector2 startPos;

            if (worldStartTarget != null)
            {
                Camera cam = GetWorldCamera();
                if (cam == null)
                    return;

                Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldStartTarget.position);
                startPos = ScreenToLocal(screen);
            }
            else
            {
                if (startRect == null)
                    return;

                startPos = GetAnchoredPos(startRect);
            }

            float smoothTime = Mathf.Max(0.0001f, followSmoothTime);
            currentEndPos = Vector2.SmoothDamp(
                currentEndPos,
                targetEndPos,
                ref velocity,
                smoothTime
            );

            Vector2 delta = targetEndPos - currentEndPos;
            float stretch = Mathf.Clamp(delta.magnitude * stretchMultiplier, 0f, maxStretch);
            Vector2 stretchedEnd = currentEndPos;
            if (delta.sqrMagnitude > 0.0001f)
                stretchedEnd += delta.normalized * stretch;

            DrawCurve(startPos, stretchedEnd);
        }

        // ========================
        // PUBLIC
        // ========================

        public void ShowFromCard(RectTransform cardRect)
        {
            if (cardRect == null)
                return;

            worldStartTarget = null;
            startRect = cardRect;
            isVisible = true;
            targetEndPos = currentEndPos;
            velocity = Vector2.zero;

            SetSegmentsActive(true);
        }

        public void ShowFromWorld(Transform worldStart)
        {
            if (worldStart == null)
                return;

            startRect = null;
            worldStartTarget = worldStart;
            isVisible = true;
            targetEndPos = currentEndPos;
            velocity = Vector2.zero;
            SetSegmentsActive(true);
        }

        public void UpdateTowardScreen(Vector2 screenPos)
        {
            if (!isVisible)
                return;

            targetEndPos = ScreenToLocal(screenPos);
        }

        public void UpdateTowardWorld(Transform target)
        {
            if (!isVisible || target == null)
                return;

            Camera cam = GetWorldCamera();
            if (cam == null)
                return;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
            targetEndPos = ScreenToLocal(screen);
        }

        public void UpdateTowardEnemy(Transform enemyAnchor)
        {
            UpdateTowardWorld(enemyAnchor);
        }

        public void Hide()
        {
            isVisible = false;
            worldStartTarget = null;
            startRect = null;
            velocity = Vector2.zero;
            SetSegmentsActive(false);
        }

        // ========================
        // CORE DRAW
        // ========================

        private void DrawCurve(Vector2 start, Vector2 end)
        {
            Vector2 control = (start + end) * 0.5f + Vector2.up * curveHeight;

            for (int i = 0; i < segmentCount; i++)
            {
                float t = i / (float)(segmentCount - 1);
                Vector2 pos = EvaluateBezier(start, control, end, t);

                segments[i].anchoredPosition = pos;
            }
        }

        private Vector2 EvaluateBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float omt = 1f - t;
            return omt * omt * a + 2f * omt * t * b + t * t * c;
        }

        // ========================
        // HELPERS
        // ========================

        private void BuildSegments()
        {
            if (segmentPrefab == null || container == null || segmentCount <= 0)
                return;

            for (int i = 0; i < segmentCount; i++)
            {
                var seg = Instantiate(segmentPrefab, container);
                segments.Add(seg.rectTransform);
            }
        }

        private void SetSegmentsActive(bool value)
        {
            foreach (var s in segments)
            {
                if (s != null)
                    s.gameObject.SetActive(value);
            }
        }

        private Vector2 GetAnchoredPos(RectTransform rect)
        {
            if (rect == null)
                return Vector2.zero;

            Camera cam = GetWorldCamera();
            if (cam == null)
                return Vector2.zero;

            Vector3 worldCenter = rect.TransformPoint(rect.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

            return ScreenToLocal(screen);
        }

        private Vector2 ScreenToLocal(Vector2 screen)
        {
            if (container == null)
                return Vector2.zero;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container,
                screen,
                GetUiEventCamera(),
                out var local);

            return local;
        }

        private Camera GetWorldCamera()
        {
            if (worldCamera != null)
                return worldCamera;

            return Camera.main;
        }

        private Camera GetUiEventCamera()
        {
            if (canvas == null)
                return null;

            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera;
        }
    }
}
```

## FILE: WorldToUIFollow.cs
**Path:** `Assets/Scripts/CardBattle/UI/World/WorldToUIFollow.cs`
```csharp
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
            if (target == null ||
                !target.gameObject.activeInHierarchy ||
                uiRectTransform == null ||
                parentCanvas == null)
            {
                if (uiRectTransform != null && uiRectTransform.gameObject.activeSelf)
                    uiRectTransform.gameObject.SetActive(false);

                return;
            }

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
```

## FILE: FloatingTextUI.cs
**Path:** `Assets/Scripts/CardBattle/UI/Floating/FloatingTextUI.cs`
```csharp
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
```

## FILE: BattleFloatingTextSpawner.cs
**Path:** `Assets/Scripts/CardBattle/UI/Floating/BattleFloatingTextSpawner.cs`
```csharp
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
```

## FILE: TurnPresentationController.cs
**Path:** `Assets/Scripts/CardBattle/UI/Presentation/TurnPresentationController.cs`
```csharp
using System.Collections;
using TMPro;
using UnityEngine;

namespace CardBattle.Core
{
    /// <summary>
    /// Presentation-only turn intro banner. Shows BATTLE START or TURN N before the player can act.
    /// </summary>
    public class TurnPresentationController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.15f;
        [SerializeField] private float holdDuration = 0.35f;
        [SerializeField] private float fadeOutDuration = 0.20f;

        [Header("Scale")]
        [SerializeField] private float startScale = 0.92f;
        [SerializeField] private float endScale = 1f;

        private Vector3 baseTextScale = Vector3.one;

        private void Awake()
        {
            if (turnText != null)
                baseTextScale = turnText.rectTransform.localScale;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            gameObject.SetActive(false);
        }

        public IEnumerator PlayTurnIntro(int turnNumber)
        {
            if (turnText == null || canvasGroup == null)
                yield break;

            turnText.text = turnNumber <= 1 ? "BATTLE START" : $"TURN {turnNumber}";

            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;

            var scaleTarget = turnText.rectTransform;
            scaleTarget.localScale = baseTextScale * startScale;

            float fadeIn = Mathf.Max(0.01f, fadeInDuration);
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeIn);
                canvasGroup.alpha = t;
                scaleTarget.localScale = baseTextScale * Mathf.Lerp(startScale, endScale, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            scaleTarget.localScale = baseTextScale * endScale;

            elapsed = 0f;
            while (elapsed < holdDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float fadeOut = Mathf.Max(0.01f, fadeOutDuration);
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOut);
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
```

## FILE: BattleStatusIconPanelUI.cs
**Path:** `Assets/Scripts/CardBattle/UI/Status/BattleStatusIconPanelUI.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    public class BattleStatusIconPanelUI : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private BattleUnit target;

        [Header("Database")]
        [SerializeField] private StatusIconDatabase iconDatabase;

        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private StatusIconSlotUI slotPrefab;

        [Header("Options")]
        [SerializeField] private bool hideWhenEmpty = true;
        [SerializeField] private bool refreshOnStart = true;
        [SerializeField] private bool verboseLogs;

        private StatusController subscribedStatusController;
        private readonly List<StatusDisplayData> displayBuffer = new();
        private readonly List<StatusIconSlotUI> spawnedSlots = new();

        private void Start()
        {
            if (refreshOnStart)
                Refresh();
        }

        private void OnEnable()
        {
            SubscribeStatusController();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeStatusController();
        }

        private void OnDestroy()
        {
            UnsubscribeStatusController();
            ClearSlots();
        }

        public void SetTarget(BattleUnit unit)
        {
            if (target == unit)
            {
                Refresh();
                return;
            }

            UnsubscribeStatusController();
            target = unit;
            SubscribeStatusController();
            Refresh();
        }

        public void ClearTarget()
        {
            SetTarget(null);
        }

        public void Refresh()
        {
            if (target == null ||
                !target.IsAlive ||
                target.StatusController == null)
            {
                ClearSlots();
                SetRootVisible(false);
                return;
            }

            int count = target.StatusController.BuildStatusDisplayData(displayBuffer);
            if (count <= 0)
            {
                ClearSlots();
                SetRootVisible(false);
                return;
            }

            if (slotPrefab == null || slotContainer == null)
            {
                if (verboseLogs)
                    Debug.LogWarning("[BattleStatusIconPanelUI] Missing slotPrefab or slotContainer.", this);

                ClearSlots();
                SetRootVisible(false);
                return;
            }

            SetRootVisible(true);
            RebuildSlots();
        }

        private void RebuildSlots()
        {
            ClearSlots();

            Sprite buffArrow = iconDatabase != null ? iconDatabase.BuffArrowIcon : null;
            Sprite debuffArrow = iconDatabase != null ? iconDatabase.DebuffArrowIcon : null;

            for (int i = 0; i < displayBuffer.Count; i++)
            {
                StatusDisplayData data = displayBuffer[i];
                StatusIconDatabase.StatusIconEntry entry = null;

                if (iconDatabase != null)
                    iconDatabase.TryGet(data.Type, out entry);

                StatusIconSlotUI slot = Instantiate(slotPrefab, slotContainer);
                slot.Bind(data, entry, buffArrow, debuffArrow);
                spawnedSlots.Add(slot);
            }

            if (verboseLogs)
                Debug.Log($"[BattleStatusIconPanelUI] Built {spawnedSlots.Count} status slots for {target.name}.", this);
        }

        private void ClearSlots()
        {
            for (int i = spawnedSlots.Count - 1; i >= 0; i--)
            {
                StatusIconSlotUI slot = spawnedSlots[i];
                if (slot == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(slot.gameObject);
                else
                    DestroyImmediate(slot.gameObject);
            }

            spawnedSlots.Clear();
        }

        private void SetRootVisible(bool visible)
        {
            if (root == null)
                return;

            if (!visible && hideWhenEmpty)
                root.SetActive(false);
            else if (visible)
                root.SetActive(true);
        }

        private void SubscribeStatusController()
        {
            if (target?.StatusController == null || subscribedStatusController == target.StatusController)
                return;

            UnsubscribeStatusController();
            subscribedStatusController = target.StatusController;
            subscribedStatusController.OnStatusesChanged += HandleStatusesChanged;
        }

        private void UnsubscribeStatusController()
        {
            if (subscribedStatusController == null)
                return;

            subscribedStatusController.OnStatusesChanged -= HandleStatusesChanged;
            subscribedStatusController = null;
        }

        private void HandleStatusesChanged()
        {
            Refresh();
        }
    }
}
```

## FILE: StatusIconDatabase.cs
**Path:** `Assets/Scripts/CardBattle/UI/Status/StatusIconDatabase.cs`
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle.Core
{
    [CreateAssetMenu(
        fileName = "StatusIconDatabase",
        menuName = "Card Battle/UI/Status Icon Database",
        order = 50)]
    public class StatusIconDatabase : ScriptableObject
    {
        [Serializable]
        public class StatusIconEntry
        {
            public StatusEffectType statusType;
            public Sprite icon;
            public string displayName;
            public bool isDebuff;
        }

        [SerializeField] private List<StatusIconEntry> entries = new();

        [Header("Direction Icons")]
        [SerializeField] private Sprite buffArrowIcon;
        [SerializeField] private Sprite debuffArrowIcon;

        public Sprite BuffArrowIcon => buffArrowIcon;
        public Sprite DebuffArrowIcon => debuffArrowIcon;

        public bool TryGet(StatusEffectType type, out StatusIconEntry entry)
        {
            entry = null;

            if (entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                StatusIconEntry candidate = entries[i];
                if (candidate == null || candidate.statusType != type)
                    continue;

                entry = candidate;
                return true;
            }

            return false;
        }
    }
}
```

## FILE: StatusIconSlotUI.cs
**Path:** `Assets/Scripts/CardBattle/UI/Status/StatusIconSlotUI.cs`
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class StatusIconSlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image statusIconImage;
        [SerializeField] private Image directionIconImage;
        [SerializeField] private TextMeshProUGUI numberText;

        [Header("Options")]
        [SerializeField] private bool hideNumberWhenZero = true;

        public void Bind(
            StatusDisplayData data,
            StatusIconDatabase.StatusIconEntry entry,
            Sprite buffArrowIcon,
            Sprite debuffArrowIcon)
        {
            if (statusIconImage != null)
            {
                Sprite statusSprite = entry != null ? entry.icon : null;
                statusIconImage.sprite = statusSprite;
                statusIconImage.enabled = statusSprite != null;
            }

            if (directionIconImage != null)
            {
                Sprite directionSprite = data.IsDebuff ? debuffArrowIcon : buffArrowIcon;
                directionIconImage.sprite = directionSprite;
                directionIconImage.enabled = directionSprite != null;
            }

            if (numberText != null)
            {
                bool showNumber = !hideNumberWhenZero || data.DisplayNumber > 0;
                numberText.gameObject.SetActive(showNumber);

                if (showNumber)
                    numberText.text = data.DisplayNumber.ToString();
                else
                    numberText.text = string.Empty;
            }
        }

        public void Clear()
        {
            if (statusIconImage != null)
            {
                statusIconImage.sprite = null;
                statusIconImage.enabled = false;
            }

            if (directionIconImage != null)
            {
                directionIconImage.sprite = null;
                directionIconImage.enabled = false;
            }

            if (numberText != null)
            {
                numberText.text = string.Empty;
                numberText.gameObject.SetActive(false);
            }
        }
    }
}
```
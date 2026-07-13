using UnityEngine;
using UnityEngine.EventSystems;

namespace CardBattle.Core
{
    public class TargetSelectionSystem : MonoBehaviour
    {
        public enum GuideStartSource
        {
            Card,
            Player
        }

        [SerializeField] private GuideStartSource guideStartSource = GuideStartSource.Card;
        [SerializeField] private PlayerBattleUnit player;
        [SerializeField] private HandUIController handUIController;
        [SerializeField] private BattleActionRunner battleActionRunner;
        [SerializeField] private EnemyTargetHighlight[] enemyHighlights;
        [SerializeField] private TargetGuideLineUI guideLine;
        [SerializeField] private RectTransform handContainer;
        [SerializeField] private float handPaddingX = 30f;
        [SerializeField] private float handPaddingY = 20f;

        [Header("Battle State")]
        [SerializeField] private BattleOutcomeController battleOutcomeController;

        public bool IsSelectingTarget => _pendingCard != null;

        private CardInstance _pendingCard;
        private RectTransform _selectedCardRect;
        private RectTransform _selectedCardGuideStartAnchor;
        private bool _canShowGuideLine;

        private bool HasBattleEnded =>
            battleOutcomeController != null &&
            battleOutcomeController.IsBattleEnded;

        private void OnEnable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void OnDisable()
        {
            if (battleOutcomeController != null)
                battleOutcomeController.OnBattleEnded -= HandleBattleEnded;

            ForceCancelTargetSelection();
        }

        private void Update()
        {
            if (HasBattleEnded)
            {
                if (IsSelectingTarget)
                    ForceCancelTargetSelection();

                return;
            }

            if (!IsSelectingTarget)
                return;

            // คลิกขวา
            if (Input.GetMouseButtonDown(1))
            {
                CancelTargetSelection();
                return;
            }

            // กด ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargetSelection();
                return;
            }

            bool insideSelectedCard = IsPointerInsideHandArea();
            if (insideSelectedCard)
            {
                _canShowGuideLine = false;
                if (guideLine != null)
                    guideLine.Hide();
                return;
            }

            if (!_canShowGuideLine)
            {
                _canShowGuideLine = true;
                ShowGuideLineFromCurrentStartSource();
            }

            if (_canShowGuideLine && guideLine != null)
            {
                Transform hoveredEnemyAnchor = GetHoveredEnemyAnchor();
                if (hoveredEnemyAnchor != null)
                    guideLine.UpdateTowardEnemy(hoveredEnemyAnchor);
                else
                    guideLine.UpdateTowardScreen(Input.mousePosition);
            }
        }

        /// <summary>This flow only supports picking one enemy; gate entry by card rules.</summary>
        private bool CanUseSingleEnemySelection(CardData data)
        {
            if (data == null)
                return false;

            return data.TargetMode == CardTargetMode.SingleEnemy;
        }

        public void BeginTargetSelection(CardInstance card)
        {
            if (HasBattleEnded)
                return;

            if (card?.Data == null)
                return;

            if (!CanUseSingleEnemySelection(card.Data))
                return;

            _pendingCard = card;
            _canShowGuideLine = false;
            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;

            SetHighlight(true);

            if (handUIController != null)
            {
                var view = handUIController.GetViewForCard(card);
                if (view != null)
                {
                    _selectedCardRect = view.LayoutRect;
                    _selectedCardGuideStartAnchor = view.GuideStartAnchor != null
                        ? view.GuideStartAnchor
                        : null;
                }
            }

            if (guideLine != null)
                guideLine.Hide();

            Debug.Log($"Selecting target for card: {card.Data.DisplayName}");
        }

        public void CancelTargetSelection()
        {
            if (_pendingCard == null)
                return;

            Debug.Log("Target selection cancelled.");
            ForceCancelTargetSelection();
        }

        public void ForceCancelTargetSelection()
        {
            _pendingCard = null;
            SetHighlight(false);

            if (handUIController != null)
                handUIController.DeselectCurrentCard();

            if (guideLine != null)
                guideLine.Hide();

            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;
            _canShowGuideLine = false;
        }

        public void ConfirmTarget(EnemyBattleUnit target)
        {
            if (HasBattleEnded ||
                _pendingCard == null ||
                battleActionRunner == null ||
                target == null ||
                !target.IsAlive)
            {
                return;
            }

            SetHighlight(false);

            battleActionRunner.TryPlayCard(_pendingCard, target);
            _pendingCard = null;

            if (guideLine != null)
                guideLine.Hide();

            _selectedCardRect = null;
            _selectedCardGuideStartAnchor = null;
            _canShowGuideLine = false;
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            ForceCancelTargetSelection();
        }

        public void SetEnemyHighlights(EnemyTargetHighlight[] highlights)
        {
            enemyHighlights = highlights;
        }

        private void SetHighlight(bool value)
        {
            if (enemyHighlights == null)
                return;

            for (int i = 0; i < enemyHighlights.Length; i++)
            {
                if (enemyHighlights[i] != null)
                    enemyHighlights[i].SetSelectable(value);
            }
        }

        private Transform GetHoveredEnemyAnchor()
        {
            if (EventSystem.current == null)
                return null;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            for (int i = 0; i < raycastResults.Count; i++)
            {
                var go = raycastResults[i].gameObject;
                if (go == null)
                    continue;

                var unit = go.GetComponentInParent<EnemyBattleUnit>();
                if (unit != null && unit.IsAlive)
                {
                    if (unit.UIAnchorTargetGuide != null)
                        return unit.UIAnchorTargetGuide;

                    if (unit.UIAnchorDamage != null)
                        return unit.UIAnchorDamage;

                    if (unit.UIAnchorHP != null)
                        return unit.UIAnchorHP;

                    return unit.transform;
                }
            }

            // TODO: Swap to a dedicated hover-tracking source if introduced later.
            return null;
        }

        private bool IsPointerInsideHandArea()
        {
            if (handContainer == null)
                return false;

            Canvas canvas = handContainer.GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = canvas.worldCamera;

            Vector3[] corners = new Vector3[4];
            handContainer.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);

            min.x -= handPaddingX;
            min.y -= handPaddingY;
            max.x += handPaddingX;
            max.y += handPaddingY;

            Vector2 mouse = Input.mousePosition;
            return mouse.x >= min.x && mouse.x <= max.x &&
                   mouse.y >= min.y && mouse.y <= max.y;
        }

        private void ShowGuideLineFromCurrentStartSource()
        {
            if (guideLine == null)
                return;

            if (guideStartSource == GuideStartSource.Card)
            {
                RectTransform startAnchor = _selectedCardGuideStartAnchor != null
                    ? _selectedCardGuideStartAnchor
                    : _selectedCardRect;

                if (startAnchor != null)
                    guideLine.ShowFromCard(startAnchor);
            }
            else
            {
                if (player != null)
                {
                    Transform startWorld = player.UIAnchorTargetGuide != null
                        ? player.UIAnchorTargetGuide
                        : player.transform;

                    if (startWorld != null)
                        guideLine.ShowFromWorld(startWorld);
                }
            }
        }
    }
}

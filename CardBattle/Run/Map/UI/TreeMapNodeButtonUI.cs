using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardBattle.Core
{
    public class TreeMapNodeButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject selectedRoot;
        [SerializeField] private GameObject completedRoot;
        [SerializeField] private GameObject lockedRoot;
        [SerializeField] private GameObject availableRoot;
        [SerializeField] private GameObject currentRoot;

        [Header("State Colors")]
        [SerializeField] private Color lockedColor = new Color(0.25f, 0.25f, 0.28f, 1f);
        [SerializeField] private Color availableColor = new Color(0.85f, 0.72f, 0.35f, 1f);
        [SerializeField] private Color currentColor = new Color(0.35f, 0.65f, 0.9f, 1f);
        [SerializeField] private Color completedColor = new Color(0.45f, 0.62f, 0.48f, 1f);

        public string NodeId { get; private set; } = string.Empty;
        public MapNodeData NodeData { get; private set; }
        public MapNodeState CurrentState { get; private set; } = MapNodeState.Locked;

        public event Action<string> OnNodeClicked;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleButtonClicked);
                button.onClick.AddListener(HandleButtonClicked);
            }
        }

        public void Bind(MapNodeData nodeData)
        {
            NodeData = nodeData;
            NodeId = nodeData != null ? nodeData.NodeId : string.Empty;

            if (titleText != null)
            {
                titleText.text = nodeData != null ? nodeData.DisplayName : string.Empty;
            }

            if (typeText != null)
            {
                typeText.text = nodeData != null ? nodeData.NodeType.ToString() : string.Empty;
            }
        }

        public void RefreshState(MapNodeState state, bool isSelected, bool canStartBattle)
        {
            CurrentState = state;

            if (stateText != null)
                stateText.text = state.ToString();

            if (selectedRoot != null)
                selectedRoot.SetActive(isSelected);

            if (lockedRoot != null)
                lockedRoot.SetActive(state == MapNodeState.Locked);

            if (availableRoot != null)
                availableRoot.SetActive(state == MapNodeState.Available);

            if (currentRoot != null)
                currentRoot.SetActive(state == MapNodeState.Current);

            if (completedRoot != null)
                completedRoot.SetActive(state == MapNodeState.Completed);

            Color background = state switch
            {
                MapNodeState.Locked => lockedColor,
                MapNodeState.Available => availableColor,
                MapNodeState.Current => currentColor,
                MapNodeState.Completed => completedColor,
                _ => lockedColor
            };

            if (backgroundImage != null)
                backgroundImage.color = background;

            float alpha = state switch
            {
                MapNodeState.Locked => 0.35f,
                MapNodeState.Completed => 0.75f,
                MapNodeState.Available => 1f,
                MapNodeState.Current => 1f,
                _ => 1f
            };

            if (canvasGroup != null)
                canvasGroup.alpha = alpha;

            SetInteractable(canStartBattle);
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }

        public void Clear()
        {
            NodeId = string.Empty;
            NodeData = null;
            CurrentState = MapNodeState.Locked;

            if (titleText != null)
                titleText.text = string.Empty;

            if (typeText != null)
                typeText.text = string.Empty;

            if (stateText != null)
                stateText.text = string.Empty;

            if (selectedRoot != null)
                selectedRoot.SetActive(false);

            if (lockedRoot != null)
                lockedRoot.SetActive(false);

            if (availableRoot != null)
                availableRoot.SetActive(false);

            if (currentRoot != null)
                currentRoot.SetActive(false);

            if (completedRoot != null)
                completedRoot.SetActive(false);

            SetInteractable(false);
        }

        private void HandleButtonClicked()
        {
            if (string.IsNullOrWhiteSpace(NodeId))
                return;

            OnNodeClicked?.Invoke(NodeId);
        }
    }
}

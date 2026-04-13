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
    }
}
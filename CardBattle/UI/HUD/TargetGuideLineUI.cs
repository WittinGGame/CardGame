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
        [SerializeField] private float followSpeed = 15f;

        private readonly List<RectTransform> segments = new List<RectTransform>();

        private RectTransform startRect;
        private Transform worldStartTarget;
        private Vector2 currentEnd;
        private Vector2 targetEnd;
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

            float t = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            currentEnd = Vector2.Lerp(currentEnd, targetEnd, t);

            DrawCurve(startPos, currentEnd);
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
            targetEnd = currentEnd;

            SetSegmentsActive(true);
        }

        public void ShowFromWorld(Transform worldStart)
        {
            if (worldStart == null)
                return;

            startRect = null;
            worldStartTarget = worldStart;
            isVisible = true;
            targetEnd = currentEnd;
            SetSegmentsActive(true);
        }

        public void UpdateTowardScreen(Vector2 screenPos)
        {
            if (!isVisible)
                return;

            targetEnd = ScreenToLocal(screenPos);
        }

        public void UpdateTowardWorld(Transform target)
        {
            if (!isVisible || target == null)
                return;

            Camera cam = GetWorldCamera();
            if (cam == null)
                return;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
            targetEnd = ScreenToLocal(screen);
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
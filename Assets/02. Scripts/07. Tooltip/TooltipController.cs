using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Utilities;

namespace Arcanist
{
    public class TooltipController : MonoBehaviour
    {
        public static TooltipController Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform tooltipRect;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Position")]
        [SerializeField] private Vector2 cursorOffset = new Vector2(24.0f, 24.0f);
        [SerializeField] private Vector2 screenPadding = new Vector2(24.0f, 24.0f);

        private Camera uiCamera;
        private bool isVisible;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (tooltipRect == null)
                tooltipRect = transform as RectTransform;


            uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (isVisible) FollowCursor();
        }

        public void Show(CardData cardData)
        {
            if (cardData == null)
            {
                Hide();
                return;
            }

            Show(cardData.Icon, cardData.CardName, cardData.TypeText, cardData.Description);
        }

        public void Show(Sprite icon, string itemName, string itemType, string description)
        {
            if (canvasGroup == null || tooltipRect == null)
                return;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
                nameText.text = itemName;

            if (typeText != null)
                typeText.text = itemType;

            if (descriptionText != null)
                descriptionText.text = description;

            isVisible = true;
            canvasGroup.alpha = 1.0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            FollowCursor();
        }

        public void Hide()
        {
            isVisible = false;

            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0.0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private void FollowCursor()
        {
            if (rootCanvas == null || tooltipRect == null)
                return;

            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            Vector2 screenPosition = GetTooltipScreenPosition(SimpleInput.GetMousePosition());

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPosition
            );

            tooltipRect.anchoredPosition = localPosition;
            ClampToCanvas(canvasRect);
        }

        private Vector2 GetTooltipScreenPosition(Vector2 mousePosition)
        {
            float width = tooltipRect.rect.width * rootCanvas.scaleFactor * tooltipRect.localScale.x;
            float height = tooltipRect.rect.height * rootCanvas.scaleFactor * tooltipRect.localScale.y;

            bool showRight = mousePosition.x + cursorOffset.x + width <= Screen.width - screenPadding.x;
            bool showDown = mousePosition.y - cursorOffset.y - height >= screenPadding.y;

            float pivotX = showRight ? 0.0f : 1.0f;
            float pivotY = showDown ? 1.0f : 0.0f;
            tooltipRect.pivot = new Vector2(pivotX, pivotY);

            float x = showRight
                ? mousePosition.x + cursorOffset.x
                : mousePosition.x - cursorOffset.x;

            float y = showDown
                ? mousePosition.y - cursorOffset.y
                : mousePosition.y + cursorOffset.y;

            return new Vector2(x, y);
        }

        private void ClampToCanvas(RectTransform canvasRect)
        {
            if (canvasRect == null || tooltipRect == null)
                return;

            Vector2 anchoredPosition = tooltipRect.anchoredPosition;
            Vector2 size = tooltipRect.rect.size;
            Vector2 pivot = tooltipRect.pivot;
            Rect canvasRectValue = canvasRect.rect;

            float left = anchoredPosition.x - size.x * pivot.x;
            float right = anchoredPosition.x + size.x * (1.0f - pivot.x);
            float bottom = anchoredPosition.y - size.y * pivot.y;
            float top = anchoredPosition.y + size.y * (1.0f - pivot.y);

            float minX = canvasRectValue.xMin + screenPadding.x;
            float maxX = canvasRectValue.xMax - screenPadding.x;
            float minY = canvasRectValue.yMin + screenPadding.y;
            float maxY = canvasRectValue.yMax - screenPadding.y;

            if (left < minX)
                anchoredPosition.x += minX - left;
            else if (right > maxX)
                anchoredPosition.x -= right - maxX;

            if (bottom < minY)
                anchoredPosition.y += minY - bottom;
            else if (top > maxY)
                anchoredPosition.y -= top - maxY;

            tooltipRect.anchoredPosition = anchoredPosition;
        }
    }
}

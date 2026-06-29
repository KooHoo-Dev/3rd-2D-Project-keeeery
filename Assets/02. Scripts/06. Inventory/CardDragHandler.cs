using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arcanist
{
    [RequireComponent(typeof(InventoryCardSlot))]
    public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image dragIconPrefab;

        private InventoryCardSlot cardSlot;
        private Image dragIcon;
        private RectTransform dragIconRect;
        private Canvas rootCanvas;
        private RectTransform rootCanvasRect;
        private CanvasGroup canvasGroup;

        public InventoryCardSlot CardSlot => cardSlot;

        private void Awake()
        {
            cardSlot = GetComponent<InventoryCardSlot>();
            rootCanvas = GetComponentInParent<Canvas>();
            rootCanvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (cardSlot.CardData == null || !cardSlot.IsOwned || rootCanvas == null || rootCanvasRect == null)
                return;

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.6f;

            dragIcon = dragIconPrefab != null
                ? Instantiate(dragIconPrefab, rootCanvas.transform)
                : CreateDefaultDragIcon(rootCanvas.transform);

            dragIconRect = dragIcon.rectTransform;

            Image sourceIcon = cardSlot.CardIconImage;

            if (sourceIcon != null)
            {
                dragIcon.sprite = sourceIcon.sprite;
                dragIcon.color = sourceIcon.color;
                dragIcon.preserveAspect = sourceIcon.preserveAspect;
                dragIconRect.sizeDelta = sourceIcon.rectTransform.rect.size;
            }

            dragIcon.raycastTarget = false;
            dragIcon.transform.SetAsLastSibling();
            MoveDragIcon(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragIcon == null)
                return;

            MoveDragIcon(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            cardSlot.HandleDropped(eventData);

            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1.0f;

            if (dragIcon != null)
                Destroy(dragIcon.gameObject);

            dragIcon = null;
            dragIconRect = null;
        }

        private void MoveDragIcon(PointerEventData eventData)
        {
            if (dragIconRect == null || rootCanvasRect == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            dragIconRect.anchoredPosition = localPoint;
        }

        private Image CreateDefaultDragIcon(Transform parent)
        {
            GameObject iconObject = new GameObject("CardDragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            iconObject.transform.SetParent(parent, false);

            RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(64.0f, 64.0f);

            CanvasGroup canvasGroup = iconObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0.75f;
            canvasGroup.blocksRaycasts = false;

            return iconObject.GetComponent<Image>();
        }
    }
}

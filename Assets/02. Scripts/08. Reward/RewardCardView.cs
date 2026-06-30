using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arcanist
{
    public class RewardCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("UI")]
        [SerializeField] private Image frameImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text gradeText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject notObtainedObject;

        [Header("Grade Colors")]
        [SerializeField] private Color commonColor = Color.white;
        [SerializeField] private Color rareColor = new Color(0.30f, 0.64f, 1.0f);
        [SerializeField] private Color epicColor = new Color(0.62f, 0.31f, 0.93f);
        [SerializeField] private Color legendaryColor = new Color(1.0f, 0.82f, 0.40f);

        [Header("Interaction")]
        [SerializeField] private float hoverScale = 1.08f;
        [SerializeField] private Color pressedTint = new Color(0.8f, 0.8f, 0.8f, 1.0f);

        private CardData cardData;
        private RewardController rewardController;
        private CanvasGroup canvasGroup;
        private Color gradeColor;
        private Vector3 defaultScale;
        private bool isInteractable = true;

        public CardData CardData => cardData;
        public CanvasGroup CanvasGroup => canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            defaultScale = transform.localScale;
        }

        public void Initialize(RewardController controller)
        {
            rewardController = controller;
        }

        public void SetCard(CardData data)
        {
            cardData = data;

            if (data == null)
            {
                Clear();
                return;
            }

            SetIcon(data.Icon);
            SetText(nameText, data.CardName);
            SetText(typeText, data.TypeText);
            SetText(descriptionText, data.Description);
            ApplyGrade(data.Grade, data.GradeText);

            if (notObtainedObject != null)
                notObtainedObject.SetActive(false);
        }

        public void SetInteractable(bool value)
        {
            isInteractable = value;
            canvasGroup.interactable = value;
            canvasGroup.blocksRaycasts = value;
        }

        public void RefreshDefaultScale()
        {
            defaultScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (CanPlayInteraction())
                transform.localScale = defaultScale * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!CanPlayInteraction())
                return;

            transform.localScale = defaultScale;
            SetFrameColor(gradeColor);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            SetFrameColor(MultiplyColor(gradeColor, pressedTint));
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            SetFrameColor(gradeColor);
            rewardController?.SelectRewardCard(this);
        }

        private bool CanPlayInteraction()
        {
            return isInteractable && rewardController != null && !rewardController.IsSelected(this);
        }

        private void ApplyGrade(CardGrade grade, string gradeName)
        {
            gradeColor = GetGradeColor(grade);

            SetFrameColor(gradeColor);

            if (gradeText != null)
            {
                gradeText.text = gradeName;
                gradeText.color = gradeColor;
            }
        }

        private Color GetGradeColor(CardGrade grade)
        {
            switch (grade)
            {
                case CardGrade.Common:
                    return commonColor;
                case CardGrade.Rare:
                    return rareColor;
                case CardGrade.Epic:
                    return epicColor;
                case CardGrade.Legendary:
                    return legendaryColor;
                default:
                    return commonColor;
            }
        }

        private void Clear()
        {
            SetIcon(null);
            SetText(nameText, string.Empty);
            SetText(typeText, string.Empty);
            SetText(gradeText, string.Empty);
            SetText(descriptionText, string.Empty);
            gradeColor = commonColor;
            SetFrameColor(gradeColor);

            if (notObtainedObject != null)
                notObtainedObject.SetActive(false);
        }

        private void SetIcon(Sprite icon)
        {
            if (iconImage == null)
                return;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        private void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private void SetFrameColor(Color color)
        {
            if (frameImage != null)
                frameImage.color = color;
        }

        private Color MultiplyColor(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        }
    }
}

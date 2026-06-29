using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcanist
{
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CardData cardData;
        private bool isObtained = true;

        public void SetCard(CardData newCardData, bool obtained = true)
        {
            cardData = newCardData;
            isObtained = obtained;
        }

        public void Clear()
        {
            cardData = null;
            isObtained = true;

            if (TooltipController.Instance != null)
                TooltipController.Instance.Hide();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (TooltipController.Instance == null || cardData == null)
                return;

            TooltipController.Instance.Show(cardData, isObtained);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (TooltipController.Instance == null)
                return;

            TooltipController.Instance.Hide();
        }

        private void OnDisable()
        {
            if (TooltipController.Instance != null)
                TooltipController.Instance.Hide();
        }
    }
}

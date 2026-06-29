using UnityEngine;
using UnityEngine.EventSystems;

namespace Arcanist
{
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        
        private CardData cardData;

        public void SetCardData(CardData newCardData)
        {
            cardData = newCardData;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (TooltipController.Instance == null || cardData == null)
                return;

            TooltipController.Instance.Show(cardData);
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

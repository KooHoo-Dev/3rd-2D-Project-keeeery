using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

namespace Arcanist
{
    public class PauseMenuButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerClickHandler
    {
        private enum ButtonType
        {
            Resume, Inventory, ReturnToCheckPoint, Option, MainMenu
        }

        [SerializeField] private ButtonType buttonType;
        [SerializeField] private float hoverScaleMultiplier = 1.1f;

        private Vector3 originalScale;
        private TMP_Text text;
        private Color originalColor;
        private OverlayController overlayController;

        public void Initialize(OverlayController controller)
        {
            overlayController = controller;
        }

        private void Awake()
        {
            originalScale = transform.localScale;

            text = GetComponent<TMP_Text>();

            if (text != null)
                originalColor = text.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = originalScale * hoverScaleMultiplier;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (text != null)
                text.color = new Color(0.7f, 0.7f, 0.7f, originalColor.a);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (text != null)
                text.color = originalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            switch (buttonType)
            {
                case ButtonType.Resume:
                    overlayController.Hide();
                    break;

                case ButtonType.Inventory:
                    overlayController.ShowInventory();
                    break;
                    
                case ButtonType.ReturnToCheckPoint:
                    overlayController.ReturnToCheckpoint();
                    break;

                case ButtonType.Option:
                    overlayController.ShowOption();
                    break;

                case ButtonType.MainMenu:
                    Time.timeScale = 1.0f;
                    SceneManager.LoadScene("Main");
                    break;
            }
        }
    }
}